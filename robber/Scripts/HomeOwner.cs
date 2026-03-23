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

    private Point[] localPoints;
	private Point currentPoint;
	private Point targetPoint;
	private Point nextPoint = null;
	private List<Point> path = new List<Point>();
	private List<Point> chasePath = new List<Point>();

	private int index = 0;
	private bool isMoving = false;
	private bool isWaiting = false;
	private bool isChasing = false;

	[Export] public float Speed = 30.0f;
    [Export] public float ArrivalTolerance = 5.0f; // How close counts as "arrived"

    public void MoveTo(Vector2 targetGlobalPos, double delta)
    {
        Vector2 direction = GlobalPosition.DirectionTo(targetGlobalPos);

        if (GlobalPosition.DistanceTo(targetGlobalPos) > ArrivalTolerance)
        {
            Velocity = direction * Speed;
        }
        else
        {
            Velocity = Vector2.Zero;
        }
		// LookAt(targetGlobalPos);
		
		float rotationSpeed = 3.0f;

		// Smooth rotation
    	float targetAngle = GlobalPosition.AngleToPoint(targetGlobalPos);
    	Rotation = (float)Mathf.LerpAngle(Rotation, targetAngle, rotationSpeed * delta);

        MoveAndSlide();
    }

    public override void _Ready()
    {
		debugLight.Enabled = false;

        localPoints = PathfindingPoints.allPoints; 

        GD.Print($"home_owner stored {localPoints.Length} points");
    	
		Position = localPoints[0].GlobalPosition; // Start point

		currentPoint = localPoints[0].neighbors[1];

		Position = currentPoint.GlobalPosition;

		AStar.floorLayer = floor;
		AStar.CreatePoints();
	}

    public override void _Draw()
    {
		if (debugMode)
		{
			foreach (var p in localPoints) {
				Vector2 pointPos = ToLocal(p.GlobalPosition);
				DrawCircle(pointPos, 3.5f, Colors.Red, true);

				foreach (Point n in p.neighbors) {
					
					if (n != null)
						DrawLine(pointPos, ToLocal(n.GlobalPosition), Colors.Red, 0.5f, true);
				}
			}

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

			foreach (var s in AStar.starPoints)
			{
				var current = ToLocal(s.GlobalPosition);
				
				if (Position.DistanceTo(s.GlobalPosition) < 20.0f)
				{
					DrawCircle(current, 3.5f, Colors.DarkOrange, true);
				}
				else
				{
					DrawCircle(current, 3.5f, Colors.Purple, true);
				}

			}

			// if (chasePath != null)
			// {
			// 	for (int i = path.Count - 1; i >= 0; i--)
			// 	{
			// 		Vector2 origin = ToLocal(chasePath[i].GlobalPosition);

			// 		DrawCircle(origin, 3.5f, Colors.Black, true);

			// 		if (i > 0)
			// 			DrawLine(origin, ToLocal(chasePath[i - 1].GlobalPosition), Colors.Black, 0.5f, true);
			// 	}
			// }
		}
    }

	private async void StartWait(float seconds)
	{
		isWaiting = true;
		GD.Print($"Wait start for {seconds} seconds");
		
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
		
		GD.Print("Wait end");
		isWaiting = false;
	}

	private List<Point> ScanPoint(Point start, Point target) 
	{
		List<Point> path = new List<Point>();

		foreach (Point p in localPoints) p.visited = false;

		Point _currentPoint = start;

		int maxSteps = 100;
		int currentStep = 0;

		while (_currentPoint != target || currentStep <= maxSteps)
		{
			path.Add(_currentPoint);

			currentStep++;
			_currentPoint.visited = true;
			
			Point _closestNeighbor = null;
			float shortestDistance = float.MaxValue;

			foreach (Point n in _currentPoint.neighbors)
			{
				if (n.visited || n == null) continue;

				float distance = _currentPoint.GlobalPosition.DistanceTo(n.GlobalPosition);

				if (n == target)
				{
					_closestNeighbor = n;
					break;
				}

				if (distance < shortestDistance)
				{
					shortestDistance = distance;
					_closestNeighbor = n;
				}
			}

			if (_closestNeighbor == null)
			{
				GD.Print("No path");
				return null;
			}

			_currentPoint = _closestNeighbor;

			if (_currentPoint == target)
        	{
            	path.Add(target);
            	break;
        	}
		}

		return path;
	}

	private void PatrolState(double delta)
	{
		if (!isMoving && !isWaiting) 
		{
			RandomNumberGenerator rnd = new RandomNumberGenerator();
			int r = rnd.RandiRange(0, localPoints.Length - 1);
			GD.Print($"Random number = {r}");

			targetPoint = localPoints[r];
			GD.Print($"Target = {targetPoint.Name}");
		
			if (targetPoint == currentPoint) return; // If new target point is our current point we repeat

			path = ScanPoint(currentPoint, targetPoint);

			if (path == null) return; // If no pah found return and try with new target

			GD.Print("Path = " + string.Join(" -> ", path.Select(p => p.Name)));
			isMoving = true;
		}
		
		if (index < path.Count)
		{
			nextPoint = path[index];
			
			MoveTo(nextPoint.GlobalPosition, delta);

			if (GlobalPosition.DistanceTo(nextPoint.GlobalPosition) < ArrivalTolerance)
			{
				index++;	
				currentPoint = nextPoint;
			}
		}

		if (GlobalPosition.DistanceTo(targetPoint.GlobalPosition) < ArrivalTolerance)
		{
			isMoving = false;
			index = 0;
			path = new List<Point>();

			if (!isWaiting) StartWait(5.0f); // Wait 5 seconds before starting new route
		}
	}

	private void ChaseState(double delta)
	{
		chasePath = AStar.GetPath(GlobalPosition, _player.GlobalPosition);
		
		if (chasePath == null) return;

		if (index < chasePath.Count)
		{
			nextPoint = chasePath[index];
			
			MoveTo(nextPoint.GlobalPosition, delta);

			if (GlobalPosition.DistanceTo(nextPoint.GlobalPosition) < ArrivalTolerance)
			{
				index++;	
				currentPoint = nextPoint;
			}
		}

		if (GlobalPosition.DistanceTo(_player.GlobalPosition) < ArrivalTolerance)
		{
			isMoving = false;
			index = 0;
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

		//Doesn't work togheter

		// var bodies = viewArea.GetOverlappingBodies();
		
		// if (bodies.Contains(_player))
		// {
		// 	ChaseState(delta);
		// 	path = new List<Point>();
		// 	isMoving = false;
		// 	isWaiting = false;
		// }
		
		PatrolState(delta);
    }
}





