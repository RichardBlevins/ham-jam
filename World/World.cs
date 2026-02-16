using Godot;
using System;

public partial class World : Node2D
{
    private Camera2D camera2D;

    public override void _Ready()
    {
        camera2D = GetNode<Camera2D>("Camera2D");

        // Get the Room node that emits the signal
        Player player = GetNode<Player>("Player"); // adjust path as needed

        // Connect the signal
        player.OnNormalRoomEntered += OnNormalRoomEntered;
    }

    // This method will be called whenever the signal is emitted
    private void OnNormalRoomEntered(Vector2 cameraPosition)
    {
        camera2D.GlobalPosition = cameraPosition;
        
    }
}
