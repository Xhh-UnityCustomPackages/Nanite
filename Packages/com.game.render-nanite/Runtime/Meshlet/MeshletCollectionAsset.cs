using System;
using UnityEngine;

namespace Nanite.Runtime
{
    /// <summary>
    /// 由Mesh转化而成
    /// </summary>
    public class MeshletCollectionAsset : ScriptableObject
    {
        public static readonly MeshOptimizer.MeshletGenerationParams MeshletGenerationParams = new()
        {
            MaxVertices = MeshletConfiguration.MaxMeshletVertices,
            MaxTriangles = MeshletConfiguration.MaxMeshletTriangles,
            ConeWeight = MeshletConfiguration.MeshletConeWeight,
        };
        
        [HideInInspector] public string SourceMeshGUID = string.Empty;
        [HideInInspector] public string SourceMeshName = string.Empty;//原始Mesh名字
        [HideInInspector] public int SourceSubmeshIndex = -1;//原始Mesh SubMeshIndex

        public Bounds Bounds;//Mesh包围盒信息
        public int MeshLODLevelCount;
        public int LeafMeshletCount;
        public int[] MeshLODLevelNodeCounts = Array.Empty<int>();
        // public AAAAMeshLODNode[] MeshLODNodes = Array.Empty<AAAAMeshLODNode>();
        // public AAAAMeshlet[] Meshlets = Array.Empty<AAAAMeshlet>();
        // public AAAAMeshletVertex[] VertexBuffer = Array.Empty<AAAAMeshletVertex>();
        public byte[] IndexBuffer = Array.Empty<byte>();
    }
}
