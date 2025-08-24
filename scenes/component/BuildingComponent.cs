using Godot;

namespace Game.Component;

// This class have some functionality and some data configuration
// related to properties of the building
public partial class BuildingComponent : Node2D
{
	// How far from this building can you place new buildings
	[Export]
	public int BuildableRadius { get; private set; }

	public override void _Ready()
	{
		// Add BuildingComponent to a group. Groups provide a tagging 
		// system for nodes, allowing you to identify similar nodes.
		AddToGroup(nameof(BuildingComponent));
	}

	public Vector2I GetGridCellPosition()
	{
		var gridPosition = GlobalPosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

}
