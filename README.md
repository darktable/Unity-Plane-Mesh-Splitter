# Unity Plane Mesh Splitter

A simple tool which lets you split any mesh into smaller submeshes. At first it was designed to work with imported Tiled2Unity terrains, but I rewrote it to work with everything you can throw at it.

### What is the purpose of this tool?

Say you have a gigantic terrain in a single mesh. Unity is going to process the entire mesh when rendering it (even though only a small section in front of the camera is visible). This tool lets your split this large mesh into smaller submeshes which should greatly improve the performance thanks to the built-in Unity frustum culling (only visible meshes will be rendered).

### Features

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
- Submeshes persist when saving the scene.

![alt tag](http://i.imgur.com/5PzoVFc.jpg)

# Installation

You have two options
- Download the .unitypackage from 'releases' section and import it in Unity.
- Clone this repository and put the scripts into your assets folder.

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

