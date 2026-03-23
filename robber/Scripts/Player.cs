using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 50.0f;

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		if (direction != Vector2.Zero)
		{
			Velocity = direction * Speed;
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, Speed);
		}

		LookAt(GetGlobalMousePosition());

		MoveAndSlide();
	}
}
