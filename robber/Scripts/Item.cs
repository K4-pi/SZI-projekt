using Godot;
using System;
using System.Collections.Generic;

public partial class Item : Node2D
{
	public Point closestPoint {get; private set;}
	private Area2D pickupArea;
	private Sprite2D itemTexture;
	[Export] public float itemValue {get; private set;}
	[Export] public bool rotate;

	[Export] public Label timeLabel;
	public double lastSeen = 0.0f;

    public override void _Ready()
    {
		closestPoint = null;

		itemTexture = GetNode<Sprite2D>("Item_sprite");
		pickupArea = GetNode<Area2D>("PickUpArea");

        if (rotate)
		{
			RandomNumberGenerator rng = new RandomNumberGenerator();

			// Rotate(rng.RandfRange(0.0f, 360.0f));

			itemTexture.Rotate(rng.RandfRange(0.0f, 360.0f));
		}
    }

    public override void _Process(double delta)
    {
		timeLabel.Text = $"{Mathf.RoundToInt((float)lastSeen)}";

		lastSeen += delta;

        var bodies = pickupArea.GetOverlappingBodies();

		foreach (var b in bodies)
		{
			if (b.IsInGroup("player") && Input.IsActionJustPressed("interact"))
			{
				GD.Print("Pickeup item");
				EventBus.Instance.EmitSignal(EventBus.SignalName.OnItemPickUp, itemValue, "expensive");
				EventBus.Instance.EmitSignal(EventBus.SignalName.RemoveItemPoint, this);
				QueueFree();	
			} 
		}
    }

	public void GenerateClosestPoint(List<Point> points)
	{
		Point point = null;

		foreach (var p in points)
		{
			if (point == null)
			{
				point = p;
				continue;
			} 

			float pointDistance = GlobalPosition.DistanceTo(p.GlobalPosition);

			if (pointDistance < GlobalPosition.DistanceTo(point.GlobalPosition))
			{
				point = p;
			}
		}

		closestPoint = point;
	}
    
}
