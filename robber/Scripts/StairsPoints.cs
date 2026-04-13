using Godot;
using System;

public partial class StairsPoints : Point 
{
	[Export] public Area2D teleportArea;
	// [Export] public Node2D destinationPoint;
	[Export] public StairsPoints mainNeighbor; // Opposite stairs parter -> basement or basement -> parter

    public override void _PhysicsProcess(double delta)
    {
        var bodies = teleportArea.GetOverlappingBodies();

		if (bodies.Count > 0)
		{
			foreach (var b in bodies)
			{
				b.GlobalPosition = mainNeighbor.GlobalPosition;
			}
		}
    }
}
