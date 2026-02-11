using Godot;
using System;

public partial class Rope : Line2D
{
	[Export] public Node2D StartNode;
	[Export] public Node2D EndNode;
	
	[Export] public int Segments = 10;
	[Export] public float Slack = 50.0f;
	[Export] public float PullStrength = 200.0f;
	[Export] public float MaxRopeLength = 150.0f;
	[Export] public bool EnablePull = true;
	
	public override void _Ready()
	{
		Width = 1.5f;
		DefaultColor = new Color(1f, 0.8f, 0.9f); 
	}

	public override void _Process(double delta)
	{
		if (StartNode == null || EndNode == null)
			return;
			
		UpdateRope();
		
		if (EnablePull)
			PullEndNode(delta);
	}
	
	private void UpdateRope()
	{
		ClearPoints();
		
		Vector2 start = StartNode.GlobalPosition;
		Vector2 end = EndNode.GlobalPosition;
		
		// Simple rope with curve (catenary approximation)
		for (int i = 0; i <= Segments; i++)
		{
			float t = (float)i / Segments;
			Vector2 point = start.Lerp(end, t);
			
			// Add slack/sag in the middle
			float sag = Mathf.Sin(t * Mathf.Pi) * Slack;
			point.Y += sag;
			
			AddPoint(ToLocal(point));
		}
	}
	
	private void PullEndNode(double delta)
	{
		Vector2 start = StartNode.GlobalPosition;
		Vector2 end = EndNode.GlobalPosition;
		float distance = start.DistanceTo(end);
		
		// Only pull if rope is stretched beyond max length
		if (distance > MaxRopeLength)
		{
			Vector2 direction = (start - end).Normalized();
			float pullForce = (distance - MaxRopeLength) * PullStrength * (float)delta;
			
			// Check if EndNode is a CharacterBody2D
			if (EndNode is CharacterBody2D character)
			{
				character.Velocity += direction * pullForce;
			}
			// Check if EndNode is a RigidBody2D
			else if (EndNode is RigidBody2D rigid)
			{
				rigid.ApplyCentralForce(direction * pullForce * 100);
			}
			// Fallback: directly move any Node2D
			else
			{
				EndNode.GlobalPosition += direction * pullForce;
			}
		}
	}
}
