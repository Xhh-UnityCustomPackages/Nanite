#ifndef _OCCLUSION_CULLING_COMMON_H
#define _OCCLUSION_CULLING_COMMON_H

#include "Packages/rendergrouprenderer/Runtime/OcclusionCulling/OcclusionTestCommon.hlsl"
#include "Packages/rendergrouprenderer/Runtime/OcclusionCulling/OcclusionCullingCommonShaderVariables.cs.hlsl"
#include "Packages/rendergrouprenderer/Runtime/OcclusionCulling/OcclusionCullingCommon.cs.hlsl"
#include "Packages/rendergrouprenderer/Runtime/OcclusionCulling/GeometryUtilities.hlsl"

TEXTURE2D(_OccluderDepthPyramid);
SAMPLER(s_linear_clamp_sampler);


bool IsOcclusionVisible(float3 frontCenterPosRWS, float2 centerPosNDC, float2 radialPosNDC, int subviewIndex)
{
    bool isVisible = true;
    float queryClosestDepth = ComputeNormalizedDeviceCoordinatesWithZ(frontCenterPosRWS, _ViewProjMatrix[subviewIndex]).z;
    bool isBehindCamera = dot(frontCenterPosRWS, _FacingDirWorldSpace[subviewIndex].xyz) >= 0.f;

    float2 centerCoordInTopMip = centerPosNDC * _DepthSizeInOccluderPixels.xy;
    float radiusInPixels = length((radialPosNDC - centerPosNDC) * _DepthSizeInOccluderPixels.xy);

    // log2 of the radius in pixels for the gather4 mip level
    int mipLevel = 0;
    float mipPartUnused = frexp(radiusInPixels, mipLevel);
    mipLevel = max(mipLevel + 1, 0);

    if (mipLevel < OCCLUSIONCULLINGCOMMONCONFIG_MAX_OCCLUDER_MIPS && !isBehindCamera)
    {
        // scale our coordinate to this mip
        float2 centerCoordInChosenMip = ldexp(centerCoordInTopMip, -mipLevel);
        int4 mipBounds = _OccluderMipBounds[mipLevel];
        mipBounds.y += subviewIndex * _OccluderMipLayoutSizeY;

        // if ((_OcclusionTestDebugFlags & OCCLUSIONTESTDEBUGFLAG_ALWAYS_PASS) == 0)
        {
            // gather4 occluder depths to cover this radius
            float2 gatherUv = (float2(mipBounds.xy) + clamp(centerCoordInChosenMip, .5f, float2(mipBounds.zw) - .5f)) * _OccluderDepthPyramidSize.zw;
            float4 gatherDepths = GATHER_TEXTURE2D(_OccluderDepthPyramid, s_linear_clamp_sampler, gatherUv);
            float occluderDepth = FarthestDepth(gatherDepths);
            isVisible = IsVisibleAfterOcclusion(occluderDepth, queryClosestDepth);
        }
    }
    
    return isVisible;
}

bool IsOcclusionVisible(BoundingObjectData data, int subviewIndex)
{
    return IsOcclusionVisible(data.frontCenterPosRWS, data.centerPosNDC, data.radialPosNDC, subviewIndex);
}

#endif
