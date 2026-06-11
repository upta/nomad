# Claude Code Stop hook: block ending the turn while gameplay code has
# uncommitted changes with no validation run newer than the latest change.
# Emits {"decision":"block","reason":...} once; stop_hook_active prevents loops.

$ErrorActionPreference = "SilentlyContinue"

try { $hookInput = [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { exit 0 }
if ($hookInput.stop_hook_active) { exit 0 }

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $repoRoot

$changed = @(git status --porcelain -- client/game server/src client/validation 2>$null)
if ($changed.Count -eq 0) { exit 0 }

# Newest mtime among the changed gameplay files
$latestChange = [datetime]::MinValue
foreach ($line in $changed) {
    if ($line.Length -lt 4) { continue }
    $path = $line.Substring(3).Trim().Trim('"')
    if ($path -match '->') { $path = ($path -split '->')[-1].Trim().Trim('"') }
    $full = Join-Path $repoRoot $path
    if (Test-Path $full -PathType Leaf) {
        $mtime = (Get-Item $full).LastWriteTime
        if ($mtime -gt $latestChange) { $latestChange = $mtime }
    }
}
if ($latestChange -eq [datetime]::MinValue) { exit 0 }

# Newest validation run (each scenario run writes a summary.json)
$latestRun = Get-ChildItem (Join-Path $repoRoot "client\artifacts") -Recurse -Filter "summary.json" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($latestRun -and $latestRun.LastWriteTime -gt $latestChange) { exit 0 }

$reason = "Gameplay code under client/game, server/src, or client/validation has changed, but no validation run is newer than the latest change. Per the project's validation-first policy: make sure scenarios cover this change (validate-gameplay skill), run ./tools/run_all_scenarios.ps1, review the checkpoint screenshots, and confirm green. If validation genuinely does not apply to these specific changes, briefly tell the user why and then stop."

@{ decision = "block"; reason = $reason } | ConvertTo-Json -Compress
exit 0
