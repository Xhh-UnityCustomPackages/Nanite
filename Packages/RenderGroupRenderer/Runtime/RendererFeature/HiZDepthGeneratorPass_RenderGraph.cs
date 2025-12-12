using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RenderGroupRenderer
{
    public partial class HiZDepthGeneratorPass : ScriptableRenderPass
    {
        private class UpdateOccludersPassData
        {
            public OccluderParameters occluderParams;
            public OccluderHandles occluderHandles;
            public OccluderContext ctx;
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            TextureHandle cameraDepthTexture = resourceData.activeDepthTexture;//使用 resourceData.cameraDepthTexture 有问题
           
            
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
                builder.SetRenderFunc((UpdateOccludersPassData data, ComputeGraphContext context) =>
                {
                    OccluderSubviewUpdate subviewUpdate = new OccluderSubviewUpdate(cameraData);
                    ctx.CreateFarDepthPyramid(context.cmd, in data.occluderParams, in data.occluderHandles, m_Planes, m_HiZCS, subviewUpdate);
                });
            }
        }
    }
}