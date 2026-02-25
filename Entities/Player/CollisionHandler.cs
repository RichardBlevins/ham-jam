using Godot;
using System.Collections.Generic;

public partial class CollisionHandler : Node2D
{
    private AnimationPlayer _animationPlayer;
    private Sprite2D _joshua;
    private Player player;
    
    public override void _Ready()
    {
        base._Ready();

        player = GetParent<Player>();
        
    }

    public void PlayerCollisionHandler()
	{
		//camera movment 
        Area2D hitbox = GetNode<Area2D>("Hitbox");
        foreach (Area2D area in hitbox.GetOverlappingAreas()) {
            if (area.IsInGroup("NRoom")) {
                
                Godot.Vector2 position = area.GlobalPosition;
                player.EmitSignal(Player.SignalName.OnNormalRoomEntered, position);
            }
        }

	}

    public override void _Process(double delta)
    {
        base._Process(delta);
        PlayerCollisionHandler();
    }
}
