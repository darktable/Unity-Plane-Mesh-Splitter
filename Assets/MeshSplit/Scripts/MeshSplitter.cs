/* https://github.com/artnas/Unity-Plane-Mesh-Splitter */

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MeshSplit.Scripts.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace MeshSplit.Scripts
{
    public class MeshSplitter
    {
        private static readonly MeshUpdateFlags MeshUpdateFlags = MeshUpdateFlags.DontNotifyMeshUsers 
                                                                  | MeshUpdateFlags.DontValidateIndices 
                                                                  | MeshUpdateFlags.DontRecalculateBounds 
                                                                  | MeshUpdateFlags.DontResetBoneBounds;
        
        private readonly MeshSplitParameters _parameters;
        private Mesh _sourceMesh;
        
        /* Mesh data */
        // private Vector3[] _vertices;
        // private int[] _indices;
        // private List<List<Vector2>> _uvChannels;
        // private Vector3[] _normals;
        // private Color32[] _colors;
        
        private Dictionary<Vector3Int, List<int>> _pointIndicesMap;

        public MeshSplitter(MeshSplitParameters parameters)
        {
            _parameters = parameters;
        }

        public List<(Vector3Int gridPoint, Mesh mesh)> Split(Mesh mesh)
        {
            _sourceMesh = mesh;

            var vertexAttributes = _sourceMesh.GetVertexAttributes();
            foreach (var vertexAttribute in vertexAttributes)
            {
                Debug.Log(vertexAttribute);
            }

            PerformanceMonitor.Start("CreatePointIndicesMap");
            CreatePointIndicesMap();
            PerformanceMonitor.Stop("CreatePointIndicesMap");

            PerformanceMonitor.Start("CreateChildMeshes");
            var childMeshes = CreateChildMeshes();
            PerformanceMonitor.Stop("CreateChildMeshes");
            
            return childMeshes;
        }

        private void CreatePointIndicesMap()
        {
            // Create a list of triangle indices from our mesh for every grid node
            _pointIndicesMap = new Dictionary<Vector3Int, List<int>>();

            var meshIndices = _sourceMesh.triangles;
            var meshVertices = _sourceMesh.vertices;

            for (var i = 0; i < meshIndices.Length; i += 3)
            {
                // middle of the current triangle (average of its 3 verts).
                var currentPoint = (meshVertices[meshIndices[i]] + meshVertices[meshIndices[i + 1]] + meshVertices[meshIndices[i + 2]]) / 3;

                // calculate coordinates of the closest grid node.
                // ignore an axis (set it to 0) if its not enabled
                var gridPos = new Vector3Int(
                    _parameters.SplitAxes.x ? Mathf.RoundToInt(Mathf.Round(currentPoint.x / _parameters.GridSize) * _parameters.GridSize) : 0,
                    _parameters.SplitAxes.y ? Mathf.RoundToInt(Mathf.Round(currentPoint.y / _parameters.GridSize) * _parameters.GridSize) : 0,
                    _parameters.SplitAxes.z ? Mathf.RoundToInt(Mathf.Round(currentPoint.z / _parameters.GridSize) * _parameters.GridSize) : 0
                );

                // check if the dictionary has a key (our grid position). Add it / create a list for it if it doesnt.
                if (!_pointIndicesMap.ContainsKey(gridPos))
                {
                    _pointIndicesMap.Add(gridPos, new List<int>());
                }

                // add these triangle indices to the list
                _pointIndicesMap[gridPos].Add(meshIndices[i]);
                _pointIndicesMap[gridPos].Add(meshIndices[i + 1]);
                _pointIndicesMap[gridPos].Add(meshIndices[i + 2]);
            }
        }

        private List<(Vector3Int gridPoint, Mesh mesh)> CreateChildMeshes()
        {
            var subMeshBuilder = new SubMeshBuilder(_pointIndicesMap);
            var meshDataArray = subMeshBuilder.Build(_sourceMesh, _parameters);

            var meshes = new List<Mesh>(meshDataArray.Length);
            var gridPoints = _pointIndicesMap.Keys.ToArray();
            var gridSize = new Vector3(_parameters.GridSize, _parameters.GridSize, _parameters.GridSize);

            foreach (var gridPoint in gridPoints)
            {
                meshes.Add(new Mesh
                {
                    name = $"SubMesh {gridPoint}",
                    bounds = new Bounds(gridPoint, gridSize)
                });
            }

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, meshes, MeshUpdateFlags);

            foreach (var mesh in meshes)
            {
                Ayy(mesh);
                mesh.RecalculateBounds(MeshUpdateFlags);
            }

            return new List<(Vector3Int gridPoint, Mesh mesh)>(gridPoints.Zip(meshes, (point, mesh) => (point, mesh)));
        }

        // private unsafe void Ayy(Mesh mesh)
        // {
        //     var nativeBuffer = mesh.GetNativeVertexBufferPtr(0);
        //
        //     var stride = mesh.GetVertexBufferStride(0);
        //     var vertices = mesh.vertexCount;
        //
        //     Debug.Log($"vertices: {vertices}, stride: {stride}, data size: {vertices * stride}");
        //     
        //     var pointer = nativeBuffer.ToPointer(); //pointer to start of row
        //
        //     var buffer = mesh.GetVertexBuffer(0);
        //
        //     var bytes = new byte[stride * vertices];
        //     buffer.GetData(bytes);
        //     
        //     // var bytes = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(pointer, vertices * stride, Allocator.Temp);
        //     Debug.Log(bytes.Length);
        //
        //     // for (var i = 0; i < bytes.Length; i++)
        //     // {
        //     //     Debug.Log(bytes[i]);
        //     // }
        //     
        //     // mesh.SetVertexBufferParams(vertices, letterMesh.GetVertexAttributes());
        //     mesh.SetVertexBufferData(bytes, 0, 0, vertices * stride, 0, MeshUpdateFlags);
        //     
        //     buffer.Dispose();
        //     
        //     // for (var i = 0; i < vertices * stride; i++)
        //     // {
        //     //     pointer[i] = 0;
        //     // }
        //     // for (int c = 0; c < w; c++)
        //     // {
        //     //     pointer[c * ch] = 0; //red
        //     //     pointer[c * ch + 1] = 0; //green
        //     //     pointer[c * ch + 2] = 0; //blue
        //     //
        //     //     //also equivalent to *(pI + c*ch)  = 0 - i.e. using pointer arythmetic;
        //     // }
        // }
    }
}