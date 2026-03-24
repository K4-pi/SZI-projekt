using Godot;
using System;

public partial class Dollar : Area2D
{
	[Export] CollisionShape2D collision;

	RandomNumberGenerator randomGenerator;

	public override void _Ready()
	{
		randomGenerator = new RandomNumberGenerator();
		float rand = randomGenerator.RandfRange(0.0f, 360.0f);

		Rotation = rand;
	}

	public override void _Process(double delta)
	{
		var obj = GetOverlappingBodies();

		foreach (var o in obj)
		{
			if (o is Player player)
			{
				GD.Print($"Player collected money");
				QueueFree();
			}
		}
	}
}
