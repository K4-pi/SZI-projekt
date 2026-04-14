using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class HomeOwner : CharacterBody2D
{
	private bool debugMode = false;

	private AStartPoints AStar = new AStartPoints();

	[Export] public PackedScene dollarScene;
	[Export] public Node[] expensiveItems;

	[Export] public AudioStreamPlayer2D audioSource;
	[Export] public AudioStreamPlayer gameOverSource; // Temporary

	[Export] PointLight2D debugLight;
	[Export] Area2D viewArea;
	[Export] Area2D hitBox;
	[Export] TileMapLayer floor;
	[Export] Node2D _player;
	[Export] RayCast2D rayToPlayer;
	[Export] Node2D[] stairsPoints;

	private RandomNumberGenerator randomGenerator;

	private Node itemsParent;

	Dictionary<Point, Item> itemsAtPoints; // Points near item to patrol
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
		if (!isMoving && !isWaiting) // Get a point to go to 
		{
			if (itemsAtPoints.Count <= 0) return;
			
			int r = randomGenerator.RandiRange(0, itemsAtPoints.Count - 1);
			targetPoint = itemsAtPoints.Keys.ToArray<Point>()[r];
			

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
		
		if (isMoving && !isWaiting) // Go to that point
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

						itemsAtPoints[targetPoint].lastSeen = 0.0f;

						if (!isWaiting) StartWait(2.5f); 
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

	public override void _Ready()
    {
		randomGenerator = new RandomNumberGenerator();
		path = new List<Point>();

		itemsAtPoints = new Dictionary<Point, Item>();

		debugLight.Enabled = false;

		AStar.floorLayer = floor;
		AStar.CreatePoints(stairsPoints);

		itemsParent = GetNode<Node>("/root/Main/ValuablesItems");

		CreateItems(AStar.starPoints, 20);

		currentPoint = itemsAtPoints.Keys.ToArray<Point>()
			[new RandomNumberGenerator().RandiRange(0, itemsAtPoints.Count - 1)]; // Spawn at one of items

		GlobalPosition = currentPoint.GlobalPosition;
		
		EventBus.Instance.RemoveItemPoint += (Item item) =>
		{
			if (itemsAtPoints.Count > 2) // don't remove point when there is less than 2 of them
			{
				foreach (var it in itemsAtPoints)
				{
					if (it.Value == item) itemsAtPoints.Remove(it.Key);
				}
			}

			Speed *= 1.2f; // Owner speeds up when you steal item
		};
	}

	private void CreateItems(List<Point> points, int itemsCount)
	{
		RandomNumberGenerator randomGenerator = new RandomNumberGenerator();

		GD.Print("Creating items");

		for (int i = 0; i < itemsCount; i++)
		{
			int pointIndex = randomGenerator.RandiRange(0, points.Count - 1);

			Node2D itemInstance = dollarScene.Instantiate<Node2D>(); 

			itemsParent.AddChild(itemInstance);

			itemInstance.Position = points[pointIndex].GlobalPosition; 
		}

		foreach (var item in expensiveItems)
		{
			if (item is Item it)
			{
				it.GenerateClosestPoint(AStar.starPoints);

				Point p = it.closestPoint;
				if (p != null)
				{
					itemsAtPoints.Add(p, it);
	
					// float value = it.itemValue;

					// // Simple probability of getting item
					// for (float i = 0.0f; i < value; i++)
					// {
					// 	patrolPoints.Add(p);				
					// }
				} 
			}
		}
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

			if (itemsAtPoints.Count > 0)
			{
				foreach (var p in itemsAtPoints.Keys.ToArray<Point>())
				{
					Vector2 origin = ToLocal(p.GlobalPosition);

					DrawCircle(origin, 4.0f, Colors.Yellow, true);
				}
			}
		}
    }

    public override void _Process(double delta)
    {
		QueueRedraw();

		if (isChasing) audioSource.PitchScale = 2.0f;
		else audioSource.PitchScale = 1.0f;

		if (!audioSource.Playing && (isMoving || isChasing)) audioSource.Play();
		else if (audioSource.Playing && !(isMoving || isChasing)) audioSource.Stop();

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
		var viewBodies = viewArea.GetOverlappingBodies();

		rayToPlayer.TargetPosition = ToLocal(_player.Position);
		rayToPlayer.ForceRaycastUpdate();

		var hitBodies = hitBox.GetOverlappingBodies();

		if (hitBodies.Contains(_player))
		{
			audioSource.Stop();
			gameOverSource.Play();

			// Need to change to emit signal so player can freeze _Process(delta)
			// _player.QueueFree();
			_player.SetProcess(false);
			_player.SetPhysicsProcess(false);

			SetProcess(false);
			SetPhysicsProcess(false);
		}

		// CHASE
		if (viewBodies.Contains(_player) && rayToPlayer.GetCollider() == _player)
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
