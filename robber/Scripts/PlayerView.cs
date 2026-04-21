using Godot;
using System;

public partial class PlayerView : Camera2D
{
	[Export] public Godot.AudioStreamPlayer expensiveItemSound;
	[Export] public Godot.AudioStreamPlayer dollarSound;

	[Export] public Node2D player; 

	[Export] public Label pointsLabel;
	[Export] public Label fpsLabel;
	[Export] public HSlider staminaBar;

	public float points = 0.0f;

    public override void _Ready()
    {
		EventBus.Instance.OnItemPickUp += HandleItemPickUp;
    }

	public override void _Process(double delta)
	{

		Position = player.GlobalPosition;

		pointsLabel.Text = $"$: {points}";
		fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";

		staminaBar.Value = ((Player)player).stamina; 
	}

	private void HandleItemPickUp(float value, string audioType)
	{
		points += value; 

		pointsLabel.Text = $"$: {points}";

		if (audioType == "expensive") expensiveItemSound.Play();
		else if (audioType == "dollar") dollarSound.Play();
	}
}
