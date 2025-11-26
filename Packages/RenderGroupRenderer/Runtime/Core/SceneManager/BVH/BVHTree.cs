using System;
using System.Collections.Generic;
using UnityEngine;

namespace RenderGroupRenderer
{
    public class BVHTree
    {
        private BVHNode m_Root;
        private int maxObjectsPerLeaf = 8;
        
        public BVHNode Root => m_Root;
    
        public BVHTree(List<RenderGroup> sceneObjects)
        {
            m_Root = BuildTree(sceneObjects);
        }

        private BVHNode BuildTree(List<RenderGroup> objects)
        {
            if (objects.Count == 0) return null;
            
            // 计算所有物体的总包围盒
            Bounds totalBounds = objects[0].bounds;
            foreach (var obj in objects)
            {
                totalBounds.Encapsulate(obj.bounds);
            }
        
            BVHNode node = new BVHNode(totalBounds);
        
            // 如果物体数量小于阈值，创建叶子节点
            if (objects.Count <= maxObjectsPerLeaf)
            {
                node.AddItems(objects);
                return node;
            }
        
            // 选择分割轴（选择最长的轴）
            Vector3 size = totalBounds.size;
            int splitAxis = (size.x > size.y && size.x > size.z) ? 0 : 
                (size.y > size.z) ? 1 : 2;
        
            // 按中心点排序
            objects.Sort((a, b) => 
                a.bounds.center[splitAxis].CompareTo(b.bounds.center[splitAxis]));
        
            // 分割物体列表
            int mid = objects.Count / 2;
            var leftObjects = objects.GetRange(0, mid);
            var rightObjects = objects.GetRange(mid, objects.Count - mid);
        
            // 递归构建子树
            node.left = BuildTree(leftObjects);
            node.right = BuildTree(rightObjects);

            return node;
        }

        public void FrustumCull(Plane[] frustumPlanes, List<BVHNode> visibleNodes, uint[] cullResultArray, ref int itemCount)
        {
            m_Root.FrustumCull(frustumPlanes, visibleNodes, ref cullResultArray, ref itemCount);
        }

        public void DrawTargetDepth(int displayDepth)
        {
            m_Root?.DrawTargetDepth(displayDepth);
        }
        
        //提供一个遍历方法
        public void Iteration(Action<RenderGroup> actionRenderGroup, Action<BVHNode> actionNode)
        {
            m_Root.Iteration(actionRenderGroup, actionNode);
        }
    }
}