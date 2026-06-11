# Runs the SpacetimeDB-backed validation scenarios (client/validation/scenarios_stdb/)
# against a real local SpacetimeDB instance using an ephemeral database.
#
#   ./scripts/run_stdb_scenarios.ps1                  # full STDB suite
#   ./scripts/run_stdb_scenarios.ps1 -Scenario client/validation/scenarios_stdb/movement_round_trip.json
#   ./scripts/run_stdb_scenarios.ps1 -KeepDatabase    # leave the ephemeral DB for inspection

param(
    [string]$Scenario = "",
    [string]$GodotExe = $env:GODOT_EXE,
    [string]$ServerUri = "http://localhost:3000",
    [switch]$KeepDatabase
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$serverDir = Join-Path $repoRoot "server"

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

$dbName = "nomad-test-" + (Get-Date -Format "yyyyMMddHHmmss")
Write-Host "Publishing module to ephemeral database '$dbName'..."
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
$exitCode = 1
try {
    if ($Scenario) {
        & (Join-Path $repoRoot "tools/run_scenario.ps1") -Scenario $Scenario -GodotExe $GodotExe
    }
    else {
        & (Join-Path $repoRoot "tools/run_all_scenarios.ps1") -ScenarioDirectory "validation/scenarios_stdb" -GodotExe $GodotExe
    }
    $exitCode = $LASTEXITCODE
}
finally {
    Remove-Item Env:NOMAD_STDB_URI, Env:NOMAD_STDB_DB -ErrorAction SilentlyContinue
    if ($KeepDatabase) {
        Write-Host "Keeping ephemeral database '$dbName' (NOMAD_STDB_DB) for inspection."
    }
    else {
        Write-Host "Deleting ephemeral database '$dbName'..."
        spacetime delete $dbName --server local --yes 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not delete '$dbName' — clean up with: spacetime delete $dbName --server local"
        }
    }
}

exit $exitCode
