using Godot;
using System;
using System.Collections.Generic;

public partial class Point : Node2D
{
	public bool visited {get; set;}
	public List<Point> neighbors = new List<Point>();

	public void Setup(Vector2 globalPos)
    {
        GlobalPosition = globalPos;
		visited = false;
    }
}
