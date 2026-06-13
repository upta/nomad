# Runs the SpacetimeDB-backed validation scenarios (client/validation/scenarios_stdb/)
# against a real local SpacetimeDB instance. Each scenario gets its OWN ephemeral
# database so state and config can never leak between scenarios.
#
# Scenarios run in PARALLEL (-MaxParallel, default 2). The module is built ONCE up
# front, then each worker publishes that prebuilt wasm to its own database via
# `spacetime publish --bin-path` (no rebuild, no cargo lock — safe to run concurrently)
# and drives the Godot client in its own process. Per-worker isolation comes from a
# private environment block (unique NOMAD_STDB_DB + NOMAD_CLIENT_ID → isolated
# `.nomad-<id>` token file), never the shared process environment.
#
#   ./scripts/run_stdb_scenarios.ps1                  # full STDB suite (parallel)
#   ./scripts/run_stdb_scenarios.ps1 -MaxParallel 1   # serial (for debugging)
#   ./scripts/run_stdb_scenarios.ps1 -Scenario client/validation/scenarios_stdb/movement_round_trip.json
#   ./scripts/run_stdb_scenarios.ps1 -KeepDatabase    # keep DBs of failed scenarios
#                                                     # (single-scenario runs: keep regardless)

param(
    [string]$Scenario = "",
    [string]$GodotExe = $env:GODOT_EXE,
    [string]$ServerUri = "http://localhost:3000",
    # Default 2: all clients share one local SpacetimeDB server, so it — not CPU — is the
    # bottleneck. Beyond ~2 concurrent clients the server lags: connected scenarios either
    # fail an assertion (a sync timeout — a REAL failure, reported honestly) or pass their
    # assertions and then crash on teardown with 0xC0000005 (reported flaky/green, since
    # the scenario validated). Raise for a faster one-off, but expect reds as it saturates.
    [int]$MaxParallel = 2,
    [switch]$KeepDatabase
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$serverDir = Join-Path $repoRoot "server"
$clientDir = Join-Path $repoRoot "client"

if ($MaxParallel -lt 1) {
    throw "MaxParallel must be 1 or greater."
}

if (-not (Get-Command spacetime -ErrorAction SilentlyContinue)) {
    throw "spacetime CLI not found on PATH."
}

function Test-ServerUp {
    spacetime list --server local 2>$null | Out-Null
    return ($LASTEXITCODE -eq 0)
}

# Server lifecycle decision: start on demand, leave running.
if (-not (Test-ServerUp)) {
    Write-Host "Starting local SpacetimeDB server..."
    Start-Process spacetime -ArgumentList "start" -WindowStyle Hidden
    $deadline = (Get-Date).AddSeconds(30)
    while (-not (Test-ServerUp)) {
        if ((Get-Date) -gt $deadline) {
            throw "SpacetimeDB server did not become ready within 30 seconds."
        }
        Start-Sleep -Milliseconds 500
    }
}

# Build the module ONCE. Each per-scenario publish then reuses this exact wasm via
# --bin-path, so publishes never rebuild and can run concurrently.
Write-Host "Building server module..."
Push-Location $serverDir
try {
    spacetime build --module-path ./src
    if ($LASTEXITCODE -ne 0) {
        throw "spacetime build failed."
    }
}
finally {
    Pop-Location
}

# Locate the compiled wasm the build just produced (prefer the wasm-opt output that a
# normal publish would upload; fall back to the for-publish artifact).
$wasmPath = $null
$optWasm = Get-ChildItem (Join-Path $serverDir "src") -Recurse -Filter "*.opt.wasm" -File -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($optWasm) {
    $wasmPath = $optWasm.FullName
}
else {
    $forPublish = Get-ChildItem (Join-Path $serverDir "src") -Recurse -Filter "*.wasm" -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match "for-publish" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($forPublish) { $wasmPath = $forPublish.FullName }
}
if (-not $wasmPath) {
    throw "Could not locate the compiled wasm under $serverDir/src after build."
}

if ($Scenario) {
    $resolved = if ([System.IO.Path]::IsPathRooted($Scenario)) {
        $Scenario
    }
    elseif (Test-Path (Join-Path $repoRoot $Scenario)) {
        Join-Path $repoRoot $Scenario
    }
    elseif (Test-Path (Join-Path $clientDir $Scenario)) {
        Join-Path $clientDir $Scenario
    }
    else {
        throw "Scenario not found: $Scenario"
    }
    $scenarioFiles = @(Get-Item $resolved)
}
else {
    $scenarioFiles = @(
        Get-ChildItem (Join-Path $clientDir "validation/scenarios_stdb") -Filter *.json -File | Sort-Object Name
    )
    if ($scenarioFiles.Count -eq 0) {
        throw "No scenario contracts found under client/validation/scenarios_stdb."
    }
}

$runStamp = Get-Date -Format "yyyyMMddHHmmss"
$isSingle = [bool]$Scenario
$runScenarioScript = Join-Path $repoRoot "tools/run_scenario.ps1"

# Build the work list up front so each scenario has a stable, unique database name and
# client id (→ isolated token file) regardless of completion order.
$work = @()
$index = 0
foreach ($scenarioFile in $scenarioFiles) {
    $index++
    $work += [pscustomobject]@{
        Index    = $index
        Name     = $scenarioFile.Name
        FullName = $scenarioFile.FullName
        Db       = "nomad-test-{0}-{1:d2}" -f $runStamp, $index
        ClientId = "test-{0}-{1:d2}" -f $runStamp, $index
    }
}

Write-Host ("Running {0} STDB scenario(s) with up to {1} in parallel..." -f $work.Count, $MaxParallel)

$results = $work | ForEach-Object -ThrottleLimit ([Math]::Max(1, $MaxParallel)) -Parallel {
    $item = $_
    $wasmPath = $using:wasmPath
    $ServerUri = $using:ServerUri
    $runScenarioScript = $using:runScenarioScript
    $GodotExe = $using:GodotExe
    $keepRequested = [bool]$using:KeepDatabase
    $isSingle = $using:isSingle

    # 1) Publish the prebuilt wasm to this scenario's own database (concurrent-safe).
    & spacetime publish $item.Db --yes --server local --bin-path $wasmPath *> $null
    $publishOk = ($LASTEXITCODE -eq 0)

    $finalExit = 1
    $status = "publish_failed"
    $artifact = $null
    $capturedOutput = $null

    if ($publishOk) {
        # 2) Drive the client in its own process with an ISOLATED environment block.
        #    Cloning the parent env and overlaying keeps PATH/etc. intact while making
        #    the DB/identity selection private to this child (no shared $env: race).
        $childEnv = @{}
        foreach ($entry in [System.Environment]::GetEnvironmentVariables().GetEnumerator()) {
            if ($null -ne $entry.Key) { $childEnv[[string]$entry.Key] = [string]$entry.Value }
        }
        $childEnv["NOMAD_STDB_URI"] = $ServerUri
        $childEnv["NOMAD_STDB_DB"] = $item.Db
        $childEnv["NOMAD_CLIENT_ID"] = $item.ClientId

        $outFile = [System.IO.Path]::GetTempFileName()
        $errFile = [System.IO.Path]::GetTempFileName()
        $argList = @("-NoProfile", "-File", $runScenarioScript, "-Scenario", $item.FullName, "-SkipArtifactPrune")
        if ($GodotExe) { $argList += @("-GodotExe", $GodotExe) }

        try {
            $proc = Start-Process -FilePath "pwsh" -ArgumentList $argList -Environment $childEnv `
                -RedirectStandardOutput $outFile -RedirectStandardError $errFile -WindowStyle Hidden -PassThru
            $null = $proc.Handle  # cache handle so ExitCode is readable after exit
            $proc.WaitForExit()
            $procExit = $proc.ExitCode

            $capturedOutput = (Get-Content $outFile -Raw -ErrorAction SilentlyContinue)
            $resultLine = (Get-Content $outFile -ErrorAction SilentlyContinue | Where-Object { $_ -like "RESULT *" } | Select-Object -Last 1)
            if ($resultLine) {
                try {
                    $parsed = $resultLine.Substring(7) | ConvertFrom-Json
                    $finalExit = [int]$parsed.final_exit_code
                    $status = [string]$parsed.status
                    $artifact = [string]$parsed.artifact_path
                }
                catch {
                    $finalExit = $procExit
                    $status = if ($procExit -eq 0) { "pass" } else { "failed" }
                }
            }
            else {
                $finalExit = $procExit
                $status = if ($procExit -eq 0) { "pass" } else { "failed" }
            }
        }
        finally {
            Remove-Item $outFile, $errFile -ErrorAction SilentlyContinue
        }
    }

    # 3) Verdict by ASSERTION status, not process exit. The verifier writes summary.json
    #    (carried on the RESULT line as `status`) BEFORE the engine quits, so a scenario
    #    that asserts cleanly is status=='pass' regardless of a later teardown crash. A
    #    non-zero exit WITH status=='pass' is the 0xC0000005 shutdown crash seen under
    #    load — the scenario validated, so it is flaky (green), not a failure. Any other
    #    status (assertion_failure / timeout / runtime_error, or a pre-summary crash that
    #    run_scenario reports as 'failed') is a real failure that must NOT be forgiven.
    $validated = ($status -eq "pass")
    $flaky = ($validated -and $finalExit -ne 0)   # passed assertions, crashed on teardown

    # Keep the DB only for genuine failures (or single-scenario debug runs) when asked.
    $keep = $keepRequested -and ((-not $validated) -or $isSingle)
    if ($keep) {
        Write-Host ("  keeping database '{0}' for inspection." -f $item.Db)
    }
    else {
        & spacetime delete $item.Db --server local --yes *> $null
    }

    $label = if (-not $validated) { $status } elseif ($flaky) { "flaky ($status; teardown crash $finalExit)" } else { "pass" }
    $color = if (-not $validated) { "Red" } elseif ($flaky) { "Yellow" } else { "Green" }
    Write-Host ("  [{0}] {1} -> {2}" -f $item.Index, $item.Name, $label) -ForegroundColor $color

    # Surface full child output for real failures (and single-scenario debug runs).
    if (((-not $validated) -or $isSingle) -and -not [string]::IsNullOrWhiteSpace($capturedOutput)) {
        Write-Host ("----- output: {0} -----" -f $item.Name)
        Write-Host $capturedOutput
    }

    [pscustomobject]@{
        Scenario  = $item.Name
        Database  = $item.Db
        ExitCode  = $finalExit
        Status    = $status
        Validated = $validated
        Flaky     = $flaky
        Artifact  = $artifact
        Kept      = $keep
    }
}

$results = @($results | Sort-Object Scenario)

# The suite fails iff any scenario did not VALIDATE (status != 'pass'). A flaky scenario
# (assertions passed, engine crashed on teardown under load) does NOT fail the suite but
# is surfaced loudly and persisted for trend analysis. Real assertion failures are never
# downgraded — there is no "pass it again uncontended" forgiveness, which would mask a
# genuine load-sensitive regression (exactly the class this parallel suite exists to catch).
$failedResults = @($results | Where-Object { -not $_.Validated })
$flakyResults = @($results | Where-Object { [bool]$_.Flaky })
$suiteExitCode = if ($failedResults.Count -gt 0) { 1 } else { 0 }
$suiteStatus = if ($failedResults.Count -gt 0) { "failed" } elseif ($flakyResults.Count -gt 0) { "flaky" } else { "pass" }

# Persist a machine-readable record (mirrors the pure suite's suite.json) so flaky runs
# leave a durable, auditable trail even when the suite exits green.
$suiteId = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$suitePath = Join-Path $clientDir (Join-Path "artifacts" (Join-Path "stdb_suites" $suiteId))
$null = New-Item -ItemType Directory -Path $suitePath -Force
$validatedCount = @($results | Where-Object { $_.Validated }).Count
$suite = [ordered]@{
    suite_run_id     = $suiteId
    suite_status     = $suiteStatus
    max_parallel     = $MaxParallel
    scenario_count   = $results.Count
    validated_count  = $validatedCount
    failed_count     = $failedResults.Count
    flaky_count      = $flakyResults.Count
    flaky_scenarios  = @($flakyResults | Select-Object -ExpandProperty Scenario)
    failed_scenarios = @($failedResults | Select-Object -ExpandProperty Scenario)
    scenarios        = @($results | ForEach-Object {
            [ordered]@{
                scenario  = [string]$_.Scenario
                status    = [string]$_.Status
                exit_code = [int]$_.ExitCode
                validated = [bool]$_.Validated
                flaky     = [bool]$_.Flaky
                database  = [string]$_.Database
            }
        })
}
$suite | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $suitePath "suite.json") -Encoding utf8

Write-Host ""
Write-Host "=== STDB suite summary ===" -ForegroundColor Cyan
$results | Format-Table Scenario, Status, ExitCode, Database -AutoSize | Out-String | Write-Host
Write-Host ("{0}/{1} validated (max parallel {2}) -> {3}" -f $validatedCount, $results.Count, $MaxParallel, $suiteStatus.ToUpper())
if ($flakyResults.Count -gt 0) {
    Write-Host ("FLAKY ({0}) — assertions passed but the engine crashed on teardown under load: {1}" -f $flakyResults.Count, (@($flakyResults | Select-Object -ExpandProperty Scenario) -join ", ")) -ForegroundColor Yellow
}
if ($failedResults.Count -gt 0) {
    Write-Host ("FAILED ({0}): {1}" -f $failedResults.Count, (@($failedResults | Select-Object -ExpandProperty Scenario) -join ", ")) -ForegroundColor Red
}

exit $suiteExitCode
