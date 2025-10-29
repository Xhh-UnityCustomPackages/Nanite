using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace RenderGroupRenderer
{
    [CreateAssetMenu(fileName = "RenderGroupData", menuName = "RenderGroup/RenderGroupData", order = 1)]
    public class RenderGroupData : ScriptableObject
    {
        public int totalCount;
        public List<RenderGroupItemData> groupDatas = new();
    }

    [System.Serializable]
    public class RenderGroupItemData
    {
        public Bounds bounds;
        public TransformData transform;
        [TableList]
        public List<RenderItemData> itemDatas = new();
    }

    [System.Serializable]
    public class RenderItemData
    {
        public Bounds bounds;
        public TransformData transform;
        public int itemID;
    }
    
    [System.Serializable]
    public class TransformData
    {
        public float3 position;
        public float3 rotation;
        public float3 scale;

        public TransformData(float3 position, float3 rotation, float3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public TransformData(Transform transform)
        {
            this.position = transform.position;
            this.rotation = transform.eulerAngles;
            this.scale = transform.lossyScale;
        }

        public float4x4 GetTransformMatrix()
        {
            return Matrix4x4.TRS(position, Quaternion.Euler(rotation), scale);
        }
    }
}
