using Godot;
using System;

public partial class Player : CharacterBody2D
{
	private float Speed = 50.0f;

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		float speed = Speed;

		if (Input.IsActionPressed("run")) speed *= 1.5f;

		if (direction != Vector2.Zero)
		{
			Velocity = direction * speed;
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, speed);
		}

		LookAt(GetGlobalMousePosition());

		MoveAndSlide();
	}
}
