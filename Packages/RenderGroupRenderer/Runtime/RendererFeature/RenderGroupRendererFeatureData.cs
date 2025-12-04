using UnityEngine;

namespace RenderGroupRenderer
{
    // [CreateAssetMenu(fileName = "RenderGroupRendererFeatureData", menuName = "RenderGroup/RenderGroupRendererFeatureData", order = 1)]
    public class RenderGroupRendererFeatureData : ScriptableObject
    {
        public ComputeShader buildHiZCS;
    }
}