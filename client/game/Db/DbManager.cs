#nullable enable

namespace Nomad.Game.Db;

using Godot;
using SpacetimeDB.Types;

public partial class DbManager : Node
{
    private DbConnection? _conn;

    public RemoteTables? Tables => _conn?.Db;
    public RemoteReducers? Reducers => _conn?.Reducers;
    public new bool IsConnected => _conn?.IsActive ?? false;

    public override void _Ready()
    {
        _conn = DbConnection
            .Builder()
            .WithUri("http://localhost:3000")
            .WithDatabaseName("nomad")
            .OnConnect(OnConnected)
            .OnConnectError(ex => GD.PrintErr($"[DbManager] Connection failed: {ex.Message}"))
            .OnDisconnect(
                (conn, ex) =>
                {
                    if (ex != null)
                        GD.PrintErr($"[DbManager] Disconnected with error: {ex.Message}");
                    else
                        GD.Print("[DbManager] Disconnected.");
                }
            )
            .Build();
    }

    public override void _Process(double delta)
    {
        _conn?.FrameTick();
    }

    public override void _ExitTree()
    {
        _conn?.Disconnect();
    }

    private static void OnConnected(DbConnection conn, SpacetimeDB.Identity identity, string token)
    {
        GD.Print($"[DbManager] Connected. Identity: {identity}");

        conn.SubscriptionBuilder()
            .OnApplied(ctx =>
                GD.Print("[DbManager] Subscription applied — initial table data loaded.")
            )
            .SubscribeToAllTables();
    }
}
