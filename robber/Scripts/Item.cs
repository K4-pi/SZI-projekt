using Godot;
using System.Collections.Generic;

public partial class Item : Node2D
{
	public Point closestPoint {get; private set;}
	private Sprite2D itemTexture;
	private Label timeLabel;
	public Label calculatedValueLabel;

	private Node2D player;
	
	[Export] public float itemValue {get; private set;}
	[Export] public bool rotate;

	public float lastSeen = 0.0f;

	private bool showTime;

    public override void _Ready()
    {
		closestPoint = null;
		showTime = false;

		itemTexture = GetNode<Sprite2D>("Item_sprite");
		timeLabel = GetNode<Label>("time");
		calculatedValueLabel = GetNode<Label>("value");

		timeLabel.Hide();
		calculatedValueLabel.Hide();

		player = GetTree().Root.GetNode<Node2D>("Main/player");

    if (rotate)
		{
			RandomNumberGenerator rng = new RandomNumberGenerator();

			itemTexture.Rotate(rng.RandfRange(0.0f, 360.0f));
		}
    }

    public override void _Process(double delta)
    {

		if (Input.IsActionJustPressed("toggle_timers"))
		{
			if (!showTime) showTime = true;
			else showTime = false;
		}

		if (showTime)
		{
			calculatedValueLabel.Show();

			timeLabel.Show();
			timeLabel.Text = $"{Mathf.RoundToInt(lastSeen)}";
		}
		else
		{
			calculatedValueLabel.Hide();
			timeLabel.Hide();
		}

		lastSeen += (float)delta;

		if (GlobalPosition.DistanceTo(player.GlobalPosition) < 25f && Input.IsActionJustPressed("interact"))
		{
			GD.Print("Picked up item");
			EventBus.Instance.EmitSignal(EventBus.SignalName.OnItemPickUp, itemValue, "expensive");
			EventBus.Instance.EmitSignal(EventBus.SignalName.RemoveItemPoint, this);
			QueueFree();	
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
