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
    public partial class HiZDepthGeneratorPass : ScriptableRenderPass
    {
        private OccluderContext ctx;
        private readonly ComputeShader m_HiZCS;

        private readonly ProfilingSampler m_ProfilingSamplerUpdateOccluders;
        private NativeArray<Plane> m_Planes;//摄像机平面
        
        public RTHandle occluderDepthPyramid => ctx.occluderDepthPyramid;
        
        public HiZDepthGeneratorPass(RenderGroupRendererFeatureData m_FeatureData)
        {
            m_ProfilingSamplerUpdateOccluders = new ProfilingSampler("UpdateOccluders");
            m_HiZCS = m_FeatureData.buildHiZCS;
            ctx = new();
        }
        
        public void Dispose()
        {
            ctx.Dispose();
        }

        void UpdatePlanes(NativeArray<Plane> planes)
        {
            m_Planes.CopyFrom(planes);
        }

        //兼容模式
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            
            var desc = cameraData.cameraTargetDescriptor;
            desc.graphicsFormat = GraphicsFormat.None;
            desc.depthBufferBits = 32;
        
            int scaledWidth = (int)(cameraData.camera.pixelWidth * cameraData.renderScale);
            int scaledHeight = (int)(cameraData.camera.pixelHeight * cameraData.renderScale);
            OccluderParameters occluderParams = new()
            {
                depthSize = new Vector2Int(scaledWidth, scaledHeight),
            };
            
            occluderParams.depthTextureRTHandle = renderingData.cameraData.renderer.cameraDepthTargetHandle;
            ctx.PrepareOccluders(occluderParams);
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSamplerUpdateOccluders))
            {
                ctx.CreateFarDepthPyramid(cmd, in occluderParams, m_Planes, m_HiZCS, 0);
                cmd.SetGlobalTexture("_OccluderDepthPyramid", ctx.occluderDepthPyramid);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
    }
}
