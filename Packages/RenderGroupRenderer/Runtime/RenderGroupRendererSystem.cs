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
        [Header("Settings")]
        public bool useLOD = false;
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
        
        public RenderGroup[] renderGroups => m_RenderGroups;
        public RendererInfoModule infoModule => m_InfoModule;

        private void Awake()
        {
            CreateRenderGroup();
            CreateRenderArgs();

            m_SceneModule = new();
            m_SceneModule.Init(m_RenderGroups);
            
            m_InfoModule = new(this);
            m_InfoModule.Init(renderGroupData,  renderInfoData);
            
            m_CullingModule = new(this);
            m_CullingModule.SetCullingCamera(CullingCamera);
            m_CullingModule.AddToBVHFrustumCull(m_SceneModule.BVHTree);

            m_RendererModule = new RenderGroupRenderer(this);
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
            int itemID = 0;
            for (int i = 0; i < groupDatas.Count; i++)
            {
                var groupData = groupDatas[i];
                RenderGroup renderGroup = new RenderGroup();
                renderGroup.groupID = i;
                renderGroup.bounds = groupData.bounds;
                renderGroup.items = new RenderGroupItem[groupData.itemDatas.Count];
                for (int j = 0; j < groupData.itemDatas.Count; j++)
                {
                    var itemData = groupData.itemDatas[j];
                    
                    RenderGroupItem renderGroupItem = new RenderGroupItem(itemData.bounds, itemData.itemID);
                    renderGroupItem.groupID = i;
                    renderGroupItem.itemID = itemID++;
                    renderGroup.items[j] = renderGroupItem;
                }

                m_RenderGroups[i] = renderGroup;
            }
        }

        void CreateRenderArgs()
        {
            int renderItemsCount = renderInfoData.renderItems.Count;
            int totalCount = renderInfoData.renderItems.Count;
            if (useLOD)
            {
                totalCount *= Define.LOD_COUNT;//每个里面分为3级LOD
            }

            m_RenderArgsItems = new RenderArgsItem[totalCount];
            var renderItems = renderInfoData.renderItems;
            for (int i = 0; i < renderItemsCount; i++)
            {
                var renderItem = renderItems[i].data;

                if (useLOD)
                {
                    var lodInfo = renderItem.lod0Info;
                    var mesh = lodInfo.mesh;
                    var material = lodInfo.material;
                    int itemArgOffset = (i * Define.LOD_COUNT + 0) * 5;
                    RenderArgsItem argItem = new(mesh, material, itemArgOffset);
                    m_RenderArgsItems[i * Define.LOD_COUNT + 0] = argItem;

                    lodInfo = renderItems[i].data.lod1Info;
                    mesh = lodInfo.mesh;
                    material = lodInfo.material;
                    itemArgOffset = (i * Define.LOD_COUNT + 1) * 5;
                    argItem = new(mesh, material, itemArgOffset);
                    m_RenderArgsItems[i * Define.LOD_COUNT + 1] = argItem;

                    lodInfo = renderItems[i].data.lod2Info;
                    mesh = lodInfo.mesh;
                    material = lodInfo.material;
                    itemArgOffset = (i * Define.LOD_COUNT + 2) * 5;
                    argItem = new(mesh, material, itemArgOffset);
                    m_RenderArgsItems[i * Define.LOD_COUNT + 2] = argItem;
                }
                else
                {
                    var lodInfo = renderItem.lod0Info;
                    var mesh = lodInfo.mesh;
                    var material = lodInfo.material;
                    int itemArgOffset = i * 5;
                    RenderArgsItem argItem = new(mesh, material, itemArgOffset);
                    m_RenderArgsItems[i] = argItem;
                }
            }

            // LoadRenderArgsItem();
        }

        void LoadRenderArgsItem()
        {
            Debug.LogError($"总数量:{m_RenderArgsItems.Length}");
            for (int i = 0; i < m_RenderArgsItems.Length; i++)
            {
                Debug.LogError($"{i}:{m_RenderArgsItems[i].argsOffset}");
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

            m_RendererModule.OnLateUpdate();
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