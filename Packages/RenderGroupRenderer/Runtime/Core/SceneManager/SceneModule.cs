using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RenderGroupRenderer
{
    //场景管理 可以由四叉树 八叉树 BVH等实现
    public class SceneModule
    {
        private BVHTree m_BVHTree;
        public BVHTree BVHTree => m_BVHTree;
        
        // private TOctree2<RenderGroup> m_Octree;
        // public TOctree2<RenderGroup> Octree => m_Octree;
        
        public void Init(NativeArray<RenderGroup> m_RenderGroups)
        {
            
            m_BVHTree = new(m_RenderGroups.ToList());
            
            
            // 计算所有物体的总包围盒
            // var totalBounds = m_RenderGroups[0].bounds;
            // foreach (var obj in m_RenderGroups)
            // {
            //     totalBounds.Encapsulate(obj.bounds);
            // }
            // m_Octree = new TOctree2<RenderGroup>(totalBounds.Origin, totalBounds.BoxExtent.x, new MyOctreeSemantics());
            // for (int i = 0; i < m_RenderGroups.Length; i++)
            // {
            //     m_Octree.AddElement(m_RenderGroups[i]);
            // }
        }
    }
}
