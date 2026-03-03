using Godot;

/// <summary>
/// Places wall tiles to seal open doorway connectors.
/// Targets the "Floors-Walls" TileMapLayer specifically (ignores Interior, etc.).
/// </summary>
public class DoorwayCloser
{
    private const string WallLayerName = "Floors-Walls";
    private const int TileSourceId = 1; // Must match the atlas source ID in your TileSet

    // Horizontal doorway settings
    private const int HorizontalTileCount = 3;
    private static readonly Vector2I BottomLeftTile  = new(1, 0);
    private static readonly Vector2I BottomRightTile = new(2, 0);
    private static readonly Vector2I TopLeftTile     = new(1, 4);
    private static readonly Vector2I TopRightTile    = new(3, 4);

    // Vertical doorway settings
    private const int VerticalTileCount = 5;
    private static readonly Vector2I RightTopTile    = new(5, 3);
    private static readonly Vector2I RightMidTile    = new(5, 1);
    private static readonly Vector2I RightBottomTile = new(5, 2);
    private static readonly Vector2I LeftTopTile     = new(0, 0);
    private static readonly Vector2I LeftMidTile     = new(0, 1);
    private static readonly Vector2I LeftBottomTile  = new(0, 2);

    /// <summary>
    /// Closes a single connector by filling its doorway with wall tiles on the Floors-Walls layer.
    /// </summary>
    public void CloseConnector(Area2D connector, Area2D room)
    {
        TileMapLayer wallLayer = room.GetNodeOrNull<TileMapLayer>(WallLayerName);
        if (wallLayer == null)
        {
            GD.PrintErr($"Room missing '{WallLayerName}' layer — cannot close connector.");
            return;
        }

        string name = connector.Name;
        Vector2I baseTile = wallLayer.LocalToMap(connector.Position);
        GD.Print($"Closing {name} at tile {baseTile}");

        if (name.Contains("+Y"))
            FillHorizontal(wallLayer, baseTile + new Vector2I(0, 1),  BottomLeftTile, BottomRightTile);
        else if (name.Contains("-Y"))
            FillHorizontal(wallLayer, baseTile + new Vector2I(0, -1), TopLeftTile,    TopRightTile);
        else if (name.Contains("+X"))
            FillVertical(wallLayer, baseTile + new Vector2I(-1, 0), RightTopTile, RightMidTile, RightBottomTile);
        else if (name.Contains("-X"))
            FillVertical(wallLayer, baseTile + new Vector2I(1, 0),  LeftTopTile,  LeftMidTile,  LeftBottomTile);
    }

    private static void FillHorizontal(TileMapLayer map, Vector2I pos, Vector2I leftTile, Vector2I rightTile)
    {
        int half = HorizontalTileCount / 2;
        for (int i = -half; i <= half; i++)
        {
            Vector2I tile = (i == -half) ? leftTile : (i == half) ? rightTile : leftTile;
            map.SetCell(pos + new Vector2I(i, 0), TileSourceId, tile);
        }
    }

    private static void FillVertical(TileMapLayer map, Vector2I pos, Vector2I topTile, Vector2I midTile, Vector2I bottomTile)
    {
        int half = VerticalTileCount / 2;
        for (int i = -half; i <= half; i++)
        {
            Vector2I tile = (i <= -1) ? topTile : (i == 0) ? midTile : bottomTile;
            map.SetCell(pos + new Vector2I(0, i), TileSourceId, tile);
        }
    }
}
