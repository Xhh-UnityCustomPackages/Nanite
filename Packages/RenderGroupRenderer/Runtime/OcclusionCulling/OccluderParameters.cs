using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace RenderGroupRenderer
{
    public struct OccluderParameters
    {
        /// <summary>The depth texture to read.</summary>
        public TextureHandle depthTexture;
        /// <summary>The size in pixels of the area of the depth data to read.</summary>
        public Vector2Int depthSize;
        
        public RTHandle depthTextureRTHandle;// 这是为了兼容非RenderGraph模式
    }
    
    internal struct OccluderHandles
    {
        public TextureHandle occluderDepthPyramid;
    }
}