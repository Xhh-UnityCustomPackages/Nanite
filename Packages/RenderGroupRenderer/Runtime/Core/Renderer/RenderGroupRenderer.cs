using UnityEngine;
using Sirenix.OdinInspector;

namespace RenderGroupRenderer
{
    public class RenderGroupRenderer
    {
        private uint[] m_Args;
        private ComputeBuffer m_ArgsBuffer;
        private RenderGroup[] m_RenderGroups;
        private Bounds m_Bounds;

        public delegate void UpdateMaterial(MaterialPropertyBlock mpb);
        public UpdateMaterial updateMaterial;
        
        public void Init(uint[] args, ComputeBuffer argsBuffer, RenderGroup[] renderGroups)
        {
            m_Args = args;
            m_ArgsBuffer = argsBuffer;
            m_RenderGroups = renderGroups;
            m_Bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        }


        public void OnUpdate()
        {
            UpdateGPUBuffer();
            Renderer();
        }

        private void UpdateGPUBuffer()
        {
            //这样就会重置数据
            m_ArgsBuffer.SetData(m_Args);
        }

        private void Renderer()
        {
            //如果当前Group没有被CPU剔除的话 进入渲染

            //GPUCulling

            for (int i = 0; i < m_RenderGroups.Length; i++)
            {
                var renderGroup = m_RenderGroups[i];
                if (!renderGroup.IsShow) continue;

                for (int j = 0; j < renderGroup.items.Length; j++)
                {
                    var renderItem = renderGroup.items[j];
                    var mesh = renderItem.mesh;
                    var material = renderItem.material;
                    var mpb = renderItem.MaterialPropertyBlock;
                    updateMaterial?.Invoke(mpb);
                    int argsOffset = renderItem.argsOffset;
                    Graphics.DrawMeshInstancedIndirect(mesh, 0, material, m_Bounds, m_ArgsBuffer, argsOffset, mpb);
                }
            }
        }

       
    }
}