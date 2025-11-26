using RenderGroupRenderer.Info;
using UnityEngine;
using Sirenix.OdinInspector;

namespace RenderGroupRenderer
{
    public class RenderGroupRenderer
    {
        private RenderArgsItem[] m_RenderItems;
        private Bounds m_Bounds;

        public ComputeShader m_CullingCS;
        private ComputeBuffer m_FrustumPlanesBuffer;
        

        public ComputeShader m_SortCS;
        private ComputeBuffer m_CullSortIDBuffer;
        private uint[] m_CullSortIDsArray;
        private ComputeBuffer m_InsertCountBuffer;
        private uint[] m_InsertCountArray;

        public delegate void UpdateMaterial(MaterialPropertyBlock mpb);
        public UpdateMaterial updateMaterial;

        private RendererInfoModule m_InfoModule;
        private CullingModule m_CullingModule;
        private RenderGroupRendererSystem m_System;

        public RenderGroupRenderer(RenderGroupRendererSystem system)
        {
            m_System = system;
        }

        public void Init(RenderArgsItem[] renderGroups, RendererInfoModule infoModule, CullingModule cullingModule)
        {
            m_RenderItems = renderGroups;
            m_Bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
            
            m_InfoModule = infoModule;
            m_CullingModule = cullingModule;
        }

        public void Dispose()
        {
            m_FrustumPlanesBuffer?.Dispose();
            m_CullSortIDBuffer?.Dispose();
            m_InsertCountBuffer?.Dispose();
        }


        public void OnLateUpdate()
        {
            UpdateGPUBuffer();
            GPUCulling();
            Sort();
            Renderer();
        }

        private void UpdateGPUBuffer()
        {
            if (m_FrustumPlanesBuffer == null)
            {
                m_FrustumPlanesBuffer = new ComputeBuffer(6, sizeof(float) * 4);
            }
            
            //清空剔除结果
            m_InfoModule.argsBuffer.SetData(m_InfoModule.args);
            //回退到CPU剔除的结果
            m_InfoModule.cullResultBuffer.SetData(m_InfoModule.cullResult);
        }

        private void GPUCulling()
        {
            int totalCount = m_InfoModule.rendererItemCount;
            
            int kernel = 0;
            m_CullingCS.SetBuffer(kernel, "_IndirectArgsBuffer", m_InfoModule.argsBuffer);//间接绘制Buffer
            m_CullingCS.SetBuffer(kernel, "_BoundsBuffer", m_InfoModule.boundsBuffer);//每个物体的包围盒信息
            m_CullingCS.SetBuffer(kernel, "_RenderIDBuffer", m_InfoModule.rendererIDBuffer);
            m_CullingCS.SetBuffer(kernel, "_CullResultBuffer", m_InfoModule.cullResultBuffer);
            
            m_CullingCS.SetInt("_ItemCount", totalCount);
            m_CullingCS.SetInt("_RenderCount", m_InfoModule.renderTypeCount);

            m_FrustumPlanesBuffer.SetData(m_CullingModule.CameraData.cullingPlanes);
            m_CullingCS.SetBuffer(kernel, "_FrustumPlanesBuffer", m_FrustumPlanesBuffer);
            
            m_SortCS.GetKernelThreadGroupSizes(kernel, out var threadGroupSizeX, out _, out _);
            int threadGroups = Mathf.CeilToInt(totalCount / (float)threadGroupSizeX);
            m_CullingCS.Dispatch(kernel, threadGroups, 1, 1);
        }

        void Sort()
        {
            int totalCount = m_InfoModule.rendererItemCount;
            if (m_CullSortIDBuffer == null)
            {
                m_CullSortIDBuffer = new ComputeBuffer(totalCount, sizeof(uint));
                m_CullSortIDsArray = new uint[totalCount];
            }

            if (m_InsertCountBuffer == null)
            {
                m_InsertCountBuffer = new ComputeBuffer(m_InfoModule.renderTypeCount, sizeof(uint), ComputeBufferType.Counter);
                m_InsertCountArray = new uint[m_InfoModule.renderTypeCount];
            }

            //清空排序结果
            m_CullSortIDBuffer.SetData(m_CullSortIDsArray);
            //清空插入结果
            m_InsertCountBuffer.SetData(m_InsertCountArray);

            int kernel = 0;
            m_SortCS.SetBuffer(kernel, "_IndirectArgsBuffer", m_InfoModule.argsBuffer); //间接绘制Buffer
            m_SortCS.SetBuffer(kernel, "_RenderIDBuffer", m_InfoModule.rendererIDBuffer);
            m_SortCS.SetBuffer(kernel, "_CullResultBuffer", m_InfoModule.cullResultBuffer);
            m_SortCS.SetBuffer(kernel, "_SortIDBuffer", m_CullSortIDBuffer);
            m_SortCS.SetBuffer(kernel, "_InsertCountBuffer", m_InsertCountBuffer);

            m_SortCS.SetInt("_ItemCount", totalCount);
            m_SortCS.GetKernelThreadGroupSizes(kernel, out var threadGroupSizeX, out _, out _);
            int threadGroups = Mathf.CeilToInt(totalCount / (float)threadGroupSizeX);
            m_SortCS.Dispatch(kernel, threadGroups, 1, 1);
            
        }

        private void Renderer()
        {
            for (int i = 0; i < m_RenderItems.Length; i++)
            {
                var renderItem = m_RenderItems[i];

                var mesh = renderItem.mesh;
                var material = renderItem.material;
                var mpb = renderItem.MaterialPropertyBlock;
                updateMaterial?.Invoke(mpb);
                mpb.SetInt("_ArgIndex", i);
                mpb.SetBuffer("_SortIDBuffer", m_CullSortIDBuffer);
                int argsOffset = renderItem.argsOffset;
                Graphics.DrawMeshInstancedIndirect(mesh, 0, material, m_Bounds, m_InfoModule.argsBuffer, argsOffset * sizeof(uint), mpb);
            }
        }

       
    }
}