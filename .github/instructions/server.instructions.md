---
applyTo: "server/**"
---

# SpacetimeDB Server Instructions

This is a SpacetimeDB project. For detailed information see [spacetimedb.com/llms.txt](https://spacetimedb.com/llms.txt).

## Core Rules

1. **Reducers are transactional** — they do not return data to callers
2. **Reducers must be deterministic** — no filesystem, network, timers, or random
3. **Read data via tables/subscriptions** — not reducer return values
4. **Auto-increment IDs are not sequential** — gaps are normal, don't use for ordering
5. **`ctx.Sender` is the authenticated principal** — never trust identity args from clients

## Build, Publish, Generate

```powershell
spacetime build --module-path ./src                 # build module
spacetime publish nomad --module-path ./src          # publish to local
spacetime publish nomad --clear-database -y --module-path ./src  # schema change republish
spacetime generate --lang csharp --out-dir ../client/Db --module-path ./src  # client bindings
spacetime logs nomad                                # view server logs
```

## Table Definitions

Tables MUST use `partial struct` or `partial class`.

```csharp
// ✅ CORRECT
[SpacetimeDB.Table(Accessor = "Players", Public = true)]
public partial struct Player
{
    [SpacetimeDB.PrimaryKey]
    public Identity Identity;
    public bool IsConnected;
}

// ❌ WRONG — missing partial
public struct Player { }
```

### Column Types

`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `bool`, `string`, `Identity`, `Timestamp`, `ScheduleAt`, `T?` (nullable), `List<T>` (arrays)

### Auto-increment Insert

```csharp
var player = ctx.Db.Players.Insert(new Player { Identity = ctx.Sender, IsConnected = true, PlayerEntityId = 0 });
ulong actualId = player.PlayerEntityId;  // 0 triggers auto-increment
```

### Indexes

- Index names must be unique across the entire module (all tables)
- Always use full namespace: `[SpacetimeDB.Index.BTree(...)]` — not `[Index.BTree(...)]`
- Schema ↔ code coupling: renaming/removing an index requires updating all query code

## Reducers

Reducers are static methods on `public static partial class Module`. The class is split across multiple files in `Reducers/` — each file contributes methods to the same partial class.

```csharp
public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void MoveEntity(ReducerContext ctx, uint entityId, float x, float y)
    {
        var ownership = ctx.Db.EntityOwnership.EntityId.Find(entityId)
            ?? throw new Exception("Entity not owned");
        if (ownership.Owner != ctx.Sender)
            throw new Exception("Not authorized");

        var entity = ctx.Db.Entities.EntityId.Find(entityId)!.Value;
        ctx.Db.Entities.EntityId.Update(entity with { PositionX = x, PositionY = y });
    }
}
```

### Lifecycle Hooks (CRITICAL)

Lifecycle hook names MUST NOT start with "On" — this causes STDB0010 error.

```csharp
// ✅ CORRECT
[SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
public static void ClientConnected(ReducerContext ctx) { }

// ❌ WRONG — STDB0010 error
[SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
public static void OnClientConnected(ReducerContext ctx) { }
```

### Update Pattern (CRITICAL)

Always use `Find()` + `Update(row with { ... })`. Never partial-update structs.

```csharp
// ✅ CORRECT
if (ctx.Db.Players.Identity.Find(ctx.Sender) is Player player)
{
    ctx.Db.Players.Identity.Update(player with { IsConnected = true });
}

// ❌ WRONG — nulls out other fields
ctx.Db.Players.Identity.Update(new Player { Identity = ctx.Sender, IsConnected = true });
```

### Validation & Error Handling

- Validate inputs: throw `Exception` to roll back the transaction
- Check ownership before mutating entities
- Use `ctx.Timestamp` for timestamps, not `DateTime.UtcNow`
- Scheduled reducers: `ScheduledAt = new ScheduleAt.Time(ctx.Timestamp + TimeSpan.FromSeconds(60))`

## ⛔ Common Mistakes

| Wrong | Right | Error |
|-------|-------|-------|
| `ctx.Db.User` when `Accessor = "user"` | `ctx.Db.user` | Case-sensitive |
| `ctx.Db.Table.Get(id)` | `ctx.Db.Table.Id.Find(id)` | Method not found |
| `[Index.BTree(...)]` | `[SpacetimeDB.Index.BTree(...)]` | Ambiguous with `System.Index` |
| `Optional<string>` | `string?` | Type not found |
| `.csproj` name mismatch | `NomadServer.csproj` | Publish fails silently |
| .NET 9 SDK | .NET 8 SDK only | WASI compilation fails |
| Missing WASI workload | `dotnet workload install wasi-experimental` | Build fails |
| `[Procedure]` attribute | Reducers only | Procedures not supported in C# |
| Missing `Public = true` | Add to `[Table]` attribute | Clients can't subscribe |
| `Random` in reducers | Avoid non-deterministic code | Sandbox violation |
| async/await in reducers | Synchronous only | Not supported |
| `TaggedEnum<(A, B)>` | `TaggedEnum<(A A, B B)>` | Missing variant names |
| `partial struct : TaggedEnum` | `partial record : TaggedEnum` | Sum types must be record |

## Feature Implementation Checklist

When implementing a feature that spans backend and client:

1. **Backend:** Define table(s) to store the data
2. **Backend:** Define reducer(s) to mutate the data
3. **Client:** Subscribe to the table(s)
4. **Client:** Call the reducer(s) from UI — **don't forget this step!**
5. **Client:** Render the data from the table(s)

**Common mistake:** Building backend tables/reducers but forgetting to wire up the client to call them.

## Debugging Checklist

1. Is SpacetimeDB server running? (`spacetime start`)
2. Is the module published? (`spacetime publish`)
3. Are client bindings generated? (`spacetime generate`)
4. Check server logs for errors (`spacetime logs nomad`)
5. **Is the reducer actually being called from the client?**
