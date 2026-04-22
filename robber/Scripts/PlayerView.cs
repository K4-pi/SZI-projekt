using Godot;

public partial class PlayerView : Camera2D
{
	[Export] public Godot.AudioStreamPlayer expensiveItemSound;
	[Export] public Godot.AudioStreamPlayer dollarSound;

	[Export] public Node2D player; 

	[Export] public Label pointsLabel;
	[Export] public Label fpsLabel;
	[Export] public Label itemsRatio;

	private float points = 0.0f;
	private int pickedUpItems = 0;

    public override void _Ready()
    {
		EventBus.Instance.OnItemPickUp += HandleItemPickUp;
    }

    public override void _ExitTree()
    {
        EventBus.Instance.OnItemPickUp -= HandleItemPickUp; 
    }

	public override void _Process(double delta)
	{
		Position = player.GlobalPosition;

		pointsLabel.Text = $"$:{points}";
		itemsRatio.Text = $"{pickedUpItems}/6";
		fpsLabel.Text = $"FPS:{Engine.GetFramesPerSecond()}";

	}

	private void HandleItemPickUp(float value, string audioType)
	{
		points += value; 
		pointsLabel.Text = $"$: {points}";

		if (audioType == "expensive")
		{
			expensiveItemSound.Play();
			pickedUpItems++;
		} 
		else if (audioType == "dollar") dollarSound.Play();
	}
}
