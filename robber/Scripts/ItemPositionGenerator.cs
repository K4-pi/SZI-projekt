using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ItemPositionGenerator : Node
{
    public override void _Ready()
    {
        var itemsInstances = GetTree().Root.GetNode<Node>("Main/ExpensiveItems").GetChildren();
        var itemSpawnPoints = GetChildren().ToList();

        RandomNumberGenerator rng = new RandomNumberGenerator();

        foreach (Node2D item in itemsInstances)
        {
            if (item.Name == "tv") continue; // Dont random place TV

            int n = rng.RandiRange(0, itemSpawnPoints.Count - 1); 

            item.Position = ((Node2D)itemSpawnPoints[n]).GlobalPosition; 

            itemSpawnPoints.RemoveAt(n);
        }
    }
}
