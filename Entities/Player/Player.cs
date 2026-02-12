using Godot;
using System;
using Godot.Collections;

public partial class Player : CharacterBody2D
{
	private enum PlayerState
	{
		WALKING,
		EATING,
		RUNNING
	}
	
	PlayerState currentState = PlayerState.WALKING;
	
	[Export] public float Speed;
	public float WalkingSpeed = 75.0f;
	public float RunningSpeed = 125.0f;
	public float EatingSpeed = 45.0f;

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
		if (direction != Vector2.Zero) // checks if the players velocity is not zero and moves the player if true
		{
			velocity = direction.Normalized() * Speed;
		}

		Velocity = velocity;

		StateMachine();
		PlayerOinkEvent();
		MoveAndSlide();

	}


	private void PlayerOinkEvent()
	{
		bool spaceJustPressed = Input.IsActionJustPressed("ui_accept"); // creates a boolean var on the player spaceinput

		if (spaceJustPressed) //checks if that input is true then ...
		{
			Area2D oinkradius = GetNode<Area2D>("OinkRadius");  //get the oink radius node from the node tree
			
			foreach (Node2D body in oinkradius.GetOverlappingBodies()) // checks for any 2d nodes in the oinkradius
			{
				if (body is RigidBody2D rigidBody && body != this && body.IsInGroup("Child")) { //checks if node2d is a rigid body type with the child group and is not the player 
						rigidBody.EmitSignal("OnLured", GlobalPosition, 2000.0); 
						break;
				}
			}
		}
		bool eatPressed = Input.IsActionJustPressed("eat");
		
		Area2D eatradius = GetNode<Area2D>("EatRadius");
		if (eatPressed)
		{
			foreach (Node body in eatradius.GetOverlappingBodies())
			{
				if (body is RigidBody2D rigidBody && body != this && body.IsInGroup("Child") && currentState != PlayerState.EATING)
				{
					body.QueueFree();
					currentState = PlayerState.EATING;
					break;
				}
			}	
		}

	}

	private void StateMachine()
	{
		switch (currentState)
		{
			case PlayerState.EATING:
				Speed = EatingSpeed;
				break;
			
			case PlayerState.WALKING:
				Speed = WalkingSpeed;
				break;
			
			case PlayerState.RUNNING:
				Speed = RunningSpeed;
				break;
		}
	}
}
