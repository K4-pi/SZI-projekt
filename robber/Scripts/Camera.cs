using Godot;
using System;

public partial class Camera : Node2D
{
	private float x = 1f;
	private float mod = 0.01f;
	private float offset;

	[Export] public RayCast2D rayToPlayer;

	private Area2D cameraVision;
	private Player player;

    public override void _Ready()
	{
		cameraVision = GetNode<Area2D>("camera_vision");

		player = GetTree().Root.GetNode<CharacterBody2D>("Main/player") as Player; 

		GetNode<AnimatedSprite2D>("camera_anim").Play();

		offset = Rotation;
	}

    public override void _PhysicsProcess(double delta)
	{
		if (Position.DistanceTo(ToLocal(player.GlobalPosition)) > 200f) return;

		if (x >= 1f || x <= -1f) mod *= -1f;
		x += mod;

		Rotation = Mathf.Sin(x * 1.5f) + offset;

		rayToPlayer.TargetPosition = rayToPlayer.ToLocal(player.GlobalPosition);

		var bodies = cameraVision.GetOverlappingBodies();

		if (bodies.Contains(player) && rayToPlayer.GetCollider() == player)
		{
			// cameraLight.Color = Color.FromOkHsl(1f, 0f, 0f, 0.5f); 

			GD.Print("PLAYER IN CAMERA VIEW");
			EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerSeenByCamera);
		}
	}
}
