using Godot;
using System;

public partial class Point : Node2D
{
	public bool visited {get; set;}
	[Export] public Point[] neighbors {get; set;}

	public override void _Ready()
	{
		visited = false;
	}

	public void Setup(Vector2 globalPos)
    {
        GlobalPosition = globalPos;
    }
}
