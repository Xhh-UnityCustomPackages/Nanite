using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RenderGroupRenderer.Info;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using ReadOnly = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace RenderGroupRenderer
{
    //需要做个收集器 交给RendererGraph去渲染
    public class RenderGroupRendererSystem : MonoBehaviour
    {
        [Header("Settings")]
        public bool enableRender = true;
        [Header("Data")] 
        public RenderGroupData renderGroupData;
        public RenderInfoData renderInfoData;
        
        private SceneModule m_SceneModule;

      
        
        [Header("Culling")] 
        public Camera CullingCamera;
        [ShowInInspector, HideInEditorMode] 
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
        public CullingModule cullingModule => m_CullingModule;

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
            m_RendererModule.Init(m_RenderArgsItems);
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
                FBoxSphereBounds bounds = new FBoxSphereBounds();
                bounds.SetMinMax(groupData.bounds.min, groupData.bounds.max);
                renderGroup.bounds = bounds;
                renderGroup.items = new RenderGroupItem[groupData.itemDatas.Count];
                for (int j = 0; j < groupData.itemDatas.Count; j++)
                {
                    var itemData = groupData.itemDatas[j];
                    FBoxSphereBounds itemBounds = new FBoxSphereBounds(itemData.bounds);
                    RenderGroupItem renderGroupItem = new RenderGroupItem(itemBounds, itemData.itemID);
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
            totalCount *= Define.LOD_COUNT;//每个里面分为3级LOD
            

            m_RenderArgsItems = new RenderArgsItem[totalCount];
            var renderItems = renderInfoData.renderItems;
            for (int i = 0; i < renderItemsCount; i++)
            {
                var renderItem = renderItems[i].data;

                // if (useLOD)
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
                // else
                // {
                //     var lodInfo = renderItem.lod0Info;
                //     var mesh = lodInfo.mesh;
                //     var material = lodInfo.material;
                //     int itemArgOffset = i * 5;
                //     RenderArgsItem argItem = new(mesh, material, itemArgOffset);
                //     m_RenderArgsItems[i] = argItem;
                // }
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
            m_CullingModule.OnUpdate();
            m_CullingModule.OnLateUpdate();
        }
        
        private void LateUpdate()
        {
            m_RendererModule.OnLateUpdate();
            
            if(enableRender)
                m_RendererModule.Renderer();
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
                    m_RenderGroups[i].OnDrawGizmos(m_CullingModule);
                }
            }
        }
    }
}