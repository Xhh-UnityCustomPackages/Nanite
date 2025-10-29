using System;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Nanite.Runtime
{
  
    
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Meshlet//对应 meshopt_Meshlet
    {
        public uint VertOffset;
        public uint TriangleOffset;
        public uint VertCount;
        public uint TriangleCount;
    }
    
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public class MeshletCollection
    {
        [ReadOnly] public uint[] triangles;
        [ReadOnly] public uint[] vertices;
        [ReadOnly] public Vector3[] optimizedVertices;//每个顶点的位置
        public Meshlet[] meshlets;
        public BoundsData[] boundsDataArray;
    }
    
    [Serializable]
    public struct BoundsData
    {
        public Vector4 BoundingSphere;
        public Vector4 NormalCone;
        public float ApexOffset;
        public const int SIZE = sizeof(float) * 4 + sizeof(float) * 4 + sizeof(float) * 1;
    }

    [Serializable]
    public struct Cluster
    {
        public int mip;
        public int[] indices;
        public BoundsData selfBounds;
        public BoundsData parentBounds;
    }
}