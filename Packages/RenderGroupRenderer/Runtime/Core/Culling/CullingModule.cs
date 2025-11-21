using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace RenderGroupRenderer
{
    public class CullingModule
    {
        private CameraData m_CameraData;

        private NativeArray<Bounds> m_CullingBoundsNativeArray;
        private NativeArray<bool> m_CullingResultNativeArray;

        public CameraData CameraData => m_CameraData;
        public NativeArray<bool> CullingResultNativeArray => m_CullingResultNativeArray;

        public CullingModule()
        {
            m_CameraData = new CameraData();
        }

        public void SetCullingCamera(Camera camera)
        {
            m_CameraData.SetCamera(camera);
        }

        // public void Init(RenderGroup[] renderGroups)
        // {
        //     int groupCount = renderGroups.Length;
        //     m_CullingBoundsNativeArray = new NativeArray<Bounds>(groupCount, Allocator.Persistent);
        //     m_CullingResultNativeArray = new NativeArray<bool>(groupCount, Allocator.Persistent);
        //     for (int i = 0; i < groupCount; i++)
        //     {
        //         m_CullingBoundsNativeArray[i] = renderGroups[i].bounds;
        //         m_CullingResultNativeArray[i] = false;
        //     }
        // }

        #region Scene Culling 粗粒度剔除

        private BVHTree m_BVHTree;
        private List<BVHNode> m_VisibleNodes = new ();
        private int m_AfterBVHCullingRenderItemCount;
        
        public void AddToBVHFrustumCull(BVHTree tree)
        {
            m_BVHTree = tree;
        }
        #endregion

        public void Dispose()
        {
            m_CullingBoundsNativeArray.Dispose();
            m_CullingResultNativeArray.Dispose();
            m_CameraData.Dispose();
        }

        public void OnUpdate()
        {
        }

        public void OnLateUpdate()
        {
            if (m_CameraData.IsCameraDirty())
            {
                m_CameraData.CalculateCameraData();
                //场景BVH剔除
                BVHCulling();
                
                //根据剩余节点构造剔除后的Navive数据
                CreateCullingItemData();
                
                //逐RenderGroupItemData剔除
                CPUCulling();
            }
        }

        void BVHCulling()
        {
            if (m_BVHTree == null)
                return;
            
            //Debug代码 可以移除
            m_BVHTree.Iteration(g => g.SetCPUCullingResult(RenderGroup.ShowState.BVHCulling), null);

            m_VisibleNodes.Clear();
            m_AfterBVHCullingRenderItemCount = 0;
            m_BVHTree.FrustumCull(m_CameraData.cullingPlanes, m_VisibleNodes, ref m_AfterBVHCullingRenderItemCount);
        }
        
        void CreateCullingItemData()
        {
            if (m_CullingBoundsNativeArray.IsCreated)
            {
                if (m_CullingBoundsNativeArray.Length != m_AfterBVHCullingRenderItemCount)
                {
                    m_CullingBoundsNativeArray.Dispose();
                    m_CullingResultNativeArray.Dispose();
                    m_CullingBoundsNativeArray = new NativeArray<Bounds>(m_AfterBVHCullingRenderItemCount, Allocator.Persistent);
                    m_CullingResultNativeArray = new NativeArray<bool>(m_AfterBVHCullingRenderItemCount, Allocator.Persistent);
                }
            }
            else
            {
                m_CullingBoundsNativeArray = new NativeArray<Bounds>(m_AfterBVHCullingRenderItemCount, Allocator.Persistent);
                m_CullingResultNativeArray = new NativeArray<bool>(m_AfterBVHCullingRenderItemCount, Allocator.Persistent);
            }
            
            int index = 0;
            for (int i = 0; i < m_VisibleNodes.Count; i++)
            {
                var renerGroups = m_VisibleNodes[i].Objects;
                for (int n = 0; n < renerGroups.Count; n++)
                {
                    var renderGroup = renerGroups[n];
                    renderGroup.SetCPUCullingResult(RenderGroup.ShowState.PassBVHCulling);
                    m_CullingBoundsNativeArray[index] = renderGroup.bounds;
                    m_CullingResultNativeArray[index] = false;
                    
                    index++;
                }
            }

        }

        void CPUCulling()
        {
            int length = m_CullingBoundsNativeArray.Length;
            if (length <= 0)
                return;

            var cullJobs = RenderGroupCulling.CreateJob(m_CameraData.cullingPlaneArray, m_CullingBoundsNativeArray, m_CullingResultNativeArray);
            var job = cullJobs.Schedule(length, length);
            job.Complete();
        }

        public void OnDrawGizmos()
        {
            foreach (var node in m_VisibleNodes)
            {
                Gizmos.DrawWireCube(node.Bounds.center, node.Bounds.size);
            }
            // Gizmos.DrawFrustum(c);
            // for (int i = 0; i < m_CullingBoundsNativeArray.Length; i++)
            // {
            //     var bounds = m_CullingBoundsNativeArray[i];
            //     var result = m_CullingResultNativeArray[i];
            //     Gizmos.color = result ? Color.green : Color.red;
            //     Gizmos.DrawWireCube(bounds.center, bounds.size);
            // }
        }
    }
}