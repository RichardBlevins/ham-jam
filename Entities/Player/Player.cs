using Godot;
using System;
using Godot.Collections;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	private enum PlayerState
	{
		WALKING,
		EATING,
		RUNNING,
		IDLE
	}

	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private List<Action> _oinks;


	PlayerState currentState = PlayerState.IDLE;


	[Export] public float Speed;
	public float WalkingSpeed = 75.0f;
	public float RunningSpeed = 125.0f;
	public float EatingSpeed = 45.0f;
	private float eatTimer = 0f;


	private AnimationPlayer _animationPLayer;
	private Sprite2D _joshua;
	private int facingDirection = 1; // 1 for right, -1 for left


	public override void _Ready()
	{
		base._Ready();
		_animationPLayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_joshua = GetNode<Sprite2D>("Joshua"); // Adjust path if needed

		_rng.Randomize();

		_oinks = new List<Action> {
			
			// lambda "() =>" this essentially creates a small function 
			// thats not defined instead of making a whole new named function that will only have a line of code.???

			() =>
			{
				var oink = GetNode<AudioStreamPlayer2D>("Oink");
				oink.PitchScale = _rng.RandfRange(0.8f, 1.2f);
				oink.Play();
			},
			() => {
				var oink = GetNode<AudioStreamPlayer2D>("Oink");
				oink.PitchScale = _rng.RandfRange(0.8f, 1.2f);
				oink.Play();
			},
			() => {
				var oink = GetNode<AudioStreamPlayer2D>("Oink");
				oink.PitchScale = _rng.RandfRange(0.8f, 1.2f);
				oink.Play();
			}};

	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Vector2.Zero;
		// Get the input direction for top-down movement
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero) // checks if the players velocity is not zero and moves the player if true
		{
			if (direction.X < 0 && facingDirection != 1)
			{
				facingDirection = 1;
				_joshua.FlipH = false; // Facing right
			}
			else if (direction.X > 0 && facingDirection != -1)
			{
				facingDirection = -1;
				_joshua.FlipH = true; // Facing left (flipped)
			}
			currentState = PlayerState.WALKING;
			velocity = direction.Normalized() * Speed;
		} else {currentState = PlayerState.IDLE;}
		

		Velocity = velocity;
		

		//=============================== EAT COOLDOWN
		if (eatTimer > 0)
		{
			eatTimer -= (float)delta;

			if (eatTimer <= 0)
			{
				currentState = PlayerState.WALKING;
			}
			else
			{
				currentState = PlayerState.EATING;
			}
		}
		//=========================================


		StateMachine();
		PlayerOinkEvent();
		MoveAndSlide();

	}


	private void PlayerOinkEvent()
	{
		bool spaceJustPressed = Input.IsActionJustPressed("ui_accept"); // creates a boolean var on the player spaceinput

		if (spaceJustPressed) //checks if that input is true then ...
		{
			OinkSoundEffect();
			
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
					Eating(3.0f);
					break;
				}
			}	
		}

	}
	public void Eating(float duration)
	{
		eatTimer = duration;
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
				if (facingDirection == -1) {
					_animationPLayer.PlayBackwards("Walk");
				} else {_animationPLayer.Play("Walk");}
					
				break;
			
			case PlayerState.RUNNING:
				Speed = RunningSpeed;
				break;

			case PlayerState.IDLE:
				Speed = WalkingSpeed;
				_animationPLayer.Play("Idle");
				break;
		}
	}
	private void OinkSoundEffect() {

	int index = _rng.RandiRange(0, _oinks.Count - 1); 
	_oinks[index].Invoke();
	}
}
