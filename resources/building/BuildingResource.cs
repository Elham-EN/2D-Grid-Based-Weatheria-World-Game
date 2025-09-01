using Godot;

namespace Game.Resources.Building;

// Allow us to create text file that stores some data that we can use. 
// Create custom resource that has some exported properties that will 
// tell what the building name is?, what the buildable radius? and so on. 
// Use resource only when you have configurable data that you want to use.
[GlobalClass] // Able to create resource from Godot eidtor
public partial class BuildingResource : Resource
{
	[Export]
	public int BuildableRadius { get; private set; }

	[Export]
	public int ResourceRadius { get; private set; }

	[Export]
	public PackedScene BuildingScene { get; private set; }
}
