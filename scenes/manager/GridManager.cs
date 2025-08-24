using System.Collections.Generic;
using Godot;

namespace Game.Manager;

// GridManager handles all grid-related operations
public partial class GridManager : Node
{
	// Tracks which grid positions already have buildings on them, preventing 
	// players from placing multiple buildings in the same location
	// This stores grid coordinates like (3, 2), (5, 7), etc. - each representing 
	// one occupied grid cell. Each grid cell can only hold one building
	private HashSet<Vector2> occupiedCells = new HashSet<Vector2>();
	[Export]
	private TileMapLayer highlightTileMapLayer;
	// Contain Terrian information: e.g. sand
	[Export]
	private TileMapLayer baseTerrainTileMapLayer;

	public override void _Ready()
	{

	}

	// MOUSE-TO-GRID CONVERSION: Convert mouse pixel position to grid coordinates
	public Vector2 GetMouseGridCellPosition()
	{
		var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();

		var gridPosition = mousePosition / 64;

		gridPosition = gridPosition.Floor();

		return gridPosition;
	}

	// Checks if a position is available for building & Returns 
	// true if the position is NOT in the occupied set
	// When called: Before allowing building placement
	public bool IsTilePositionValid(Vector2 tilePosition)
	{
		return !occupiedCells.Contains(tilePosition);
	}

	// Records that a position now has a building & 
	// When called: After successfully placing a building
	public void MarkTileAsOccupied(Vector2 tilePosition)
	{
		occupiedCells.Add(tilePosition);
	}

	public void HighlightValidTilesInRadius(Vector2 rootCell, int radius)
	{
		ClearHighlightedTiles();
        // Loop through all tiles in the radius
		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				// If a tile is NOT valid (occupied) → continue (skip to next iteration)
				//  This gives players clear indication of where they can place buildings 
				// within the radius, while occupied spots remain unhighlighted.
				if (!IsTilePositionValid(new Vector2(x, y))) continue;
				// If a tile IS valid (available) → highlight it. Only available tiles 
				// get highlighted
				highlightTileMapLayer.SetCell(
					new Vector2I((int)x, (int)y), 0, Vector2I.Zero);
			}
		}
	}

	public void ClearHighlightedTiles()
	{
		highlightTileMapLayer.Clear();
	}
}
