using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using ZLinq;

namespace RenderGroupRenderer
{
    public class RenderGroupRendererWindow : OdinEditorWindow
    {
        public Transform root;

        [MenuItem("Tools/RenderGroupRendererWindow")]
        public static void ShowWindow()
        {
            var window = GetWindow<RenderGroupRendererWindow>();
            window.titleContent = new GUIContent("Render Group Renderer");
            window.Show();
        }

        private int m_Count = 0;

        [Button]
        void BakeScene()
        {
            if (root == null) return;

            m_Count = 0;
            var groupData = CreateInstance<RenderGroupData>();
            foreach (var child in root.Descendants())
            {
                ProcessGo(child, groupData);
            }

            groupData.totalCount = m_Count;
            var savePath = $"Assets/{root.name}.asset";
            AssetDatabase.CreateAsset(groupData, savePath);

            EditorGUIUtility.PingObject(groupData);
            Selection.activeObject = groupData;
        }

        void ProcessGo(Transform target, RenderGroupData groupData)
        {
            GameObject outermostRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(target.gameObject);
            bool isRootPrefab = outermostRoot == target.gameObject;

            if (isRootPrefab)
            {
                string objPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(outermostRoot);
                if (!string.IsNullOrEmpty(objPath))
                {
                    float3 position = target.position;

                    Debug.LogError(target.gameObject.name);

                    var renderers = target.gameObject.GetComponentsInChildren<Renderer>();
                    Bounds bounds = new Bounds(position, Vector3.zero);
                    foreach (var renderer in renderers)
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }

                    RenderGroupItemData groupItemData = new RenderGroupItemData();
                    groupItemData.bounds = bounds;
                    groupItemData.transform = new TransformData(target);
                    groupItemData.itemDatas = new();

                    foreach (var _child in target.DescendantsAndSelf())
                    {
                        if (!PrefabUtility.IsPartOfAnyPrefab(_child))
                            continue;
                        var renderer = target.GetComponent<Renderer>();
                        RenderItemData itemData = new();
                        itemData.transform = new TransformData(_child);
                        itemData.bounds = renderer.bounds;
                        groupItemData.itemDatas.Add(itemData);
                        m_Count++;
                    }

                    groupData.groupDatas.Add(groupItemData);
                }
            }
        }
    }
}