using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class HomeOwner : CharacterBody2D
{
	private bool debugMode = false;

	private AStartPoints AStar = new AStartPoints();

	[Export] PointLight2D debugLight;
	[Export] Area2D viewArea;
	[Export] TileMapLayer floor;
	[Export] Node2D _player;
	[Export] RayCast2D rayToPlayer;

	private RandomNumberGenerator randomGenerator;
    
	private List<Point> path;
	private Point currentPoint;
	private Point targetPoint;
	private Point nextPoint = null;

	private int index = 0;
	private bool isMoving = false;
	private bool isWaiting = false;
	private bool isChasing = false;
	private bool lostPlayer = false;

	[Export] public float Speed = 30.0f;
    [Export] public float ArrivalTolerance = 0.75f; // How close to count as "arrived" to point

    public void MoveTo(Vector2 targetGlobalPos, double delta)
    {
		float speed = Speed;

		if (isChasing) speed *= 2.0f;

        Vector2 direction = GlobalPosition.DirectionTo(targetGlobalPos);

        if (GlobalPosition.DistanceTo(targetGlobalPos) > ArrivalTolerance)
        {
            Velocity = direction * speed;
        }
        else
        {
            Velocity = Vector2.Zero;
        }
		
		float rotationSpeed = 3.0f;
		Vector2 rotationTarget = targetGlobalPos;

		if (isChasing) rotationTarget = _player.GlobalPosition;

		// Smooth rotation
    	float targetAngle = GlobalPosition.AngleToPoint(rotationTarget);
    	Rotation = (float)Mathf.LerpAngle(Rotation, targetAngle, rotationSpeed * delta);

        MoveAndSlide();
    }

	private async void StartWait(float seconds)
	{
		isWaiting = true;
		GD.Print($"Wait start for {seconds} seconds");
		
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
		
		GD.Print("Wait end");
		isWaiting = false;
	}

	private void PatrolState(double delta)
	{
		if (!isMoving && !isWaiting) 
		{
			if (AStar.starPoints.Count <= 0) return;

			int r = randomGenerator.RandiRange(0, AStar.starPoints.Count - 1);
			targetPoint = AStar.starPoints[r];

			if (currentPoint == null && targetPoint == null || targetPoint == currentPoint) return;

			path = AStar.GetPath(currentPoint.GlobalPosition, targetPoint.GlobalPosition);

			if (path.Count <= 0) 
			{
				GD.Print("No path found");
				return; 
			}

			GD.Print($"Path to {targetPoint.Name} Length = {path.Count}");
			index = 0; 
			isMoving = true;
		}
		
		if (isMoving && !isWaiting)
		{
			if (index < path.Count)
			{
				nextPoint = path[index];
				MoveTo(nextPoint.GlobalPosition, delta);

				// Arrived to nextPoint
				if (GlobalPosition.DistanceTo(nextPoint.GlobalPosition) < ArrivalTolerance)
				{
					currentPoint = nextPoint;
					index++;    
					
					if (index >= path.Count)
					{
						GD.Print("Path end");
						isMoving = false;
						index = 0;
						path.Clear();

						if (!isWaiting) StartWait(5.0f); 
					}
				}
			}
		}
	}

	private void ChaseState(double delta)
	{
		if (AStar.starPoints.Count <= 0 || _player == null) return;

		path = AStar.GetPath(GlobalPosition, _player.GlobalPosition);

		if (path == null || path.Count <= 0) GD.Print("Chase path error");

		nextPoint = (path.Count > 1) ? path[1] : path[0];

		MoveTo(nextPoint.GlobalPosition, delta);

		if (GlobalPosition.DistanceTo(nextPoint.GlobalPosition) < ArrivalTolerance)
		{
			currentPoint = nextPoint;
		}
	}

	// Chyba już nie potrzbene ale zostaje dla bezpieczeństwa
	//
	// private Point GetClosestPoint(Vector2 target)
	// {
	// 	Point closestPoint = null;
	// 	float closestDistance = float.MaxValue;
	// 	foreach (var p in AStar.starPoints)
	// 	{
	// 		float distance = target.DistanceTo(p.GlobalPosition);
	// 		if (distance < 10.0f)
	// 		{
	// 			if (distance < closestDistance)
	// 			{
	// 				closestPoint = p;
	// 				closestDistance = distance;
	// 			}
	// 		}
	// 	}
	// 	return closestPoint;
	// }

	public override void _Ready()
    {
		randomGenerator = new RandomNumberGenerator();
		path = new List<Point>();

		debugLight.Enabled = false;

		AStar.floorLayer = floor;
		AStar.CreatePoints();

		currentPoint = AStar.starPoints[12]; // Placeholder spawnpoint

		GlobalPosition = currentPoint.GlobalPosition;
	}

	public override void _Draw()
    {
		// Dot map
		if (debugMode)
		{
			foreach (var p in AStar.starPoints) {
				Vector2 pointPos = ToLocal(p.GlobalPosition);
				DrawCircle(pointPos, 3.5f, Colors.Red, true);

				foreach (Point n in p.neighbors) {
					
					if (n != null)
						DrawLine(pointPos, ToLocal(n.GlobalPosition), Colors.Red, 0.5f, true);
				}
			}

			// Path to target
			if (path != null)
			{
				for (int i = path.Count - 1; i >= 0; i--)
				{
					Vector2 origin = ToLocal(path[i].GlobalPosition);

					DrawCircle(origin, 3.5f, Colors.Green, true);

					if (i > 0)
						DrawLine(origin, ToLocal(path[i - 1].GlobalPosition), Colors.Green, 0.5f, true);
				}	
			}	
		}
    }

    public override void _Process(double delta)
    {

        if (Input.IsActionJustPressed("toggle_debug"))
		{
			if (debugMode) debugMode = false;
			else debugMode = true;
		}

		if (debugMode) debugLight.Enabled = true;
		else debugLight.Enabled = false;
    }

    public override void _PhysicsProcess(double delta)
    {
		QueueRedraw();

		var bodies = viewArea.GetOverlappingBodies();

		rayToPlayer.TargetPosition = ToLocal(_player.Position);
		rayToPlayer.ForceRaycastUpdate();

		// CHASE
		if (bodies.Contains(_player) && rayToPlayer.GetCollider() == _player)
		{	
			ChaseState(delta);
			isMoving = false;
			isWaiting = false;
			
			isChasing = true;
			lostPlayer = true;	
		}
		else if (lostPlayer) // POST CHASE (lost player)
		{
			targetPoint = path[path.Count - 1];
			MoveTo(nextPoint.GlobalPosition, delta);

			if (currentPoint == targetPoint)
			{
				// currentPoint = GetClosestPoint(GlobalPosition);
				lostPlayer = false;
				isChasing = false;
			}
			else if (GlobalPosition.DistanceTo(nextPoint.GlobalPosition) < ArrivalTolerance)
			{
				int localIndex = path.IndexOf(nextPoint);

				currentPoint = nextPoint;
				
				if (localIndex < path.Count - 1) nextPoint = path[localIndex + 1];
				else nextPoint = targetPoint;
			}
		}
		else PatrolState(delta); // PATROL
    }
}
