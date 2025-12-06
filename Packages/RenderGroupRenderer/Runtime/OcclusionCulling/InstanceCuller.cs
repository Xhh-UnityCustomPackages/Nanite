using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace RenderGroupRenderer
{
    public struct InstanceCuller : IDisposable
    {
        private ProfilingSampler m_ProfilingSampleInstanceOcclusionTest;

        public void Init()
        {
            m_ProfilingSampleInstanceOcclusionTest = new ProfilingSampler("InstanceOcclusionTest");
        }

        public void Dispose()
        {
        }
        
        private class InstanceOcclusionTestPassData
        {
            // public OcclusionCullingSettings settings;
            // public InstanceOcclusionTestSubviewSettings subviewSettings;
            public OccluderHandles occluderHandles;
            // public IndirectBufferContextHandles bufferHandles;
        }

        public void InstanceOcclusionTest(RenderGraph renderGraph)
        {
            using (var builder = renderGraph.AddComputePass<InstanceOcclusionTestPassData>("Instance Occlusion Test", out var passData, m_ProfilingSampleInstanceOcclusionTest))
            {
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((InstanceOcclusionTestPassData data, ComputeGraphContext context) =>
                {
                    
                });
            }
        }
    }
}