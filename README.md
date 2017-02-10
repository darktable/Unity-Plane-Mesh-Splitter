# Unity Plane Mesh Splitter
A simple script which let's you split any mesh into smaller chunks. At first it was designed to work with imported Tiled2Unity terrains, but I rewrote it to work with everything you can throw at it. This is my very first public project ever.

# Features
- Very simple.
- Customizable grid size.
- You can choose which axes you want to use. Any combination should work.
- UVs and Normals work.
- Doesn't modify the existing mesh.

![alt tag](http://i.imgur.com/86I7Apw.png)

# Installation
Drag the folder containing MeshSplit.cs and Editor folder to your Assets folder.

# Usage
Put the "Mesh Split" component on the game object you want to split and press the "Split" button. Press "Clear" to revert.

Requirements:
- Mesh Filter component
