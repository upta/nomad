# SpacetimeDB Server Module

SDK reference: [spacetimedb.com/llms.txt](https://spacetimedb.com/llms.txt). Requires .NET 8 SDK with the `wasi-experimental` workload (.NET 9 WASI compilation fails).

## Core rules

1. **Reducers are transactional and return nothing** — clients read data via table subscriptions, never reducer return values
2. **Reducers must be deterministic** — no filesystem, network, timers, `Random`, `DateTime.UtcNow` (use `ctx.Timestamp`), or async/await
3. **`ctx.Sender` is the authenticated principal** — never trust identity arguments from clients; check ownership before mutating
4. **Auto-increment IDs are not sequential** — gaps are normal, never use for ordering

## Commands (from `server/`)

```powershell
spacetime build --module-path ./src
spacetime publish nomad --yes --server local --module-path ./src                  # keep data
spacetime publish nomad --delete-data=always --yes --server local --module-path ./src  # wipe data
spacetime generate --lang csharp --out-dir ../client/Db --module-path ./src      # client bindings
spacetime logs nomad --server local
```

After any schema or reducer-signature change: build → publish → regenerate client bindings, or the client won't compile/connect.

## Tables (`src/Tables/`)

`partial struct` with the table attribute — missing `partial` fails to compile; missing `Public = true` means clients can't subscribe:

```csharp
[SpacetimeDB.Table(Accessor = "Players", Public = true)]
public partial struct Player
{
    [SpacetimeDB.PrimaryKey]
    public Identity Identity;
    public bool IsConnected;
}
```

- Column types: numeric primitives, `bool`, `string`, `Identity`, `Timestamp`, `ScheduleAt`, `T?`, `List<T>`
- Auto-increment: insert with `0`, read the assigned id off the returned row
- Indexes: full namespace `[SpacetimeDB.Index.BTree(...)]` (bare `[Index.BTree]` collides with `System.Index`); index names unique across the whole module
- Sum types: `partial record` + `TaggedEnum<(A NameA, B NameB)>` — variants must be named, structs not allowed

## Reducers (`src/Reducers/`)

Static methods on `public static partial class Module`, one file per reducer (all contribute to the same partial class). Validate inputs and `throw` to roll back the transaction.

```csharp
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
```

### Lifecycle hooks — names must NOT start with `On` (STDB0010)

```csharp
[SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
public static void ClientConnected(ReducerContext ctx) { }   // ✅
// ❌ OnClientConnected → STDB0010 (the official template docs are wrong for SDK 2.4.x)
```

### Updates — always `Find()` + `Update(row with { ... })`

```csharp
if (ctx.Db.Players.Identity.Find(ctx.Sender) is Player player)
    ctx.Db.Players.Identity.Update(player with { IsConnected = true });
// ❌ Update(new Player { Identity = ..., IsConnected = true }) — nulls every other field
```

Scheduled reducers: `ScheduledAt = new ScheduleAt.Time(ctx.Timestamp + TimeSpan.FromSeconds(60))`.

## Common mistakes

| Wrong | Right |
|---|---|
| `ctx.Db.Players` when `Accessor = "players"` | Accessor casing is exact |
| `ctx.Db.Table.Get(id)` | `ctx.Db.Table.Id.Find(id)` |
| `Optional<string>` | `string?` |
| `[Procedure]` | Reducers only — procedures unsupported in C# |

## Feature checklist (backend ↔ client)

1. Table(s) to store the data
2. Reducer(s) to mutate it
3. Client subscribes to the table(s)
4. Client **actually calls the reducer(s)** — the most common miss
5. Client renders from the table(s)

Debugging: is `spacetime start` running → module published → bindings regenerated → `spacetime logs nomad` clean → is the reducer actually being invoked from the client?
