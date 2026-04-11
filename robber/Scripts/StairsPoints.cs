using Godot;
using System;

public partial class StairsPoints : Point 
{
	[Export] public StairsPoints mainNeighbor; // Opposite stairs parter -> basement or basement -> parter
}
