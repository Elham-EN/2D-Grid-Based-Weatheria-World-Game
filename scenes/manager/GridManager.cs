using System.Collections.Generic;
using System.Linq;
using Game.Component;
using Godot;

namespace Game.Manager;

/// <summary>
/// Manages the game's grid-based building system, tracking occupied tiles 
/// and validating placement rules.
/// </summary>
public partial class GridManager : Node
{
	// Cached buildable tiles system that maintains a running list of valid tiles
	// That always knows exactly which grid positions allow new building placement
	private HashSet<Vector2I> validBuildableTiles = new HashSet<Vector2I>();
	[Export]
	private TileMapLayer highlightTileMapLayer;
	// Contain Terrian information: e.g. sand
	[Export]
	private TileMapLayer baseTerrainTileMapLayer;

	// Subscribe to building placement events when GridManager starts up
	public override void _Ready()
	{
		// Connect our handler method to the BuildingPlaced signal 
		// from GameEvents
		GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
	}

	// MOUSE-TO-GRID CONVERSION: Convert mouse pixel position to grid coordinates
	public Vector2I GetMouseGridCellPosition()
	{
		var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();

		var gridPosition = mousePosition / 64;

		gridPosition = gridPosition.Floor();

		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

	// Could a building theoretically be placed here based on terrain?" This method 
	// checks the underlying tilemap data to determine if a tile is suitable for 
	// construction - essentially asking if the terrain type (sand, grass) supports 
	// buildings versus impassable terrain (water, mountains).
	public bool IsTilePositionValid(Vector2I tilePosition)
	{
		// Returns the TileData object associated with the given cell,
		var customData = baseTerrainTileMapLayer.GetCellTileData(tilePosition);
		// If that given grid cell has no custom data, that means it's not buildable
		// So that means you cannot place building the that grid cell.
		if (customData == null) return false;
		// Return if this is buildable
		return (bool)customData.GetCustomData("buildable");
		
	}

    // Can a building be placed here right now given the current game state?" This 
	// method checks if the tile is both valid terrain AND within range of existing 
	// buildings that extend the building area. 
	public bool IsTilePositionBuildable(Vector2I tilePosition)
	{
		return validBuildableTiles.Contains(tilePosition);
	}
    
	// Shows yellow highlight tiles to guide player where they can build
	public void HighlightBuildableTiles()
	{
		// Loop through each cached buildable position
		foreach (var tilePosition in validBuildableTiles)
		{
			// Display yellow tile at this position to show it's available for 
			// building
			highlightTileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
		}
	}

	// This clears the visual feedback showing buildable areas
	public void ClearHighlightedTiles()
	{
		highlightTileMapLayer.Clear();
	}

	// Adds all buildable tiles around a newly placed building to the cache
	private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		// It calculates the grid position of the newly placed building
		var rootCell = buildingComponent.GetGridCellPosition();
		// Retrieves the building's configurable radius
		var radius = buildingComponent.BuildableRadius;
		// Iterates through all grid positions within that radius
		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x, y);
				// Skip tiles with unsuitable terrain (water, rocks) - only check 
				// buildable ground types
				if (!IsTilePositionValid(tilePosition)) continue;
				// Valid positions are added to the cache, extending the player's 
				// buildable area
				validBuildableTiles.Add(tilePosition);
			}
		}
		// Remove the building's tile from buildable cache since buildings can't stack 
		// on each other. This prevents players from placing multiple buildings on the 
		// same grid position by ensuring occupied tiles aren't marked as buildable.
		validBuildableTiles.Remove(buildingComponent.GetGridCellPosition());
	} 
	
	// Event handler: Automatically updates the grid's buildable areas when a 
	// new building is placed
	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpdateValidBuildableTiles(buildingComponent);
	}
}
