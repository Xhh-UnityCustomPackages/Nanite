using System;
using Nanite.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Nanite.Editor
{
    internal static partial class MeshletCollectionBuilder
    {
        //这是为了把Meshlet合并成一个
        private static NativeArray<NativeList<int>> GroupMeshlets(MeshLODNodeLevel meshLODNodeLevel, int meshletsPerGroup, Allocator allocator)
        {
            int graphNodeCount = meshLODNodeLevel.Nodes.Length;
            int partitionsCount = Mathf.CeilToInt((float) graphNodeCount / meshletsPerGroup);
            if (partitionsCount <= 1)//不够数量合并 
            {
                var groups = new NativeArray<NativeList<int>>(1, allocator, NativeArrayOptions.UninitializedMemory);
                var allNodes = new NativeList<int>(graphNodeCount, allocator);

                for (int i = 0; i < graphNodeCount; i++)
                {
                    allNodes.Add(i);
                }

                groups[0] = allNodes;

                return groups;
            }


            // NativeArray<NativeHashSet<Edge>> edgeSets = CollectEdgeSets(meshLODNodeLevel);
            //
            // NativeArray<int> adjacencyMatrix = CreateAdjacencyMatrix(edgeSets, Allocator.TempJob);
            // edgeSets.Dispose();
            //
            //
            // var adjacencyIndexList = new NativeArray<int>(graphNodeCount + 1, Allocator.Temp)
            // {
            //     [0] = 0,
            // };
            // var adjacencyList = new NativeList<int>(graphNodeCount, Allocator.Temp);
            // var adjacencyWeightList = new NativeList<int>(graphNodeCount, Allocator.Temp);
            //
            // for (int node1 = 0; node1 < graphNodeCount; node1++)
            // {
            //     int totalEdgeCount = 0;
            //
            //     for (int node2 = 0; node2 < graphNodeCount; node2++)
            //     {
            //         int weight = adjacencyMatrix[node1 * graphNodeCount + node2];
            //         if (weight > 0)
            //         {
            //             adjacencyList.Add(node2);
            //             adjacencyWeightList.Add(weight);
            //             ++totalEdgeCount;
            //         }
            //     }
            //
            //     adjacencyIndexList[node1 + 1] = adjacencyIndexList[node1] + totalEdgeCount;
            // }
            //
            // adjacencyMatrix.Dispose();

            return default;

            // var graphAdjacencyStructure = new AAAAMETIS.GraphAdjacencyStructure
            // {
            //     VertexCount = graphNodeCount,
            //     AdjacencyIndexList = adjacencyIndexList,
            //     AdjacencyList = adjacencyList.AsArray(),
            //     AdjacencyWeightList = adjacencyWeightList.AsArray(),
            // };
            //
            // NativeArray<METISOptions> options = AAAAMETIS.CreateOptions(Allocator.Temp);
            //
            // METISStatus status = AAAAMETIS.PartGraphKway(graphAdjacencyStructure, Allocator.Temp, partitionsCount, options,
            //     out NativeArray<int> vertexPartitioning
            // );
            // Assert.IsTrue(status == METISStatus.METIS_OK);
            //
            // adjacencyIndexList.Dispose();
            // adjacencyList.Dispose();
            // adjacencyWeightList.Dispose();
            // options.Dispose();
            //
            // NativeArray<NativeList<int>> meshletGrouping =
            //     ConstructMeshletGroupingFromVertexPartitioning(vertexPartitioning, allocator, partitionsCount, meshletsPerGroup);

            // vertexPartitioning.Dispose();
            //
            // return meshletGrouping;
        }
         
        private static NativeArray<NativeHashSet<Edge>> CollectEdgeSets(MeshLODNodeLevel nodeLevel)
        {
            var edgeSets = new NativeArray<NativeHashSet<Edge>>(nodeLevel.Nodes.Length, Allocator.TempJob);
        
            for (int i = 0; i < edgeSets.Length; i++)
            {
                edgeSets[i] = new NativeHashSet<Edge>((int) (MeshletConfiguration.MaxMeshletTriangles * 3), Allocator.TempJob);
            }
        
            new CollectMeshletEdgesJob
                {
                    EdgeSets = edgeSets,
                    NodeLevel = nodeLevel,
                }.Schedule(edgeSets.Length, 4)
                .Complete();
        
            return edgeSets;
        }
        //
        // private static NativeArray<int> CreateAdjacencyMatrix(NativeArray<NativeHashSet<Edge>> edgeSets, Allocator allocator)
        // {
        //     int graphNodeCount = edgeSets.Length;
        //     var adjacencyMatrix = new NativeArray<int>(graphNodeCount * graphNodeCount, allocator);
        //
        //     var nodePairs = new NativeList<int2>(graphNodeCount * graphNodeCount, Allocator.TempJob);
        //
        //     for (int node1 = 0; node1 < graphNodeCount; node1++)
        //     {
        //         for (int node2 = node1 + 1; node2 < graphNodeCount; node2++)
        //         {
        //             nodePairs.Add(new int2(node1, node2));
        //         }
        //     }
        //
        //     new FillAdjacencyMatrixJob
        //         {
        //             NodeCount = graphNodeCount,
        //             AdjacencyMatrix = adjacencyMatrix,
        //             EdgeSets = edgeSets,
        //             NodePairs = nodePairs.AsArray(),
        //         }.Schedule(nodePairs.Length, 4)
        //         .Complete();
        //
        //     nodePairs.Dispose();
        //     return adjacencyMatrix;
        // }
        
        [BurstCompile]
        private struct CollectMeshletEdgesJob : IJobParallelFor
        {
            [NativeDisableContainerSafetyRestriction]
            public MeshLODNodeLevel NodeLevel;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<NativeHashSet<Edge>> EdgeSets;

            public void Execute(int index)
            {
                MeshLODNode lodNode = NodeLevel.Nodes[index];
                MeshLODNodeLevel.MeshletNodeList meshletsNodeList = NodeLevel.MeshletsNodeLists[lodNode.MeshletNodeListIndex];

                MeshOptimizer.MeshletBuildResults meshletBuildResults = meshletsNodeList.MeshletBuildResults;
                meshopt_Meshlet meshlet = meshletBuildResults.Meshlets[lodNode.MeshletIndex];

                NativeHashSet<Edge> edgeSet = EdgeSets[index];

                for (int i = 0; i < meshlet.TriangleCount; i++)
                {
                    int baseIndex = (int) (meshlet.TriangleOffset + i * 3);
                    uint index0 = meshletBuildResults.Vertices[(int) meshlet.VertexOffset + meshletBuildResults.Indices[baseIndex + 0]];
                    uint index1 = meshletBuildResults.Vertices[(int) meshlet.VertexOffset + meshletBuildResults.Indices[baseIndex + 1]];
                    uint index2 = meshletBuildResults.Vertices[(int) meshlet.VertexOffset + meshletBuildResults.Indices[baseIndex + 2]];

                    edgeSet.Add(new Edge(index0, index1));
                    edgeSet.Add(new Edge(index1, index2));
                    edgeSet.Add(new Edge(index2, index0));
                }
            }
        }
         
        private readonly struct Edge : IEquatable<Edge>
        {
            public readonly uint Index0;
            public readonly uint Index1;

            public Edge(uint index0, uint index1)
            {
                Index0 = math.min(index0, index1);
                Index1 = math.max(index0, index1);
            }

            public bool Equals(Edge other) =>
                Index0 == other.Index0 && Index1 == other.Index1;

            public override bool Equals(object obj) => obj is Edge other && Equals(other);

            public override int GetHashCode() => unchecked((int) (Index0 * 397 ^ Index1));
        }
    }
}