# Runs both validation suites: pure client scenarios, then SpacetimeDB-backed
# scenarios against an ephemeral local database. Fails if either suite fails.

param(
    [string]$GodotExe = $env:GODOT_EXE
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent

Write-Host "=== Pure validation suite (client/validation/scenarios) ===" -ForegroundColor Cyan
& (Join-Path $repoRoot "tools/run_all_scenarios.ps1") -GodotExe $GodotExe
$pureExit = $LASTEXITCODE

Write-Host "=== SpacetimeDB validation suite (client/validation/scenarios_stdb) ===" -ForegroundColor Cyan
& (Join-Path $repoRoot "scripts/run_stdb_scenarios.ps1") -GodotExe $GodotExe
$stdbExit = $LASTEXITCODE

Write-Host ""
Write-Host ("Pure suite: " + $(if ($pureExit -eq 0) { "PASS" } else { "FAIL ($pureExit)" }))
Write-Host ("STDB suite: " + $(if ($stdbExit -eq 0) { "PASS" } else { "FAIL ($stdbExit)" }))

exit [Math]::Max($pureExit, $stdbExit)
