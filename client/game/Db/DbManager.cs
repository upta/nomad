#nullable enable

namespace Nomad.Game.Db;

using Godot;
using SpacetimeDB.Types;

public partial class DbManager : Node
{
    private const string DefaultDbName = "nomad";
    private const string DefaultUri = "http://localhost:3000";

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
        var clientId = GetClientId();

        if (clientId != "main")
            GetWindow().Title = $"Nomad — {clientId}";

        SpacetimeDB.AuthToken.Init($".nomad-{clientId}");

        Connection = DbConnection
            .Builder()
            .WithUri(GetConfigValue("NOMAD_STDB_URI", DefaultUri))
            .WithDatabaseName(GetConfigValue("NOMAD_STDB_DB", DefaultDbName))
            .WithToken(SpacetimeDB.AuthToken.Token)
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

    private static string GetClientId()
    {
        var args = OS.GetCmdlineUserArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--client")
                return args[i + 1];
        }
        return GetConfigValue("NOMAD_CLIENT_ID", "main");
    }

    private static string GetConfigValue(string variable, string fallback)
    {
        var value = OS.GetEnvironment(variable);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private void OnConnected(DbConnection conn, SpacetimeDB.Identity identity, string token)
    {
        GD.Print($"[DbManager] Connected. Identity: {identity}");
        SpacetimeDB.AuthToken.SaveToken(token);

        conn.SubscriptionBuilder()
            .OnApplied(ctx =>
            {
                GD.Print("[DbManager] Subscription applied.");
                OnDataReady?.Invoke();
            })
            .SubscribeToAllTables();
    }
}
