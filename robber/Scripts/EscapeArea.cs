using Godot;
using System;

public partial class EscapeArea : Area2D
{
	public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		var bodies = GetOverlappingBodies();

		foreach (var b in bodies)
			if (b is Player) EventBus.Instance.EmitSignal(EventBus.SignalName.Escape);
	}
}
