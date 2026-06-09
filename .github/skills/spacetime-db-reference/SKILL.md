---
name: spacetime-db-reference
description: SpacetimeDB CLI and C# SDK quick reference. Use when working with spacetime CLI commands, writing C# tables/reducers/types, or debugging build/publish issues.
---

# SpacetimeDB Reference

## CLI Quick Reference

### Build & Publish
```powershell
spacetime build --module-path ./src --debug        # debug build
spacetime build --module-path ./src                # release build
spacetime publish nomad --delete-data=always --yes --server local --module-path ./src  # clear + republish
spacetime publish nomad --yes --server local --module-path ./src   # publish without clearing
```

### Generate & Dev
```powershell
spacetime generate --lang csharp --out-dir ../client/Db --module-path ./src   # client bindings
spacetime dev --client-lang typescript --module-bindings-path ./client/src/bindings  # auto-rebuild+publish+generate
```

### Database Interaction
```powershell
spacetime sql nomad "SELECT * FROM Players" --server local
spacetime call nomad MoveEntity --server local --anonymous '[0, 1.0, 2.0]'
spacetime logs nomad --server local -n 100
spacetime describe nomad --server local --json
spacetime list --server local
```

### Server Management
```powershell
spacetime start                                    # start local instance
spacetime server list                              # list configured servers
spacetime server add local --url http://localhost:3000 --default
spacetime server clear                             # clear local data
```

---

## C# Module Structure

All tables, types, and reducers go inside `public static partial class Module`. The class is split across files via `partial`:

```csharp
using SpacetimeDB;

public static partial class Module
{
    // Tables, types, and reducers here
}
```

## Tables

`[SpacetimeDB.Table(...)]` on a `public partial struct`. `Accessor` should be PascalCase:

```csharp
[SpacetimeDB.Table(Accessor = "Players", Public = true)]
public partial struct Player
{
    [PrimaryKey]
    public Identity Identity;
    public bool IsConnected;
    public int PlayerEntityId;
}
```

Options: `Accessor = "PascalCase"` (recommended), `Public = true`, `Scheduled = nameof(ReducerFn)`, `ScheduledAt = nameof(field)`.

`ctx.Db` accessors use the `Accessor` name: `ctx.Db.Players`, `ctx.Db.Entities`.

## Column Types

| C# type | Notes |
|---------|-------|
| `byte` / `ushort` / `uint` / `ulong` | unsigned integers |
| `sbyte` / `short` / `int` / `long` | signed integers |
| `float` / `double` | floats |
| `bool` | boolean |
| `string` | text |
| `Identity` | user identity |
| `ConnectionId` | connection handle |
| `Timestamp` | server timestamp (microseconds since epoch) |

## Column Attributes

```csharp
[PrimaryKey]              // primary key
[AutoInc]                 // auto-increment (use 0 as placeholder on insert)
[Unique]                  // unique constraint
[SpacetimeDB.Index.BTree] // btree index (enables .Filter() on this column)
```

## DB Operations

```csharp
ctx.Db.Entity.Insert(new Entity { Name = "Sample" });        // Insert
ctx.Db.Entity.Id.Find(entityId);                             // Find by PK → Entity?
ctx.Db.Entity.Identity.Find(ctx.Sender);                     // Find by unique column
ctx.Db.Item.AuthorId.Filter(authorId);                       // Filter by index → IEnumerable
ctx.Db.Entity.Iter();                                        // All rows → IEnumerable
ctx.Db.Entity.Count;                                         // Count rows
ctx.Db.Entity.Id.Update(existing with { Name = "new" });     // Update by PK
ctx.Db.Entity.Id.Delete(entityId);                           // Delete by PK
```

## Reducers

```csharp
[SpacetimeDB.Reducer]
public static void MoveEntity(ReducerContext ctx, int entityId, float x, float y) { ... }
```

## Lifecycle Hooks

```csharp
[SpacetimeDB.Reducer(ReducerKind.Init)]              // once on first publish
[SpacetimeDB.Reducer(ReducerKind.ClientConnected)]   // on each client connect
[SpacetimeDB.Reducer(ReducerKind.ClientDisconnected)] // on each client disconnect
```

**CRITICAL:** Method names must NOT start with "On" (STDB0010 error). Use `ClientConnected`, not `OnConnect`.

## Reducer Context API

```csharp
ctx.Sender           // Identity of the caller (for auth checks)
ctx.Timestamp        // deterministic server timestamp
ctx.Rng.Next(1, 7)   // deterministic random [1, 7)
```

## Custom Types

```csharp
[SpacetimeDB.Type]
public enum EntityType : uint { None, Player }

[SpacetimeDB.Type]
public partial struct DbVector2 { public float X; public float Y; }
```

## Auto-Increment IDs

- Annotate: `[PrimaryKey]` + `[AutoInc]` on `int` or `ulong`
- Insert with 0: `EntityId = 0`
- Capture result: `var entity = ctx.Db.Entities.Insert(new Entity { EntityId = 0 });`
- Use immediately: `entity.EntityId` holds the generated ID for foreign keys
- **Gaps are normal** — auto-increment values are NOT sequential
