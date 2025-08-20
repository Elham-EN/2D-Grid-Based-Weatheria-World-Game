using Godot;

namespace Game;

// What This Game Does
// This creates a cursor that snaps to a grid - like in 
// strategy games where you select tiles or place buildings.
// This is the foundation for a grid-based strategy or building game - similar to:
// Tower defense games (like Plants vs. Zombies)

public partial class Main : Node2D
{
    // Reference our visual cursor
    private Sprite2D sprite;
    // Perfect for initialization code that should run once
    // Set up initial values, connect signals, or prepare your object
    public override void _Ready()
    {
        // When the game starts, search through the scene tree to find a 
        // specific node named "Cursor" in our scene and connect it to our code.
        // Returns a reference to that node so you can use it
        sprite = GetNode<Sprite2D>("Cursor");
    }

    // Every Frame Updates: We want the cursor to follow the mouse constantly, 
    // so we check every frame.
    public override void _Process(double delta)
    {
        // How to get the grid cell that the mouse is currently over?

        // 1.First get the mouse position in the world. This method
        // return a vector that contain X & Y Position Vector2(250, 180)
        // Gets the exact pixel coordinates where the mouse cursor is located
        var mousePosition = GetGlobalMousePosition();
        // Convert to Grid Space:
        // 2.Take the pixel position and divide by 64 to convert it to grid 
        // coordinates.
        // Our world is divided into 64×64 pixel squares. This math tells us 
        // "which grid square is the mouse in?" For example, pixel (250, 180) 
        // becomes grid (3.9, 2.8).
        var gridPosition = mousePosition / 64;
        // Snap to Whole Grid Numbers: (3.9, 2.8) becomes (3, 2)
        // Rounds DOWN to the nearest whole number
        // We don't want partial grid positions. The cursor should be in grid 
        // cell (3, 2), not floating between cells.
        gridPosition = gridPosition.Floor();
        // Move Cursor(Sprite2D) to Grid Position:
        // Convert grid coordinates back to pixels and move our cursor sprite there
        //  Now that we know which grid cell the mouse is over, we move our cursor 
        // sprite to the exact center of that grid cell. Grid (3, 2) becomes 
        // pixel (192, 128).
        sprite.GlobalPosition = gridPosition * 64;
    }
}

// Now we have:
// A cursor that follows your mouse
// The cursor snaps perfectly to a grid system
