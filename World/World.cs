using System.Collections.Generic;
using Godot;

public partial class World : Node2D
{
    private Camera2D camera2D;
    private PackedScene roomScene;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private List<Area2D> allRooms = new List<Area2D>(); // Track all rooms
    private List<Area2D> activeRooms = new List<Area2D>(); // Track last 5 rooms for spawning
    private const int MaxActiveRooms = 5;

    public override void _Ready()
    {
        camera2D = GetNode<Camera2D>("Camera2D");

        // Get the Room node that emits the signal
        Player player = GetNode<Player>("Player"); // adjust path as needed

        // Connect the signal when player enters a normal room
        player.OnNormalRoomEntered += OnNormalRoomEntered;
        
        Manager.Instance.MaxRooms = 50;
        
        // Load the room scene
        roomScene = GD.Load<PackedScene>("res://World/Rooms/room0.tscn");
        
        // Example: Instantiate a room
        SpawnRoom(new Vector2(0, 0));

    }
    
    private void SpawnRoom(Vector2 position, Area2D fromConnector = null)
    {
        // Instantiate the room
        Area2D newRoom = roomScene.Instantiate<Area2D>();
        
        // Add to the Rooms node first (needed for GlobalPosition to work)
        Node2D roomsNode = GetNode<Node2D>("Rooms");
        roomsNode.AddChild(newRoom);
        
        // Track this room in all rooms
        allRooms.Add(newRoom);
        
        // Add to active rooms (last 5 rooms)
        activeRooms.Add(newRoom);
        
        // Keep only the last 5 rooms in active pool
        if (activeRooms.Count > MaxActiveRooms)
        {
            Area2D removedRoom = activeRooms[0];
            activeRooms.RemoveAt(0);
            GD.Print($"Removed room from active pool. Active rooms: {activeRooms.Count}");
        }
        
        // If spawning from a connector, align by opposite connector
        if (fromConnector != null)
        {
            string connectorName = fromConnector.Name;
            string oppositeConnectorName = GetOppositeConnectorName(connectorName);
            
            // Get the opposite connector in the new room
            Node2D newConnectors = newRoom.GetNode<Node2D>("Connectors");
            Area2D oppositeConnector = newConnectors.GetNode<Area2D>(oppositeConnectorName);
            
            if (oppositeConnector == null)
            {
                GD.PrintErr($"Could not find connector: {oppositeConnectorName}");
                return;
            }
            
            // Get the actual collision shape centers to properly align
            CollisionShape2D fromShape = fromConnector.GetNode<CollisionShape2D>("CollisionShape2D");
            CollisionShape2D toShape = oppositeConnector.GetNode<CollisionShape2D>("CollisionShape2D");
            
            // Calculate the actual global position of the collision shapes
            Vector2 fromShapeGlobal = fromConnector.GlobalPosition + fromShape.Position;
            
            // Position new room so the opposite connector's collision aligns with fromConnector's collision
            Vector2 toShapeLocal = oppositeConnector.Position + toShape.Position;
            newRoom.GlobalPosition = fromShapeGlobal - toShapeLocal;
            
            GD.Print($"Spawning from {connectorName} to {oppositeConnectorName} at {newRoom.GlobalPosition}");
        }
        else
        {
            // Initial room, just use the position
            newRoom.GlobalPosition = position;
        }
        
        Manager.Instance.Rooms += 1;
        
        // Use a short timer to ensure physics has updated
        var timer = GetTree().CreateTimer(0.05);
        timer.Timeout += CheckAllRoomsForConnectors;
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
            // Close all open connectors when max rooms is reached
            CloseAllOpenConnectors();
            return;
        }
            
        // Collect all free connectors from ONLY the active rooms (last 5)
        List<(Area2D connector, int roomIndex)> weightedConnectors = new List<(Area2D, int)>();
        
        for (int i = 0; i < activeRooms.Count; i++)
        {
            Area2D room = activeRooms[i];
            Node2D connectors = room.GetNode<Node2D>("Connectors");
            
            foreach (Node child in connectors.GetChildren())
            {
                if (child is Area2D connector && !connector.HasOverlappingAreas())
                {
                    weightedConnectors.Add((connector, i));
                }
            }
        }
        
        if (weightedConnectors.Count == 0)
        {
            GD.Print("No free connectors available in active rooms");
            return;
        }
        
        // Weight selection: newer rooms (higher index in activeRooms) have higher chance
        int totalWeight = 0;
        foreach (var (_, roomIndex) in weightedConnectors)
        {
            totalWeight += roomIndex + 1;
        }
        
        // Pick random weighted connector
        _rng.Randomize();
        int randomWeight = _rng.RandiRange(0, totalWeight - 1);
        
        int currentWeight = 0;
        Area2D chosenConnector = null;
        
        foreach (var (connector, roomIndex) in weightedConnectors)
        {
            currentWeight += roomIndex + 1;
            if (randomWeight < currentWeight)
            {
                chosenConnector = connector;
                break;
            }
        }
        
        if (chosenConnector != null)
        {
            GD.Print($"Spawning new room at connector: {chosenConnector.Name} (Active rooms: {activeRooms.Count})");
            SpawnRoom(Vector2.Zero, chosenConnector);
        }
    }
    
    private void CloseAllOpenConnectors()
    {
        GD.Print("Max rooms reached - closing all open connectors");
        
        // Check all rooms for open connectors
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
        
        // Convert connector position to tilemap coordinates
        Vector2I baseTilePos = tileMap.LocalToMap(localPos);
        
        GD.Print($"Closing {connectorName} at tile position {baseTilePos}");
        
        // Place wall tiles based on connector direction
        // Doorways are 3 blocks wide
        // Top: (0,0)-(5,0), Bottom: (1,4)-(4,4), Right: (5,0)-(5,3), Left: (0,0)-(0,3)
        if (connectorName.Contains("+Y"))
        {
            // Top exit - place 3 top wall tiles inward
            Vector2I tilePos = baseTilePos + new Vector2I(0, 1);
            tileMap.SetCell(tilePos + new Vector2I(-1, 0), 0, new Vector2I(1, 0)); 
            tileMap.SetCell(tilePos, 0, new Vector2I(1, 0));
            tileMap.SetCell(tilePos + new Vector2I(1, 0), 0, new Vector2I(2, 0));
        }
        else if (connectorName.Contains("-Y"))
        {
            // Bottom exit - place 3 bottom wall tiles inward
            Vector2I tilePos = baseTilePos + new Vector2I(0, -1);
            tileMap.SetCell(tilePos + new Vector2I(-1, 0), 0, new Vector2I(1, 4));
            tileMap.SetCell(tilePos, 0, new Vector2I(2, 4));
            tileMap.SetCell(tilePos + new Vector2I(1, 0), 0, new Vector2I(3, 4));
        }
        else if (connectorName.Contains("+X"))
        {
            // Right exit - place 3 right wall tiles inward
            Vector2I tilePos = baseTilePos + new Vector2I(-1, 0);
            tileMap.SetCell(tilePos + new Vector2I(0, -1), 0, new Vector2I(5, 0));
            tileMap.SetCell(tilePos, 0, new Vector2I(5, 1));
            tileMap.SetCell(tilePos + new Vector2I(0, 1), 0, new Vector2I(5, 2));
        }
        else if (connectorName.Contains("-X"))
        {
            // Left exit - place 3 left wall tiles inward
            Vector2I tilePos = baseTilePos + new Vector2I(1, 0);
            tileMap.SetCell(tilePos + new Vector2I(-1, -1), 0, new Vector2I(0, 0));
            tileMap.SetCell(tilePos, 0, new Vector2I(0, 1));
            tileMap.SetCell(tilePos + new Vector2I(-1, 1), 0, new Vector2I(0, 2));
        }
    }
    
    private void CheckConnectors(Area2D room)
    {
        // Access the connectors
        Node2D connectors = room.GetNode<Node2D>("Connectors");
        List<Area2D> freeConnectors = new List<Area2D>();
        
        foreach (Node child in connectors.GetChildren())
        {
            if (child is Area2D connector)
            {
                // Check if connector is colliding
                bool isColliding = connector.HasOverlappingAreas();
                
                if (!isColliding)
                {
                    // Add to free connectors list
                    freeConnectors.Add(connector);
                }
            }
        }
        
        // If there are free connectors and we haven't reached max rooms, spawn a new room
        if (freeConnectors.Count > 0 && Manager.Instance.Rooms < Manager.Instance.MaxRooms)
        {
            _rng.Randomize();
            
            // Pick a random free connector
            int randomIndex = _rng.RandiRange(0, freeConnectors.Count - 1);
            Area2D chosenConnector = freeConnectors[randomIndex];
            
            GD.Print($"Spawning new room at connector: {chosenConnector.Name}");
            SpawnRoom(Vector2.Zero, chosenConnector);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        // Count the rooms
        Node2D Rooms = GetNode<Node2D>("Rooms");
        Manager.Instance.Rooms = Rooms.GetChildCount();
        
        if (Manager.Instance.Rooms <= Manager.Instance.MaxRooms)
        {
            //SpawnRoom(new Vector2(0, 0));
        }
    }

    // This method will be called whenever the signal is emitted
    private void OnNormalRoomEntered(Vector2 cameraPosition)
    {
        camera2D.GlobalPosition = cameraPosition;
        
    }
}
