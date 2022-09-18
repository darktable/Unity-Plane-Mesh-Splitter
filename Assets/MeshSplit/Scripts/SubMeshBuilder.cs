using System;
using System.Collections.Generic;
using System.Linq;
using MeshSplit.Scripts.Models;
using MeshSplit.Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeshSplit.Scripts
{
    public class SubMeshBuilder
    {
        private readonly Dictionary<Vector3Int, List<int>> _pointIndices;
        private readonly byte[] _vertexData;
        private readonly int _vertexBufferStride;
        private readonly VertexAttributeDescriptor[] _vertexAttributeDescriptors;

        public SubMeshBuilder(Dictionary<Vector3Int, List<int>> pointIndices, byte[] vertexData, int vertexBufferStride, VertexAttributeDescriptor[] vertexAttributeDescriptors)
        {
            _pointIndices = pointIndices;
            _vertexData = vertexData;
            _vertexBufferStride = vertexBufferStride;
            _vertexAttributeDescriptors = vertexAttributeDescriptors;
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

            // var vertexAttributes = GetVertexAttributes();

            var vertexData = new NativeArray<byte>(_vertexData, Allocator.TempJob);
            var vertexAttributes =
                new NativeArray<VertexAttributeDescriptor>(_vertexAttributeDescriptors, Allocator.Persistent);

            for (var i = 0; i < sourceMeshDataArray.Length; i++)
            {
                // get mesh data arrays
                // GetMeshData(mesh, 
                //     out var vertices, out var normals, out var colors, 
                //     out var uv0, out var uv1, 
                //     out var uv2, out var uv3, 
                //     out var uv4, out var uv5, 
                //     out var uv6, out var uv7);
                //
                // // create parallel job
                // var buildJob = new BuildSubMeshJob
                // {
                //     SourceSubMeshIndex = i,
                //     TargetMeshDataArray = meshDataArray,
                //     UvChannels = splitParameters.UvChannels,
                //     VertexAttributes = vertexAttributes,
                //     UseVertexNormals = splitParameters.UseVertexNormals,
                //     UseVertexColors = splitParameters.UseVertexColors,
                //     AllIndices = allIndices,
                //     IndexRanges = indexRangesArray,
                //     Vertices = vertices,
                //     Normals = normals,
                //     Colors = colors,
                //     Uv0 = uv0, Uv1 = uv1, Uv2 = uv2, Uv3 = uv3, Uv4 = uv4, Uv5 = uv5, Uv6 = uv6, Uv7 = uv7
                // };

                var buildJob = new BuildSubMeshJob2()
                {
                    AllIndices = allIndices,
                    IndexRanges = indexRangesArray,
                    VertexData = vertexData,
                    VertexStride = _vertexBufferStride,
                    VertexAttributeDescriptors = vertexAttributes,
                    SourceSubMeshIndex = i,
                    TargetMeshDataArray = meshDataArray
                };

                // schedule job
                var slice = sourceMeshDataArray.Length / 7;
                var jobHandle = buildJob.Schedule(gridPoints.Length, slice);
                
                // wait for completion
                jobHandle.Complete();
            }

            vertexData.Dispose();
            vertexAttributes.Dispose();

            // dispose
            allIndices.Dispose();
            indexRangesArray.Dispose();
            gridPoints.Dispose();

            sourceMeshDataArray.Dispose();

            return meshDataArray;
        }

        [BurstCompile]
        private unsafe struct BuildSubMeshJob2 : IJobParallelFor
        {
            private static readonly MeshUpdateFlags MeshUpdateFlags = MeshUpdateFlags.DontNotifyMeshUsers 
                                                                      | MeshUpdateFlags.DontValidateIndices 
                                                                      | MeshUpdateFlags.DontResetBoneBounds;
            public int SourceSubMeshIndex;
            [NativeDisableParallelForRestriction]
            public Mesh.MeshDataArray TargetMeshDataArray;

            [NativeDisableContainerSafetyRestriction]
            public NativeList<int> AllIndices;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<int2> IndexRanges;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<byte> VertexData;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<VertexAttributeDescriptor> VertexAttributeDescriptors;
            [ReadOnly]
            public int VertexStride;

            public void Execute(int index)
            {
                var writableMeshData = TargetMeshDataArray[index];

                var indexOffset = IndexRanges[index].x;
                var vertexCount = IndexRanges[index].y;
                
                var indices = new NativeList<uint>(100, Allocator.Temp);
                
                var vertexData = new NativeArray<byte>(VertexStride * vertexCount, Allocator.Temp);

                var vertexIndex = 0;

                // iterate triangle indices in pairs of 3
                for (uint i = 0; i < vertexCount; i += 3)
                {
                    // indices of the triangle
                    var a = (uint)(indexOffset + i);
                    var b = (uint)(indexOffset + i + 1);
                    var c = (uint)(indexOffset + i + 2);

                    AddVertex(vertexData, (int)a, vertexIndex++);
                    AddVertex(vertexData, (int)b, vertexIndex++);
                    AddVertex(vertexData, (int)c, vertexIndex++);
                    
                    indices.Add(i);
                    indices.Add(i+1);
                    indices.Add(i+2);
                }
                
                // apply vertex data
                writableMeshData.SetVertexBufferParams(vertexCount, VertexAttributeDescriptors);
                var writableMeshVertexData = writableMeshData.GetVertexData<byte>();
                writableMeshVertexData.CopyFrom(vertexData);
                
                // TODO 16 bit indexing
                // var indexFormat = indices.Length >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;

                // apply index data
                indices.Resize(indices.Length, NativeArrayOptions.UninitializedMemory);
                writableMeshData.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
                var indexData = writableMeshData.GetIndexData<uint>();
                indexData.CopyFrom(indices);
                
                // writableMeshData.subMeshCount = SourceSubMeshIndex + 1;
                writableMeshData.subMeshCount = 1;
                writableMeshData.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length), MeshUpdateFlags);

                // dispose
                indices.Dispose();
                vertexData.Dispose();
            }

            private void AddVertex(NativeArray<byte> targetVertexData, int sourceVertexIndex, int targetVertexIndex)
            {
                var sourceIndex = AllIndices[sourceVertexIndex];

                var sourcePtr = (void*)IntPtr.Add((IntPtr)VertexData.GetUnsafePtr(), sourceIndex * VertexStride);
                var targetPtr = (void*)IntPtr.Add((IntPtr)targetVertexData.GetUnsafePtr(), targetVertexIndex * VertexStride);

                UnsafeUtility.MemCpy(targetPtr, sourcePtr, VertexStride);
            }
        }
    }
}