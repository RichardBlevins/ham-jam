using System.Collections.Generic;
using Godot;

public partial class World : Node2D
{
    [Export] public int MaxRooms = 5;
    [Export] public int MaxActiveRooms = 5;

    [ExportGroup("Room Configuration")]
    [Export] public PackedScene[] RoomScenes = new PackedScene[0];
    [Export] public int[] RoomWeights = new int[0];
    [Export] public int[] RoomMaxCounts = new int[0];  // -1 = unlimited
    [Export] public int[] RoomGuaranteed = new int[0];  // 0 = no, 1 = yes
    [Export] public float PhysicsUpdateDelay = 0.05f;

    private Camera2D _camera;
    private RoomSelector _roomSelector;
    private DoorwayCloser _doorwayCloser = new();
    private RandomNumberGenerator _rng = new();

    private List<Area2D> _allRooms = new();
    private List<Area2D> _activeRooms = new();

    public override void _Ready()
    {
        _rng.Randomize();
        _camera = GetNode<Camera2D>("Camera2D");
        _roomSelector = new RoomSelector(RoomScenes, RoomWeights, RoomMaxCounts, RoomGuaranteed);

        Manager.Instance.MaxRooms = MaxRooms;

        var player = GetNode<Player>("Player");
        player.OnNormalRoomEntered += pos => _camera.GlobalPosition = pos;

        if (RoomScenes.Length == 0)
        {
            GD.PrintErr("No room scenes configured!");
            return;
        }

        SpawnRoom(Vector2.Zero, null, 0);
    }

    #region Room Spawning

    private void SpawnRoom(Vector2 position, Area2D fromConnector = null, int forcedIndex = -1)
    {
        int index = _roomSelector.SelectRoom(forcedIndex);
        if (index == -1)
        {
            GD.PrintErr("No rooms available to spawn!");
            return;
        }

        Area2D room = InstantiateRoom(index);
        PositionRoom(room, position, fromConnector);
        Manager.Instance.Rooms++;

        // Wait one physics frame so overlap detection updates before scanning connectors
        GetTree().CreateTimer(PhysicsUpdateDelay).Timeout += ProcessConnectors;
    }

    private Area2D InstantiateRoom(int index)
    {
        Area2D room = _roomSelector.GetScene(index).Instantiate<Area2D>();
        GetNode<Node2D>("Rooms").AddChild(room);

        _allRooms.Add(room);
        _activeRooms.Add(room);

        if (_activeRooms.Count > MaxActiveRooms)
            _activeRooms.RemoveAt(0);

        return room;
    }

    #endregion

    #region Room Positioning

    private void PositionRoom(Area2D room, Vector2 position, Area2D fromConnector)
    {
        if (fromConnector == null)
        {
            room.GlobalPosition = position;
            return;
        }

        string opposite = GetOppositeConnector(fromConnector.Name);
        Area2D target = room.GetNodeOrNull<Node2D>("Connectors")?.GetNodeOrNull<Area2D>(opposite);

        if (target == null)
        {
            GD.PrintErr($"Missing connector: {opposite}");
            room.GlobalPosition = position;
            return;
        }

        var fromShape = fromConnector.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        var toShape = target.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        if (fromShape == null || toShape == null)
        {
            GD.PrintErr("Connector missing CollisionShape2D!");
            room.GlobalPosition = position;
            return;
        }

        room.GlobalPosition = (fromConnector.GlobalPosition + fromShape.Position)
                             - (target.Position + toShape.Position);

        GD.Print($"Connected {fromConnector.Name} → {opposite}");
    }

    private static string GetOppositeConnector(string name) => name switch
    {
        "+X_Connector" => "-X_Connector",
        "-X_Connector" => "+X_Connector",
        "+Y_Connector" => "-Y_Connector",
        "-Y_Connector" => "+Y_Connector",
        _ => name
    };

    #endregion

    #region Connector Processing

    private void ProcessConnectors()
    {
        if (Manager.Instance.Rooms >= Manager.Instance.MaxRooms)
        {
            CloseAllOpenConnectors();
            return;
        }

        var free = FindFreeConnectors(_activeRooms);

        if (free.Count == 0)
        {
            ForceSpawnFromAnyConnector();
            return;
        }

        Area2D chosen = PickWeightedConnector(free);
        if (chosen != null)
        {
            GD.Print($"Spawning at connector: {chosen.Name}");
            SpawnRoom(Vector2.Zero, chosen);
        }
    }

    private void CloseAllOpenConnectors()
    {
        GD.Print("Max rooms reached — closing all open connectors");

        foreach (Area2D room in _allRooms)
        {
            if (!IsInstanceValid(room)) continue;

            Node2D connectors = room.GetNodeOrNull<Node2D>("Connectors");
            if (connectors == null) continue;

            foreach (Node child in connectors.GetChildren())
            {
                if (child is Area2D connector && !connector.HasOverlappingAreas())
                    _doorwayCloser.CloseConnector(connector, room);
            }
        }
    }

    private List<(Area2D connector, int weight)> FindFreeConnectors(List<Area2D> rooms)
    {
        var result = new List<(Area2D, int)>();

        for (int i = 0; i < rooms.Count; i++)
        {
            Node2D connectors = rooms[i].GetNodeOrNull<Node2D>("Connectors");
            if (connectors == null) continue;

            foreach (Node child in connectors.GetChildren())
            {
                if (child is Area2D connector && !connector.HasOverlappingAreas())
                    result.Add((connector, i + 1));
            }
        }

        return result;
    }

    private void ForceSpawnFromAnyConnector()
    {
        GD.Print("No free connectors — forcing spawn");

        var all = new List<Area2D>();
        foreach (Area2D room in _activeRooms)
        {
            Node2D connectors = room.GetNodeOrNull<Node2D>("Connectors");
            if (connectors == null) continue;

            foreach (Node child in connectors.GetChildren())
            {
                if (child is Area2D c) all.Add(c);
            }
        }

        if (all.Count > 0)
            SpawnRoom(Vector2.Zero, all[_rng.RandiRange(0, all.Count - 1)]);
    }

    private Area2D PickWeightedConnector(List<(Area2D connector, int weight)> connectors)
    {
        if (connectors.Count == 0) return null;

        int total = 0;
        foreach (var (_, w) in connectors) total += w;

        int roll = _rng.RandiRange(0, total - 1);
        int cumulative = 0;
        foreach (var (connector, w) in connectors)
        {
            cumulative += w;
            if (roll < cumulative) return connector;
        }

        return connectors[0].connector;
    }

    #endregion
}
