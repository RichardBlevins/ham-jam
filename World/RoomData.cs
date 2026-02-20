using Godot;

[GlobalClass]
public partial class RoomEntry : Resource
{
    [Export] public PackedScene RoomScene;
    [Export] public int Weight = 1;
    [Export] public int MaxCount = -1; // -1 = unlimited
}
