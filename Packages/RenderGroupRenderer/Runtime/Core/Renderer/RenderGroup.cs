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
        public Bounds bounds;
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

        public void OnDrawGizmos()
        {
            switch (m_ShowState)
            {
                case ShowState.BVHCulling: Gizmos.color = Color.red; break;
                case ShowState.PassBVHCulling: Gizmos.color = Color.cyan; break;
                case ShowState.PassFrustumCulling: Gizmos.color = Color.green; break;
            }
            
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            for (int i = 0; i < items.Length; i++)
            {
                Gizmos.DrawWireCube(items[i].bounds.center, items[i].bounds.size);
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
        public Bounds bounds;
        public uint renderID;
        
        public RenderGroupItem(Bounds bounds, uint renderID)
        {
            this.bounds = bounds;
            this.renderID = renderID;
        }
    }
}