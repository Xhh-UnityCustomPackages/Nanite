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
        
        
        
        private ComputeBuffer m_ArgsBuffer;
        [ShowInInspector, ReadOnly] private uint[] m_Args;

        public uint[] args => m_Args;
        public ComputeBuffer argsBuffer => m_ArgsBuffer;
        
        public void Init(RenderGroupData renderGroupData, RenderInfoData renderInfoData)
        {
            CreateBuffer(renderGroupData, renderInfoData);
        }

        void CreateBuffer(RenderGroupData renderGroupData, RenderInfoData renderInfoData)
        {
            int count = renderGroupData.totalCount;
            int index = 0;
            int groupID = 0;
            int argsLastIndex = 0;
            //
            m_TransformBuffer = new ComputeBuffer(count, Marshal.SizeOf<float4x4>());
            m_TransformsArray = new float4x4[count];
            m_BoundsBuffer = new ComputeBuffer(count, Marshal.SizeOf<Bounds>());
            m_BoundsArray = new Bounds[count];
            m_GroupIDBuffer = new ComputeBuffer(count, Marshal.SizeOf<int>());
            m_GroupIDsArray = new int[count];

            m_Args = new uint[5 * count];
            m_ArgsBuffer = new ComputeBuffer(m_Args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            
            foreach (var groupData in renderGroupData.groupDatas)
            {
                foreach (var itemData in groupData.itemDatas)
                {
                    m_TransformsArray[index] = itemData.transform.GetTransformMatrix();
                    m_BoundsArray[index] = itemData.bounds;
                    m_GroupIDsArray[index] = groupID;
                    index++;

                    var renderInfo = renderInfoData.GetItemData(itemData.itemID);
                    var mesh = renderInfo.mesh;
                    m_Args[argsLastIndex++] = mesh.GetIndexCount(0); // index count per instance
                    m_Args[argsLastIndex++] = 0;
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // start index location
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // base vertex location
                    m_Args[argsLastIndex++] = mesh.GetBaseVertex(0); // start instance location
                }

                groupID++;
            }

            m_TransformBuffer.SetData(m_TransformsArray);
            m_BoundsBuffer.SetData(m_BoundsArray);
            m_GroupIDBuffer.SetData(m_GroupIDsArray);
            m_ArgsBuffer.SetData(m_Args);
        }

        public void UpdateMaterial(MaterialPropertyBlock mpb)
        {
            mpb.SetBuffer("_TransformBuffer", m_TransformBuffer);
            mpb.SetBuffer("_GroupIDBuffer", m_GroupIDBuffer);
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