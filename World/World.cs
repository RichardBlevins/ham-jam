using System.Collections.Generic;
using Godot;

public partial class World : Node2D
{
    [Export] public int MaxRooms = 5;
    [Export] public int MaxActiveRooms = 5;
    
    [ExportGroup("Room Configuration")]
    [Export] public PackedScene[] RoomScenes = new PackedScene[0];
    [Export] public int[] RoomWeights = new int[0];
    [Export] public int[] RoomMaxCounts = new int[0]; // -1 = unlimited
    [Export] public int[] RoomGuaranteed = new int[0]; // 0 = no, 1 = yes
    
    [Export] public float PhysicsUpdateDelay = 0.05f;
    
    // Tile counts for closing doorways
    [ExportGroup("Doorway Closure Settings")]
    [Export] public int HorizontalDoorwayTileCount = 3;
    [Export] public int VerticalDoorwayTileCount = 5;
    
    private Camera2D camera2D;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    
    // Track all spawned rooms
    private List<Area2D> allRooms = new List<Area2D>();
    
    // Track last N rooms for weighted spawning
    private List<Area2D> activeRooms = new List<Area2D>();
    
    // Track spawn counts per room type
    private Dictionary<int, int> roomSpawnCounts = new Dictionary<int, int>();

    public override void _Ready()
    {
        camera2D = GetNode<Camera2D>("Camera2D");
        Player player = GetNode<Player>("Player");
        player.OnNormalRoomEntered += OnNormalRoomEntered;
        
        Manager.Instance.MaxRooms = MaxRooms;
        
        if (RoomScenes.Length == 0)
        {
            GD.PrintErr("No room scenes specified! Add room scenes in the inspector.");
        }
        
        // Initialize default weights if not set
        if (RoomWeights.Length != RoomScenes.Length)
        {
            GD.Print("Room weights array size mismatch - using default weight of 1 for all rooms");
        }
        
        // Initialize default max counts if not set
        if (RoomMaxCounts.Length != RoomScenes.Length)
        {
            GD.Print("Room max counts array size mismatch - using unlimited (-1) for all rooms");
        }
        
        // Initialize spawn counts
        for (int i = 0; i < RoomScenes.Length; i++)
        {
            roomSpawnCounts[i] = 0;
        }
        
        // Always spawn room0 as the first room
        SpawnRoom(new Vector2(0, 0), null, 0);
    }
    
    private void SpawnRoom(Vector2 position, Area2D fromConnector = null, int forcedRoomIndex = -1)
    {
        if (RoomScenes.Length == 0)
        {
            GD.PrintErr("Cannot spawn room: No room scenes available!");
            return;
        }
        
        int selectedIndex = (forcedRoomIndex >= 0) ? forcedRoomIndex : SelectRandomRoomIndex();
        
        if (selectedIndex == -1)
        {
            GD.PrintErr("No rooms available to spawn!");
            return;
        }
        
        roomSpawnCounts[selectedIndex]++;
        
        Area2D newRoom = InstantiateRoom(selectedIndex);
        PositionRoom(newRoom, position, fromConnector);
        
        Manager.Instance.Rooms += 1;
        
        var timer = GetTree().CreateTimer(PhysicsUpdateDelay);
        timer.Timeout += CheckAllRoomsForConnectors;
    }
    
    private int SelectRandomRoomIndex()
    {
        // First check if there are guaranteed rooms that haven't spawned yet
        int guaranteedRoom = GetUnspawnedGuaranteedRoom();
        if (guaranteedRoom != -1)
        {
            GD.Print($"Spawning guaranteed room {guaranteedRoom}");
            return guaranteedRoom;
        }
        
        // Otherwise use normal weighted selection
        List<(int index, int weight)> availableRooms = GetAvailableRooms();
        
        if (availableRooms.Count == 0)
        {
            return -1;
        }
        
        return PickWeightedRandom(availableRooms);
    }
    
    private int GetUnspawnedGuaranteedRoom()
    {
        for (int i = 0; i < RoomScenes.Length; i++)
        {
            if (RoomScenes[i] == null) continue;
            
            bool isGuaranteed = (i < RoomGuaranteed.Length) && RoomGuaranteed[i] == 1;
            bool hasNotSpawned = roomSpawnCounts[i] == 0;
            
            if (isGuaranteed && hasNotSpawned)
            {
                return i;
            }
        }
        
        return -1;
    }
    
    private List<(int index, int weight)> GetAvailableRooms()
    {
        List<(int index, int weight)> availableRooms = new List<(int, int)>();
        
        for (int i = 0; i < RoomScenes.Length; i++)
        {
            if (RoomScenes[i] == null) continue;
            
            int weight = (i < RoomWeights.Length) ? RoomWeights[i] : 1;
            int maxCount = (i < RoomMaxCounts.Length) ? RoomMaxCounts[i] : -1;
            bool canSpawn = (maxCount == -1 || roomSpawnCounts[i] < maxCount);
            
            if (canSpawn)
            {
                availableRooms.Add((i, weight));
            }
        }
        
        return availableRooms;
    }
    
    private int PickWeightedRandom(List<(int index, int weight)> availableRooms)
    {
        int totalWeight = 0;
        foreach (var (_, weight) in availableRooms)
        {
            totalWeight += weight;
        }
        
        _rng.Randomize();
        int randomValue = _rng.RandiRange(0, totalWeight - 1);
        
        int currentWeight = 0;
        foreach (var (index, weight) in availableRooms)
        {
            currentWeight += weight;
            if (randomValue < currentWeight)
            {
                return index;
            }
        }
        
        return availableRooms[0].index;
    }
    
    private Area2D InstantiateRoom(int roomIndex)
    {
        PackedScene roomScene = RoomScenes[roomIndex];
        Area2D newRoom = roomScene.Instantiate<Area2D>();
        
        Node2D roomsNode = GetNode<Node2D>("Rooms");
        roomsNode.AddChild(newRoom);
        
        allRooms.Add(newRoom);
        activeRooms.Add(newRoom);
        
        if (activeRooms.Count > MaxActiveRooms)
        {
            activeRooms.RemoveAt(0);
        }
        
        int maxCount = (roomIndex < RoomMaxCounts.Length) ? RoomMaxCounts[roomIndex] : -1;
        GD.Print($"Spawned room {roomIndex} (count: {roomSpawnCounts[roomIndex]}/{maxCount})");
        
        return newRoom;
    }
    
    private void PositionRoom(Area2D newRoom, Vector2 position, Area2D fromConnector)
    {
        if (fromConnector == null)
        {
            newRoom.GlobalPosition = position;
            return;
        }
        
        string connectorName = fromConnector.Name;
        string oppositeConnectorName = GetOppositeConnectorName(connectorName);
        
        Node2D newConnectors = newRoom.GetNode<Node2D>("Connectors");
        Area2D oppositeConnector = newConnectors.GetNode<Area2D>(oppositeConnectorName);
        
        if (oppositeConnector == null)
        {
            GD.PrintErr($"Could not find connector: {oppositeConnectorName}");
            return;
        }
        
        CollisionShape2D fromShape = fromConnector.GetNode<CollisionShape2D>("CollisionShape2D");
        CollisionShape2D toShape = oppositeConnector.GetNode<CollisionShape2D>("CollisionShape2D");
        
        Vector2 fromShapeGlobal = fromConnector.GlobalPosition + fromShape.Position;
        Vector2 toShapeLocal = oppositeConnector.Position + toShape.Position;
        
        newRoom.GlobalPosition = fromShapeGlobal - toShapeLocal;
        
        GD.Print($"Connected {connectorName} to {oppositeConnectorName}");
    }
    
    private string GetOppositeConnectorName(string connectorName)
    {
        return connectorName switch
        {
            "+X_Connector" => "-X_Connector",
            "-X_Connector" => "+X_Connector",
            "+Y_Connector" => "-Y_Connector",
            "-Y_Connector" => "+Y_Connector",
            _ => connectorName
        };
    }
    
    private void CheckAllRoomsForConnectors()
    {
        if (Manager.Instance.Rooms >= Manager.Instance.MaxRooms)
        {
            CloseAllOpenConnectors();
            return;
        }
        
        List<(Area2D connector, int roomIndex)> freeConnectors = CollectFreeConnectors();
        
        if (freeConnectors.Count == 0)
        {
            HandleNoFreeConnectors();
            return;
        }
        
        Area2D chosenConnector = PickWeightedConnector(freeConnectors);
        
        if (chosenConnector != null)
        {
            GD.Print($"Spawning at connector: {chosenConnector.Name}");
            SpawnRoom(Vector2.Zero, chosenConnector);
        }
    }
    
    private List<(Area2D connector, int roomIndex)> CollectFreeConnectors()
    {
        List<(Area2D connector, int roomIndex)> freeConnectors = new List<(Area2D, int)>();
        
        for (int i = 0; i < activeRooms.Count; i++)
        {
            Area2D room = activeRooms[i];
            Node2D connectors = room.GetNode<Node2D>("Connectors");
            
            foreach (Node child in connectors.GetChildren())
            {
                if (child is Area2D connector && !connector.HasOverlappingAreas())
                {
                    freeConnectors.Add((connector, i));
                }
            }
        }
        
        return freeConnectors;
    }
    
    private void HandleNoFreeConnectors()
    {
        GD.Print("No free connectors - forcing spawn");
        
        List<Area2D> allConnectors = new List<Area2D>();
        
        foreach (Area2D room in activeRooms)
        {
            Node2D connectors = room.GetNode<Node2D>("Connectors");
            
            foreach (Node child in connectors.GetChildren())
            {
                if (child is Area2D connector)
                {
                    allConnectors.Add(connector);
                }
            }
        }
        
        if (allConnectors.Count > 0)
        {
            _rng.Randomize();
            int randomIndex = _rng.RandiRange(0, allConnectors.Count - 1);
            Area2D forcedConnector = allConnectors[randomIndex];
            
            SpawnRoom(Vector2.Zero, forcedConnector);
        }
    }
    
    private Area2D PickWeightedConnector(List<(Area2D connector, int roomIndex)> connectors)
    {
        int totalWeight = 0;
        foreach (var (_, roomIndex) in connectors)
        {
            totalWeight += roomIndex + 1;
        }
        
        _rng.Randomize();
        int randomWeight = _rng.RandiRange(0, totalWeight - 1);
        
        int currentWeight = 0;
        foreach (var (connector, roomIndex) in connectors)
        {
            currentWeight += roomIndex + 1;
            if (randomWeight < currentWeight)
            {
                return connector;
            }
        }
        
        return null;
    }
    
    private void CloseAllOpenConnectors()
    {
        GD.Print("Max rooms reached - closing all open connectors");
        
        foreach (Area2D room in allRooms)
        {
            Node2D connectors = room.GetNode<Node2D>("Connectors");
            TileMapLayer tileMap = room.GetNode<TileMapLayer>("Floors-Walls");
            
            foreach (Node child in connectors.GetChildren())
            {
                if (child is Area2D connector && !connector.HasOverlappingAreas())
                {
                    CloseConnector(connector, tileMap, room);
                }
            }
        }
    }
    
    private void CloseConnector(Area2D connector, TileMapLayer tileMap, Area2D room)
    {
        string connectorName = connector.Name;
        Vector2 localPos = connector.Position;
        Vector2I baseTilePos = tileMap.LocalToMap(localPos);
        
        GD.Print($"Closing {connectorName} at {baseTilePos}");
        
        if (connectorName.Contains("+Y"))
        {
            CloseHorizontalDoorway(tileMap, baseTilePos, new Vector2I(0, 1), new Vector2I(1, 0), new Vector2I(2, 0));
        }
        else if (connectorName.Contains("-Y"))
        {
            CloseHorizontalDoorway(tileMap, baseTilePos, new Vector2I(0, -1), new Vector2I(1, 4), new Vector2I(3, 4));
        }
        else if (connectorName.Contains("+X"))
        {
            CloseVerticalDoorway(tileMap, baseTilePos, new Vector2I(-1, 0), new Vector2I(5, 3), new Vector2I(5, 1), new Vector2I(5, 2));
        }
        else if (connectorName.Contains("-X"))
        {
            CloseVerticalDoorway(tileMap, baseTilePos, new Vector2I(1, 0), new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(0, 2));
        }
    }
    
    private void CloseHorizontalDoorway(TileMapLayer tileMap, Vector2I baseTilePos, Vector2I offset, Vector2I leftTile, Vector2I rightTile)
    {
        Vector2I tilePos = baseTilePos + offset;
        int halfCount = HorizontalDoorwayTileCount / 2;
        
        for (int i = -halfCount; i <= halfCount; i++)
        {
            Vector2I tileCoord = (i == -halfCount) ? leftTile : (i == halfCount) ? rightTile : leftTile;
            tileMap.SetCell(tilePos + new Vector2I(i, 0), 0, tileCoord);
        }
    }
    
    private void CloseVerticalDoorway(TileMapLayer tileMap, Vector2I baseTilePos, Vector2I offset, Vector2I topTile, Vector2I midTile, Vector2I bottomTile)
    {
        Vector2I tilePos = baseTilePos + offset;
        int halfCount = VerticalDoorwayTileCount / 2;
        
        for (int i = -halfCount; i <= halfCount; i++)
        {
            Vector2I tileCoord = (i <= -1) ? topTile : (i == 0) ? midTile : bottomTile;
            tileMap.SetCell(tilePos + new Vector2I(0, i), 0, tileCoord);
        }
    }


    private void OnNormalRoomEntered(Vector2 cameraPosition)
    {
        camera2D.GlobalPosition = cameraPosition;
    }
}
