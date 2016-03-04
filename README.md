# Unity Plane Mesh Splitter
A simple script which enables you to split a 2D mesh into chunks. Designed to work well with Tiled2Unity, but should work with any kind of 2D mesh. Also my very first public project ever.

# Installation
Put MeshSplit.cs and the Editor folder in the Assets folder in your Unity project.

# Usage
Put the "Mesh Split" component on the game object you want to split and hit the "Split" button.

Requirements:
- Mesh Filter component
- A mesh which only uses 2 axes:
  - x, y
  - x, z
- Mesh Renderer component (optional)
