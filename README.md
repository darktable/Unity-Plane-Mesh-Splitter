# Unity Plane Mesh Splitter

#### [Unity package](https://github.com/artnas/Unity-Plane-Mesh-Splitter/releases) now available.

A simple tool which lets you split any mesh into smaller submeshes. At first it was designed to work with imported Tiled2Unity terrains, but I rewrote it to work with everything you can throw at it. 

In 2022 I rewrote it again to work much faster, thanks to parallel burst compiled code. It also uses the exact vertex data format as the original mesh to prevent discrepancies.

### What is the purpose of this tool?

Say you have a gigantic terrain in a single mesh. Unity is going to process the entire mesh when rendering it even though only a small section in front of the camera is visible. This tool lets your split this large mesh into smaller submeshes which should greatly improve the performance thanks to the built-in Unity frustum culling (only visible meshes will be rendered).

### Features

- Simple and very fast thanks to burst compiled code.
- Customization:
  - Grid size
  - Multiple axes (in any combination)
  - Ability to generate colliders
- Keeps original mesh vertex data format
  - Automatically uses 16 or 32 bit indexing based on vertex count 
- Doesn't modify the existing mesh.
- Can be used both in editor and at runtime.
- Submeshes persist when saving the scene.

![alt tag](http://i.imgur.com/5PzoVFc.jpg)

# Installation

You have two options
- Download the .unitypackage from 'releases' section and import it in Unity.
- Clone this repository and put the scripts into your assets folder.

# Usage - MeshSplitController component

Add the "MeshSplitController" component to the game object you want to split and press the "Create submeshes" button. Press "Clear submeshes" to revert.

# Usage - API

```csharp
// mesh to split
Mesh mesh;
            
// create a mesh splitter with some parameters (see MeshSplitParameters.cs for default settings)
var meshSplitter = new MeshSplitter(new MeshSplitParameters
{
    GridSize = 32,
    GenerateColliders = true
});

// split mesh into submeshes assigned to points
var subMeshes = meshSplitter.Split(mesh);
```
