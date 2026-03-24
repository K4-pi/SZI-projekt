using Godot;
using System;

public partial class AudioStreamPlayer : Godot.AudioStreamPlayer
{
	[Export] Node2D valuableItems;

	public override void _Ready()
	{
		foreach (var item in valuableItems.GetChildren())
		{
			item.TreeExited += OnItemPickUp;
		}
	}

	private void OnItemPickUp()
	{
		if (!Playing) Play();
	}
}
