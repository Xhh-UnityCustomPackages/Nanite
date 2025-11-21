using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RenderGroupRenderer
{
    //给定几种预制体 
    [ExecuteAlways]
    public class ScatterObject : MonoBehaviour
    {
        [Header("生成设置")]
        [Tooltip("要生成的预制体列表")]
        public GameObject[] prefabs;
        
        
        [Tooltip("要生成的物体数量")]
        public int spawnCount = 100;
        
        
        [Header("生成区域设置")]
        [Tooltip("生成区域的中心点")]
        public Vector3 spawnCenter = Vector3.zero;
    
        [Tooltip("生成区域的尺寸")]
        public Vector3 spawnAreaSize = new Vector3(50f, 10f, 50f);

        [SerializeField]
        private List<GameObject> spawnedObjects = new List<GameObject>();

        [Button]
        void Clear()
        {
            spawnedObjects.ForEach(DestroyImmediate);
            spawnedObjects.Clear();
        }

        [Button]
        void Generate()
        {
            Clear();
            
            GameObject randomPrefab = prefabs[Random.Range(0, prefabs.Length)];

            for (int i = 0; i < spawnCount; i++)
            {
                // 计算随机位置
                Vector3 randomPosition = GetRandomPosition();
        
                // 创建物体
                GameObject newObject =PrefabUtility.InstantiatePrefab(randomPrefab) as GameObject;
                newObject.transform.position = randomPosition;
                newObject.transform.rotation = Quaternion.identity;
                newObject.transform.SetParent(this.transform);
                
                spawnedObjects.Add(newObject);
            }
            
        }
        
        private Vector3 GetRandomPosition()
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f),
                Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f)
            );
        
            return spawnCenter + randomPosition;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(spawnCenter, spawnAreaSize);
        }
    }
}
