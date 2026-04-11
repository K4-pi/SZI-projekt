using Godot;
using System;
using System.Collections.Generic;

public partial class Point : Node2D
{
	public bool visited {get; set;}
	// public Point[] neighbors {get; set;}
	public List<Point> neighbors {get; set;}

	public void Setup(Vector2 globalPos)
    {
        GlobalPosition = globalPos;
		visited = false;
		neighbors = new List<Point>();
    }
}
