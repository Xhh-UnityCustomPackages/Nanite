using UnityEngine;

namespace RenderGroupRenderer
{
    [CreateAssetMenu(fileName = "RenderItemInfoData", menuName = "RenderGroup/RenderItemInfoData", order = 1)]
    public class RenderItemInfoData : ScriptableObject
    {
        public int lodCount;
        //使用三级LOD
        public Vector3 lodDistance = new Vector3(10, 20, 30);
        public RenderItemLODData lod0Info;
        public RenderItemLODData lod1Info;
        public RenderItemLODData lod2Info;
    }
    
    [System.Serializable]
    public class RenderItemLODData
    {
        public Mesh mesh;
        public Material material;
    }
}