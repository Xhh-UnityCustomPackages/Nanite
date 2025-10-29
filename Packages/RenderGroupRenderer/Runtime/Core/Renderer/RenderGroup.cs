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
        public Bounds bounds;
        public RenderGroupItem[] items;
        private bool m_IsShow = false;

        public bool IsShow => m_IsShow;
        
        public void SetCullingResult(bool isShow)
        {
            m_IsShow = isShow;
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = m_IsShow ? Color.green : Color.red;
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
        public Bounds bounds;
        public uint renderID;
        
        public RenderGroupItem(Bounds bounds, uint renderID)
        {
            this.bounds = bounds;
            this.renderID = renderID;
        }
    }
}