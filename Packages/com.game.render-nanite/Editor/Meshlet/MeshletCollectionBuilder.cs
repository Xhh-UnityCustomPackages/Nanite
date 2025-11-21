using System;
using Nanite.Runtime;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nanite.Editor
{
    internal static partial class MeshletCollectionBuilder
    {
        public static unsafe void Generate(MeshletCollectionAsset meshletCollection, in Parameters parameters)
        {
            meshletCollection.SourceMeshGUID = parameters.SourceMeshGUID;
            meshletCollection.SourceMeshName = parameters.Mesh.name;
            meshletCollection.SourceSubmeshIndex = parameters.SubMeshIndex;
            meshletCollection.Bounds = parameters.Mesh.bounds;
            
            using Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(parameters.Mesh);
            Mesh.MeshData data = dataArray[0];
            uint vertexBufferStride = (uint) data.GetVertexBufferStride(0);
            uint vertexPositionOffset = (uint) data.GetVertexAttributeOffset(VertexAttribute.Position);
            NativeArray<float> vertexData = data.GetVertexData<float>();
            
            //这取出的是全部Index信息
            NativeArray<uint> indexDataU32;
            if (data.indexFormat == IndexFormat.UInt16)//这个是常规格式
            {
                NativeArray<ushort> indexDataU16 = data.GetIndexData<ushort>();
                indexDataU32 = CastIndices16To32(indexDataU16, Allocator.TempJob);
                indexDataU16.Dispose();
            }
            else
            {
                NativeArray<uint> indexData = data.GetIndexData<uint>();
                indexDataU32 = new NativeArray<uint>(indexData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                indexDataU32.CopyFrom(indexData);
                indexData.Dispose();
            }
            
            //获取SubMesh信息
            SubMeshDescriptor subMeshDescriptor = data.GetSubMesh(parameters.SubMeshIndex);
            //index 根据submesh 进行截断
            indexDataU32 = indexDataU32.GetSubArray(subMeshDescriptor.indexStart, subMeshDescriptor.indexCount);
            uint baseVertex = (uint) subMeshDescriptor.baseVertex;

            for (int i = 0; i < indexDataU32.Length; i++)
            {
                indexDataU32[i] += baseVertex;
            }
            
            int uvStream = data.GetVertexAttributeStream(VertexAttribute.TexCoord0);
            uint uvStreamStride = (uint) (uvStream >= 0 ? data.GetVertexBufferStride(uvStream) : 0);
            NativeArray<float> uvVertexData = uvStream >= 0 ? data.GetVertexData<float>(uvStream) : default;
            byte* pVerticesUV = uvVertexData.IsCreated ? (byte*) uvVertexData.GetUnsafeReadOnlyPtr() : null;
            uint vertexUVOffset = (uint) data.GetVertexAttributeOffset(VertexAttribute.TexCoord0);
            
            uint vertexCount = (uint) subMeshDescriptor.vertexCount;
            
            // if (parameters.OptimizeVertexCache)
            // {
            //     NativeArray<uint> sourceIndices = indexDataU32;
            //     indexDataU32 = AAAAMeshOptimizer.OptimizeVertexCache(Allocator.TempJob, sourceIndices, vertexCount);
            //     sourceIndices.Dispose();
            // }

            MeshOptimizer.MeshletGenerationParams meshletGenerationParams = MeshletCollectionAsset.MeshletGenerationParams;
            const Allocator allocator = Allocator.TempJob;
            MeshOptimizer.MeshletBuildResults mainMeshletBuildResults = MeshOptimizer.BuildMeshlets(allocator,
                vertexData, vertexPositionOffset, vertexBufferStride, indexDataU32,
                meshletGenerationParams
            );
            
            var meshLODLevels = new NativeList<MeshLODNodeLevel>(allocator);
            var topLOD = new MeshLODNodeLevel
            {
                Nodes = new NativeArray<MeshLODNode>(mainMeshletBuildResults.Meshlets.Length, allocator),
                MeshletsNodeLists = new NativeArray<MeshLODNodeLevel.MeshletNodeList>(1, allocator)
                {
                    [0] = new MeshLODNodeLevel.MeshletNodeList
                    {
                        MeshletBuildResults = mainMeshletBuildResults,
                    },
                },
            };

            //计算子节点信息
            for (int i = 0; i < topLOD.Nodes.Length; ++i)
            {
                MeshOptimizer.MeshletBuildResults meshletBuildResults = topLOD.MeshletsNodeLists[0].MeshletBuildResults;
                meshopt_Meshlet meshlet = meshletBuildResults.Meshlets[i];
                meshopt_Bounds bounds =
                    MeshOptimizer.ComputeMeshletBounds(meshletBuildResults, i, vertexData, vertexPositionOffset, vertexBufferStride);
                
                topLOD.TriangleCount += meshlet.TriangleCount;
                topLOD.Nodes[i] = new MeshLODNode
                {
                    MeshletNodeListIndex = 0,
                    MeshletIndex = i,
                    ChildGroupIndex = -1,
                    Error = 0.0f,
                    Bounds = math.float4(bounds.Center[0], bounds.Center[1], bounds.Center[2], bounds.Radius),
                };
            }
            
            meshLODLevels.Add(topLOD);
            
            var vertexLayout = new MeshOptimizer.VertexLayout
            {
                Vertices = vertexData,
                UV = uvVertexData,
                PositionOffset = vertexPositionOffset,
                PositionStride = vertexBufferStride,
                UVOffset = vertexUVOffset,
                UVStride = vertexBufferStride,
            };
            BuildLodGraph(meshLODLevels, allocator, vertexLayout, meshletGenerationParams, parameters);
        }
        
        private static NativeArray<uint> CastIndices16To32(NativeArray<ushort> indices, Allocator allocator)
        {
            var result = new NativeArray<uint>(indices.Length, allocator);
            for (int i = 0; i < indices.Length; i++)
            {
                result[i] = indices[i];
            }
            return result;
        }

        private static void BuildLodGraph(NativeList<MeshLODNodeLevel> levels, Allocator allocator,
            in MeshOptimizer.VertexLayout vertexLayout, MeshOptimizer.MeshletGenerationParams meshletGenerationParams, in Parameters parameters)
        {
            MeshOptimizer.SimplifyMode simplifyMode = MeshOptimizer.SimplifyMode.Normal;

            while (levels[^1].Nodes.Length > 1)
            {
                ref MeshLODNodeLevel previousLevel = ref levels.ElementAt(levels.Length - 1);
                if (previousLevel.Nodes.Length < 2)
                {
                    break;
                }
                
                var newLevelNodes = new NativeList<MeshLODNode>(previousLevel.Nodes.Length / 2, Allocator.TempJob);
                var meshletNodeLists = new NativeList<MeshLODNodeLevel.MeshletNodeList>(previousLevel.MeshletsNodeLists.Length / 2, Allocator.TempJob);
                uint newTriangleCount = 0;
                
                const int meshletsPerGroup = 4;//固定把4个小meshlet重新生成 一个新的meshlet
                
                NativeArray<NativeList<int>> childMeshletGroups = GroupMeshlets(previousLevel, meshletsPerGroup, Allocator.TempJob);
            }
        }

        public struct Parameters
        {
            public Mesh Mesh;
            public string SourceMeshGUID;
            public int SubMeshIndex;
            public Action<string> LogErrorHandler;
            public bool OptimizeVertexCache;
            public int MaxMeshLODLevelCount;
            public float TargetError;
            public float TargetErrorSloppy;
            public float MinTriangleReductionPerStep;
        }

        public struct SphereBounds
        {
            public float3 center;
            public float radius;
        }
        
        //可以理解位MeshCluster
        private struct MeshLODNode : IDisposable
        {
            public int MeshletNodeListIndex;
            public int MeshletIndex;
            public int ChildGroupIndex;

            public float4 Bounds;//自身包围盒 为啥就用一个float4就可以表示了 是因为这里用的是包围球 xyz存的中心点 w存的半径
            public float Error;

            public float4 ParentBounds;//父节点包围盒
            public float ParentError;

            public void Dispose() { }
        }
        
        private struct MeshLODNodeLevel : IDisposable
        {
            public NativeArray<MeshLODNode> Nodes;
            public NativeArray<MeshletNodeList> MeshletsNodeLists;
            public NativeArray<NativeList<int>> Groups;
            public uint TriangleCount;

            public void Dispose()
            {
                foreach (MeshLODNode node in Nodes)
                {
                    node.Dispose();
                }

                foreach (MeshletNodeList meshletsNodeList in MeshletsNodeLists)
                {
                    MeshletNodeList listCopy = meshletsNodeList;
                    listCopy.MeshletBuildResults.Dispose();
                }

                MeshletsNodeLists.Dispose();
                Nodes.Dispose();
                foreach (NativeList<int> group in Groups)
                {
                    group.Dispose();
                }
                Groups.Dispose();
            }

            public struct MeshletNodeList
            {
                public MeshOptimizer.MeshletBuildResults MeshletBuildResults;
            }
        }
    }
}