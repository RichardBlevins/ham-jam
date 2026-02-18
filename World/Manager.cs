using Godot;
using System;
using System.Threading.Tasks;

public partial class Manager : Node
{
	public static Manager Instance { get; private set; }
	
	

	public override void _Ready()
	{
		Instance = this;
		var Manager = GetNode<Manager>("/root/Manager");
		int maxRooms;
	}
	
	public async Task Wait(float waitTime)
	{
		await ToSignal(GetTree().CreateTimer(waitTime), "timeout");
	}

}
