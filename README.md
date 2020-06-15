# Unity Plane Mesh Splitter

A simple script which lets you split any mesh into smaller submeshes. At first it was designed to work with imported Tiled2Unity terrains, but I rewrote it to work with everything you can throw at it.

# Features

- Simple and fairly fast.
- Customization:
  - Grid size
  - Multiple axes (in any combination)
  - Ability to generate colliders
  - ...
- Supports all vertex data:
  - Normals
  - Colors
  - Multiple uv channels
- Doesn't modify the existing mesh.
- Can be used both in editor and at runtime.

![alt tag](http://i.imgur.com/5PzoVFc.jpg)

# Usage - MeshSplitController component

Put the "MeshSplitController" component on the game object you want to split and press the "Create submeshes" button. Press "Clear submeshes" to revert.

# Usage - API

// your mesh

Mesh mesh;
            
// create a mesh splitter with some parameters (see MeshSplitParameters.cs for default settings)

var meshSplitter = new MeshSplitter(new MeshSplitParameters
{
    GridSize = 32,
    GenerateColliders = true
});

// create submeshes assigned to points

var subMeshes = meshSplitter.Split(mesh);

