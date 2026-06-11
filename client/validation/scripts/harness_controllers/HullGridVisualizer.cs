namespace Nomad.Validation.HarnessControllers;

using Godot;
using Nomad.Game.Ship;

public partial class HullGridVisualizer : Node2D
{
    private const float CellSize = 48f;
    private const float Margin = 24f;

    private HullTemplate _hull = null!;

    public void SetHull(HullTemplate hull)
    {
        _hull = hull;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_hull is null)
            return;

        var gridWidth = _hull.GridWidth * CellSize;
        var gridHeight = _hull.GridHeight * CellSize;

        // Background fill
        DrawRect(
            new Rect2(Vector2.Zero, new Vector2(gridWidth + Margin * 2, gridHeight + Margin * 2)),
            new Color(0.05f, 0.05f, 0.08f)
        );

        var gridOrigin = new Vector2(Margin, Margin);

        // Grid cells
        for (var x = 0; x < _hull.GridWidth; x++)
        {
            for (var y = 0; y < _hull.GridHeight; y++)
            {
                var cellRect = new Rect2(
                    gridOrigin + new Vector2(x * CellSize, y * CellSize),
                    new Vector2(CellSize, CellSize)
                );
                DrawRect(cellRect, new Color(0.12f, 0.12f, 0.16f));
                DrawRect(cellRect, new Color(0.2f, 0.2f, 0.25f), false);
            }
        }

        // Room slots
        foreach (var slot in _hull.RoomSlots)
        {
            var slotRect = new Rect2(
                gridOrigin + new Vector2(slot.PositionX * CellSize, slot.PositionY * CellSize),
                new Vector2(slot.Width * CellSize, slot.Height * CellSize)
            );
            DrawRect(slotRect, new Color(0.15f, 0.5f, 0.85f, 0.35f));
            DrawRect(slotRect, new Color(0.3f, 0.7f, 1.0f, 0.7f), false, 2f);

            // Slot index label — approximate center
            var center = slotRect.Position + slotRect.Size * 0.5f;
            DrawString(
                ThemeDB.FallbackFont,
                center - new Vector2(6, 8),
                slot.SlotIndex.ToString(),
                fontSize: 16
            );
        }
    }
}
