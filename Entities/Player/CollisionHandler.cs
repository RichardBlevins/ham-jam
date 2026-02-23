using Godot;
using System.Collections.Generic;

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

        foreach (Node node in GetTree().GetNodesInGroup("Oinks"))
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
		}

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
        		//EAT COOLDOWN
        PlayerInteractionsHandler();
    }


    private void OinkSoundEffect()
    {
        if (_oinks.Count == 0)
            return;

        int index = _rng.RandiRange(0, _oinks.Count - 1);

        var oink = _oinks[index];
        oink.PitchScale = _rng.RandfRange(0.1f, 1f);
        oink.Play();
    }
}
