namespace Nomad.Game.Map;

using Godot;

public partial class ShipGrid : Node2D
{
    private const int TileSize = 64;
    private const int GridWidth = 8;
    private const int GridHeight = 6;

    private static readonly Color FloorColor = new(0.25f, 0.28f, 0.33f);
    private static readonly Color WallColor = new(0.45f, 0.48f, 0.50f);
    private static readonly Color InteriorColor = new(0.30f, 0.33f, 0.38f);

    public override void _Draw()
    {
        var halfW = GridWidth * TileSize / 2f;
        var halfH = GridHeight * TileSize / 2f;

        // Hull background (filled)
        DrawRect(
            new Rect2(-halfW, -halfH, GridWidth * TileSize, GridHeight * TileSize),
            FloorColor,
            true
        );

        // Interior rooms (simple 3x2 room layout with a center corridor)
        var roomW = 3 * TileSize;
        var roomH = 2 * TileSize;

        // Top-left room
        DrawRect(
            new Rect2(-halfW + TileSize, -halfH + TileSize, roomW, roomH),
            InteriorColor,
            true
        );
        DrawRect(
            new Rect2(-halfW + TileSize, -halfH + TileSize, roomW, roomH),
            WallColor,
            false,
            2
        );

        // Top-right room
        DrawRect(
            new Rect2(-halfW + 4 * TileSize, -halfH + TileSize, roomW, roomH),
            InteriorColor,
            true
        );
        DrawRect(
            new Rect2(-halfW + 4 * TileSize, -halfH + TileSize, roomW, roomH),
            WallColor,
            false,
            2
        );

        // Bottom-left room
        DrawRect(
            new Rect2(-halfW + TileSize, -halfH + 3 * TileSize, roomW, roomH),
            InteriorColor,
            true
        );
        DrawRect(
            new Rect2(-halfW + TileSize, -halfH + 3 * TileSize, roomW, roomH),
            WallColor,
            false,
            2
        );

        // Bottom-right room
        DrawRect(
            new Rect2(-halfW + 4 * TileSize, -halfH + 3 * TileSize, roomW, roomH),
            InteriorColor,
            true
        );
        DrawRect(
            new Rect2(-halfW + 4 * TileSize, -halfH + 3 * TileSize, roomW, roomH),
            WallColor,
            false,
            2
        );

        // Hull outline
        DrawRect(
            new Rect2(-halfW, -halfH, GridWidth * TileSize, GridHeight * TileSize),
            WallColor,
            false,
            3
        );
    }
}
