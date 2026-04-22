using Godot;

public partial class MainMenu : CanvasLayer
{
	Button start;
	Button exit;

	public override void _Ready()
	{
		start = GetNode<Button>("Start");		
		exit = GetNode<Button>("Exit");

		start.Pressed += StartHandler;
		exit.Pressed  += ExitHandler;
	}

    public override void _ExitTree()
    {
        start.Pressed -= StartHandler;
		exit.Pressed  -= ExitHandler;
    }

	private void StartHandler()
	{
		GetTree().ChangeSceneToFile("res://Scenes/main.tscn");
	}

	private void ExitHandler()
	{
		GetTree().Quit();
	}
}
