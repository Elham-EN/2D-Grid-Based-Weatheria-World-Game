using Game.Component;
using Godot;

/// <summary>
/// GameEvents serves as the central communication hub for game-wide events.
/// 
/// PURPOSE: This autoload class acts as a global event broadcaster, allowing 
/// different game systems to communicate without direct dependencies. When 
/// important events occur (like building placement), this class emits signals 
/// that any interested system can listen to and respond accordingly.
/// 
/// As an autoload node, it's created at game startup and remains accessible 
/// throughout the entire game session from any other script.
/// </summary>
public partial class GameEvents : Node
{
	// Global singleton instance for accessing GameEvents from anywhere in the code.
	// Private set ensures only this class can assign the instance value.
	public static GameEvents Instance { get; private set; }

	// Custom signal that broadcasts when any building is placed. Carries the 
	// BuildingComponent as data.
	[Signal]
	public delegate void BuildingPlacedEventHandler(BuildingComponent buildingComponent);

	// Special Godot lifecycle method called at key moments during node creation
	public override void _Notification(int what)
	{
		// NotificationSceneInstantiated fires after node creation but before _Ready()
		if (what == NotificationSceneInstantiated)
		{
			// Set global reference at this timing so other nodes can access it during 
			// their _Ready() methods
			Instance = this;
		}
	}

	// Static method to broadcast BuildingPlaced signal from anywhere in the code
	public static void EmitBuildingPlaced(BuildingComponent buildingComponent)
	{
		// Uses the global Instance to emit the signal with the building data to 
		// all listeners
		Instance.EmitSignal(SignalName.BuildingPlaced, buildingComponent);
	}
	
}
