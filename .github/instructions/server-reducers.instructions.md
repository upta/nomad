---
applyTo: "server/src/Reducers/**"
---

# SpacetimeDB Reducer Instructions

These instructions apply to files under `server/src/Reducers/**`.

## Core Rules

- Every reducer must validate the current GamePhase if it's phase-specific
- **Reducers are transactional** — they do not return data to callers
- **Reducers must be deterministic** — no filesystem, network, timers, or random
- **Read data via tables/subscriptions** — not reducer return values
- **`ctx.Sender` is the authenticated principal** — never trust identity args
- **Lifecycle hooks must NOT start with "On"** — `ClientConnected`, not `OnConnect` (STDB0010 error, despite SDK template docs showing OnConnect)

## Update Pattern (CRITICAL)

```csharp
// ✅ CORRECT — use `with` expression
if (ctx.Db.Players.Identity.Find(ctx.Sender) is Player player)
{
    ctx.Db.Players.Identity.Update(player with { IsConnected = true });
}

// ❌ WRONG — partial update nulls out other fields!
ctx.Db.Players.Identity.Update(new Player { Identity = ctx.Sender, IsConnected = true });
```

## Table Accessor Casing

Table accessor casing is exact — `ctx.Db.user` for `Accessor = "user"`, NOT `ctx.Db.User`.

## Build

- Build with `spacetime build` in `server/` after changes
- Check integration tests in `client/validation/scenarios/` after implementing a reducer
