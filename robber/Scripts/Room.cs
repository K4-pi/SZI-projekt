using Godot;
using System;

public partial class Room : Area2D 
{
	[Export] public Area2D[] neighborRooms;
}
