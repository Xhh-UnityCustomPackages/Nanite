using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
            Flags.bUseVisibilityOctree = false;
        }

        public void SetCullingCamera(Camera camera)
        {
            m_CameraData.SetCamera(camera);
        }

        #region Scene Culling 粗粒度剔除

        private BVHTree m_BVHTree;
        // private TOctree2<RenderGroup> m_Octree;
        private NativeList<int> m_VisibleNodes = new (Allocator.Persistent);
        
        public void AddToBVHFrustumCull(BVHTree tree)
        {
            m_BVHTree = tree;
        }
        // public void AddToSceneFrustumCull(TOctree2<RenderGroup> tree)
        // {
        //     m_Octree = tree;
        // }
        #endregion

        public void Dispose()
        {
            m_CameraData.Dispose();
            ConvexVolume.Dispose();
            m_VisibleNodes.Dispose();
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
                if (Flags.bUseVisibilityOctree)
                {
                    //场景BVH剔除
                    BVHCulling();
                }
                
                // CullOctree();
                
                CPUPreItemCulling();
            }
        }

        private FSceneBitArray OutVisibleNodes;
        
        // void CullOctree()
        // {
        //     if(m_Octree == null)
        //         return;
        //
        //     if (OutVisibleNodes == null)
        //         OutVisibleNodes = new();
        //     OutVisibleNodes.Init(false, m_Octree.GetNumNodes() * 2);
        //     
        //     Profiler.BeginSample("CullingModule CullOctree");
        //     
        //     m_Octree.FindElementsWithPredicate(CullOcttreeNode, Func);
        //     Profiler.EndSample();
        // }

        bool CullOcttreeNode(uint ParentNodeIndex, uint NodeIndex, FOctreeNodeContext contenxt)
        {
            // If the parent node is completely contained there is no need to test containment
            if (ParentNodeIndex != FOctreeElementId2.INDEX_NONE && !OutVisibleNodes[(ParentNodeIndex * 2) + 1])
            {
                OutVisibleNodes[NodeIndex * 2] = true;
                OutVisibleNodes[NodeIndex * 2 + 1] = false;
                return true;
            }
                    
            bool bIntersects = false;
            bIntersects = ConvexVolume.IntersectBox(contenxt.Bounds.Center, contenxt.Bounds.Extent);
                    
            if (bIntersects)
            {
                OutVisibleNodes[NodeIndex * 2] = true;
                OutVisibleNodes[NodeIndex * 2 + 1] = ConvexVolume.GetBoxIntersectionOutcode(contenxt.Bounds.Center, contenxt.Bounds.Extent).GetOutside();
            }
                    
            return bIntersects;
        }

        void BVHCulling()
        {
            if (m_BVHTree == null)
                return;

            Profiler.BeginSample("CullingModule BVHCulling");
            m_BVHTree.Iteration(g => g.SetCPUCullingResult(RenderGroup.ShowState.BVHCulling), null);
            m_VisibleNodes.Clear();
            m_BVHTree.FrustumCull(Flags, ConvexVolume, m_VisibleNodes, m_System.infoModule.cullResult);
            
            //更新 info剔除结果
            Profiler.EndSample();
        }
        
        void CPUPreItemCulling()
        {
            if (!Flags.bUseVisibilityOctree)
            {
                Profiler.BeginSample("CullingModule Reset Visible Nodes");
                if (m_VisibleNodes.Length != m_System.renderGroups.Length)
                {
                    m_VisibleNodes.Clear();
                    foreach (var renderGroup in m_System.renderGroups)
                    {
                        renderGroup.SetCPUCullingResult(RenderGroup.ShowState.BVHCulling);
                        m_VisibleNodes.Add(renderGroup.groupID);
                    }
                }
                Profiler.EndSample();
            }
            
            if (m_VisibleNodes.Length <= 0)
                return;

            Profiler.BeginSample("CullingModule CPU Group Culling");
            var cullingBoundsNativeArray = new NativeArray<FBoxSphereBounds>(m_VisibleNodes.Length, Allocator.TempJob);
            var cullingGroupIDsNativeArray = new NativeArray<int>(m_VisibleNodes.Length, Allocator.TempJob);
            var cullingResultNativeArray = new NativeArray<bool>(m_VisibleNodes.Length, Allocator.TempJob);

            
            Profiler.BeginSample("CullingModule Prepare Group Culling Data");

            // for (int i = 0; i < m_VisibleNodes.Length; i++)
            // {
            //     var renderGroup = m_VisibleNodes[i];
            //     renderGroup.SetCPUCullingResult(RenderGroup.ShowState.PassBVHCulling);
            //     cullingGroupIDsNativeArray[i] = renderGroup.groupID;
            //     cullingBoundsNativeArray[i] = renderGroup.bounds;
            //     cullingResultNativeArray[i] = false;
            // }
            var prepareJobs = PrepareRenderGroupCulling.CreateJob(m_VisibleNodes, m_System.groupBoundsArray, cullingGroupIDsNativeArray, cullingBoundsNativeArray, cullingResultNativeArray);
            var preparejob = prepareJobs.Schedule(m_VisibleNodes.Length, 32);
            preparejob.Complete();
            
            Profiler.EndSample();

            Profiler.BeginSample("CullingModule Group Culling");
            int length = cullingBoundsNativeArray.Length;
            var cullJobs = RenderGroupCulling.CreateJob(Flags, ConvexVolume, cullingBoundsNativeArray, cullingResultNativeArray);
            var job = cullJobs.Schedule(length, 32);
            job.Complete();
            Profiler.EndSample();
            
            Profiler.BeginSample("CullingModule Deal Group Culling Data");
            //读取Group剔除结果 直接设置GroupItem的剔除结果
            //这样写有GC问题 弃用
            // Parallel.For(0, cullingResultNativeArray.Length,  index =>
            // {
            //     // bool result = cullingResultNativeArray[index];
            //     // var groupID = cullingGroupIDsNativeArray[index];
            //     // var renderGroup = m_System.renderGroups[groupID];
            //     // if(result)
            //     //     renderGroup.SetCPUCullingResult(RenderGroup.ShowState.PassBVHCulling);
            //     // m_System.renderGroups[groupID] = renderGroup;
            //     // for (int j = 0; j < renderGroup.itemCount; j++)
            //     // {
            //     //     var itemID = m_System.renderItems[renderGroup.itemStartIndex + j].itemID;
            //     //     m_System.infoModule.cullResult[itemID] = (uint)(result ? 1 : 0);
            //     // }
            // });
            
            var afterJobs = AfterRenderGroupCulling.CreateJob(cullingGroupIDsNativeArray, cullingResultNativeArray, m_System.renderGroups, m_System.renderItems, m_System.infoModule.cullResult);
            var afterjob = afterJobs.Schedule(length, 32);
            afterjob.Complete();
            
            Profiler.EndSample();

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
                var renderGroup = m_System.renderGroups[node];
                // Gizmos.DrawWireSphere(node.Bounds.Origin, node.Bounds.SphereRadius);
                Gizmos.DrawWireCube(renderGroup.bounds.Origin, 2 * renderGroup.bounds.BoxExtent);
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