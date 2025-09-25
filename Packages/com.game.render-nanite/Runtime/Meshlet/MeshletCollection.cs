using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Nanite.Runtime
{
    [CreateAssetMenu(fileName = "MeshletAsset", menuName = "Nanite/Meshlet/Create MeshletAsset.asset", order = 1)]
    public class MeshletAsset : ScriptableObject
    {
        public MeshletCollection Collection;
        public Mesh SourceMesh;
    }
    
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Meshlet
    {
        public uint VertOffset;
        public uint PrimOffset;
        public uint VertCount;
        public uint PrimCount;
    }
    
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public class MeshletCollection
    {
        [HideInInspector] public uint[] triangles;
        [HideInInspector] public uint[] vertices;
        public Meshlet[] meshlets;
        public BoundsData[] boundsDataArray;
        [HideInInspector] public Vector3[] optimizedVertices;
    }
    
    [Serializable]
    public struct BoundsData
    {
        public Vector4 BoundingSphere;
        public Vector4 NormalCone;
        public float ApexOffset;
        public const int SIZE = sizeof(float) * 4 + sizeof(float) * 4 + sizeof(float) * 1;
    }
}