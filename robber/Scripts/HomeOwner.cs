using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class HomeOwner : CharacterBody2D
{
	private AStartPoints AStar = new AStartPoints();

	[Export] public PackedScene dollarScene;
	[Export] public Node[] expensiveItems;

	[Export] public AudioStreamPlayer2D audioSource;
	[Export] public AudioStreamPlayer gameOverSource; // Temporary

	[Export] PointLight2D debugLight;
	[Export] Area2D viewArea;
	[Export] Area2D hitBox;
	[Export] TileMapLayer floor;
	[Export] Node2D player;
	[Export] RayCast2D rayToPlayer;
	[Export] Node2D[] stairsPoints;

	private List<Area2D> rooms = new List<Area2D>();

	Dictionary<Point, Item> itemsAtPoints; // Points near item to patrol

	private Area2D previousRoom = null;
	private Area2D currentRoom = null;

	private List<Point> path;
	private Point currentPoint;
	private Point targetPoint;
	private Point nextPoint = null;

	private int index = 0;

	private byte wanderIndex = 0;
	private byte debugState = 0;

	private bool generatedPoints = false;
	private bool isMoving = false;
	private bool isWaiting = false;
	private bool wasChasing = false;

	[Export] public float baseSpeed = 30.0f;
	[Export] public float sprintMultiplier = 2f;
    [Export] public float ArrivalTolerance = 0.75f; // How close to count as "arrived" to point

    public void MoveTo(Vector2 targetGlobalPos, double delta)
    {
		float speed = baseSpeed;

		if (wasChasing) speed = baseSpeed * sprintMultiplier;

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

		if (wasChasing && rayToPlayer.GetCollider() is Player)
		{
			rotationTarget = player.GlobalPosition;
		} 

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

			float lowestPathValue = 0f;

			if (itemsAtPoints.Count > 1)
			{
				foreach (var dictObj in itemsAtPoints) // Points evaluation
				{
					if (dictObj.Key == currentPoint)
					{
						dictObj.Value.calculatedValueLabel.Text = $"start";			
						continue;
					}	

					if (currentPoint == null) return;

					List<Point> tmpPath = AStar.GetPath(currentPoint.GlobalPosition, dictObj.Key.GlobalPosition);

					float pointValue = 
						(((float)tmpPath.Count / 5f) + (dictObj.Value.itemValue / 2f)) * 
						dictObj.Value.lastSeen;

					dictObj.Value.calculatedValueLabel.Text = $"{Mathf.Round(pointValue)}";

					if (pointValue > lowestPathValue)
					{
						lowestPathValue = pointValue;

						path = tmpPath;
					}
				}
			}
			else // When less than 2 items left
			{
				RandomNumberGenerator rng = new RandomNumberGenerator();

				int n = rng.RandiRange(0, rooms.Count() - 1);

				path = AStar.GetPath(GlobalPosition, rooms[n].GlobalPosition);
			}

			if (path.Count <= 0) 
			{
				GD.Print("No path found");
				return; 
			}

			targetPoint = path[path.Count - 1];

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
						
						itemsAtPoints[currentPoint].lastSeen = 0.0f;
						
						path.Clear();
						
						if (!isWaiting) StartWait(2.5f); 
					}
				}
			}
		}
	}

	private void ChaseState(double delta)
	{
		if (AStar.starPoints.Count <= 0 || player == null) return;

		path = AStar.GetPath(GlobalPosition, player.GlobalPosition);

		if (path == null || path.Count <= 0) GD.Print("Chase path error");

		nextPoint = (path.Count > 1) ? path[1] : path[0];

		MoveTo(nextPoint.GlobalPosition, delta);

		if (GlobalPosition.DistanceTo(nextPoint.GlobalPosition) < ArrivalTolerance)
		{
			currentPoint = nextPoint;
		}
	}

	private void PostChaseState(double delta)
	{
		targetPoint = path[path.Count - 1];
		MoveTo(nextPoint.GlobalPosition, delta);

		if (currentPoint == targetPoint && !isWaiting)
		{
			if (wanderIndex > 2) // Cancel after 3 cycles
			{
				wasChasing = false;
				isMoving = false;
				path.Clear();
				targetPoint = currentPoint;

				wanderIndex = 0; // reset wander count

				return;
			}

			if (wanderIndex == 0) // First wander go to closest room
			{
				int closest = int.MaxValue;

				List<Point> tmpPath = new List<Point>();

				foreach (var room in rooms)
				{
					tmpPath = AStar.GetPath(GlobalPosition, room.GlobalPosition);

					if (tmpPath.Count() < closest)
					{
						closest = tmpPath.Count();
						path = tmpPath;
						currentRoom = room;
					}
				}
			}
			else
			{
				foreach (var room in rooms)
				{
					if (room.GetOverlappingBodies().Contains(this))
					{
						GD.Print($"BOT starts wander at {room.Name}");

						var nRooms = ((Room)room).neighborRooms;

						RandomNumberGenerator rng = new RandomNumberGenerator();

						int neighborsCount = nRooms.Count();

						previousRoom = currentRoom;

						do
						{
							currentRoom = nRooms[rng.RandiRange(0, neighborsCount - 1)];
						} while (neighborsCount > 1 && currentRoom == previousRoom);
						
						path = AStar.GetPath(currentPoint.GlobalPosition, currentRoom.GlobalPosition);

						break;
					}
				}
			}

			wanderIndex++;
			isMoving = true;
		}
		else if (GlobalPosition.DistanceTo(nextPoint.GlobalPosition) < ArrivalTolerance)
		{
			int localIndex = path.IndexOf(nextPoint);

			currentPoint = nextPoint;
			
			if (localIndex < path.Count - 1) nextPoint = path[localIndex + 1];
			else nextPoint = targetPoint;
		}
	}

	public override void _Ready()
    {
		path = new List<Point>();

		itemsAtPoints = new Dictionary<Point, Item>();

		debugLight.Enabled = false;

		AStar.floorLayer = floor;
		
		EventBus.Instance.RemoveItemPoint += (Item item) =>
		{
			foreach (var it in itemsAtPoints)
			{
				if (it.Value == item) itemsAtPoints.Remove(it.Key);
			}

			baseSpeed *= 1.1f; // Owner speeds up when you steal item
		};

		rooms = GetTree().Root.GetNode<Node>("Main/Rooms")
			.GetChildren()
			.Cast<Area2D>()
			.ToList();
	}

	private void CreateItems(List<Point> points, int itemsCount)
	{
		RandomNumberGenerator randomGenerator = new RandomNumberGenerator();

		GD.Print("Creating items");
		
		Node dollarsParent = GetNode<Node>("/root/Main/Dollars");

		for (int i = 0; i < itemsCount; i++)
		{
			int pointIndex = randomGenerator.RandiRange(0, points.Count - 1);

			Node2D itemInstance = dollarScene.Instantiate<Node2D>(); 

			dollarsParent.AddChild(itemInstance);

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
				} 
			}
		}
	}

	public override void _Draw()
    {
		if (debugState > 0)
		{
			// Dot map
			foreach (var p in AStar.starPoints) {
	
				if (debugState == 1 && Position.DistanceTo(p.GlobalPosition) > 75f) continue;

				Vector2 pointPos = ToLocal(p.GlobalPosition);

				DrawCircle(pointPos, 3.5f, Colors.Purple, true);

				foreach (Point n in p.neighbors) {
					
					if (n != null)
						DrawLine(pointPos, ToLocal(n.GlobalPosition), Colors.Purple, 0.5f, true);
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

			// Points for items
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
		// if (currentRoom != null) GD.Print($"Current room = {currentRoom.Name}");	
		// if (previousRoom != null) GD.Print($"Previous room = {previousRoom.Name}");

		if (!generatedPoints) // initialization is needed here because can't call physic functions in _Ready()
		{
			RayCast2D checkWallRay = new RayCast2D();
			GetTree().Root.AddChild(checkWallRay);

			checkWallRay.CollisionMask = 2;

			AStar.CreatePoints(stairsPoints, checkWallRay);
			CreateItems(AStar.starPoints, 20);

			currentPoint = itemsAtPoints.Keys.ToArray<Point>()
				[new RandomNumberGenerator().RandiRange(0, itemsAtPoints.Count - 1)]; // Spawn at one of items

			GlobalPosition = currentPoint.GlobalPosition;

			generatedPoints = true;

			checkWallRay.QueueFree();
		}

		if (debugState > 0) QueueRedraw();

		if (wasChasing) audioSource.PitchScale = 2.0f;
		else audioSource.PitchScale = 1.0f;

		if (!audioSource.Playing && (isMoving || wasChasing)) audioSource.Play();
		else if (audioSource.Playing && !(isMoving || wasChasing)) audioSource.Stop();

        if (Input.IsActionJustPressed("toggle_debug"))
		{
			if (debugState >= 2) debugState = 0;
			else debugState++;
		}

		if (debugState > 0) debugLight.Enabled = true;
		else debugLight.Enabled = false;
	}

    public override void _PhysicsProcess(double delta)
    {
		if (!generatedPoints) return;

		var hitBodies = hitBox.GetOverlappingBodies();

		if (hitBodies.Contains(player)) // Catch when close
		{
			audioSource.Stop();
			gameOverSource.Play();

			// Frezzes player processes
			player.SetProcess(false);
			player.SetPhysicsProcess(false);

			SetProcess(false);
			SetPhysicsProcess(false);
		}

		var viewBodies = viewArea.GetOverlappingBodies();

		if (Position.DistanceTo(player.GlobalPosition) < 230f)
		{
			rayToPlayer.TargetPosition = rayToPlayer.ToLocal(player.GlobalPosition);
		}
		
		// CHASE
		if (viewBodies.Contains(player) && rayToPlayer.GetCollider() is Player)
		{
			ChaseState(delta);
			isMoving = true;
			isWaiting = false;
			
			wasChasing = true;

			wanderIndex = 0; // reset wander count
		}

		else if (wasChasing) PostChaseState(delta); // POST CHASE (lost player)
		else PatrolState(delta); // PATROL
    }
}
