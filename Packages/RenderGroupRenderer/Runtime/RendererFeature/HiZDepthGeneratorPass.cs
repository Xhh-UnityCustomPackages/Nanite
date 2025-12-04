using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RenderGroupRenderer
{
    public class HiZDepthGeneratorPass : ScriptableRenderPass
    {
        private OccluderContext ctx;
        private ComputeShader m_HiZCS;

        private ProfilingSampler m_ProfilingSamplerUpdateOccluders;
        private NativeArray<Plane> m_Planes;
        
       
        
        public HiZDepthGeneratorPass(RenderGroupRendererFeatureData m_FeatureData)
        {
            m_HiZCS = m_FeatureData.buildHiZCS;
            ctx = new();
            
            m_ProfilingSamplerUpdateOccluders = new ProfilingSampler("UpdateOccluders");
        }

        private class UpdateOccludersPassData
        {
            public OccluderParameters occluderParams;
            public OccluderHandles occluderHandles;
            public OccluderContext ctx;
        }

        public void Dispose()
        {
            ctx.Dispose();
        }

        void UpdatePlanes(NativeArray<Plane> planes)
        {
            m_Planes.CopyFrom(planes);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            TextureHandle cameraDepthTexture = resourceData.activeDepthTexture;//使用 resourceData.cameraDepthTexture有问题
           
            
            var desc = cameraData.cameraTargetDescriptor;
            desc.graphicsFormat = GraphicsFormat.None;
            desc.depthBufferBits = 32;

            OccluderParameters occluderParams = new()
            {
                depthSize = new Vector2Int(cameraData.scaledWidth, cameraData.scaledHeight),
            };

            using (var builder = renderGraph.AddComputePass<UpdateOccludersPassData>("Update Occluders", out var passData, m_ProfilingSamplerUpdateOccluders))
            {
                passData.occluderParams = occluderParams;
                ctx.PrepareOccluders(passData.occluderParams);
                var occluderHandles = ctx.Import(renderGraph);
                
                builder.AllowGlobalStateModification(true);
                passData.ctx = ctx;
                passData.occluderHandles = occluderHandles;
                
                passData.occluderParams.depthTexture = cameraDepthTexture;
                builder.UseTexture(cameraDepthTexture);
                builder.UseTexture(passData.occluderHandles.occluderDepthPyramid, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((UpdateOccludersPassData data, ComputeGraphContext context) =>
                {
                    ctx.CreateFarDepthPyramid(context.cmd, in data.occluderParams, in data.occluderHandles, m_Planes, m_HiZCS, 0);
                });
            }
        }
    }

    internal struct OccluderMipBounds
    {
        public Vector2Int offset;
        public Vector2Int size;
    }

    struct OccluderContext : IDisposable
    {
        private static class ShaderIDs
        {
            public static readonly int _SrcDepth = Shader.PropertyToID("_SrcDepth");
            public static readonly int _DstDepth = Shader.PropertyToID("_DstDepth");
            public static readonly int OccluderDepthPyramidConstants = Shader.PropertyToID("OccluderDepthPyramidConstants");
        }
        
        public const int k_FirstDepthMipIndex = 3; // 8x8 tiles
        public const int k_MaxOccluderMips = (int)OcclusionCullingCommonConfig.MaxOccluderMips;
        public const int k_MaxSilhouettePlanes = (int)OcclusionCullingCommonConfig.MaxOccluderSilhouettePlanes;
        public const int k_MaxSubviewsPerView = (int)OcclusionCullingCommonConfig.MaxSubviewsPerView;
        
        public Vector2Int occluderDepthPyramidSize; // at least the size of N mip layouts tiled vertically (one per subview)
        public RTHandle occluderDepthPyramid;
        
        public Vector2Int depthBufferSize;
        
        public NativeArray<OccluderMipBounds> occluderMipBounds;
        public Vector2Int occluderMipLayoutSize; // total size of 2D layout specified by occluderMipBounds
        
        public ComputeBuffer constantBuffer;//常量Buffer 只有下面1个
        public NativeArray<OccluderDepthPyramidConstants> constantBufferData;
        
        public void Dispose()
        {
            if (occluderMipBounds.IsCreated)
                occluderMipBounds.Dispose();
            
            if (occluderDepthPyramid != null)
            {
                occluderDepthPyramid.Release();
                occluderDepthPyramid = null;
            }
            
            if (constantBuffer != null)
            {
                constantBuffer.Release();
                constantBuffer = null;
            }

            if (constantBufferData.IsCreated)
                constantBufferData.Dispose();
        }
        
        public OccluderHandles Import(RenderGraph renderGraph)
        {
            RenderTargetInfo rtInfo = new RenderTargetInfo
            {
                width = occluderDepthPyramidSize.x,
                height = occluderDepthPyramidSize.y,
                volumeDepth = 1,
                msaaSamples = 1,
                format = GraphicsFormat.R32_SFloat,
                bindMS = false,
            };
            OccluderHandles occluderHandles = new OccluderHandles()
            {
                occluderDepthPyramid = renderGraph.ImportTexture(occluderDepthPyramid, rtInfo)
            };
            // if (occlusionDebugOverlay != null)
            //     occluderHandles.occlusionDebugOverlay = renderGraph.ImportBuffer(occlusionDebugOverlay);
            return occluderHandles;
        }

        public void PrepareOccluders(in OccluderParameters occluderParams)
        {
            depthBufferSize = occluderParams.depthSize;
            UpdateMipBounds();
            AllocateTexturesIfNecessary(false);
        }

        private void UpdateMipBounds()
        {
            int occluderPixelSize = 1 << k_FirstDepthMipIndex;
            Vector2Int topMipSize = (depthBufferSize + (occluderPixelSize - 1) * Vector2Int.one) / occluderPixelSize;
            
            Vector2Int totalSize = Vector2Int.zero;
            Vector2Int mipOffset = Vector2Int.zero;
            Vector2Int mipSize = topMipSize;

            if (!occluderMipBounds.IsCreated)
                occluderMipBounds = new NativeArray<OccluderMipBounds>(k_MaxOccluderMips, Allocator.Persistent);
            
            for (int mipIndex = 0; mipIndex < k_MaxOccluderMips; ++mipIndex)
            {
                occluderMipBounds[mipIndex] = new OccluderMipBounds { offset = mipOffset, size = mipSize };

                totalSize.x = Mathf.Max(totalSize.x, mipOffset.x + mipSize.x);
                totalSize.y = Mathf.Max(totalSize.y, mipOffset.y + mipSize.y);

                if (mipIndex == 0)
                {
                    mipOffset.x = 0;
                    mipOffset.y += mipSize.y;
                }
                else
                {
                    mipOffset.x += mipSize.x;
                }
                mipSize.x = (mipSize.x + 1) / 2;
                mipSize.y = (mipSize.y + 1) / 2;
            }

            occluderMipLayoutSize = totalSize;
        }

        private void AllocateTexturesIfNecessary(bool debugOverlayEnabled)
        {
            Vector2Int minDepthPyramidSize = new Vector2Int(occluderMipLayoutSize.x, occluderMipLayoutSize.y);
            if (occluderDepthPyramidSize.x < minDepthPyramidSize.x || occluderDepthPyramidSize.y < minDepthPyramidSize.y || occluderDepthPyramid == null)
            {
                if (occluderDepthPyramid != null)
                    occluderDepthPyramid.Release();
                occluderDepthPyramidSize = minDepthPyramidSize;
                occluderDepthPyramid = RTHandles.Alloc(
                    occluderDepthPyramidSize.x, occluderDepthPyramidSize.y,
                    format: GraphicsFormat.R32_SFloat,
                    dimension: TextureDimension.Tex2D,                    
                    filterMode: FilterMode.Point,
                    wrapMode: TextureWrapMode.Clamp,
                    enableRandomWrite: true,
                    name: "Occluder Depths");
            }

            if (constantBuffer == null)
                constantBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<OccluderDepthPyramidConstants>(), ComputeBufferType.Constant);
            
            if (!constantBufferData.IsCreated)
                constantBufferData = new NativeArray<OccluderDepthPyramidConstants>(1, Allocator.Persistent);
        }

        internal static void SetKeyword(ComputeCommandBuffer cmd, ComputeShader cs, in LocalKeyword keyword, bool value)
        {
            if (value)
                cmd.EnableKeyword(cs, keyword);
            else
                cmd.DisableKeyword(cs, keyword);
        }

        private OccluderDepthPyramidConstants SetupFarDepthPyramidConstants(NativeArray<Plane> silhouettePlanes)
        {
            OccluderDepthPyramidConstants cb = new OccluderDepthPyramidConstants();
            
            // write globals
            cb._OccluderMipLayoutSizeX = (uint)occluderMipLayoutSize.x;
            cb._OccluderMipLayoutSizeY = (uint)occluderMipLayoutSize.y;

            // Matrix4x4 viewProjMatrix
            //     = cameraData.GetProjectionMatrix()
            //       * cameraData.GetViewMatrix()
            //        * Matrix4x4.Translate(-update.viewOffsetWorldSpace);
            Matrix4x4 viewProjMatrix = Matrix4x4.identity;
            Matrix4x4 invViewProjMatrix = viewProjMatrix.inverse;
            
            unsafe
            {
                for (int j = 0; j < 16; ++j)
                    cb._InvViewProjMatrix[16 * 1 + j] = invViewProjMatrix[j];

                // cb._SrcOffset[4 * 1 + 0] = 0;
                // cb._SrcOffset[4 * 1 + 1] = 0;
                // cb._SrcOffset[4 * 1 + 2] = 0;
                // cb._SrcOffset[4 * 1 + 3] = 0;
            }
            
            // TODO: transform these planes from world space into NDC space planes
            for (int i = 0; i < k_MaxSilhouettePlanes; ++i)
            {
                Plane plane = new Plane(Vector3.zero, 0.0f);
                if (i < silhouettePlanes.Length)
                    plane = silhouettePlanes[i];
                unsafe
                {
                    cb._SilhouettePlanes[4 * i + 0] = plane.normal.x;
                    cb._SilhouettePlanes[4 * i + 1] = plane.normal.y;
                    cb._SilhouettePlanes[4 * i + 2] = plane.normal.z;
                    cb._SilhouettePlanes[4 * i + 3] = plane.distance;
                }
            }
            cb._SilhouettePlaneCount = (uint)silhouettePlanes.Length;
            return cb;
        }

        public void CreateFarDepthPyramid(ComputeCommandBuffer cmd, in OccluderParameters occluderParams, in OccluderHandles occluderHandles, NativeArray<Plane> silhouettePlanes, ComputeShader occluderDepthPyramidCS, int occluderDepthDownscaleKernel)
        {
            OccluderDepthPyramidConstants cb = SetupFarDepthPyramidConstants(silhouettePlanes);
            
            var cs = occluderDepthPyramidCS;
            int kernel = occluderDepthDownscaleKernel;

            var srcKeyword = new LocalKeyword(cs, "USE_SRC");
            
            // RTHandle depthTexture = (RTHandle)occluderParams.depthTexture;
            // bool srcIsMsaa = depthTexture?.isMSAAEnabled ?? false;
            
            int mipCount = k_FirstDepthMipIndex + k_MaxOccluderMips;//共11级
            
            //每次生成4级
            for (int mipIndexBase = 0; mipIndexBase < mipCount - 1; mipIndexBase += 4)
            {
                cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._DstDepth, occluderHandles.occluderDepthPyramid);
                
                bool useSrc = (mipIndexBase == 0);
                SetKeyword(cmd, cs, srcKeyword, useSrc);
                
                if (useSrc)
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._SrcDepth, occluderParams.depthTexture);
                
                cb._MipCount = (uint)Math.Min(mipCount - 1 - mipIndexBase, 4);
                
                Vector2Int srcSize = Vector2Int.zero;
                for (int i = 0; i < 5; ++i)
                {
                    Vector2Int offset = Vector2Int.zero;
                    Vector2Int size = Vector2Int.zero;
                    int mipIndex = mipIndexBase + i;
                    if (mipIndex == 0)
                    {
                        size = occluderParams.depthSize;
                    }
                    else
                    {
                        int occMipIndex = mipIndex - k_FirstDepthMipIndex;
                        if (0 <= occMipIndex && occMipIndex < k_MaxOccluderMips)
                        {
                            offset = occluderMipBounds[occMipIndex].offset;
                            size = occluderMipBounds[occMipIndex].size;
                        }
                    }
                    if (i == 0)
                        srcSize = size;
                    //记录下4个级别的在Atlas下的大小和坐标位置
                    unsafe
                    {
                        cb._MipOffsetAndSize[4 * i + 0] = (uint)offset.x;
                        cb._MipOffsetAndSize[4 * i + 1] = (uint)offset.y;
                        cb._MipOffsetAndSize[4 * i + 2] = (uint)size.x;
                        cb._MipOffsetAndSize[4 * i + 3] = (uint)size.y;
                    }
                }
                
                constantBufferData[0] = cb;
                cmd.SetBufferData(constantBuffer, constantBufferData);//Buffer填充数据
                cmd.SetComputeConstantBufferParam(cs, ShaderIDs.OccluderDepthPyramidConstants, constantBuffer, 0, constantBuffer.stride);//CS填充Buffer
                
                cmd.DispatchCompute(cs, kernel, (srcSize.x + 15) / 16, (srcSize.y + 15) / 16, 1);
            }
        }
        
        
        
    }
}
