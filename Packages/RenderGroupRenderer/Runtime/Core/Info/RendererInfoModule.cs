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
        private ComputeBuffer m_CullResultBuffer;
        [ShowInInspector, ReadOnly] private uint[] m_CullResultArray;
      
        
        
        
        private ComputeBuffer m_ArgsBuffer;
        [ShowInInspector, ReadOnly] private uint[] m_Args;

        public uint[] cullResult => m_CullResultArray;
        public uint[] args => m_Args;
        public ComputeBuffer argsBuffer => m_ArgsBuffer;
        public ComputeBuffer boundsBuffer => m_BoundsBuffer;
        public ComputeBuffer rendererIDBuffer => m_RenderIDBuffer;
        public ComputeBuffer cullResultBuffer => m_CullResultBuffer;
        public int rendererItemCount { get; set; }
        public int renderTypeCount { get; private set; }

        private RenderGroupRendererSystem m_System;

        public RendererInfoModule(RenderGroupRendererSystem system)
        {
            m_System = system;
        }

        public void Init(RenderGroupData renderGroupData, RenderInfoData renderInfoData)
        {
            CreateBuffer(renderGroupData, renderInfoData);
            renderTypeCount = renderInfoData.renderItems.Count;
        }

        void CreateDataAndBuffer<T>(int count, out ComputeBuffer computeBuffer, out T[] dataArray) where T : struct
        {
            computeBuffer = new ComputeBuffer(count, Marshal.SizeOf<T>());
            dataArray = new T[count];
        }

        void CreateBuffer(RenderGroupData renderGroupData, RenderInfoData renderInfoData)
        {
            int count = renderGroupData.totalCount;

            int index = 0;
            
            //
            CreateDataAndBuffer(count, out m_TransformBuffer, out m_TransformsArray);
            CreateDataAndBuffer(count, out m_BoundsBuffer, out m_BoundsArray);
            CreateDataAndBuffer(count, out m_GroupIDBuffer, out m_GroupIDsArray);
            CreateDataAndBuffer(count, out m_RenderIDBuffer, out m_RenderIDsArray);
            CreateDataAndBuffer(count, out m_CullResultBuffer, out m_CullResultArray);

            for (int i = 0; i < renderGroupData.groupDatas.Count; i++)
            {
                var groupData = renderGroupData.groupDatas[i];
                for (int j = 0; j < groupData.itemDatas.Count; j++)
                {
                    var itemData = groupData.itemDatas[j];
                    m_TransformsArray[index] = itemData.transform.GetTransformMatrix();
                    m_BoundsArray[index] = itemData.bounds;
                    m_GroupIDsArray[index] = i;
                    m_RenderIDsArray[index] = itemData.itemID;
                    m_CullResultArray[index] = 1;//设置为1 为都显示状态
                    index++;
                }
            }

            m_TransformBuffer.SetData(m_TransformsArray);
            m_BoundsBuffer.SetData(m_BoundsArray);
            m_GroupIDBuffer.SetData(m_GroupIDsArray);
            m_RenderIDBuffer.SetData(m_RenderIDsArray);
            m_CullResultBuffer.SetData(m_CullResultArray);
            
            rendererItemCount = index;
            
            int argsLastIndex = 0;
            int argsLength = 5 * renderInfoData.renderItems.Count;
            if (m_System.useLOD)
            {
                argsLength *= 3;
            }

            m_Args = new uint[argsLength];
            m_ArgsBuffer = new ComputeBuffer(m_Args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            for (int i = 0; i < renderInfoData.renderItems.Count; i++)
            {
                if (m_System.useLOD)
                {
                    var lod0Info = renderInfoData.renderItems[i].data.lod0Info;
                    var lod1Info = renderInfoData.renderItems[i].data.lod1Info;
                    var lod2Info = renderInfoData.renderItems[i].data.lod2Info;
                    var mesh = lod0Info.mesh;
                    m_Args[argsLastIndex++] = mesh.GetIndexCount(0); // index count per instance
                    m_Args[argsLastIndex++] = 0;
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // start index location
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // base vertex location
                    m_Args[argsLastIndex++] = 0; //mesh.GetBaseVertex(0); // start instance location
                    mesh = lod1Info.mesh;
                    m_Args[argsLastIndex++] = mesh.GetIndexCount(0); // index count per instance
                    m_Args[argsLastIndex++] = 0;
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // start index location
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // base vertex location
                    m_Args[argsLastIndex++] = 0; //mesh.GetBaseVertex(0); // start instance location
                    mesh = lod2Info.mesh;
                    m_Args[argsLastIndex++] = mesh.GetIndexCount(0); // index count per instance
                    m_Args[argsLastIndex++] = 0;
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // start index location
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // base vertex location
                    m_Args[argsLastIndex++] = 0; //mesh.GetBaseVertex(0); // start instance location
                }
                else
                {
                    var lod0Info = renderInfoData.renderItems[i].data.lod0Info;
                    var mesh = lod0Info.mesh;
                    m_Args[argsLastIndex++] = mesh.GetIndexCount(0); // index count per instance
                    m_Args[argsLastIndex++] = 0;
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // start index location
                    m_Args[argsLastIndex++] = mesh.GetIndexStart(0); // base vertex location
                    m_Args[argsLastIndex++] = 0; //mesh.GetBaseVertex(0); // start instance location
                }
            }

            // LogArgs();
            
            m_ArgsBuffer.SetData(m_Args);
        }

        void LogArgs()
        {
            for (int i = 0; i < m_Args.Length; i++)
            {
                Debug.LogError($"{i} : {m_Args[i]}");
            }
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
            m_CullResultBuffer.Dispose();
        }
    }
}