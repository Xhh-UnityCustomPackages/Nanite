using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace RenderGroupRenderer
{
    public class BVHNode
    {
        private FBoxSphereBounds m_Bounds;
        public BVHNode left = null;
        public BVHNode right = null;
        private List<RenderGroup> m_Objects;
        
        public FBoxSphereBounds Bounds => m_Bounds;
        public bool IsLeaf => left == null && right == null;
        public List<RenderGroup> Objects => m_Objects;
        
        public BVHNode(FBoxSphereBounds bounds)
        {
            m_Bounds = bounds;
        }

        public void AddItems(List<RenderGroup> items)
        {
            if (m_Objects == null)
                m_Objects = new();
            m_Objects.AddRange(items);
        }

        #region Gizmos
        public void DrawTargetDepth(int depth)
        {
            if (depth <= 0)
            {
                DrawGizmos();
            }
            else
            {
                right?.DrawTargetDepth(depth - 1);
                left?.DrawTargetDepth(depth - 1);
            }
        }
        
        void DrawGizmos()
        {
            Gizmos.DrawWireCube(this.m_Bounds.Origin, this.m_Bounds.BoxExtent * 2);
            Gizmos.DrawSphere(this.m_Bounds.min, 0.1f);
            Gizmos.DrawSphere(this.m_Bounds.max, 0.1f);
        }
        #endregion

        void SetGroupCullResult(ref uint[] cullResultArray, bool show)
        {
            if (m_Objects == null)
            {
                return;
            }

            for (var i = 0; i < m_Objects.Count; i++)
            {
                for (var j = 0; j < m_Objects[i].items.Length; j++)
                {
                    cullResultArray[m_Objects[i].items[j].itemID] = (uint)(show ? 1 : 0);
                }
            }
        }

        public void FrustumCull(FFrustumCullingFlags Flags, FConvexVolume convexVolume, NativeList<int> visibleNodes, ref uint[] cullResultArray)
        {
            //这个是Debug逻辑 可以移除或者用宏开启
            if (IsLeaf)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    m_Objects[i].SetCPUCullingResult(RenderGroup.ShowState.BVHCulling);
                }
            }
            
            if (Flags.bUseSphereTestFirst)
            {
                //先用球体包围盒剔除
                if (!convexVolume.IntersectSphere(m_Bounds.Origin, m_Bounds.SphereRadius))
                {
                    SetGroupCullResult(ref cullResultArray, false);
                    return;
                }
            }

            if (!convexVolume.IntersectBox(m_Bounds.Origin, m_Bounds.BoxExtent))
            {
                SetGroupCullResult(ref cullResultArray, false);
                return;
            }

            //过了视锥剔除
            if (IsLeaf)
            {
                foreach (var renderGroup in m_Objects)
                {
                    visibleNodes.Add(renderGroup.groupID);
                }
                
                SetGroupCullResult(ref cullResultArray, true);
            }
            else
            {
                left.FrustumCull(Flags, convexVolume, visibleNodes, ref cullResultArray);
                right.FrustumCull(Flags, convexVolume, visibleNodes, ref cullResultArray);
            }
        }


        public void Iteration(Action<RenderGroup> actionRenderGroup, Action<BVHNode> actionNode)
        {
            actionNode?.Invoke(this);
            
            if (IsLeaf)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    actionRenderGroup?.Invoke(m_Objects[i]);
                }
            }
            else
            {
                left.Iteration(actionRenderGroup, actionNode);
                right.Iteration(actionRenderGroup, actionNode);
            }
        }
    }
}
