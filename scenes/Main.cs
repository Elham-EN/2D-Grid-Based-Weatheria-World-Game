using Godot;

namespace Game;

// GAME OVERVIEW:
// This creates a grid-based building placement system - like tower defense games
// Player moves mouse -> cursor snaps to grid -> click to place buildings
// Core mechanics: cursor tracking + building placement + grid alignment

public partial class Main : Node2D
{
    // GAME OBJECTS WE NEED TO CONTROL:

    // The primary placement cursor - a visual indicator that shows exactly where a building 
    // will be placed. This sprite follows the mouse but snaps to grid boundaries to maintain 
    // visual consistency
    private Sprite2D cursor;
    // A template/blueprint for building objects that serves as our factory pattern implementation. 
    // This scene file contains all the components a building needs (sprite, collision, scripts) 
    // pre-configured and ready for instantiation
    private PackedScene buildingScene;
    // The user interface element that initiates placement mode.
    private Button placeBuildingButton;
    // A specialized rendering layer designed specifically for drawing 
    // temporary highlight effects.
    private TileMapLayer highlightTileMapLayer;

    // STATE MANAGEMENT VARIABLES: These track the current state of our placement system

    // Tracks which grid cell the mouse is currently positioned over.
    // Null indicate that there is no currently hovered grid cell
    private Vector2? hoveredGridCell;


    // INITIALIZATION: Set up everything before the game starts
    public override void _Ready()
    {
        // RESOURCE LOADING: Load our building template from the filesystem into memory
        // This operation reads the .tscn file (which is actually a serialized scene graph)
        // and prepares it for rapid instantiation during gameplay. Loading happens once
        // at startup to avoid performance hitches during placement operations
        buildingScene = GD.Load<PackedScene>("res://scenes/building/Building.tscn");

        // Node References: Establish connections to the visual elementscthat exist in our 
        // scene tree.
        cursor = GetNode<Sprite2D>("Cursor");
        placeBuildingButton = GetNode<Button>("PlaceBuildingButton");
        highlightTileMapLayer = GetNode<TileMapLayer>("HighlightTileMapLayer");

        // INITIAL STATE CONFIGURATION: Set up the starting conditions for our system
        // We begin where no placement operations are active, which means the cursor should 
        // be hidden and no range previews should be displayed
        cursor.Visible = false;

        // EVENT HANDLER REGISTRATION: Connect our custom method to the button's signal
        // This creates a callback relationship where our OnButtonPressed method will
        // be automatically invoked whenever the button detects a press event
        placeBuildingButton.Pressed += OnButtonPressed;
    }

    // INPUT EVENT PROCESSING: This method handles discrete input events and specifically 
    // responds to mouse click events when certain conditions are met.
    public override void _UnhandledInput(InputEvent evt)
    {
        // CONDITIONAL PLACEMENT EXECUTION: We only process click events when the
        // system is currently in placement mode (indicated by cursor visibility)
        if (cursor.Visible && evt.IsActionPressed("left_click"))
        {
            // BUILDING INSTANTIATION AND PLACEMENT: Execute the core placement logic
            // that creates a new building at the current cursor position
            PlaceBuildingAtMousePosition();
            // STATE TRANSITION: Exit placement mode by hiding the cursor,
            cursor.Visible = false;
        }
    }

    // CONTINUOUS UPDATE LOOP: This method executes every frame (typically 60 times
    // per second) and handles all the real-time updates that need to happen while
    // the game is running.
    public override void _Process(double delta)
    {
        // REAL-TIME CURSOR POSITIONING: Calculate the current grid position based
        // on mouse location and update the cursor position accordingly. This happens
        // every frame to ensure the cursor always appears at the correct location
        // even as the mouse moves continuously across the screen
        var gridPosition = GetMouseGridCellPosition();
        cursor.GlobalPosition = gridPosition * 64;

        // Update operation if we're currently placing a building and either the hovered
        // grid cell is null OR the value set for hovered grid cell is not equal to the
        // current grid position.

        // Only updates the range preview when necessary, rather than redrawing it every 
        // single frame. Are we in placement mode? AND Is this the first time we're 
        // hovering over any cell? Initial state requires setup or Has the mouse moved to 
        // a different grid cell since last frame? Only update when position actually changes
        if (cursor.Visible &&
            (!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
        {
            // STATE UPDATE: Record the new grid position for future comparison
            // This allows us to detect movement in subsequent frames
            hoveredGridCell = gridPosition;
            // VISUAL REFRESH: Trigger a complete redraw of the highlight system
            // to reflect the new cursor position and range preview
            UpdateHighlightTileMapLayer();
        }
    }

    // MOUSE-TO-GRID CONVERSION: Convert mouse pixel position to grid coordinates
    private Vector2 GetMouseGridCellPosition()
    {
        var mousePosition = GetGlobalMousePosition();
        var gridPosition = mousePosition / 64;
        gridPosition = gridPosition.Floor();
        return gridPosition;
    }
    // BUILDING PLACEMENT: Create a new building at the mouse position
    private void PlaceBuildingAtMousePosition()
    {
        var building = buildingScene.Instantiate<Node2D>();
        AddChild(building);
        var gridPosition = GetMouseGridCellPosition();
        building.GlobalPosition = gridPosition * 64;
        // STATE CLEANUP: Clear the hover tracking state and update the
        // visual highlights to reflect that placement mode is ending. This prevents
        // the range preview from persisting after placement is complete
        hoveredGridCell = null;
        UpdateHighlightTileMapLayer();
    }
    // This method draw and update the range preview that helps players understand 
    // the strategic implications of their building placement.
    private void UpdateHighlightTileMapLayer()
    {
        // CLEANUP PHASE: Remove all existing highlight tiles from the
        // tilemap layer. This ensures we start with a clean slate and prevents
        // visual artifacts from previous highlight operations.
        highlightTileMapLayer.Clear();
        // If we're not currently hovering over any valid grid cell, there's nothing 
        // to highlight, so we can exit early.
        if (!hoveredGridCell.HasValue)
        {
            return;
        }
        // OUTER LOOP - COLUMN ITERATION: Process each vertical column from left to right
        // Starting 3 cells to the left of center and ending 3 cells to the right
        // This creates the horizontal span of our 7-cell-wide highlight area
        for (var x = hoveredGridCell.Value.X - 3; x <= hoveredGridCell.Value.X + 3; x++)
        {
            // INNER LOOP - ROW ITERATION: For each column, process every row from
            // top to bottom. Starting 3 cells above center and ending 3 cells below
            // This creates the vertical span of our 7-cell-tall highlight area
            for (var y = hoveredGridCell.Value.Y - 3; y <= hoveredGridCell.Value.Y + 3; y++)
            {
                // TILE PLACEMENT OPERATION: Draw a single highlight tile at the
                // current grid position. The SetCell method efficiently places a
                // tile from our configured tileset at the specified coordinates
                highlightTileMapLayer.SetCell(
                    new Vector2I((int)x, (int)y), 0, Vector2I.Zero);
            }
        }
    }

    // USER INTERFACE EVENT HANDLER: This method responds to button press events
    // and serves as the entry point for initiating building placement mode.
    public void OnButtonPressed()
    {
        cursor.Visible = true;
    }
}

// Programmatically:
// Update the tileMapLayer with the white square tile that we configured in godot
// based on a radius around the building.
// How are we going to do that?
// We need to detect when the mouse has hovered over a new grid cell while a
// building is actively being placed and everytime the grid cell changes, we need
// to clear the tile map and redraw all of the tiles to update for the new position