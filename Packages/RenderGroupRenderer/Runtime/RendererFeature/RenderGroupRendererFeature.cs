using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderGroupRenderer
{
    public class RenderGroupRendererFeature :ScriptableRendererFeature
    {
        [SerializeField] 
        private RenderGroupRendererFeatureData m_FeatureData;
        
        
        private HiZDepthGeneratorPass m_HiZPass;

        public override void Create()
        {
#if UNITY_EDITOR
            if (m_FeatureData == null)
            {
                var path = "Packages/rendergrouprenderer/Runtime/RendererFeature/RenderGroupRendererFeatureData.asset";
                m_FeatureData = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderGroupRendererFeatureData>(path);
            }
#endif

            m_HiZPass ??= new(m_FeatureData);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //只为主相机生成HiZ
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(m_HiZPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            m_HiZPass.Dispose();
        }
    }
}
