using Godot;
using System.Threading.Tasks;

public partial class Manager : Node
{
	public static Manager Instance { get; private set; }
	
	public int MaxRooms;
	public int Rooms;

	public override void _Ready()
	{
		Instance = this;
	}
	
	public async Task Wait(float waitTime)
	{
		await ToSignal(GetTree().CreateTimer(waitTime), "timeout");
	}

}
