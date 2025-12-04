using UnityEngine;
using UnityEngine.Rendering;

namespace RenderGroupRenderer
{
    [GenerateHLSL]
    internal enum OcclusionCullingCommonConfig
    {
        MaxOccluderMips = 8,
        MaxOccluderSilhouettePlanes = 6,
        MaxSubviewsPerView = 6,
        DebugPyramidOffset = 4, // TODO: rename
    }
}
