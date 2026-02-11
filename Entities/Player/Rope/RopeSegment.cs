using Godot;
using System;

public partial class RopeSegment : RigidBody2D
{
	public int IndexInArray;			// index in the rope segment array the segmant has
	public Godot.GodotObject Rope = null;	// The rope which the rope segment is apart of 

}
