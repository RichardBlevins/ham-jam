using Godot;
using System;
using Godot.Collections;

public partial class Player : CharacterBody2D
{
	public const float Speed = 100.0f;

	public override void _Ready()
	{ 
		base._Ready();
		GD.Print("Player ready");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Vector2.Zero;
		// Get the input direction for top-down movement
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity = direction.Normalized() * Speed;
		}
		Velocity = velocity;
		
		PlayerInteractEvent();
		MoveAndSlide();
		
	}
	


	
	private void PlayerInteractEvent()
	{
//		Rope rope = GetNode<Rope>("Rope");
//		bool spaceJustPressed = Input.IsActionJustPressed("ui_accept");
//		
		// Toggle detach if already attached
//	//	if (spaceJustPressed && rope.IsAttached)
	//	{
	//		rope.Detach();
	//		return;
	//	}
		
		// Try to attach to nearby RigidBody2D
//		if (spaceJustPressed)
//		{
//			Area2D roperadius = GetNode<Area2D>("Roperadius");
//
//			foreach (Node2D body in roperadius.GetOverlappingBodies())
//			{
//	
//				if (body is RigidBody2D rigidBody && body != this)
//				{
					
//					break;
//				}
//			}
//		}
	}
	
}
