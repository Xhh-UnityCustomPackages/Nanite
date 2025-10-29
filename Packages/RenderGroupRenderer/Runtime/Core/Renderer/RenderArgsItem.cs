using UnityEngine;

namespace RenderGroupRenderer
{
    /// <summary>
    /// 绘制的物体
    /// </summary>
    public class RenderArgsItem
    {
        public Mesh mesh;
        public Material material;
        public int argsOffset;
        private MaterialPropertyBlock m_MaterialPropertyBlock;
        
        public MaterialPropertyBlock MaterialPropertyBlock => m_MaterialPropertyBlock;

        public RenderArgsItem(Mesh mesh, Material material, int argsOffset)
        {
            this.mesh = mesh;
            this.material = Object.Instantiate(material);
            this.material.enableInstancing = true;
            this.argsOffset = argsOffset;
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
        }
    }
}
