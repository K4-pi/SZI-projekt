using Godot;

public partial class GameOverMenu : CanvasLayer
{
	Button retry;
	Button menu; 

	public override void _Ready()
	{
		retry = GetNode<Button>("Retry");
		menu = GetNode<Button>("Menu");

		retry.Pressed += ReloadScene; 
		menu.Pressed  += LoadMenu; 

		EventBus.Instance.GameOver += ShowButtons; 
	}

    public override void _ExitTree() 
	{
		EventBus.Instance.GameOver -= ShowButtons; // unlinking signal handlers on exit or reload
	}

	private void ReloadScene()
	{
		GetTree().ReloadCurrentScene();
	}

	private void LoadMenu()
	{
		GetTree().ChangeSceneToFile("res://Scenes/mainmenu.tscn");
	}

	private void ShowButtons()
	{
		retry.Show();
		menu.Show();
	}
}
