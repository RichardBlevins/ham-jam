using Godot;
using System;

public partial class OinkProjectile : Area2D
{
    [Export] public float Speed = 600f;

    private Vector2 _direction = Vector2.Right;

    public void SetDirection(Vector2 direction)
    {
        _direction = direction.Normalized();
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += _direction * Speed * (float)delta;
    }

    private void OnBodyEntered(Node body)
    {
        QueueFree(); // destroy on hit
    }
}