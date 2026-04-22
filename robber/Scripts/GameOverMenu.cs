using Godot;

public partial class GameOverMenu : CanvasLayer
{
	private Button retry;
	private Button menu; 

	private Label gameResultInfo;

	public override void _Ready()
	{
		retry = GetNode<Button>("Retry");
		menu = GetNode<Button>("Menu");
		gameResultInfo = GetNode<Label>("GameResult");

		retry.Hide();
		menu.Hide();
		gameResultInfo.Hide();

		retry.Pressed += ReloadScene; 
		menu.Pressed  += LoadMenu; 

		EventBus.Instance.GameOver += GameOver;
		EventBus.Instance.Escape += Escaped; 
	}

    public override void _ExitTree() 
	{
		EventBus.Instance.GameOver -= GameOver; // unlinking signal handlers on exit or reload
		EventBus.Instance.Escape -= Escaped;
	}

	private void ReloadScene()
	{
		GetTree().ReloadCurrentScene();
	}

	private void LoadMenu()
	{
		GetTree().ChangeSceneToFile("res://Scenes/mainmenu.tscn");
	}

	private void GameOver()
	{
		gameResultInfo.Show();
		gameResultInfo.Text = "Game Over";

		var player = GetTree().Root.GetNode<Node2D>("Main/player");
		var owner = GetTree().Root.GetNode<Node2D>("Main/home_owner");
		
		player.SetProcess(false);
		player.SetPhysicsProcess(false);
		
		owner.SetProcess(false);
		owner.SetPhysicsProcess(false);
	
		retry.Show();
		menu.Show();
	}

	private void Escaped()
	{
		gameResultInfo.Show();
		gameResultInfo.Text = "You Escaped!";

		var player = GetTree().Root.GetNode<Node2D>("Main/player");
		var owner = GetTree().Root.GetNode<Node2D>("Main/home_owner");

		player.SetProcess(false);
		player.SetPhysicsProcess(false);

		owner.SetProcess(false);
		owner.SetPhysicsProcess(false);

		retry.Show();
		menu.Show();
	}
}
