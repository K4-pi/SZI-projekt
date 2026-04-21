using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Cameras : Node
{
	[Export] public PackedScene camera;

	private List<Node2D> spawnPoints;

	public override void _Ready()
	{
		spawnPoints = GetChildren().Cast<Node2D>().ToList();

		RandomNumberGenerator rng = new RandomNumberGenerator();

		for (int i = 0; i < 3; i++) // Create n of cameras
		{
			int index = rng.RandiRange(0, spawnPoints.Count() - 1);

			var cam = camera.Instantiate<Node2D>();

			Node2D p = spawnPoints[index];

			p.AddChild(cam);
			cam.GlobalPosition = p.GlobalPosition;

			spawnPoints.RemoveAt(index);
		}

		foreach (var p in spawnPoints) p.QueueFree(); // Free/Destroy unused node2d points
	}
}
