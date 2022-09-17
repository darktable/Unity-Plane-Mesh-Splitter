using System;
using System.Collections.Generic;
using System.Linq;
using MeshSplit.Scripts.Models;
using MeshSplit.Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeshSplit.Scripts
{
    public class SubMeshBuilder
    {
        private Dictionary<Vector3Int, List<int>> _pointIndices;

        public SubMeshBuilder(Dictionary<Vector3Int, List<int>> pointIndices)
        {
            _pointIndices = pointIndices;
        }

        private (NativeList<int> allIndices, NativeList<int2> indexRangesArray) FlattenPointIndices()
        {
            var allIndices = new NativeList<int>(100, Allocator.Persistent);
            var ranges = new NativeList<int2>(100, Allocator.Persistent);
            
            foreach (var entry in _pointIndices)
            {
                var gridPointIndices = new NativeArray<int>(entry.Value.ToArray(), Allocator.Temp);
                
                ranges.Add(new int2(allIndices.Length, gridPointIndices.Length));
                
                allIndices.AddRange(gridPointIndices);

                gridPointIndices.Dispose();
            }

            return (allIndices, ranges);
        }
        
        public Mesh.MeshDataArray Build(Mesh mesh, MeshSplitParameters splitParameters)
        {
            var gridPoints = new NativeArray<Vector3Int>(_pointIndices.Keys.ToArray(), Allocator.Persistent);

            (NativeList<int> allIndices, NativeList<int2> indexRangesArray) = FlattenPointIndices();
            
            var meshDataArray = Mesh.AllocateWritableMeshData(_pointIndices.Count);

            var sourceMeshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);

            var vertexAttributes = GetVertexAttributes();
            
            for (var i = 0; i < sourceMeshDataArray.Length; i++)
            {
                // get mesh data arrays
                GetMeshData(mesh, 
                    out var vertices, out var normals, out var colors, 
                    out var uv0, out var uv1, 
                    out var uv2, out var uv3, 
                    out var uv4, out var uv5, 
                    out var uv6, out var uv7);
                
                // create parallel job
                var buildJob = new BuildSubMeshJob
                {
                    SourceSubMeshIndex = i,
                    TargetMeshDataArray = meshDataArray,
                    UvChannels = splitParameters.UvChannels,
                    VertexAttributes = vertexAttributes,
                    UseVertexNormals = splitParameters.UseVertexNormals,
                    UseVertexColors = splitParameters.UseVertexColors,
                    AllIndices = allIndices,
                    IndexRanges = indexRangesArray,
                    Vertices = vertices,
                    Normals = normals,
                    Colors = colors,
                    Uv0 = uv0, Uv1 = uv1, Uv2 = uv2, Uv3 = uv3, Uv4 = uv4, Uv5 = uv5, Uv6 = uv6, Uv7 = uv7
                };

                // schedule job
                var slice = sourceMeshDataArray.Length / 7;
                var jobHandle = buildJob.Schedule(gridPoints.Length, slice);
                
                // wait for completion
                jobHandle.Complete();

                // dispose all data arrays
                vertices.Dispose();
                normals.Dispose();
                colors.Dispose();
                uv0.Dispose();
                uv1.Dispose();
                uv2.Dispose();
                uv3.Dispose();
                uv4.Dispose();
                uv5.Dispose();
                uv6.Dispose();
                uv7.Dispose();
            }

            // dispose
            allIndices.Dispose();
            indexRangesArray.Dispose();
            gridPoints.Dispose();
            vertexAttributes.Dispose();
            
            sourceMeshDataArray.Dispose();

            return meshDataArray;
        }
        
        private NativeList<VertexAttributeDescriptor> GetVertexAttributes()
        {
            var vertexAttributes = new NativeList<VertexAttributeDescriptor>(1, Allocator.Persistent)
            {
                new()
                {
                    attribute = VertexAttribute.Position, dimension = 4, format = VertexAttributeFormat.Float32
                },
                new()
                {
                    attribute = VertexAttribute.Normal, dimension = 4, format = VertexAttributeFormat.Float16
                },
                new()
                {
                    attribute = VertexAttribute.Color, dimension = 4, format = VertexAttributeFormat.Float16
                },
                new()
                {
                    attribute = VertexAttribute.TexCoord0, dimension = 4, format = VertexAttributeFormat.Float16
                },
                new()
                {
                    attribute = VertexAttribute.TexCoord1, dimension = 4, format = VertexAttributeFormat.Float16
                },
                new()
                {
                    attribute = VertexAttribute.TexCoord2, dimension = 4, format = VertexAttributeFormat.Float16
                },
                new()
                {
                    attribute = VertexAttribute.TexCoord3, dimension = 4, format = VertexAttributeFormat.Float16
                },
                new()
                {
                    attribute = VertexAttribute.TexCoord4, dimension = 4, format = VertexAttributeFormat.Float16
                },
                new()
                {
                    attribute = VertexAttribute.TexCoord5, dimension = 4, format = VertexAttributeFormat.Float16
                },
                new()
                {
                    attribute = VertexAttribute.TexCoord6, dimension = 4, format = VertexAttributeFormat.Float16
                },
                new()
                {
                    attribute = VertexAttribute.TexCoord7, dimension = 4, format = VertexAttributeFormat.Float16
                }
            };
            
            return vertexAttributes;
        }

        private void GetMeshData(Mesh mesh,
            out NativeArray<Vector3> vertices, out NativeArray<Vector3> normals, out NativeArray<Color> colors,
            out NativeArray<Vector2> uv0, out NativeArray<Vector2> uv1, out NativeArray<Vector2> uv2,
            out NativeArray<Vector2> uv3, out NativeArray<Vector2> uv4, out NativeArray<Vector2> uv5,
            out NativeArray<Vector2> uv6, out NativeArray<Vector2> uv7)
        {
            vertices = default;
            normals = default;
            colors = default;
            uv0 = default;
            uv1 = default;
            uv2 = default;
            uv3 = default;
            uv4 = default;
            uv5 = default;
            uv6 = default;
            uv7 = default;
            
            var vertexAttributes = mesh.GetVertexAttributes();
            foreach (var vertexAttribute in vertexAttributes)
            {
                switch (vertexAttribute.attribute)
                {
                    case VertexAttribute.Position:
                        vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent);
                        break;
                    case VertexAttribute.Normal:
                        normals = new NativeArray<Vector3>(mesh.normals, Allocator.Persistent);
                        break;
                    case VertexAttribute.Tangent:
                        break;
                    case VertexAttribute.Color:
                        colors = new NativeArray<Color>(mesh.colors, Allocator.Persistent);
                        break;
                    case VertexAttribute.TexCoord0:
                        uv0 = new NativeArray<Vector2>(mesh.uv, Allocator.Persistent);
                        break;
                    case VertexAttribute.TexCoord1:
                        uv1 = new NativeArray<Vector2>(mesh.uv2, Allocator.Persistent);
                        break;
                    case VertexAttribute.TexCoord2:
                        uv2 = new NativeArray<Vector2>(mesh.uv3, Allocator.Persistent);
                        break;
                    case VertexAttribute.TexCoord3:
                        uv3 = new NativeArray<Vector2>(mesh.uv4, Allocator.Persistent);
                        break;
                    case VertexAttribute.TexCoord4:
                        uv4 = new NativeArray<Vector2>(mesh.uv5, Allocator.Persistent);
                        break;
                    case VertexAttribute.TexCoord5:
                        uv5 = new NativeArray<Vector2>(mesh.uv6, Allocator.Persistent);
                        break;
                    case VertexAttribute.TexCoord6:
                        uv6 = new NativeArray<Vector2>(mesh.uv7, Allocator.Persistent);
                        break;
                    case VertexAttribute.TexCoord7:
                        uv7 = new NativeArray<Vector2>(mesh.uv8, Allocator.Persistent);
                        break;
                    // case VertexAttribute.BlendWeight:
                    //     break;
                    // case VertexAttribute.BlendIndices:
                    //     break;
                }
            }
            
            if (!vertices.IsCreated)
                vertices = new NativeArray<Vector3>(0, Allocator.Persistent);
            
            if (!normals.IsCreated)
                normals = new NativeArray<Vector3>(0, Allocator.Persistent);

            if (!colors.IsCreated)
                colors = new NativeArray<Color>(0, Allocator.Persistent);
            
            if (!uv0.IsCreated)
                uv0 = new NativeArray<Vector2>(0, Allocator.Persistent);
            
            if (!uv1.IsCreated)
                uv1 = new NativeArray<Vector2>(0, Allocator.Persistent);

            if (!uv2.IsCreated)
                uv2 = new NativeArray<Vector2>(0, Allocator.Persistent);
                    
            if (!uv3.IsCreated)
                uv3 = new NativeArray<Vector2>(0, Allocator.Persistent);
            
            if (!uv4.IsCreated)
                uv4 = new NativeArray<Vector2>(0, Allocator.Persistent);
            
            if (!uv5.IsCreated)
                uv5 = new NativeArray<Vector2>(0, Allocator.Persistent);
            
            if (!uv6.IsCreated)
                uv6 = new NativeArray<Vector2>(0, Allocator.Persistent);
            
            if (!uv7.IsCreated)
                uv7 = new NativeArray<Vector2>(0, Allocator.Persistent);
        }
        
        [BurstCompile]
        private struct BuildSubMeshJob : IJobParallelFor
        {
            private static readonly MeshUpdateFlags MeshUpdateFlags = MeshUpdateFlags.DontNotifyMeshUsers 
                                                                      | MeshUpdateFlags.DontValidateIndices 
                                                                      | MeshUpdateFlags.DontRecalculateBounds 
                                                                      | MeshUpdateFlags.DontResetBoneBounds;
            public int SourceSubMeshIndex;
            [NativeDisableParallelForRestriction]
            public Mesh.MeshDataArray TargetMeshDataArray;

            [ReadOnly]
            public NativeList<int> AllIndices;
            [ReadOnly]
            public NativeList<int2> IndexRanges;

            [ReadOnly]
            public NativeList<VertexAttributeDescriptor> VertexAttributes;

            public int UvChannels;
            public bool UseVertexNormals;
            public bool UseVertexColors;

            [ReadOnly] public NativeArray<Vector3> Vertices;
            [ReadOnly] public NativeArray<Vector3> Normals;
            [ReadOnly] public NativeArray<Color> Colors;
            [ReadOnly] public NativeArray<Vector2> Uv0;
            [ReadOnly] public NativeArray<Vector2> Uv1;
            [ReadOnly] public NativeArray<Vector2> Uv2;
            [ReadOnly] public NativeArray<Vector2> Uv3;
            [ReadOnly] public NativeArray<Vector2> Uv4;
            [ReadOnly] public NativeArray<Vector2> Uv5;
            [ReadOnly] public NativeArray<Vector2> Uv6;
            [ReadOnly] public NativeArray<Vector2> Uv7;

            public void Execute(int index)
            {
                var writableMeshData = TargetMeshDataArray[index];

                var meshHasNormals = Normals.IsCreated && Normals.Length > 0;
                var meshHasColors = Colors.IsCreated && Colors.Length > 0;
                
                // mesh data lists for the new mesh
                var vertices = new NativeList<Vertex>(100, Allocator.Temp);
                var indices = new NativeList<uint>(100, Allocator.Temp);

                var indexOffset = IndexRanges[index].x;
                var indexCount = IndexRanges[index].y;

                // iterate triangle indices in pairs of 3
                for (uint i = 0; i < indexCount; i += 3)
                {
                    // indices of the triangle
                    var a = (uint)(indexOffset + i);
                    var b = (uint)(indexOffset + i + 1);
                    var c = (uint)(indexOffset + i + 2);

                    AddVertex(vertices, (int)a, meshHasNormals, meshHasColors);
                    AddVertex(vertices, (int)b, meshHasNormals, meshHasColors);
                    AddVertex(vertices, (int)c, meshHasNormals, meshHasColors);
                    
                    indices.Add(i);
                    indices.Add(i+1);
                    indices.Add(i+2);
                }
                
                vertices.Resize(vertices.Length, NativeArrayOptions.UninitializedMemory);
                indices.Resize(indices.Length, NativeArrayOptions.UninitializedMemory);

                // apply vertex data
                writableMeshData.SetVertexBufferParams(vertices.Length, VertexAttributes);
                var vertexData = writableMeshData.GetVertexData<Vertex>();
                vertexData.CopyFrom(vertices.AsArray());
                
                // TODO 16 bit indexing
                // var indexFormat = indices.Length >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;

                // apply index data
                writableMeshData.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
                var indexData = writableMeshData.GetIndexData<uint>();
                indexData.CopyFrom(indices);
                
                // writableMeshData.subMeshCount = SourceSubMeshIndex + 1;
                writableMeshData.subMeshCount = 1;
                writableMeshData.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length), MeshUpdateFlags);

                // dispose
                vertices.Dispose();
                indices.Dispose();
            }

            private void AddVertex(NativeList<Vertex> vertices, int index, bool meshHasNormals, bool meshHasColors)
            {
                var color = UseVertexColors && meshHasColors ? Colors[AllIndices[index]] : Color.white;
                
                vertices.Add(new Vertex
                {
                    position = Vertices[AllIndices[index]].ToFloat4(),
                    normal = UseVertexNormals && meshHasNormals ? (half4) Normals[AllIndices[index]].ToFloat4() : half4.zero,
                    color = new half4((half)color.r, (half)color.g, (half)color.b, (half)color.a),
                    uv0 = UvChannels >= 1 && Uv0.IsCreated ? Uv0[AllIndices[index]].ToHalf4() : half4.zero,
                    uv1 = UvChannels >= 2 && Uv1.IsCreated ? Uv1[AllIndices[index]].ToHalf4() : half4.zero,
                    uv2 = UvChannels >= 3 && Uv2.IsCreated ? Uv2[AllIndices[index]].ToHalf4() : half4.zero,
                    uv3 = UvChannels >= 4 && Uv2.IsCreated ? Uv3[AllIndices[index]].ToHalf4() : half4.zero,
                    uv4 = UvChannels >= 5 && Uv3.IsCreated ? Uv4[AllIndices[index]].ToHalf4() : half4.zero,
                    uv5 = UvChannels >= 6 && Uv4.IsCreated ? Uv5[AllIndices[index]].ToHalf4() : half4.zero,
                    uv6 = UvChannels >= 7 && Uv5.IsCreated ? Uv6[AllIndices[index]].ToHalf4() : half4.zero,
                    uv7 = UvChannels >= 8 && Uv6.IsCreated ? Uv7[AllIndices[index]].ToHalf4() : half4.zero
                });
            }
        }
    }
}