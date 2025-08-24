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
			&& gridManager.IsTilePositionValid(hoveredGridCell.Value))
		{
			PlaceBuildingAtMousePosition();

			cursor.Visible = false;
		}
	}

	// CONTINUOUS UPDATE LOOP: This method executes every frame (typically 60 times
	// per second) and handles all the real-time updates that need to happen while
	// the game is running.
	public override void _Process(double delta)
	{
		var gridPosition = gridManager.GetMouseGridCellPosition();

		cursor.GlobalPosition = gridPosition * 64;

		if (cursor.Visible &&
			(!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
		{
			hoveredGridCell = gridPosition;

			gridManager.HighlightValidTilesInRadius(hoveredGridCell.Value, 3);
		}
	}

	// BUILDING PLACEMENT: Create a new building at the mouse position
	private void PlaceBuildingAtMousePosition()
	{
		if (!hoveredGridCell.HasValue) return;

		var building = buildingScene.Instantiate<Node2D>();

		AddChild(building);

		building.GlobalPosition = hoveredGridCell.Value * 64;

		gridManager.MarkTileAsOccupied(hoveredGridCell.Value);

		hoveredGridCell = null;

		gridManager.ClearHighlightedTiles();
	}

	// USER INTERFACE EVENT HANDLER: This method responds to button press events
	// and serves as the entry point for initiating building placement mode.
	private void OnButtonPressed()
	{
		cursor.Visible = true;
	}
}
