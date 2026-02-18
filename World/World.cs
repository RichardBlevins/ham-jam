using System.Collections.Generic;
using Godot;

public partial class World : Node2D
{
    private Camera2D camera2D;

    public override void _Ready()
    {
        camera2D = GetNode<Camera2D>("Camera2D");

        // Get the Room node that emits the signal
        Player player = GetNode<Player>("Player"); // adjust path as needed

        // Connect the signal when player enters a normal room
        player.OnNormalRoomEntered += OnNormalRoomEntered;
        
        Manager.Instance.MaxRooms = 10;
        

    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Manager.Instance.Rooms <= Manager.Instance.MaxRooms)
        {
            Node2D Rooms = GetNode<Node2D>("Rooms");
            foreach (Area2D room in Rooms.GetChildren())
            {
                GD.Print(room);
            }
        }
    }

    // This method will be called whenever the signal is emitted
    private void OnNormalRoomEntered(Vector2 cameraPosition)
    {
        camera2D.GlobalPosition = cameraPosition;
        
    }
}
