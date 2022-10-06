# Unity Plane Mesh Splitter

## Description

A tool which lets you split any mesh into smaller submeshes. At first it was designed to work with imported Tiled2Unity terrains, but I rewrote it to work with everything you can throw at it. 

In 2022 I rewrote it again to improve it and solve some problems. I used the new mesh building API and simplified the vertex data copying process - it now copies data directly from the meshes vertex data buffer, retaining the same vertex data structure as the original mesh. Usage of burst compiler allowed me to greatly improve performance and spread work across multiple threads.

### What is the purpose of this tool?

Say you have a gigantic terrain in a single mesh. Unity is going to process the entire mesh when rendering it even though only a small section in front of the camera is visible. 

This tool lets your split this large mesh into smaller submeshes which should greatly improve the performance thanks to the built-in Unity frustum culling (only visible meshes will be rendered).

## Installation

Install this tool using Unity Package Manager:
https://github.com/artnas/Unity-Plane-Mesh-Splitter.git

![image](https://user-images.githubusercontent.com/14143603/194191506-c25bcf37-284c-471f-8097-7e6049f7ed31.png)

![image](https://user-images.githubusercontent.com/14143603/194191568-492678bb-00b3-4cab-9b5a-507bb20e202f.png)

for versions compatible with older versions of unity go to [Releases](https://github.com/artnas/Unity-Plane-Mesh-Splitter/releases)

### Compability

- Release version 1.0 is compatible with older versions of Unity.
- New releases work with Unity 2021.2 and above

### Features

- Simple to use
- Customization:
  - Grid size
  - Multiple axes (in any combination)
  - Generate convex/non convex colliders
- Nice performance
  - Parallelized burst code
  - Pointers and memcpy
- Maintains exact original mesh vertex data format
- Automatic 16/32 bit indexing based on vertex count
- Doesn't modify the existing mesh.
- Can be used both in editor and at runtime.
- Submeshes persist when saving the scene.

## Usage 

### MeshSplitController component

Add the "MeshSplitController" component to the game object you want to split and press the "Create submeshes" button. Press "Clear submeshes" to revert.

### API

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

![alt tag](http://i.imgur.com/5PzoVFc.jpg)

# TODO

- Support for submeshes with various materials
- Align the gizmo grid with split planes
