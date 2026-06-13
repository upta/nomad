# Runs both validation suites: pure client scenarios, then SpacetimeDB-backed
# scenarios against an ephemeral local database. Fails if either suite fails.
#
# Both suites run in parallel by default (pure: 4-wide; STDB: 2-wide, since all
# clients share one local server). Override per suite with -PureMaxParallel /
# -StdbMaxParallel; pass 1 for a fully serial run when debugging.

param(
    [string]$GodotExe = $env:GODOT_EXE,
    [int]$PureMaxParallel = 0,   # 0 = use run_all_scenarios.ps1 default
    [int]$StdbMaxParallel = 0    # 0 = use run_stdb_scenarios.ps1 default
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent

Write-Host "=== Pure validation suite (client/validation/scenarios) ===" -ForegroundColor Cyan
$pureArgs = @{ GodotExe = $GodotExe }
if ($PureMaxParallel -gt 0) { $pureArgs.MaxParallel = $PureMaxParallel }
& (Join-Path $repoRoot "tools/run_all_scenarios.ps1") @pureArgs
$pureExit = $LASTEXITCODE

Write-Host "=== SpacetimeDB validation suite (client/validation/scenarios_stdb) ===" -ForegroundColor Cyan
$stdbArgs = @{ GodotExe = $GodotExe }
if ($StdbMaxParallel -gt 0) { $stdbArgs.MaxParallel = $StdbMaxParallel }
& (Join-Path $repoRoot "scripts/run_stdb_scenarios.ps1") @stdbArgs
$stdbExit = $LASTEXITCODE

Write-Host ""
Write-Host ("Pure suite: " + $(if ($pureExit -eq 0) { "PASS" } else { "FAIL ($pureExit)" }))
Write-Host ("STDB suite: " + $(if ($stdbExit -eq 0) { "PASS" } else { "FAIL ($stdbExit)" }))

exit [Math]::Max($pureExit, $stdbExit)
