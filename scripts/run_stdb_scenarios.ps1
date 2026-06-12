# Runs the SpacetimeDB-backed validation scenarios (client/validation/scenarios_stdb/)
# against a real local SpacetimeDB instance. Each scenario gets its OWN ephemeral
# database so state and config can never leak between scenarios.
#
#   ./scripts/run_stdb_scenarios.ps1                  # full STDB suite
#   ./scripts/run_stdb_scenarios.ps1 -Scenario client/validation/scenarios_stdb/movement_round_trip.json
#   ./scripts/run_stdb_scenarios.ps1 -KeepDatabase    # keep DBs of failed scenarios
#                                                     # (single-scenario runs: keep regardless)

param(
    [string]$Scenario = "",
    [string]$GodotExe = $env:GODOT_EXE,
    [string]$ServerUri = "http://localhost:3000",
    [switch]$KeepDatabase
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$serverDir = Join-Path $repoRoot "server"
$clientDir = Join-Path $repoRoot "client"

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

# Build once up front: fail fast on compile errors and warm the incremental
# build so each per-scenario publish only pays upload + DB init.
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
$results = @()
$suiteExitCode = 0
$index = 0

foreach ($scenarioFile in $scenarioFiles) {
    $index++
    $dbName = "nomad-test-{0}-{1:d2}" -f $runStamp, $index
    Write-Host ""
    Write-Host ("=== [{0}/{1}] {2} -> {3} ===" -f $index, $scenarioFiles.Count, $scenarioFile.Name, $dbName) -ForegroundColor Cyan

    Push-Location $serverDir
    try {
        spacetime publish $dbName --yes --server local --module-path ./src
        if ($LASTEXITCODE -ne 0) {
            throw "spacetime publish to '$dbName' failed."
        }
    }
    finally {
        Pop-Location
    }

    $env:NOMAD_STDB_URI = $ServerUri
    $env:NOMAD_STDB_DB = $dbName
    $scenarioExitCode = 1
    try {
        & (Join-Path $repoRoot "tools/run_scenario.ps1") -Scenario $scenarioFile.FullName -GodotExe $GodotExe
        $scenarioExitCode = $LASTEXITCODE
    }
    catch {
        Write-Warning ("Scenario run crashed: " + $_.Exception.Message)
    }
    finally {
        Remove-Item Env:NOMAD_STDB_URI, Env:NOMAD_STDB_DB -ErrorAction SilentlyContinue

        $failed = $scenarioExitCode -ne 0
        $keep = $KeepDatabase -and ($failed -or $Scenario)
        if ($keep) {
            Write-Host "Keeping database '$dbName' for inspection."
        }
        else {
            spacetime delete $dbName --server local --yes 2>$null
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Could not delete '$dbName' — clean up with: spacetime delete $dbName --server local"
            }
        }
    }

    if ($scenarioExitCode -gt $suiteExitCode) {
        $suiteExitCode = $scenarioExitCode
    }
    $results += [pscustomobject]@{
        Scenario = $scenarioFile.Name
        Database = $dbName
        ExitCode = $scenarioExitCode
        Status   = if ($scenarioExitCode -eq 0) { "pass" } else { "FAIL" }
    }
}

Write-Host ""
Write-Host "=== STDB suite summary ===" -ForegroundColor Cyan
$results | Format-Table Scenario, Status, ExitCode, Database -AutoSize | Out-String | Write-Host
$failedCount = @($results | Where-Object { $_.ExitCode -ne 0 }).Count
Write-Host ("{0}/{1} passed" -f ($results.Count - $failedCount), $results.Count)

exit $suiteExitCode
