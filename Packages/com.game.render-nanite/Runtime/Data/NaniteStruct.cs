using JetBrains.Annotations;
using UnityEngine.Rendering;

namespace Nanite.Runtime
{
    [GenerateHLSL]
    public static class MeshletConfiguration
    {
        [UsedImplicitly]
        public const uint MaxMeshletVertices = 128;
        [UsedImplicitly]
        public const uint MaxMeshletTriangles = 128;
        [UsedImplicitly]
        public const uint MaxMeshletIndices = MaxMeshletTriangles * 3;
        [UsedImplicitly]
        public const float MeshletConeWeight = 0.25f;
    }
}