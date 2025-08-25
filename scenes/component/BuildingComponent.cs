using Godot;

namespace Game.Component;

/// <summary>
/// Reusable component that defines building properties and manages 
/// grid positioning. Attached to buildings to specify their buildable 
/// radius and provide grid utilities.
/// </summary>
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
		// Announces this building's placement to all game systems 
		// via the global event system
		Callable.From(() => GameEvents.EmitBuildingPlaced(this)).CallDeferred();
	}

	// It returns where this particular building is located in the game world
	public Vector2I GetGridCellPosition()
	{
		var gridPosition = GlobalPosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

}
