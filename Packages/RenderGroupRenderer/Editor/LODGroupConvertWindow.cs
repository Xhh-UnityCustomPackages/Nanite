using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEditor;

namespace RenderGroupRenderer
{
    public class LODGroupConvertWindow : OdinEditorWindow
    {
        [MenuItem("Tools/RenderGroupRenderer/LODGroupConvertWindow")]
        public static void ShowWindow()
        {
            var window = GetWindow<LODGroupConvertWindow>();
            window.titleContent = new GUIContent("LOD 转化工具");
            window.Show();
        }

        [OnValueChanged("OnTargetChanged")] public GameObject target;

        
        [System.Serializable]
        public class LODData
        {
            public int level;
            public float screenRelativeHeight;
            public float showDistance;
            public List<Renderer> renderers = new ();
        }
        
        private Bounds m_Bounds;
        [ShowInInspector]
        private List<LODData> m_LODs = new List<LODData>();
        
        void OnTargetChanged()
        {
            var sceneView = SceneView.lastActiveSceneView;
            var sceneCamera = sceneView.camera;
            
            if (target == null)
                return;
            m_LODs.Clear();
            var lodGroup = target.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                var lodCount = lodGroup.lodCount;
                var lods = lodGroup.GetLODs();
                for (int i = 0; i < lods.Length; i++)
                {
                    LODData lodData = new LODData();
                    lodData.level = i;
                    lodData.screenRelativeHeight = lods[i].screenRelativeTransitionHeight;
                    lodData.showDistance = CalculateDistance(sceneCamera, lodData.screenRelativeHeight, lodGroup);
                    lodData.renderers.AddRange(lods[i].renderers);
;                   m_LODs.Add(lodData);
                }
            }
            else
            {
                LODData lodData = new LODData();
                lodData.level = 0;
                lodData.screenRelativeHeight = 1;
                lodData.showDistance = -1;
                m_LODs.Add(lodData);
            }

        }
        
        public static float CalculateDistance(Camera camera, float relativeScreenHeight, LODGroup group)
        {
            //DistanceToRelativeHeight 的逆运算
            var halfAngle = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
            return (group.size * 0.5F) / (relativeScreenHeight * halfAngle);
        }
        

        [Button]
        void Save()
        {
            var path = EditorUtility.SaveFilePanelInProject("save RenderItemInfoData","RenderItemInfoData","asset","Save");
            if (!string.IsNullOrEmpty(path))
            {
                float distance0 = m_LODs[0].showDistance;
                float distance1 = m_LODs[1].showDistance;
                float distance2 = m_LODs[2].showDistance;
                int lodCount = m_LODs.Count;
                
                var oldAsset = AssetDatabase.LoadAssetAtPath<RenderItemInfoData>(path);
                if (oldAsset == null)
                {
                    var renderItemInfoData = CreateInstance<RenderItemInfoData>();
                    renderItemInfoData.lodCount = lodCount;
                    renderItemInfoData.lodDistance = new Vector3(distance0, distance1, distance2);
                    AssetDatabase.CreateAsset(renderItemInfoData, path);
                }
                else
                {
                    oldAsset.lodCount = lodCount;
                    oldAsset.lodDistance = new Vector3(distance0, distance1, distance2);
                    EditorUtility.SetDirty(oldAsset);
                    AssetDatabase.SaveAssetIfDirty(oldAsset);
                }
                
            }
        }
    }
}