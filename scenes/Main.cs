using Game.Manager;
using Game.Resources.Building;
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

	private BuildingResource towerResource;

	private BuildingResource villageResource;

	private BuildingResource toPlaceBuildingResource;

	private Button placeTowerButton;

	private Button placeVillageButton;

	private Vector2I? hoveredGridCell;

	private Node2D ySortRoot;

	// INITIALIZATION: Set up everything before the game starts
	public override void _Ready()
	{
		towerResource = GD.Load<BuildingResource>("res://resources/building/tower.tres");

		villageResource = GD.Load<BuildingResource>("res://resources/building/village.tres");

		gridManager = GetNode<GridManager>("GridManager");

		cursor = GetNode<Sprite2D>("Cursor");

		placeTowerButton = GetNode<Button>("PlaceTowerButton");

		placeVillageButton = GetNode<Button>("PlaceVillageButton");

		ySortRoot = GetNode<Node2D>("YSortRoot");

		cursor.Visible = false;

		placeTowerButton.Pressed += OnPlaceTowerButtonPressed;

		placeVillageButton.Pressed += OnPlaceVillageButtonPressed;

		gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
	}

	// Handles mouse clicks during building placement mode
	public override void _UnhandledInput(InputEvent evt)
	{
		// Check three conditions: cursor is over a grid cell, left mouse clicked, 
		// and Validates the position is within range of existing buildings and on 
		// suitable terrain
		if (hoveredGridCell.HasValue && evt.IsActionPressed("left_click")
			&& gridManager.IsTilePositionBuildable(hoveredGridCell.Value))
		{
			// Create and place the building at the hovered location
			PlaceBuildingAtMousePosition();
			// Exit building placement mode by hiding the cursor
			cursor.Visible = false;
		}
	}

	// CONTINUOUS UPDATE LOOP: 
	public override void _Process(double delta)
	{
		// Convert mouse pixel position to grid coordinates for snap-to-grid behavior
		var gridPosition = gridManager.GetMouseGridCellPosition();
		// Snap cursor visually to the grid by converting grid coords back to pixels
		cursor.GlobalPosition = gridPosition * 64;
		// Only update highlights when cursor is visible and mouse moved to a 
		// new grid cell
		if (toPlaceBuildingResource != null && cursor.Visible &&
			(!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
		{
			// Remember which grid cell we're now hovering over
			hoveredGridCell = gridPosition;
			gridManager.ClearHighlightedTiles();
			// GREEN tiles - expansion preview when hovering
			gridManager.HighlightExpandableBuildableTiles(hoveredGridCell.Value,
				toPlaceBuildingResource.BuildableRadius);
			gridManager.HighlightResourceTiles(hoveredGridCell.Value,
				toPlaceBuildingResource.ResourceRadius);
		}
	}

	// Creates and places a new building at the cursor location
	private void PlaceBuildingAtMousePosition()
	{
		// Safety check - abort if cursor isn't positioned over a valid grid cell
		if (!hoveredGridCell.HasValue) return;
		// Create new building object from the preloaded scene template
		var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
		// Add building to scene tree, which triggers its _Ready() method
		ySortRoot.AddChild(building);
		// Position building in world space by converting grid coordinates to pixels
		building.GlobalPosition = hoveredGridCell.Value * 64;
		// Clear hover state since building is now placed
		hoveredGridCell = null;
		// Clean up visual feedback - remove highlight tiles and exit placement mode
		gridManager.ClearHighlightedTiles();
	}

	// USER INTERFACE EVENT HANDLER: This method responds to button press events
	// and serves as the entry point for initiating building placement mode.
	private void OnPlaceTowerButtonPressed()
	{
		toPlaceBuildingResource = towerResource;
		cursor.Visible = true;
		// WHITE tiles - current buildable areas
		gridManager.HighlightBuildableTiles();
	}

	private void OnPlaceVillageButtonPressed()
	{
		toPlaceBuildingResource = villageResource;
		cursor.Visible = true;
		// WHITE tiles - current buildable areas
		gridManager.HighlightBuildableTiles();
	}

	private void OnResourceTilesUpdated(int resourceCount)
	{
		GD.Print(resourceCount);
	}
}
