#nullable enable

namespace Nomad.Game.Db;

using Godot;
using SpacetimeDB.Types;

public partial class DbManager : Node
{
    public event Action? OnConnectionFailed;
    public event Action? OnDataReady;

    public DbConnection Connection { get; private set; } = null!;

    public override void _EnterTree()
    {
        Name = nameof(DbManager);
    }

    public override void _ExitTree()
    {
        Connection?.Disconnect();
    }

    public override void _Process(double delta)
    {
        Connection?.FrameTick();
    }

    public void Connect()
    {
        Connection = DbConnection
            .Builder()
            .WithUri("http://localhost:3000")
            .WithDatabaseName("nomad")
            .OnConnect(OnConnected)
            .OnConnectError(e =>
            {
                GD.PushError($"[DbManager] Connection error: {e.Message}");
                OnConnectionFailed?.Invoke();
            })
            .OnDisconnect(
                (conn, e) =>
                {
                    if (e != null)
                        GD.PushError($"[DbManager] Disconnected: {e.Message}");
                }
            )
            .Build();
    }

    private void OnConnected(DbConnection conn, SpacetimeDB.Identity identity, string token)
    {
        GD.Print($"[DbManager] Connected. Identity: {identity}");

        conn.SubscriptionBuilder()
            .OnApplied(ctx =>
            {
                GD.Print("[DbManager] Subscription applied.");
                OnDataReady?.Invoke();
            })
            .SubscribeToAllTables();
    }
}
