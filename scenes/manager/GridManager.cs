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

	
	// Checks if a position is valid for building by examining layers from most 
	// visible to least visible. Trees on top override buildable sand underneath 
	// - only the topmost visible tile matters.
	public bool IsTilePositionValid(Vector2I tilePosition)
	{
		// Check each layer in visibility order (topmost first)
		foreach (var layer in allTileMapLayers)
		{
			// Get tile data at this position on current layer
			var customData = layer.GetCellTileData(tilePosition);
			// No tile on this layer - check the layer beneath it
			if (customData == null) continue;
			 // Found a tile! Return its buildable status (trees = false, sand = true)
			return (bool)customData.GetCustomData("buildable");
		}
		// No tiles found on any layer - not buildable
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

	// Shows expansion preview: green tiles for new buildable areas when placing a building
	public void HighlightExpandableBuildableTiles(Vector2I rootcell, int radius)
	{
		// Clear old visuals and redraw current buildable areas as white tiles
		ClearHighlightedTiles();
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
		var radius = buildingComponent.BuildableRadius;
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

	// Iterates through all grid positions within that radius
	private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius)
	{
		var result = new List<Vector2I>();
		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x, y);
				// Skip tiles with unsuitable terrain (water, rocks) - only check 
				// buildable ground types
				if (!IsTilePositionValid(tilePosition)) continue;
				// Only add buildable tile to the list
				result.Add(tilePosition);
			}
		}
		return result;
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
