using Game.Manager;
using Godot;

namespace Game;

// GAME OVERVIEW:
// This creates a grid-based building placement system - like tower defense games
// Player moves mouse -> cursor snaps to grid -> click to place buildings
// Core mechanics: cursor tracking + building placement + grid alignment

public partial class Main : Node
{
	private GridManager gridManager;

	private Sprite2D cursor;
	
	private PackedScene buildingScene;
	
	private Button placeBuildingButton;
 
	private Vector2I? hoveredGridCell;
	
	// INITIALIZATION: Set up everything before the game starts
	public override void _Ready()
	{
		buildingScene = GD.Load<PackedScene>("res://scenes/building/Building.tscn");

		gridManager = GetNode<GridManager>("GridManager");

		cursor = GetNode<Sprite2D>("Cursor");

		placeBuildingButton = GetNode<Button>("PlaceBuildingButton");

		cursor.Visible = false;

		placeBuildingButton.Pressed += OnButtonPressed;
	}

	// INPUT EVENT PROCESSING: This method handles discrete input events and specifically 
	// responds to mouse click events when certain conditions are met.
	public override void _UnhandledInput(InputEvent evt)
	{
		if (hoveredGridCell.HasValue && evt.IsActionPressed("left_click")
			&& gridManager.IsTilePositionBuildable(hoveredGridCell.Value))
		{
			PlaceBuildingAtMousePosition();

			cursor.Visible = false;
		}
	}

	// CONTINUOUS UPDATE LOOP: 
	public override void _Process(double delta)
	{
		var gridPosition = gridManager.GetMouseGridCellPosition();
		// Snap cursor visually to the grid by converting grid coords back to pixels
		cursor.GlobalPosition = gridPosition * 64;
		// Only update highlights when cursor is visible and mouse moved to a 
		// new grid cell
		if (cursor.Visible &&
			(!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
		{
			// Remember which grid cell we're now hovering over
			hoveredGridCell = gridPosition;
			// Show yellow highlights of all buildable areas around existing buildings
			gridManager.HighlightBuildableTiles();
		}
	}

	// Creates and places a new building at the cursor location
	private void PlaceBuildingAtMousePosition()
	{
		// Exit if no grid position is being hovered
		if (!hoveredGridCell.HasValue) return;
		// Create new building instance from the loaded scene template
		var building = buildingScene.Instantiate<Node2D>();
		// Add building to scene tree, which triggers its _Ready() method
		AddChild(building);
		// Set building's pixel position by converting grid coordinates 
		// to world position
		building.GlobalPosition = hoveredGridCell.Value * 64;
		// Clear hover state since building is now placed
		hoveredGridCell = null;
		// Remove yellow highlight tiles since placement mode is ending
		gridManager.ClearHighlightedTiles();
	}

	// USER INTERFACE EVENT HANDLER: This method responds to button press events
	// and serves as the entry point for initiating building placement mode.
	private void OnButtonPressed()
	{
		cursor.Visible = true;
	}
}
