namespace Nomad.Game.Ship;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class RoomTypeRegistry : Node
{
    private readonly Dictionary<string, RoomType> _byId = [];

    public IReadOnlyList<RoomType> All { get; private set; } = [];

    public override void _Ready()
    {
        LoadAll();
    }

    public RoomType? Find(string roomId) => _byId.TryGetValue(roomId, out var rt) ? rt : null;

    public RoomType GetRequired(string roomId) =>
        Find(roomId) ?? throw new KeyNotFoundException($"RoomType '{roomId}' not registered.");

    private void LoadAll()
    {
        var dir = "res://game/Ship/RoomTypes/";
        var paths = new[]
        {
            $"{dir}ReactorRoom.tres",
            $"{dir}BridgeRoom.tres",
            $"{dir}CloningBayRoom.tres",
            $"{dir}HydroponicsRoom.tres",
            $"{dir}WorkshopRoom.tres",
            $"{dir}KitchenRoom.tres",
            $"{dir}CargoBayRoom.tres",
        };

        var list = new List<RoomType>();
        foreach (var path in paths)
        {
            var rt = GD.Load<RoomType>(path);
            _byId[rt.RoomId] = rt;
            list.Add(rt);
        }

        All = list;

        GD.Print(
            $"[RoomTypeRegistry] Loaded {list.Count} room types: {string.Join(", ", list.Select(r => r.RoomId))}"
        );
    }
}
