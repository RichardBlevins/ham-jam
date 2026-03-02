using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Signal]
	public delegate void OnNormalRoomEnteredEventHandler(Vector2 cameraPosition);

	public enum PlayerState
	{
		WALKING,
		IDLE,
		DEAD
	}

	public PlayerState currentState = PlayerState.IDLE;

	public float Speed;
	[Export] public float WalkingSpeed = 75.0f;
	[Export] public float MaxHealth = 100.0f;
	[Export] public float Health = 100.0f;
	[Export] public float Defence = 0.0f;

	private AnimationPlayer _animationPLayer;
	private Sprite2D _joshua;
	private int facingDirection = 0;


	public override void _Ready()
	{
		base._Ready();
		_animationPLayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_joshua = GetNode<Sprite2D>("Joshua"); // Adjust path if needed
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Vector2.Zero;
		Vector2 direction = Input.GetVector("left", "right", "up", "down");

		if (direction != Vector2.Zero)
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
		
		if (Health < 0.1)
		{
			currentState = PlayerState.DEAD;
		}

		Velocity = velocity;

		StateMachine();
		MoveAndSlide();

	}

	private void StateMachine()
	{
		switch (currentState)
		{		
			case PlayerState.WALKING:
				Speed = WalkingSpeed;
				if (facingDirection == 1) {
					_animationPLayer.Play("Walk");
				}else {_animationPLayer.Play("Walk");}
				
				break;

			case PlayerState.IDLE:
				Speed = WalkingSpeed;
				_animationPLayer.Play("Idle");
				break;

			case PlayerState.DEAD:

				break;
		}
	}
}
