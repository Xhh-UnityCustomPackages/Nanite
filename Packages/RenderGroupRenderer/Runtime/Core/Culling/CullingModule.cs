using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

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

        // private NativeArray<FBoxSphereBounds> m_CullingBoundsNativeArray;
        // private NativeArray<int> m_CullingGroupIDsNativeArray;
        // private NativeArray<bool> m_CullingResultNativeArray;

        public CameraData CameraData => m_CameraData;

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
                
                //逐RenderGroupItemData剔除
                CPUPreItemCulling();
            }
        }

        void BVHCulling()
        {
            if (m_BVHTree == null)
                return;

            Profiler.BeginSample("CullingModule BVHCulling");
            if (m_System != null && m_System.showDebug)
            {
                //Debug代码 可以移除
                m_BVHTree.Iteration(g => g.SetCPUCullingResult(RenderGroup.ShowState.BVHCulling), null);
            }


            m_VisibleNodes.Clear();
            m_AfterBVHCullingRenderItemCount = 0;
            m_BVHTree.FrustumCull(Flags, ConvexVolume, m_VisibleNodes, m_System.infoModule.cullResult, ref m_AfterBVHCullingRenderItemCount);
            
            //更新 info剔除结果
            Profiler.EndSample();
        }

        void CPUPreItemCulling()
        {
            if (m_VisibleNodes.Count <= 0)
                return;
            
            Profiler.BeginSample("CullingModule CPUPreItemCulling");
            var cullingBoundsNativeArray = new NativeArray<FBoxSphereBounds>(m_AfterBVHCullingRenderItemCount, Allocator.TempJob);
            var cullingGroupIDsNativeArray = new NativeArray<int>(m_AfterBVHCullingRenderItemCount, Allocator.TempJob);
            var cullingResultNativeArray = new NativeArray<bool>(m_AfterBVHCullingRenderItemCount, Allocator.TempJob);

            int index = 0;
            for (int i = 0; i < m_VisibleNodes.Count; i++)
            {
                var renerGroups = m_VisibleNodes[i].Objects;
                for (int n = 0; n < renerGroups.Count; n++)
                {
                    var renderGroup = renerGroups[n];
                    renderGroup.SetCPUCullingResult(RenderGroup.ShowState.PassBVHCulling);
                    cullingGroupIDsNativeArray[index] = renderGroup.groupID;
                    cullingBoundsNativeArray[index] = renderGroup.bounds;
                    cullingResultNativeArray[index] = false;

                    index++;
                }
            }
            
            int length = cullingBoundsNativeArray.Length;

            var cullJobs = RenderGroupCulling.CreateJob(Flags, ConvexVolume, cullingBoundsNativeArray, cullingResultNativeArray);
            var job = cullJobs.Schedule(length, length);
            job.Complete();
            
            //读取Group剔除结果 直接设置GroupItem的剔除结果
            for (int i = 0; i < cullingResultNativeArray.Length; i++)
            {
                bool result = cullingResultNativeArray[i];
                var groupID = cullingGroupIDsNativeArray[i];
                var items = m_System.renderGroups[groupID].items;
                for (int j = 0; j < items.Length; j++)
                {
                    m_System.infoModule.cullResult[items[j].itemID] = (uint)(result?1:0);
                }
            }

            cullingBoundsNativeArray.Dispose();
            cullingGroupIDsNativeArray.Dispose();
            cullingResultNativeArray.Dispose();
            
            Profiler.EndSample();
        }

        public void OnDrawGizmos()
        {
            if (!Flags.bShouldVisibilityCull)
            {
                return;
            }
            
            foreach (var node in m_VisibleNodes)
            {
                // Gizmos.DrawWireSphere(node.Bounds.Origin, node.Bounds.SphereRadius);
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