using Godot;
using System.Collections.Generic;
using System;

public partial class ValuablesSpawner : Node2D
{
	[Export] PackedScene valuableItem;
	[Export] TileMapLayer floor;
	[Export] int itemsCount;

	public override void _Ready()
	{
		AStartPoints aStar = new AStartPoints();
		aStar.floorLayer = floor; // Nie mam pojęcia dlaczego tak ma być ale inaczej nie działa
		aStar.CreatePoints();
		var points = aStar.starPoints;

		RandomNumberGenerator randomGenerator = new RandomNumberGenerator();

		for (int i = 0; i < itemsCount; i++)
		{
			int pointIndex = randomGenerator.RandiRange(0, points.Count);

			var item = valuableItem.Instantiate<Node2D>();

			AddChild(item);
			item.Position = ToLocal(points[pointIndex].GlobalPosition);
		}
	}
}
