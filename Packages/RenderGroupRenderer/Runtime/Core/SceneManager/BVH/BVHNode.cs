using System;
using System.Collections.Generic;
using UnityEngine;

namespace RenderGroupRenderer
{
    public class BVHNode
    {
        private Bounds m_Bounds;
        public BVHNode left = null;
        public BVHNode right = null;
        private List<RenderGroup> m_Objects;
        
        public Bounds Bounds => m_Bounds;
        public bool IsLeaf => left == null && right == null;
        public List<RenderGroup> Objects => m_Objects;
        
        public BVHNode(Bounds bounds)
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
            Gizmos.DrawWireCube(this.m_Bounds.center, this.m_Bounds.size);
            Gizmos.DrawSphere(this.m_Bounds.min, 0.1f);
            Gizmos.DrawSphere(this.m_Bounds.max, 0.1f);
        }
        #endregion


        public void FrustumCull(Plane[] frustumPlanes, List<BVHNode> visibleNodes, ref int itemCount)
        {
            //这个是Debug逻辑 可以移除或者用宏开启
            if (IsLeaf)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    m_Objects[i].SetCPUCullingResult(RenderGroup.ShowState.BVHCulling);
                }
            }
            
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, m_Bounds))
            {
                return;
            }
            
            //过了视锥剔除
            if (IsLeaf)
            {
                visibleNodes.Add(this);
                itemCount += m_Objects.Count;
            }
            else
            {
                left.FrustumCull(frustumPlanes, visibleNodes, ref itemCount);
                right.FrustumCull(frustumPlanes, visibleNodes, ref itemCount);
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
