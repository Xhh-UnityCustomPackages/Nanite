using System;
using System.Collections.Generic;
using System.Linq;
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
        
        private SceneModule m_SceneModule;
        
        [Header("Culling")] 
        public Camera CullingCamera;
        private CullingModule m_CullingModule;
        private RenderGroupRenderer m_RendererModule;
        [ShowInInspector, HideInEditorMode] 
        private RendererInfoModule m_InfoModule;
        
        [Header("Shader")]
        public ComputeShader cullingShader;
        public ComputeShader sortingShader;

        [Header("Debug")] 
        public bool showDebug = false;
        
        
        [ShowInInspector, ReadOnly]
        private RenderGroup[] m_RenderGroups;
        
        private RenderArgsItem[] m_RenderArgsItems;

        private void Awake()
        {
            CreateRenderGroup();
            CreateRenderArgs();

            m_SceneModule = new();
            m_SceneModule.Init(m_RenderGroups);
           
            
            m_InfoModule = new();
            m_InfoModule.Init(renderGroupData,  renderInfoData);
            
            m_CullingModule = new();
            m_CullingModule.SetCullingCamera(CullingCamera);
            m_CullingModule.AddToBVHFrustumCull(m_SceneModule.BVHTree);
            // m_CullingModule.Init(m_RenderGroups);

            m_RendererModule = new RenderGroupRenderer();
            m_RendererModule.Init(m_RenderArgsItems, m_InfoModule, m_CullingModule);
            m_RendererModule.m_CullingCS = cullingShader;
            m_RendererModule.m_SortCS = sortingShader;
            m_RendererModule.updateMaterial = m_InfoModule.UpdateMaterial;
        }

        private void OnDestroy()
        {
            m_CullingModule.Dispose();
            m_InfoModule.Dispose();
            m_RendererModule.Dispose();
        }

        void CreateRenderGroup()
        {
            m_RenderGroups = new RenderGroup[renderGroupData.groupDatas.Count];
            var groupDatas = renderGroupData.groupDatas;
            for (int i = 0; i < groupDatas.Count; i++)
            {
                var groupData = groupDatas[i];
                RenderGroup renderGroup = new RenderGroup();
                renderGroup.bounds = groupData.bounds;
                renderGroup.items = new RenderGroupItem[groupData.itemDatas.Count];
                for (int j = 0; j < groupData.itemDatas.Count; j++)
                {
                    var itemData = groupData.itemDatas[j];
                    
                    RenderGroupItem renderGroupItem = new RenderGroupItem(itemData.bounds, itemData.itemID);
                    renderGroup.items[j] = renderGroupItem;
                }

                m_RenderGroups[i] = renderGroup;
            }
        }

        void CreateRenderArgs()
        {
            m_RenderArgsItems = new RenderArgsItem[renderInfoData.renderItems.Count];
            var renderItems = renderInfoData.renderItems;
            for (int i = 0; i < renderItems.Count; i++)
            {
                var renderItem = renderItems[i];
                var mesh = renderItem.mesh;
                var material = renderItem.material;
                int itemArgOffset = i * 5;
                RenderArgsItem argItem = new(mesh, material, itemArgOffset);
                m_RenderArgsItems[i] = argItem;
            }
        }

        private void Update()
        {
            m_CullingModule?.OnUpdate();
        }
        
        private void LateUpdate()
        {
            m_CullingModule.OnLateUpdate();
            //填充回RenderGroup
            // for (int i = 0; i < m_RenderGroups.Length; i++)
            // {
            //     m_RenderGroups[i].SetCullingResult(m_CullingModule.CullingResultNativeArray[i]);
            // }
            //剔除的Group直接设置ArgBuffer count -1 
            //这样就在发起Indirect绘制的时候 提前判断 减少空draw

            m_RendererModule.OnUpdate();
        }

        private void OnDrawGizmos()
        {
            if (!showDebug)
            {
                return;
            }

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