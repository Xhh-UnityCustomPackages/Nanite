using UnityEngine;
using UnityEngine.Rendering;

namespace RenderGroupRenderer
{
    // TODO make consistent with InstanceOcclusionCullerShaderVariables
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct OcclusionCullingCommonShaderVariables
    {
        [HLSLArray(OccluderContext.k_MaxOccluderMips, typeof(ShaderGenUInt4))]
        public fixed uint _OccluderMipBounds[OccluderContext.k_MaxOccluderMips * 4];
        
        [HLSLArray(OccluderContext.k_MaxSubviewsPerView, typeof(Matrix4x4))]
        public fixed float _ViewProjMatrix[OccluderContext.k_MaxSubviewsPerView * 16]; // from view-centered world space
        
        [HLSLArray(OccluderContext.k_MaxSubviewsPerView, typeof(Vector4))]
        public fixed float _FacingDirWorldSpace[OccluderContext.k_MaxSubviewsPerView * 4];
        
        public Vector4 _DepthSizeInOccluderPixels;
        public Vector4 _OccluderDepthPyramidSize;
        
        public uint _OccluderMipLayoutSizeX;
        public uint _OccluderMipLayoutSizeY;
    }
}