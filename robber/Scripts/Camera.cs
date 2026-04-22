using Godot;

public partial class Camera : Node2D
{
	private float x = 0f;
	private float offset;

	[Export] public RayCast2D rayToPlayer;

	private Area2D cameraVision;
	private Player player;
	private AudioStreamPlayer2D cameraAudio;

    public override void _Ready()
	{
		cameraVision = GetNode<Area2D>("camera_vision");
		cameraAudio = GetNode<AudioStreamPlayer2D>("audio");

		player = GetTree().Root.GetNode<CharacterBody2D>("Main/player") as Player; 

		GetNode<AnimatedSprite2D>("camera_anim").Play();

		offset = Rotation;
	}

    public override void _PhysicsProcess(double delta)
	{
		if (x >= 512f) x = 0f; // Operating on BIG float values breaks Sin function,
		x += 0.01f;            // so we ocasionally reset x variable

		Rotation = Mathf.Sin(x * 1.5f) + offset;

		rayToPlayer.TargetPosition = rayToPlayer.ToLocal(player.GlobalPosition);

		var bodies = cameraVision.GetOverlappingBodies();

		if (bodies.Contains(player) && rayToPlayer.GetCollider() == player)
		{
			EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerSeenByCamera);

			if (!cameraAudio.Playing) cameraAudio.Play();
		}
	}
}
