using Godot;
using System;

public partial class EventBus : Node
{
	public static EventBus Instance {get; private set;}

	[Signal] public delegate void OnItemPickUpEventHandler(float value, string audioType);
    [Signal] public delegate void RemoveItemPointEventHandler(Item item);
    [Signal] public delegate void PlayerSeenByCameraEventHandler();
    
	public override void _Ready()
    {
        Instance = this;
    }
}
