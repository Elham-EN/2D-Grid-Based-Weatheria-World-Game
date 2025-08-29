using Godot;

namespace Game.Component;

/// <summary>
/// BuildingComponents represent buildings that are already placed 
/// and existing in the game world.
/// BuildingComponent = Any placed building
/// - Base building (placed in editor) - has BuildingComponent
/// - Towers (placed by player) - has BuildingComponent
/// - Any future building types - will have BuildingComponent
/// </summary>
public partial class BuildingComponent : Node2D
{
	// Configurable radius determining how far this building extends 
	// buildable area
	[Export]
	public int BuildableRadius { get; private set; }

	public override void _Ready()
	{
		// Enables the grid system to find all buildings without 
		// hardcoded references
		AddToGroup(nameof(BuildingComponent));
		// Announces this building's placement to all game node-based systems 
		// via the global event system. It ensures the signal waits until the 
		// building's position is properly set, preventing buildings from 
		// registering at (0,0) coordinates in the grid system.
		// AddChild(building) called → _Ready() runs → signal queued for later
		// building.GlobalPosition = correctPosition sets correct position
		// End of frame → deferred signal emitted with correct position
		Callable.From(() => GameEvents.EmitBuildingPlaced(this)).CallDeferred();
	}

	// It returns where this particular building is located in the game world
	// Converts world pixel position to grid coordinates 
	public Vector2I GetGridCellPosition()
	{
		var gridPosition = GlobalPosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

}
