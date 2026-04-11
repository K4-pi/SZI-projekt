using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

public partial class AStartPoints : Node2D
{
	public TileMapLayer floorLayer;
	public TileMapLayer stairsLayer;

	public List<Point> starPoints;
	
	public void CreatePoints()
	{
		starPoints = new List<Point>();
		var cells = floorLayer.GetUsedCells();

		foreach (Vector2I cell in cells)
		{
			Point newPoint = new Point(); 

			Vector2 worldPos = floorLayer.ToGlobal(floorLayer.MapToLocal(cell));
			newPoint.Setup(worldPos);

			AddChild(newPoint);

			starPoints.Add(newPoint);
		}

		ConnectNeighbors();
	}

	public void CreatePoints(Node2D[] stairsNodes)
	{
		starPoints = new List<Point>();
		var cells = floorLayer.GetUsedCells();

		foreach (Vector2I cell in cells)
		{
			Point newPoint = new Point(); 

			Vector2 worldPos = floorLayer.ToGlobal(floorLayer.MapToLocal(cell));
			newPoint.Setup(worldPos);

			AddChild(newPoint);

			starPoints.Add(newPoint);
		}

		if (stairsNodes.Length > 0)
		{
			GD.Print("Creating stairs points");
			StairsPoints[] stairsPoints = stairsNodes
										.Cast<StairsPoints>()
										.ToArray();

			// create stairs points 
			Dictionary<StairsPoints, Point> spToPoint = new();
			foreach (StairsPoints sp in stairsPoints)
			{
				Point newPoint = new Point();
				Vector2 worldPos = sp.GlobalPosition;
				newPoint.Setup(worldPos);
				AddChild(newPoint);
				starPoints.Add(newPoint);
				spToPoint[sp] = newPoint;
			}

			// connect MAIN neighbor which is next stairs 
			foreach (StairsPoints sp in stairsPoints)
			{
				if (sp.mainNeighbor != null && spToPoint.ContainsKey(sp.mainNeighbor))
				{
					spToPoint[sp].neighbors.Add(spToPoint[sp.mainNeighbor]);
				}
			}
		}
		else GD.Print("Stairs list is empty");

		ConnectNeighbors();
	}

	public void ConnectNeighbors()
	{

		foreach (Point p in starPoints)
		{
			// p.neighbors = new List<Point>();

			List<Point> neighboringPoints = new List<Point>();

			foreach (Point n in starPoints)
			{
				if (p == n) continue;

				float distance = p.GlobalPosition.DistanceTo(n.GlobalPosition);
				
				if (distance < 23.0f) neighboringPoints.Add(n);
			}

			p.neighbors.AddRange(neighboringPoints);
		}
		
		GD.Print("AStar Neighbors connected");
	}

	public List<Point> GetPath(Vector2 startWorldPos, Vector2 endWorldPos)
	{
		Point startNode = GetClosestPoint(startWorldPos);
		Point targetNode = GetClosestPoint(endWorldPos);

		if (startNode == null || targetNode == null) return new List<Point>();

		// Open set
		List<Point> openSet = new List<Point> { startNode };
		
		// Path tracking
		Dictionary<Point, Point> cameFrom = new Dictionary<Point, Point>();

		// gScore cost from start to current node
		Dictionary<Point, float> gScore = new Dictionary<Point, float>();
		
		// fScore (gScore + heuristic distance to target)
		Dictionary<Point, float> fScore = new Dictionary<Point, float>();

		// Initialize scores
		foreach (var p in starPoints)
		{
			gScore[p] = float.MaxValue;
			fScore[p] = float.MaxValue;
		}

		gScore[startNode] = 0;
		fScore[startNode] = startNode.GlobalPosition.DistanceTo(targetNode.GlobalPosition);

		while (openSet.Count > 0)
		{
			Point current = openSet.OrderBy(p => fScore[p]).First();

			if (current == targetNode)
			{
				return ReconstructPath(cameFrom, current);
			}

			openSet.Remove(current);

			foreach (Point neighbor in current.neighbors)
			{
				float tentativeGScore = gScore[current] + current.GlobalPosition.DistanceTo(neighbor.GlobalPosition);

				if (tentativeGScore < gScore[neighbor])
				{
					cameFrom[neighbor] = current;
					gScore[neighbor] = tentativeGScore;
					fScore[neighbor] = gScore[neighbor] + neighbor.GlobalPosition.DistanceTo(targetNode.GlobalPosition);

					if (!openSet.Contains(neighbor))
					{
						openSet.Add(neighbor);
					}
				}
			}
		}

		return new List<Point>(); // No path found
	}

	private Point GetClosestPoint(Vector2 worldPos)
	{
		Point closest = null;
		float minDist = float.MaxValue;

		foreach (Point p in starPoints)
		{
			float dist = worldPos.DistanceTo(p.GlobalPosition);
			if (dist < minDist)
			{
				minDist = dist;
				closest = p;
			}
		}
		return closest;
	}

	private static List<Point> ReconstructPath(Dictionary<Point, Point> from, Point current)
	{
		List<Point> totalPath = new List<Point> { current };
		while (from.ContainsKey(current))
		{
			current = from[current];
			totalPath.Insert(0, current);
		}
		return totalPath;
	}


}
