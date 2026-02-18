using Godot;

public partial class Room0 : Area2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Node2D connectors = GetNode<Node2D>("Connectors");
		foreach (Node child in connectors.GetChildren())
   		{
       		if (child is Area2D connector)
       		{
           		GD.Print($"Found connector: {connector.Name}");
       		}
   		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
