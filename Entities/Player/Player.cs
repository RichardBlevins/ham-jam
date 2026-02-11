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
		Area2D hitbox = GetNode<Area2D>("Hitbox");

		foreach (Area2D area in hitbox.GetOverlappingAreas())
		{
			//creates a list of the groups the area is in
			Godot.Collections.Array<StringName> groups = area.GetGroups();
			
			// sets a varible for all the groups in the array
			foreach (StringName group in groups)
			{
				switch (group.ToString())
				
				{
					case "Enemy": 
						break;
				}
			}
		}
		
		Area2D  roperadius = GetNode<Area2D>("Roperadius");

		foreach (Area2D area in roperadius.GetOverlappingAreas())
		{
			Godot.Collections.Array<StringName> groups = area.GetGroups();

			foreach (StringName group in groups)
			{
				switch (group.ToString())
				{
					case "Child":
						if (Input.IsActionPressed("ui_accept"))
						{
							Rope rope = GetNode<Rope>("Rope");
							rope.EndNode = area;
						}
						break;
				}
			}
		}
	}
	
}
