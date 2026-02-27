using Godot;
using System;
using System.Collections.Generic;

public partial class InputHandlers : Node2D
{
    [Export]
    public PackedScene Oink_Projectile;
    private List<AudioStreamPlayer2D> _oinks;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private Player player;
    public override void _Ready()
    {
        base._Ready();
        
        player = GetParent<Player>();

        _rng.Randomize();

		_oinks = new List<AudioStreamPlayer2D>();

        foreach (Node node in GetTree().GetNodesInGroup("Oinks"))
        {
            if (node is AudioStreamPlayer2D audio)
            {
                _oinks.Add(audio);
            }
        }
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        InputHandler();
    }

    public void InputHandler() {

        bool spaceJustPressed = Input.IsActionJustPressed("ui_accept"); // creates a boolean var on the player spaceinput
		if (spaceJustPressed) //checks if that input is true then ...
		{
            Area2D Projectile = Oink_Projectile.Instantiate<Area2D>();
            Projectile.GlobalPosition = GlobalPosition;
            GetTree().CurrentScene.AddChild(Projectile);
			OinkSoundEffect();

		}

    }

    private void OinkSoundEffect()
    {
        if (_oinks.Count == 0)
            return;

        int index = _rng.RandiRange(0, _oinks.Count - 1);

        var oink = _oinks[index];
        oink.PitchScale = _rng.RandfRange(1f, 1.5f);
        oink.Play();
    }
}
