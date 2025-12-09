using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RenderGroupRenderer
{
    public class BVHTest : MonoBehaviour
    {
        public RenderGroupData renderGroupData;
        public RenderInfoData renderInfoData;
        
        [SerializeField]
        private Camera CullingCamera;
        
        private BVHTree bvhTree;

        [Range(0, 10)] public int displayDepth;

        private CullingModule m_CullingModule;
        
        [ShowInInspector, ReadOnly]
        private RenderGroup[] m_RenderGroups;

        void Start()
        {
            m_RenderGroups = new RenderGroup[renderGroupData.groupDatas.Count];
            var groupDatas = renderGroupData.groupDatas;
            for (int i = 0; i < groupDatas.Count; i++)
            {
                var groupData = groupDatas[i];
                RenderGroup renderGroup = new RenderGroup();
                var bounds = new FBoxSphereBounds(groupData.bounds);
                renderGroup.bounds = bounds;
                renderGroup.items = new RenderGroupItem[groupData.itemDatas.Count];
                for (int j = 0; j < groupData.itemDatas.Count; j++)
                {
                    var itemData = groupData.itemDatas[j];
                    var itemBounds = new FBoxSphereBounds(itemData.bounds);
                    RenderGroupItem renderGroupItem = new RenderGroupItem(itemBounds, itemData.itemID);
                    renderGroup.items[j] = renderGroupItem;
                }

                m_RenderGroups[i] = renderGroup;
            }
            
            
            bvhTree = new(m_RenderGroups.ToList());

            m_CullingModule = new(null);
            m_CullingModule.SetCullingCamera(CullingCamera);
            m_CullingModule.AddToBVHFrustumCull(bvhTree);
        }
        
        private void Update()
        {
            m_CullingModule?.OnUpdate();
        }

        private void LateUpdate()
        {
            m_CullingModule?.OnLateUpdate();
        }
    }
}