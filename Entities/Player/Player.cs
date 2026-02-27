using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Signal]
	public delegate void OnNormalRoomEnteredEventHandler(Vector2 cameraPosition);

	public enum PlayerState
	{
		WALKING,
		IDLE
	}

	public PlayerState currentState = PlayerState.IDLE;

	[Export] public float Speed;
	public float WalkingSpeed = 75.0f;
	private AnimationPlayer _animationPLayer;
	private Sprite2D _joshua;
	private int facingDirection = 0; // 1 for right, -1 for left


	public override void _Ready()
	{
		base._Ready();

		// ================================================== MIGHT WANNA MOVE TO A SPERATE FILE
		_animationPLayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_joshua = GetNode<Sprite2D>("Joshua"); // Adjust path if needed
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Vector2.Zero;
		// Get the input direction for top-down movement
		Vector2 direction = Input.GetVector("left", "right", "up", "down");

		if (direction != Vector2.Zero) // checks if the players velocity is not zero and moves the player if true
		{

			// ================================================== MIGHT WANNA MOVE TO A SPERATE FILE
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
			// ================================================== MIGHT WANNA MOVE TO A SPERATE FILE
			currentState = PlayerState.WALKING;
			
			velocity = direction.Normalized() * Speed;
		} else {currentState = PlayerState.IDLE;}
		

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
		}
	}
}
