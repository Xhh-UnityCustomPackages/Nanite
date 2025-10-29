using System.Collections.Generic;
using UnityEngine;

namespace RenderGroupRenderer
{
    [CreateAssetMenu(fileName = "RenderInfoData", menuName = "RenderGroup/RenderInfoData", order = 1)]
    public class RenderInfoData : ScriptableObject
    {
        public List<RenderItemInfoData> renderItems = new();
    }

    [System.Serializable]
    public class RenderItemInfoData
    {
        public int id;
        public Mesh mesh;
        public Material material;
    }
}
