using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public partial class InputHandlers : Node2D
{
    [Export] public PackedScene ProjectileScene;
    private List<AudioStreamPlayer2D> _oinks;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private Player player;
    private bool ShootCooldown;
    public override void _Ready()
    {
        base._Ready();

        ShootCooldown = true;
        
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

    public async Task InputHandler() {
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (direction != Vector2.Zero)
        {
            await Shoot(direction);
        }
		OinkSoundEffect();

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
    
    private async Task Shoot(Vector2 GetDirection)
    {
        if (ShootCooldown != false)
        {
            
            
            ShootCooldown = false;
            OinkProjectile projectile = ProjectileScene.Instantiate<OinkProjectile>();
            projectile.GlobalPosition = GlobalPosition;
            projectile.SetDirection(GetDirection);
            GetTree().CurrentScene.AddChild(projectile);
            await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
            ShootCooldown = true; 
        }
    }
}
