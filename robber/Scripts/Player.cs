using Godot;

public partial class Player : CharacterBody2D
{
	public float stamina = 100f;

	[Export] AnimatedSprite2D playerSprite;
	private float Speed = 50.0f;

    public override void _Ready()
    {
		playerSprite.AnimationLooped += StopAnimationHandler; 
		playerSprite.AnimationFinished += StopAnimationHandler;
    }

    public override void _ExitTree()
    {
        playerSprite.AnimationLooped -= StopAnimationHandler;
		playerSprite.AnimationFinished -= StopAnimationHandler;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("interact") && !playerSprite.IsPlaying()) playerSprite.Play("grab");
    }

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		float speed = Speed;

		if (Input.IsActionPressed("run") && stamina > 0.5f) 
		{
			stamina -= 0.1f;
			speed *= 1.5f;
		}
		else stamina += 0.25f;

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

	private void StopAnimationHandler()
	{
		playerSprite.Stop();
	}
}
