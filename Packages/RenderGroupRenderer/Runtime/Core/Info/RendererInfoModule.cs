using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace RenderGroupRenderer.Info
{
    [SerializeField]
    public class RendererInfoModule
    {
        //按照Item排列的
        private ComputeBuffer m_TransformBuffer; // 存放local to world
        [ShowInInspector, ReadOnly] private float4x4[] m_TransformsArray;
        private ComputeBuffer m_BoundsBuffer;
        [ShowInInspector, ReadOnly] private Bounds[] m_BoundsArray;
        private ComputeBuffer m_GroupIDBuffer;
        [ShowInInspector, ReadOnly] private int[] m_GroupIDsArray;
        private ComputeBuffer m_RenderIDBuffer;
        [ShowInInspector, ReadOnly] private uint[] m_RenderIDsArray;
      
        
        
        
        private ComputeBuffer m_ArgsBuffer;
        [ShowInInspector, ReadOnly] private uint[] m_Args;

        public uint[] args => m_Args;
        public ComputeBuffer argsBuffer => m_ArgsBuffer;
        public ComputeBuffer boundsBuffer => m_BoundsBuffer;
        public ComputeBuffer rendererIDBuffer => m_RenderIDBuffer;
        public int rendererItemCount { get; set; }
        public int renderTypeCount { get; private set; }

        public void Init(RenderGroupData renderGroupData, RenderInfoData renderInfoData)
        {
            CreateBuffer(renderGroupData, renderInfoData);
            renderTypeCount = renderInfoData.renderItems.Count;
        }

        void CreateDataBuffer<T>(int count, ref ComputeBuffer computeBuffer, ref T[] dataArray) where T : struct
        {
            computeBuffer = new ComputeBuffer(count, Marshal.SizeOf<T>());
            dataArray = new T[count];
        }

        void CreateBuffer(RenderGroupData renderGroupData, RenderInfoData renderInfoData)
        {
            int count = renderGroupData.totalCount;
            int index = 0;
            
            //
            CreateDataBuffer<float4x4>(count, ref m_TransformBuffer, ref m_TransformsArray);
            CreateDataBuffer<Bounds>(count, ref m_BoundsBuffer, ref m_BoundsArray);
            CreateDataBuffer<int>(count, ref m_GroupIDBuffer, ref m_GroupIDsArray);
            CreateDataBuffer<uint>(count, ref m_RenderIDBuffer, ref m_RenderIDsArray);

            for (int i = 0; i < renderGroupData.groupDatas.Count; i++)
            {
                var groupData = renderGroupData.groupDatas[i];
                for (int j = 0; j < groupData.itemDatas.Count; j++)
                {
                    var itemData =  groupData.itemDatas[j];
                    m_TransformsArray[index] = itemData.transform.GetTransformMatrix();
                    m_BoundsArray[index] = itemData.bounds;
                    m_GroupIDsArray[index] = i;
                    m_RenderIDsArray[index] = itemData.itemID;
                    index++;
                }
            }

            m_TransformBuffer.SetData(m_TransformsArray);
            m_BoundsBuffer.SetData(m_BoundsArray);
            m_GroupIDBuffer.SetData(m_GroupIDsArray);
            m_RenderIDBuffer.SetData(m_RenderIDsArray);
            
            rendererItemCount = index;
            
            int argsLastIndex = 0;
            m_Args = new uint[5 * renderInfoData.renderItems.Count];
            m_ArgsBuffer = new ComputeBuffer(m_Args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            for (int i = 0; i < renderInfoData.renderItems.Count; i++)
            {
                var renderInfo = renderInfoData.renderItems[i];
                var mesh = renderInfo.mesh;
                m_Args[argsLastIndex++] = mesh.GetIndexCount(0); // index count per instance
                m_Args[argsLastIndex++] = 0;
                m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // start index location
                m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // base vertex location
                m_Args[argsLastIndex++] = 0;//mesh.GetBaseVertex(0); // start instance location
            }
            
            m_ArgsBuffer.SetData(m_Args);
        }

        public void UpdateMaterial(MaterialPropertyBlock mpb)
        {
            mpb.SetBuffer("_TransformBuffer", m_TransformBuffer);
            mpb.SetBuffer("_GroupIDBuffer", m_GroupIDBuffer);
            mpb.SetBuffer("_IndirectArgsBuffer", m_ArgsBuffer);
        }

        public void Dispose()
        {
            m_TransformBuffer.Dispose();
            m_BoundsBuffer.Dispose();
            m_GroupIDBuffer.Dispose();
            m_ArgsBuffer.Dispose();
        }
    }
}