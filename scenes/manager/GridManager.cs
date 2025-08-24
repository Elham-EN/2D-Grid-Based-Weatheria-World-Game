using System.Collections.Generic;
using System.Linq;
using Game.Component;
using Godot;

namespace Game.Manager;

// GridManager handles all grid-related operations
public partial class GridManager : Node
{
	// Tracks which grid positions already have buildings on them, preventing 
	// players from placing multiple buildings in the same location
	// This stores grid coordinates like (3, 2), (5, 7), etc. - each representing 
	// one occupied grid cell. Each grid cell can only hold one building
	private HashSet<Vector2I> occupiedCells = new HashSet<Vector2I>();
	[Export]
	private TileMapLayer highlightTileMapLayer;
	// Contain Terrian information: e.g. sand
	[Export]
	private TileMapLayer baseTerrainTileMapLayer;


	// MOUSE-TO-GRID CONVERSION: Convert mouse pixel position to grid coordinates
	public Vector2I GetMouseGridCellPosition()
	{
		var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();

		var gridPosition = mousePosition / 64;

		gridPosition = gridPosition.Floor();

		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

	// Checks if a position is available for building & Returns 
	// true if the position is NOT in the occupied set
	// When called: Before allowing building placement
	public bool IsTilePositionValid(Vector2I tilePosition)
	{
		// Returns the TileData object associated with the given cell,
		var customData = baseTerrainTileMapLayer.GetCellTileData(tilePosition);
		// If that given grid cell has no custom data, that means it's not buildable
		// So that means you cannot place building the that grid cell.
		if (customData == null) return false;
		if (!(bool)customData.GetCustomData("buildable")) return false;
		return !occupiedCells.Contains(tilePosition);
	}

	// Records that a position now has a building & 
	// When called: After successfully placing a building
	public void MarkTileAsOccupied(Vector2I tilePosition)
	{
		occupiedCells.Add(tilePosition);
	}

	// Shows all areas where new buildings can be placed by highlighting tiles 
	// around existing buildings based on their individual build ranges
	// Simple flow: Clear old highlights → Find all buildings → For each building, 
	// highlight tiles within its range → Result shows total buildable area
	public void HighlightBuildableTiles()
	{
		// Remove any existing highlight tiles from previous operations
		ClearHighlightedTiles();
		// Find all nodes in the "BuildingComponent" group across the entire scene tree
		var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent))
			// Loop over all nodes that are returned & cast them to BuildingComponent
			.Cast<BuildingComponent>();
		// Process each building that has a BuildingComponent attached	
		// Old way: Highlight around where you're about to place a building
		// New way: Highlight around all existing buildings that allow expansion
		foreach (var buildingComponent in buildingComponents)
		{
			// For this building, highlight all valid tiles within its build range
			// Uses the building's current grid position as center point
			// Uses the building's configured radius to determine highlight area
			// it only highlights areas within range of your existing buildings.
			HighlightValidTilesInRadius(buildingComponent.GetGridCellPosition(),
				buildingComponent.BuildableRadius);
		}
	}

	public void ClearHighlightedTiles()
	{
		highlightTileMapLayer.Clear();
	}

	private void HighlightValidTilesInRadius(Vector2I rootCell, int radius)
	{
        // Loop through all tiles in the radius
		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x, y);
				// If a tile is NOT valid (occupied) → continue (skip to next iteration)
				// This gives players clear indication of where they can place buildings 
				// within the radius, while occupied spots remain unhighlighted.
				if (!IsTilePositionValid(tilePosition)) continue;
				// If a tile IS valid (available) → highlight it. Only available tiles 
				// get highlighted
				highlightTileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
			}
		}
	}
}
