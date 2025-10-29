using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RenderGroupRenderer.Info;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ReadOnly = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace RenderGroupRenderer
{
    public class RenderGroupRendererSystem : MonoBehaviour
    {
        [Header("Data")] 
        public RenderGroupData renderGroupData;
        public RenderInfoData renderInfoData;
        [Header("Culling")] 
        public Camera CullingCamera;
        private CullingModule m_CullingModule;
        private RenderGroupRenderer m_RendererModule;
        [ShowInInspector, HideInEditorMode] 
        private RendererInfoModule m_InfoModule;

        
        [ShowInInspector, ReadOnly]
        private RenderGroup[] m_RenderGroups;

        private void Awake()
        {
            CreateRenderGroup();
            m_InfoModule = new();
            m_InfoModule.Init(renderGroupData,  renderInfoData);
            
            m_CullingModule = new();
            m_CullingModule.SetCullingCamera(CullingCamera);
            m_CullingModule.Init(m_RenderGroups);

            m_RendererModule = new RenderGroupRenderer();
            m_RendererModule.Init(m_InfoModule.args, m_InfoModule.argsBuffer, m_RenderGroups);
            m_RendererModule.updateMaterial = m_InfoModule.UpdateMaterial;
        }

        private void OnDestroy()
        {
            m_CullingModule.Dispose();
            m_InfoModule.Dispose();
        }

        void CreateRenderGroup()
        {
            int argsOffset = 0;
            m_RenderGroups = new RenderGroup[renderGroupData.groupDatas.Count];
            var groupDatas = renderGroupData.groupDatas;
            for (int i = 0; i < groupDatas.Count; i++)
            {
                var groupData = groupDatas[i];
                RenderGroup renderGroup = new RenderGroup();
                renderGroup.bounds = groupData.bounds;
                renderGroup.items = new RenderItem[groupData.itemDatas.Count];
                renderGroup.argsOffset = argsOffset * 5;
                for (int j = 0; j < groupData.itemDatas.Count; j++)
                {
                    var itemData = groupData.itemDatas[j];
                    var renderInfo = renderInfoData.GetItemData(itemData.itemID);
                    int itemArgOffset = renderGroup.argsOffset + j * 5;
                    RenderItem renderItem = new RenderItem(itemData.bounds, renderInfo.mesh, renderInfo.material, itemArgOffset);
                    renderGroup.items[j] = renderItem;
                    argsOffset++;
                }

                m_RenderGroups[i] = renderGroup;
            }
        }

        private void Update()
        {
            m_CullingModule.OnUpdate();
            //填充回RenderGroup
            for (int i = 0; i < m_RenderGroups.Length; i++)
            {
                m_RenderGroups[i].SetCullingResult(m_CullingModule.CullingResultNativeArray[i]);
                m_RenderGroups[i].UpdateArgs(m_InfoModule.args);
            }
            //剔除的Group直接设置ArgBuffer count -1 
            //这样就在发起Indirect绘制的时候 提前判断 减少空draw

            m_RendererModule.OnUpdate();
        }

        private void OnDrawGizmos()
        {
            m_CullingModule?.OnDrawGizmos();
            if (m_RenderGroups != null)
            {
                for (int i = 0; i < m_RenderGroups.Length; i++)
                {
                    m_RenderGroups[i].OnDrawGizmos();
                }
            }
        }
    }
}