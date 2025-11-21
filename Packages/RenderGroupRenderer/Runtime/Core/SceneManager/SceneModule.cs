using System.Linq;
using UnityEngine;

namespace RenderGroupRenderer
{
    //场景管理 可以由四叉树 八叉树 BVH等实现
    public class SceneModule
    {
        private BVHTree m_BVHTree;


        public BVHTree BVHTree => m_BVHTree;
        
        public void Init(RenderGroup[] m_RenderGroups)
        {
            m_BVHTree = new(m_RenderGroups.ToList());
        }
    }
}
