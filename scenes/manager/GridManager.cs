using System;
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
	private const string IS_BUILDABLE = "is_buildable";
	private const string IS_WOOD = "is_wood";
	// Cached buildable tiles system that maintains a running list of valid tiles
	// That always knows exactly which grid positions allow new building placement
	private HashSet<Vector2I> validBuildableTiles = new HashSet<Vector2I>();
	// Visual layer that displays white highlight tiles to show buildable areas
	[Export]
	private TileMapLayer highlightTileMapLayer;
	// Contain Terrian information: e.g. sand
	[Export]
	private TileMapLayer baseTerrainTileMapLayer;

	private List<TileMapLayer> allTileMapLayers = [];

	// Subscribe to building placement events when GridManager starts up
	public override void _Ready()
	{
		// Connect our handler method to the BuildingPlaced signal 
		// from GameEvents
		GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;

		allTileMapLayers = GetAllTileMapLayers(baseTerrainTileMapLayer);
	}

	// MOUSE-TO-GRID CONVERSION: Convert mouse pixel position to grid coordinates
	public Vector2I GetMouseGridCellPosition()
	{
		var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();

		var gridPosition = mousePosition / 64;

		gridPosition = gridPosition.Floor();

		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

	
	/// <summary>
	/// Checks if a tile has specific custom data by examining layers from top 
	/// to bottom. Uses depth-first search to find what the player actually sees 
	/// (trees override sand underneath).
	/// </summary>
	public bool TileHasCustomData(Vector2I tilePosition, string dataName)
	{
		// Check each tilemap layer starting with the most visible (trees)
		foreach (var layer in allTileMapLayers)
		{
			 // Get tile information at this position on the current layer
			var customData = layer.GetCellTileData(tilePosition);
			// No tile exists on this layer - check the next layer down
			if (customData == null) continue;
			// Found a tile! Return its custom data value (buildable, wood, etc.)
			return (bool)customData.GetCustomData(dataName);
		}
		// No tiles found on any layer - return false by default
		return false;
	}

    // Can a building be placed here right now given the current game state?" This 
	// method checks if the tile is both valid terrain AND within range of existing 
	// buildings that extend the building area. 
	public bool IsTilePositionBuildable(Vector2I tilePosition)
	{
		return validBuildableTiles.Contains(tilePosition);
	}
    
	// Shows white highlight tiles to guide player where they can build
	public void HighlightBuildableTiles()
	{
		// Loop through each cached buildable position
		foreach (var tilePosition in validBuildableTiles)
		{
			// Display white tile at this position to show it's available for 
			// building
			highlightTileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
		}
	}

	/// <summary>
	/// Shows green highlight tiles around a village to indicate which trees 
	/// can be harvested.
	/// </summary>
	public void HighlightResourceTiles(Vector2I rootCell, int radius)
	{
		// Find all wood tiles within the village's harvesting range
		var resourceTiles = GetResourceTilesInRadius(rootCell, radius);
		// Set up green highlight color (Green Tile)
		var atlasCoords = new Vector2I(1, 0);
		// Draw green highlight on each tree tile that can be harvested
		foreach (var tilePosition in resourceTiles)
		{
			// Place green highlight tile at this position to show it's harvestable
			highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}

	// Shows expansion preview: green tiles for new buildable areas when placing a building
	public void HighlightExpandableBuildableTiles(Vector2I rootcell, int radius)
	{
		// Draw existing white tiles first
		HighlightBuildableTiles();
		// Get all tiles within radius that have valid terrain
		var validTiles = GetValidTilesInRadius(rootcell, radius).ToHashSet();
		// Show only NEW expansion - exclude already buildable AND occupied tiles
		var expandedTiles = validTiles.Except(validBuildableTiles).Except(GetOccupiedTiles());
		// Draw green tiles to show new areas that will become buildable
		var atlasCoords = new Vector2I(1, 0);
		// Loop through each expandable tiles position:
		foreach (var tilePosition in expandedTiles)
		{
			// Draw green expansion preview
			highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}

	// This clears the visual feedback showing buildable areas
	public void ClearHighlightedTiles()
	{
		highlightTileMapLayer.Clear();
	}

	// Adds all buildable tile within around a newly placed building to the cache
	private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		// It calculates the grid position of the newly placed building
		var rootCell = buildingComponent.GetGridCellPosition();
		// Retrieves the building's configurable radius
		var radius = buildingComponent.BuildingResource.BuildableRadius;
		var validTiles = GetValidTilesInRadius(rootCell, radius);
		// Valid positions are added to the cache, extending the player's 
		// buildable area
		validBuildableTiles.UnionWith(validTiles);
		// Remove every occupiedTiles. This prevents players from placing multiple 
		// buildings on the  same grid position by ensuring occupied tiles aren't 
		// marked as buildable.
		validBuildableTiles.ExceptWith(GetOccupiedTiles());
	}

	private IEnumerable<Vector2I> GetOccupiedTiles()
	{
		// Get all the buildings that have been currently placed in the game world
		var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent))
			.Cast<BuildingComponent>();

		// Get grid cell position of all building that have been currently placed
		// Gets their positions so we know which tiles are occupied
		var occupiedTiles = buildingComponents.Select(x => x.GetGridCellPosition());

		return occupiedTiles;
	}

	/// <summary>
	/// Generic method that finds all tiles (grid cell position) within a square radius 
	/// that pass a custom filter test. This method accepts a function as a parameter 
	/// & takes a Vector2I and returns a bool
	/// </summary>
	/// <param name="rootCell">Center position to search around</param>
	/// <param name="radius">Distance in tiles to search in all directions </param>
	/// <param name="filterFn">Function that determines which tiles to include</param>
	/// <returns>List of tile positions that passed the filter test</returns>
	private List<Vector2I> GetTilesInRadius(Vector2I rootCell, int radius,
		Func<Vector2I, bool> filterFn)
	{
		// Initialize empty collection to store tiles that pass the filter test
		var result = new List<Vector2I>();
		// Loop through X coordinates from left to right of search area
		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			// Loop through Y coordinates from top to bottom of search area
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				// Create tile position from current loop coordinates
				var tilePosition = new Vector2I(x, y);
				// If filter returns false, skip this tile and continue to next 
				// iteration
				if (!filterFn(tilePosition)) continue;
				// Filter test passed - add this tile position to results collection
				result.Add(tilePosition);
			}
		}
		// Return all tiles that passed the filter
		return result;
	}

	/// <summary>
	/// Finds all buildable tiles within the specified radius using the generic tile 
	/// search method. Filters for tiles marked with IS_BUILDABLE custom data.
	/// </summary>
	/// <param name="rootCell">Center position to search from</param>
	/// <param name="radius">Search distance in tiles</param>
	/// <returns>List of buildable tile positions</returns>
	private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius)
	{
		return GetTilesInRadius(rootCell, radius, (tilePosition) =>
		{
			// Return true only if this tile has IS_BUILDABLE custom data
			return TileHasCustomData(tilePosition, IS_BUILDABLE);
		});
	}

	/// <summary>
	/// Finds all wood resource tiles within the specified radius for village harvesting.
	/// Uses the same generic search pattern but filters specifically for wood resources.
	/// </summary>
	/// <param name="rootCell">Village position to search from</param>
	/// <param name="radius">Harvesting range in tiles</param>
	/// <returns>List of harvestable wood tile positions</returns>
	private List<Vector2I> GetResourceTilesInRadius(Vector2I rootCell, int radius)
	{
		return GetTilesInRadius(rootCell, radius, (tilePosition) =>
		{
			return TileHasCustomData(tilePosition, IS_WOOD);
		});
	}


	// Recursively collects all TileMapLayer nodes in depth-first order with most 
	// visible layers first. Ensures buildability checks examine what players see 
	// (trees) before hidden layers (sand underneath).
	private List<TileMapLayer> GetAllTileMapLayers(TileMapLayer rootTileMapLayer)
	{
		// Store all tilemap layers in visibility priority order
		var result = new List<TileMapLayer>();
		// Get child nodes of current layer
		var children = rootTileMapLayer.GetChildren();
		// Reverse order - Godot renders later children on top, we need topmost 
		// layers first
		children.Reverse();
		// Process each child tilemap layer
		foreach (var child in children)
		{
			// Only handle TileMapLayer nodes
			if (child is TileMapLayer childLayer)
			{
				// Recursively collect all layers from child's subtree first 
				// (depth-first)
				result.AddRange(GetAllTileMapLayers(childLayer));
			}
		}
		// Add current layer after all its children (children render on top)
		result.Add(rootTileMapLayer);
		return result;
	}
	
	// Event handler: Automatically updates the grid's buildable areas when a 
	// new building is placed
	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpdateValidBuildableTiles(buildingComponent);
	}
}
