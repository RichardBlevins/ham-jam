using Godot;
using System;

public partial class Child : RigidBody2D
{
	[Signal]
	public delegate void OnLuredEventHandler(Vector2 playerPosition, float strength);
	public override void _Ready()
	{
		OnLured += HandleLured;
	}

	private void HandleLured(Vector2 playerPosition, float strength)
	{
		// Direction from this body TO the player
		Vector2 directionToPlayer = GlobalPosition.DirectionTo(playerPosition);

		// Force = direction * strength
		Vector2 force = directionToPlayer * strength;

		ApplyCentralForce(force);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
