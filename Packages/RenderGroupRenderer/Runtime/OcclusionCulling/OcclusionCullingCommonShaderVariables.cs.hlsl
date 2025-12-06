//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef OCCLUSIONCULLINGCOMMONSHADERVARIABLES_CS_HLSL
#define OCCLUSIONCULLINGCOMMONSHADERVARIABLES_CS_HLSL
// Generated from RenderGroupRenderer.OcclusionCullingCommonShaderVariables
// PackingRules = Exact
CBUFFER_START(OcclusionCullingCommonShaderVariables)
    uint4 _OccluderMipBounds[8];
    float4x4 _ViewProjMatrix[6];
    float4 _FacingDirWorldSpace[6];
    float4 _DepthSizeInOccluderPixels;
    float4 _OccluderDepthPyramidSize;
    uint _OccluderMipLayoutSizeX;
    uint _OccluderMipLayoutSizeY;
CBUFFER_END


#endif
