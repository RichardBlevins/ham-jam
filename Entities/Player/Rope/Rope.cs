using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;

public partial class Rope : Node2D
{
	[Export]
	public bool StaticRopeEnd = false;                          //if the end of the rope is attached to a wall

	[Export]
	public float intervalScaleFactor = 0.03f;                        // How much to scale the spacing between the pin joints depending on rope length
	private PackedScene RopeSegmentPackedScene;                      // Var to hold the rope sgement PAcked scene
	private RopeSegment RopeStart;                                  // rope start seg
	private RopeSegment RopeEnd;                                    // rope end seg
	private PinJoint2D RopeStartPinJoint;                               //pin joint for rope start seg
	private PinJoint2D RopeEndPinJoint;                                 //pin j for rope end seg
	private List<Vector2> RopePointsLine2D;                             // Every point in the line 2d to draw the rope
	private Line2D Line2DNode;                                          // Line 2d node
	public List<RopeSegment> RopeSegments = new List<RopeSegment>();    // List with all rope segments

	public override void _Ready() 
	{
		RopePointsLine2D = new List<Vector2>();                         // create a list for the Line2D points
		RopeSegmentPackedScene = GD.Load<PackedScene>("res://Entities/Player/Rope/RopeSegment.tscn"); // load ropesegment scene
		Line2DNode = GetNode<Line2D>("Line2D");                          
		RopeStart = GetNode<RopeSegment>("RopeStart");                      //gets all of these nodes
		RopeEnd = GetNode<RopeSegment>("RopeEnd");         
		RopeStart.Rope = this;                                          //Set the parent node for these nodes to this node
		RopeEnd.Rope = this;         
		RopeStartPinJoint = GetNode<PinJoint2D>("RopeStart/PinJoint2D");   
		RopeEndPinJoint = GetNode<PinJoint2D>("RopeEnd/PinJoint2D");   
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}
}
