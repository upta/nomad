# Task 0.1: Project Restructuring — Todo

## Subtask 0.1.1: Rename C# files + add namespaces
- [ ] Rename `src/bootstrap/app_root.cs` → `src/bootstrap/AppRoot.cs`
- [ ] Add `namespace Nomad.Bootstrap;` to `AppRoot.cs`
- [ ] Rename `src/game/main.cs` → `src/game/Main.cs`
- [ ] Add `namespace Nomad.Game;` to `Main.cs`

## Subtask 0.1.2: Update .tscn files + rename
- [ ] Rename `src/bootstrap/app_root.tscn` → `src/bootstrap/AppRoot.tscn`
- [ ] Update script path in `AppRoot.tscn` to `res://bootstrap/AppRoot.cs`
- [ ] Rename `src/game/main.tscn` → `src/game/Main.tscn`
- [ ] Update script path in `Main.tscn` to `res://game/Main.cs`

## Subtask 0.1.3: Rename project identity files
- [ ] Update `src/project.godot`: name="Nomad", assembly_name="Nomad", main_scene="res://bootstrap/AppRoot.tscn"
- [ ] Rename `src/MyPrototype.csproj` → `src/Nomad.csproj`, update RootNamespace to "Nomad"
- [ ] Rename `src/MyPrototype.sln` → `src/Nomad.sln`, update references to Nomad

## Subtask 0.1.4: Move src/ → client/
- [ ] Delete `src/.godot/mono/` build artifacts
- [ ] Rename `src/` → `client/`

## Subtask 0.1.5: Update path references
- [ ] Update `symlink-config.txt`: `src/addons/` → `client/addons/`
- [ ] Update `.gitignore`: `src/addons/` → `client/addons/`
- [ ] Update `README.md`: `src/` → `client/`, `MyPrototype` → `Nomad`

## Subtask 0.1.6: Symlinks + build
- [ ] Run `.\setup.ps1` to create symlinks in `client/`
- [ ] Run `dotnet build` from `client/` — must succeed
- [ ] Verify no validation scenarios broken (none exist yet)
