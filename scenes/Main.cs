using System.Collections.Generic;
using Godot;

namespace Game;

// GAME OVERVIEW:
// This creates a grid-based building placement system - like tower defense games
// Player moves mouse -> cursor snaps to grid -> click to place buildings
// Core mechanics: cursor tracking + building placement + grid alignment

public partial class Main : Node
{
	// GAME OBJECTS WE NEED TO CONTROL:
	private Sprite2D cursor;
	
	// A template/blueprint for building objects that serves as our factory pattern 
	// implementation.
	private PackedScene buildingScene;
	
	// The user interface element that initiates placement mode.
	private Button placeBuildingButton;
 
	private TileMapLayer highlightTileMapLayer;

	private Vector2? hoveredGridCell;
	
	// To mark occupied cells (cannat have duplicate element, must be unique)
	private HashSet<Vector2> occupiedCells = new HashSet<Vector2>();


	// INITIALIZATION: Set up everything before the game starts
	public override void _Ready()
	{
		buildingScene = GD.Load<PackedScene>("res://scenes/building/Building.tscn");

		cursor = GetNode<Sprite2D>("Cursor");

		placeBuildingButton = GetNode<Button>("PlaceBuildingButton");

		highlightTileMapLayer = GetNode<TileMapLayer>("HighlightTileMapLayer");

		cursor.Visible = false;

		placeBuildingButton.Pressed += OnButtonPressed;
	}

	// INPUT EVENT PROCESSING: This method handles discrete input events and specifically 
	// responds to mouse click events when certain conditions are met.
	public override void _UnhandledInput(InputEvent evt)
	{
		if (hoveredGridCell.HasValue && evt.IsActionPressed("left_click")
			&& !occupiedCells.Contains(hoveredGridCell.Value))
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
		var gridPosition = GetMouseGridCellPosition();

		cursor.GlobalPosition = gridPosition * 64;

		if (cursor.Visible &&
			(!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
		{
			hoveredGridCell = gridPosition;

			UpdateHighlightTileMapLayer();
		}
	}

	// MOUSE-TO-GRID CONVERSION: Convert mouse pixel position to grid coordinates
	private Vector2 GetMouseGridCellPosition()
	{
		var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();

		var gridPosition = mousePosition / 64;

		gridPosition = gridPosition.Floor();

		return gridPosition;
	}

	// BUILDING PLACEMENT: Create a new building at the mouse position
	private void PlaceBuildingAtMousePosition()
	{
		if (!hoveredGridCell.HasValue) return;

		var building = buildingScene.Instantiate<Node2D>();

		AddChild(building);

		building.GlobalPosition = hoveredGridCell.Value * 64;

		occupiedCells.Add(hoveredGridCell.Value);

		hoveredGridCell = null;

		UpdateHighlightTileMapLayer();
	}
	
	// This method draw and update the range preview that helps players understand 
	// the strategic implications of their building placement.
	private void UpdateHighlightTileMapLayer()
	{
		highlightTileMapLayer.Clear();
		
		if (!hoveredGridCell.HasValue)
		{
			return;
		}
		for (var x = hoveredGridCell.Value.X - 3; x <= hoveredGridCell.Value.X + 3; x++)
		{
			for (var y = hoveredGridCell.Value.Y - 3; y <= hoveredGridCell.Value.Y + 3; y++)
			{
				highlightTileMapLayer.SetCell(
					new Vector2I((int)x, (int)y), 0, Vector2I.Zero);
			}
		}
	}

	// USER INTERFACE EVENT HANDLER: This method responds to button press events
	// and serves as the entry point for initiating building placement mode.
	private void OnButtonPressed()
	{
		cursor.Visible = true;
	}
}
