using Godot;

namespace Game;

// GAME OVERVIEW:
// This creates a grid-based building placement system - like tower defense games
// Player moves mouse -> cursor snaps to grid -> click to place buildings
// Core mechanics: cursor tracking + building placement + grid alignment

public partial class Main : Node2D
{
    // GAME OBJECTS WE NEED TO CONTROL:
    // Visual cursor that shows where we'll place buildings
    private Sprite2D sprite;
    // Template/blueprint for creating new buildings
    private PackedScene buildingScene;

    // INITIALIZATION: Set up everything before the game starts
    public override void _Ready()
    {
        // Load our building template from the file system
        // This is like loading a "cookie cutter" that we can use to make identical buildings
        buildingScene = GD.Load<PackedScene>("res://scenes/building/Building.tscn");
        // Find and connect to our cursor (node) sprite in the scene from scene tree 
        // Now our code can control where the cursor appears on screen
        sprite = GetNode<Sprite2D>("Cursor");
    }

    // INPUT HANDLING: Detect when player wants to place a building
    public override void _UnhandledInput(InputEvent evt)
    {
        // Listen for left mouse clicks
        // When player clicks, they want to place a building at the cursor location
        if (evt.IsActionPressed("left_click"))
        {
            PlaceBuildingAtMousePosition();
        }
    }

    // CONTINUOUS UPDATES: Keep cursor aligned to grid every frame (60 times per second)
    public override void _Process(double delta)
    {
        // Step 1: Figure out which grid cell the mouse is currently over
        var gridPosition = GetMouseGridCellPosition();
        // Step 2: Move our visual cursor to that exact grid cell
        // This creates the "snapping" effect - cursor jumps from cell to cell
        // Math: Convert grid coordinates back to pixel coordinates (multiply by cell size)
        sprite.GlobalPosition = gridPosition * 64;
    }

    // MOUSE-TO-GRID CONVERSION: Convert mouse pixel position to grid coordinates
    private Vector2 GetMouseGridCellPosition()
    {
        // Step 1: Get exact mouse position in pixels (like 250, 180)
        // This tells us where the mouse cursor is in the game world
        var mousePosition = GetGlobalMousePosition();
        // Step 2: Convert pixels to grid coordinates
        // Divide by 64 because each grid cell is 64x64 pixels
        // Our world is divided into 64×64 pixel squares. This math tells us 
        // "which grid square is the mouse in?" For example, pixel (250, 180) 
        // becomes grid (3.9, 2.8).
        var gridPosition = mousePosition / 64;
        // Snap to Whole Grid Numbers: (3.9, 2.8) becomes (3, 2)
        // Floor() rounds down: (3.9, 2.8) becomes (3, 2)
        // This ensures we always select a complete grid cell, not a partial one
        gridPosition = gridPosition.Floor();
        return gridPosition;
    }
    // BUILDING PLACEMENT: Create a new building at the mouse position
    // Result: Building appears exactly where the cursor was showing it 
    // would be placed
    private void PlaceBuildingAtMousePosition()
    {
        // Step 1: Create a new building from our template
        // This is like using a cookie cutter to make a new cookie
        var building = buildingScene.Instantiate<Node2D>();
        // Step 2: Add the building to our game world
        // Now it exists in the scene and will be drawn on screen
        AddChild(building);
        // Step 3: Position the building at the correct grid cell
        // Get current mouse grid position and convert to pixel coordinates
        var gridPosition = GetMouseGridCellPosition();
        building.GlobalPosition = gridPosition * 64;
    }
}




