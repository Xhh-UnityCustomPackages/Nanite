using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RenderGroupRenderer
{
    [CreateAssetMenu(fileName = "RenderInfoData", menuName = "RenderGroup/RenderInfoData", order = 1)]
    public class RenderInfoData : ScriptableObject
    {
        [TableList]
        public List<RenderItemInfo> renderItems = new();
    }

    [System.Serializable]
    public class RenderItemInfo
    {
        public RenderItemInfoData data;
    }
}