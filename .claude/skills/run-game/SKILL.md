---
name: run-game
description: Launch the real Nomad game (SpacetimeDB server + Godot client) to verify it boots and runs clean. Use to run/start the app, verify startup after changes, satisfy the "game boots clean" Definition of Done step, or debug runtime/connection issues that validation scenarios don't cover.
---

# Running the Real Game

Validation scenarios run in `--test-mode`, which skips the SpacetimeDB connection path (`client/bootstrap/AppRoot.cs` routes on the flag). Startup, connection, and subscription bugs only surface in a real run — this is required by the Definition of Done.

## 1. Server side (skip if unchanged and already running)

```powershell
spacetime start                                                              # if not already running (blocks; run in background)
cd server
spacetime build --module-path ./src
spacetime publish nomad --yes --server local --module-path ./src             # add --delete-data=always to wipe data
spacetime generate --lang csharp --out-dir ../client/Db --module-path ./src  # only if schema/reducers changed
```

Check server health: `spacetime logs nomad --server local` (look for panics/reducer errors).

## 2. Boot the client headless and check for errors

From the repo root (`godot.exe` is on PATH via godotenv; `GODOT_EXE` overrides):

```powershell
$log = Join-Path $env:TEMP "nomad_verify.log"
$p = Start-Process godot.exe -ArgumentList "--path", (Resolve-Path client).Path, "--log-file", $log, "--headless" -PassThru
Start-Sleep 12
if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force }
Select-String -Path $log -Pattern 'ERROR:', 'SCRIPT ERROR' # must produce no output
```

Pass = zero `ERROR:`/`SCRIPT ERROR` lines after ≥10 seconds of runtime. Also skim the log for connection failures (`DbManager`, SpacetimeDB) — the game treats a failed connection as non-fatal, so it won't always show as `ERROR:`.

If the client process exits immediately, run the same command without `--headless` removed but capture `$p.ExitCode`, and check the log for scene-load failures (broken `.tscn`/`.tres` references surface here, not at build time).

## 3. Seeing it (visual check)

Headless runs render nothing. For a visual look at real gameplay, prefer a validation scenario checkpoint screenshot (see the `validate-gameplay` skill) — checkpoints capture PNGs you can Read. Launch a windowed instance (`Start-Process godot.exe -ArgumentList "--path", (Resolve-Path client).Path`) only when the user wants to play-test; tell them it's up and leave it running for them.

## Cleanup

Don't leave stray Godot processes: `Get-Process godot* -ErrorAction SilentlyContinue | Stop-Process -Force` if a run hangs.
