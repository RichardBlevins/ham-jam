using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public partial class CollisionHandler : Node2D
{
    private AnimationPlayer _animationPlayer;
    private Sprite2D _joshua;
    private List<AudioStreamPlayer2D> _oinks;
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private Player player;
    private float eatTimer = 0f;
    public override void _Ready()
    {
        base._Ready();

        player = GetParent<Player>();


		_rng.Randomize();

		_oinks = new List<AudioStreamPlayer2D>();

        foreach (Node node in GetTree().GetNodesInGroup("oinks"))
        {
            if (node is AudioStreamPlayer2D audio)
            {
                _oinks.Add(audio);
            }
        }
    }

    public void PlayerInteractionsHandler()
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
				if (body is RigidBody2D rigidBody && body != this && body.IsInGroup("Child") && player.currentState != Player.PlayerState.EATING)
				{
					body.QueueFree();
					Eating(3.0f);
					break;
				}
			}	
		}

	}

    public override void _Process(double delta)
    {
        base._Process(delta);
        		//EAT COOLDOWN
		if (eatTimer > 0)
		{
			eatTimer -= (float)delta;

			if (eatTimer <= 0)
			{
				player.currentState = Player.PlayerState.WALKING;
			}
			else
			{
				player.currentState = Player.PlayerState.EATING;
			}
		}
        PlayerInteractionsHandler();
    }
    	public void Eating(float duration)
	{
		eatTimer = duration;
	}
    private void OinkSoundEffect()
    {
        if (_oinks.Count == 0)
            return;

        int index = _rng.RandiRange(0, _oinks.Count - 1);

        var oink = _oinks[index];
        oink.PitchScale = _rng.RandfRange(0.8f, 1.2f);
        oink.Play();
    }
}
