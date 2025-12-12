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
        public fixed float _ViewOriginWorldSpace[OccluderContext.k_MaxSubviewsPerView * 4];
        
        [HLSLArray(OccluderContext.k_MaxSubviewsPerView, typeof(Vector4))]
        public fixed float _FacingDirWorldSpace[OccluderContext.k_MaxSubviewsPerView * 4];
        
        public Vector4 _DepthSizeInOccluderPixels;
        public Vector4 _OccluderDepthPyramidSize;
        
        public uint _OccluderMipLayoutSizeX;
        public uint _OccluderMipLayoutSizeY;


        internal OcclusionCullingCommonShaderVariables(
            in OccluderContext occluderCtx)
        {
            int i = 0;
            // for (int i = 0; i < occluderCtx.subviewCount; ++i)
            {
                // if (occluderCtx.IsSubviewValid(i))
                {
                    unsafe
                    {
                        for (int j = 0; j < 16; ++j)
                            _ViewProjMatrix[16 * i + j] = occluderCtx.subviewData.viewProjMatrix[j];
                        
                        for (int j = 0; j < 4; ++j)
                        {
                            _ViewOriginWorldSpace[4 * i + j] = occluderCtx.subviewData.viewOriginWorldSpace[j];
                            _FacingDirWorldSpace[4 * i + j] = occluderCtx.subviewData.facingDirWorldSpace[j];
                            // _RadialDirWorldSpace[4 * i + j] = occluderCtx.subviewData[i].radialDirWorldSpace[j];
                        }
                    }
                }
            }

            _OccluderMipLayoutSizeX = (uint)occluderCtx.occluderMipLayoutSize.x;
            _OccluderMipLayoutSizeY = (uint)occluderCtx.occluderMipLayoutSize.y;
            
            _DepthSizeInOccluderPixels = occluderCtx.depthBufferSizeInOccluderPixels;
            
            Vector2Int textureSize = occluderCtx.occluderDepthPyramidSize;
            _OccluderDepthPyramidSize = new Vector4(textureSize.x, textureSize.y, 1.0f / textureSize.x, 1.0f / textureSize.y);
        }
    }
}