using Godot;
using System;
using System.Linq;

public partial class PathfindingPoints : Node2D
{
	public static Point[] allPoints {get; private set;} 

	public override void _Ready()
	{
        allPoints = GetChildren()
            .OfType<Point>()
            .ToArray();

        GD.Print($"Collected {allPoints.Length} points for pathfinding!");
	}
}
