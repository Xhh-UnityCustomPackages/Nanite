using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RenderGroupRenderer
{
    /// <summary>
    /// 一系列聚合物体
    /// </summary>
    [System.Serializable]
    public class RenderGroup
    {
        public int groupID;
        public FBoxSphereBounds bounds;
        public RenderGroupItem[] items;
       
        private ShowState m_ShowState;

        public enum ShowState
        {
            BVHCulling,//场景剔除了
            PassBVHCulling,//通过场景剔除
            PassFrustumCulling,//通过视锥剔除
        }

        public void SetCPUCullingResult(ShowState showState)
        {
            m_ShowState = showState;
        }

        public void OnDrawGizmos(CullingModule cullingModule)
        {
            switch (m_ShowState)
            {
                case ShowState.BVHCulling: Gizmos.color = Color.red; break;
                case ShowState.PassBVHCulling: Gizmos.color = Color.cyan; break;
                case ShowState.PassFrustumCulling: Gizmos.color = Color.green; break;
            }
            
            if(cullingModule.Flags.bUseSphereTestFirst)
                Gizmos.DrawWireSphere(bounds.Origin, bounds.SphereRadius);
            else
                Gizmos.DrawWireCube(bounds.Origin, 2 * bounds.BoxExtent);

            for (int i = 0; i < items.Length; i++)
            {   
                if(cullingModule.Flags.bUseSphereTestFirst)
                    Gizmos.DrawWireSphere(items[i].bounds.Origin, items[i].bounds.SphereRadius);
                else
                    Gizmos.DrawWireCube(items[i].bounds.Origin, 2 * items[i].bounds.BoxExtent);
            }
        }
    }

    /// <summary>
    /// 渲染的小分件 
    /// </summary>
    [System.Serializable]
    public class RenderGroupItem
    {
        public int groupID;
        public int itemID;
        public FBoxSphereBounds bounds;
        public uint renderID;
        
        public RenderGroupItem(FBoxSphereBounds bounds, uint renderID)
        {
            this.bounds = bounds;
            this.renderID = renderID;
        }
    }
}