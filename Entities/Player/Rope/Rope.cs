using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;

public partial class Rope : Node2D
{
	[Export]public bool StaticRopeEnd = false;                          //if the end of the rope is attached to a wall
	[Export] public float intervalScaleFactor = 0.03f;                        // How much to scale the spacing between the pin joints depending on rope length
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
		RopeStartPinJoint = GetNode<PinJoint2D>("RopeStart/PinJoint2D");        //get the rope start and end pinjoint node
		RopeEndPinJoint = GetNode<PinJoint2D>("RopeEnd/PinJoint2D");   

		SpawnRope();
	}

	public void SpawnRope() 
	{
		Vector2 ropeStartpos = RopeStartPinJoint.GlobalPosition;
		Vector2 ropeEndPos = RopeEndPinJoint.GlobalPosition;
		var dist = ropeStartpos.DistanceTo(ropeEndPos);
		
		float baseInterval = 10.0f;
		float interval = baseInterval + (dist * intervalScaleFactor);
		Vector2 direction = (ropeEndPos - ropeStartpos).Normalized();
		var numSegments = Mathf.CeilToInt(dist/interval);
		var rotationAngle = direction.Angle() - Mathf.Pi / 2;
		RopeStart.IndexInArray = 0;
		Vector2 currentPos = ropeStartpos;
		RopeSegment lastestSegment = RopeStart;

		RopeSegments.Clear();
		RopeSegments.Add(lastestSegment);

		for (int i = 0; i < numSegments; i++) 
		{
			currentPos += direction * interval;
			lastestSegment = AddRopeSegment(lastestSegment, i + 1, rotationAngle, currentPos);
			RopeSegments.Add(lastestSegment);

			var jointPos = lastestSegment.GetNode<PinJoint2D>("PinJoint2D").GlobalPosition;

			if (jointPos.DistanceTo(ropeEndPos) < interval) {
				break;
			}
		}
		ConnectRopeParts(RopeEnd, lastestSegment);
		RopeEnd.Rotation = rotationAngle;
		RopeSegments.Add(RopeEnd);

		if (StaticRopeEnd)
		{
			RopeEnd.Freeze = true;
		}
		RopeEnd.IndexInArray = numSegments;
	}


	private void ConnectRopeParts(RopeSegment a, RopeSegment b)
	{
		PinJoint2D pinJoint = a.GetNode("PinJoint2D") as PinJoint2D;        //Get the pintjoint from the first passed in the rope segment 
		pinJoint.NodeA = a.GetPath();                       // Connect the pinjoints first body to itself 
		pinJoint.NodeB = b.GetPath();                       // connect the second body to the other rope part B
	}

	public RopeSegment AddRopeSegment(Node previousSegment, int id, float rotationAngle, Vector2 position) 
	{
		// get the pint joint of the preveious segment
		PinJoint2D pinJoint = previousSegment.GetNode("PinJoint2D") as PinJoint2D; 

		var segment = RopeSegmentPackedScene.Instantiate() as RopeSegment; //Creates a new rope segment
		segment.GlobalPosition = position;                                 // set the start at the joint position
		segment.Rotation = rotationAngle;                                 // set rotation of the angle 
		segment.Rope = this;                                              // set the rope the segment is part of 
		segment.IndexInArray = id;                                        //set which index in the array the segment has

		AddChild(segment);                                               // add the segment ad a childnode to the rope node 
		pinJoint.NodeA = previousSegment.GetPath();                     //connect the pin joint node A to the parent
		pinJoint.NodeB = segment.GetPath();                             //connect the pin joint node B to the new segment
		pinJoint.Bias = 0.99f;                                          //set the pin joint bias to 0.99f
		pinJoint.Softness = 0.003f;                                     //set pin joint softeness
		return segment; // returns the new rope segment

	}
	private void UpdateLine2DRope() {
		RopePointsLine2D.Clear();
		RopePointsLine2D.Add(RopeStartPinJoint.GlobalPosition);

		foreach (var segment in RopeSegments)
		{
			RopePointsLine2D.Add(segment.GlobalPosition);
		}
		RopePointsLine2D.Add(RopeEndPinJoint.GlobalPosition);
		Line2DNode.Points = RopePointsLine2D.ToArray();
	}
	public override void _Process(double delta)
	{
		base._Process(delta);
		UpdateLine2DRope();
	}
}
