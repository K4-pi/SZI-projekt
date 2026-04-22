using Godot;

public partial class EventBus : Node
{
	public static EventBus Instance {get; private set;}

	[Signal] public delegate void OnItemPickUpEventHandler(float value, string audioType);
    [Signal] public delegate void RemoveItemPointEventHandler(Item item);
    [Signal] public delegate void PlayerSeenByCameraEventHandler();
    [Signal] public delegate void GameOverEventHandler();
    [Signal] public delegate void EscapeEventHandler();
    
	public override void _Ready()
    {
        Instance = this;
    }
}
