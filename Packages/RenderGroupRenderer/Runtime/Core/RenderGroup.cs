using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RenderGroupRenderer
{
    [System.Serializable]
    public class RenderGroup
    {
        public Bounds bounds;
        public RenderItem[] items;
        public int argsOffset;
        private bool m_IsShow = false;

        public bool IsShow => m_IsShow;
        
        public void SetCullingResult(bool isShow)
        {
            m_IsShow = isShow;
        }

        public void UpdateArgs(uint[] args)
        {
            //如果被剔除了的话,直接将整个
            for (int i = 0; i < items.Length; i++)
            {
                int index = argsOffset + 5 * i + 1;
                args[index] = (uint)(m_IsShow ? 1 : 0);
            }
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = m_IsShow ? Color.green : Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    [System.Serializable]
    public class RenderItem
    {
        public Bounds bounds;

        public Mesh mesh;
        public Material material;
        public int argsOffset;
        private MaterialPropertyBlock m_MaterialPropertyBlock;
        
        public MaterialPropertyBlock MaterialPropertyBlock => m_MaterialPropertyBlock;

        public RenderItem(Bounds bounds, Mesh mesh, Material material, int argsOffset)
        {
            this.bounds = bounds;
            this.mesh = mesh;
            this.material = Object.Instantiate(material);
            this.material.enableInstancing = true;
            this.argsOffset = argsOffset;
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
        }
    }
}