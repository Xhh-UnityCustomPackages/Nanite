using System.Collections.Generic;
using UnityEngine;

namespace RenderGroupRenderer
{
    [CreateAssetMenu(fileName = "RenderInfoData", menuName = "RenderGroup/RenderInfoData", order = 1)]
    public class RenderInfoData : ScriptableObject
    {
        public List<RenderItemInfo> renderItems = new();
    }

    [System.Serializable]
    public class RenderItemInfo
    {
        public int id;
        public RenderItemInfoData data;
    }
}