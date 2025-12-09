using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace RenderGroupRenderer
{
    [System.Serializable]
    public struct FFrustumCullingFlags
    {
        public bool bShouldVisibilityCull;
        // public bool bUseCustomCulling;
        public bool bUseSphereTestFirst;
        public bool bUseFastIntersect;
        public bool bUseVisibilityOctree;
        // public bool bHasHiddenPrimitives;
        // public bool bHasShowOnlyPrimitives;
    }
    
    public class CullingModule
    {
        public FFrustumCullingFlags Flags;
        private FConvexVolume ConvexVolume;
        
        private CameraData m_CameraData;

        private NativeArray<FBoxSphereBounds> m_CullingBoundsNativeArray;
        private NativeArray<int> m_CullingGroupIDsNativeArray;
        private NativeArray<bool> m_CullingResultNativeArray;

        public CameraData CameraData => m_CameraData;
        public NativeArray<bool> CullingResultNativeArray => m_CullingResultNativeArray;

        private RenderGroupRendererSystem m_System;

        public CullingModule(RenderGroupRendererSystem system)
        {
            m_System = system;
            m_CameraData = new CameraData();
            ConvexVolume = m_CameraData.GetCullingFrustum();

            Flags.bShouldVisibilityCull = true;
        }

        public void SetCullingCamera(Camera camera)
        {
            m_CameraData.SetCamera(camera);
        }

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
            ConvexVolume.Dispose();
        }

        public void OnUpdate()
        {
            
        }

        public void OnLateUpdate()
        {
            if (!Flags.bShouldVisibilityCull)
            {
                for (int i = 0; i < m_System.infoModule.cullResult.Length; i++)
                {
                    m_System.infoModule.cullResult[i] = 1;
                }
                return;
            }

            if (m_CameraData.IsCameraDirty())
            {
                m_CameraData.CalculateCameraData();
                ConvexVolume.Update(m_CameraData.cullingPlanes);
                //场景BVH剔除
                BVHCulling();
                
                //根据剩余节点构造剔除后的Navive数据
                CreateCullingItemData();
                
                //逐RenderGroupItemData剔除
                CPUPreItemCulling();
            }
        }

        void BVHCulling()
        {
            if (m_BVHTree == null)
                return;

            if (m_System != null && m_System.showDebug)
            {
                //Debug代码 可以移除
                m_BVHTree.Iteration(g => g.SetCPUCullingResult(RenderGroup.ShowState.BVHCulling), null);
            }


            m_VisibleNodes.Clear();
            m_AfterBVHCullingRenderItemCount = 0;
            m_BVHTree.FrustumCull(Flags, ConvexVolume, m_VisibleNodes, m_System.infoModule.cullResult, ref m_AfterBVHCullingRenderItemCount);
            
            //更新 info剔除结果
            
        }
        
        void CreateCullingItemData()
        {
            if (m_CullingBoundsNativeArray.IsCreated)
            {
                if (m_CullingBoundsNativeArray.Length != m_AfterBVHCullingRenderItemCount)
                {
                    m_CullingBoundsNativeArray.Dispose();
                    m_CullingResultNativeArray.Dispose();
                    m_CullingGroupIDsNativeArray.Dispose();
                    m_CullingBoundsNativeArray = new NativeArray<FBoxSphereBounds>(m_AfterBVHCullingRenderItemCount, Allocator.Persistent);
                    m_CullingGroupIDsNativeArray = new NativeArray<int>(m_AfterBVHCullingRenderItemCount, Allocator.Persistent);
                    m_CullingResultNativeArray = new NativeArray<bool>(m_AfterBVHCullingRenderItemCount, Allocator.Persistent);
                }
            }
            else
            {
                m_CullingBoundsNativeArray = new NativeArray<FBoxSphereBounds>(m_AfterBVHCullingRenderItemCount, Allocator.Persistent);
                m_CullingGroupIDsNativeArray = new NativeArray<int>(m_AfterBVHCullingRenderItemCount, Allocator.Persistent);
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
                    m_CullingGroupIDsNativeArray[index] = renderGroup.groupID;
                    m_CullingBoundsNativeArray[index] = renderGroup.bounds;
                    m_CullingResultNativeArray[index] = false;
                    
                    index++;
                }
            }

        }

        void CPUPreItemCulling()
        {
            int length = m_CullingBoundsNativeArray.Length;
            if (length <= 0)
                return;

            var cullJobs = RenderGroupCulling.CreateJob(Flags, ConvexVolume, m_CullingBoundsNativeArray, m_CullingGroupIDsNativeArray, m_CullingResultNativeArray);
            var job = cullJobs.Schedule(length, length);
            job.Complete();
            
            //读取Group剔除结果 直接设置GroupItem的剔除结果
            for (int i = 0; i < m_CullingResultNativeArray.Length; i++)
            {
                bool result = m_CullingResultNativeArray[i];
                var groupID = m_CullingGroupIDsNativeArray[i];
                var items = m_System.renderGroups[groupID].items;
                for (int j = 0; j < items.Length; j++)
                {
                    m_System.infoModule.cullResult[items[j].itemID] = (uint)(result?1:0);
                }
            }
        }

        public void OnDrawGizmos()
        {
            if (!Flags.bShouldVisibilityCull)
            {
                return;
            }
            
            foreach (var node in m_VisibleNodes)
            {
                if (Flags.bUseSphereTestFirst)
                    Gizmos.DrawWireSphere(node.Bounds.Origin, node.Bounds.SphereRadius);
                else
                    Gizmos.DrawWireCube(node.Bounds.Origin, 2 * node.Bounds.BoxExtent);
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