using Sirenix.OdinInspector;
using UnityEngine;

namespace Nanite.Runtime
{
    [CreateAssetMenu(fileName = "MeshletAsset", menuName = "Nanite/Meshlet/Create MeshletAsset.asset", order = 1)]
    public class MeshletAsset : ScriptableObject
    {
        public MeshletCollection Collection;
        public Mesh SourceMesh;


#if UNITY_EDITOR
        
        
        [Button]
        void GenerateMesh()
        {
            if (Collection == null || Collection.meshlets.Length <= 0) return;
            //生成Mesh
            GameObject go = new GameObject("root");
            go.transform.position = Vector3.zero;
            
            
            for (int meshletIndex = 0; meshletIndex < Collection.meshlets.Length; meshletIndex++)
            {
                var mesh = new Mesh();
                mesh.name = "Mesh";

                var meshlet = Collection.meshlets[meshletIndex];

                Vector3[] vertices = new Vector3[meshlet.TriangleCount * 3];
                int[] triangles = new int[meshlet.TriangleCount * 3];


                for (int i = 0; i < meshlet.TriangleCount; i++)
                {
                    int primitiveIndex = i;

                    // if (i >= meshlet.VertCount)
                    // {
                    //     primitiveIndex = 0;
                    // }

                    var triangle = Collection.triangles[meshlet.TriangleOffset + primitiveIndex];
                    var index0 = triangle >> 0 & 0xFF;
                    var index1 = triangle >> 8 & 0xFF;
                    var index2 = triangle >> 16 & 0xFF;

                    // Debug.LogError($"AAA:{primitiveIndex}:---{index0}:{index1}:{index2}");
                    uint vertexIndex0 = Collection.vertices[meshlet.VertOffset + index0];
                    uint vertexIndex1 = Collection.vertices[meshlet.VertOffset + index1];
                    uint vertexIndex2 = Collection.vertices[meshlet.VertOffset + index2];
                    
                    // Debug.LogError($"BBB:{primitiveIndex}:---{vertexIndex0}:{vertexIndex1}:{vertexIndex2}");
                    vertices[primitiveIndex * 3 + 0] = Collection.optimizedVertices[vertexIndex0];
                    vertices[primitiveIndex * 3 + 1] = Collection.optimizedVertices[vertexIndex1];
                    vertices[primitiveIndex * 3 + 2] = Collection.optimizedVertices[vertexIndex2];

                    // Debug.LogError($"CCC:{primitiveIndex}:---{(int)meshlet.VertOffset + primitiveIndex * 3 + 0}");
                    triangles[primitiveIndex * 3 + 0] = primitiveIndex * 3 + 0;
                    triangles[primitiveIndex * 3 + 1] = primitiveIndex * 3 + 1;
                    triangles[primitiveIndex * 3 + 2] = primitiveIndex * 3 + 2;
                }


                mesh.vertices = vertices;
                mesh.triangles = triangles;

                GameObject meshletGO = new GameObject($"Mesh_{meshletIndex}");
                meshletGO.transform.parent = go.transform;
                meshletGO.transform.position = Vector3.zero;
                meshletGO.AddComponent<MeshFilter>().mesh = mesh;
                meshletGO.AddComponent<MeshRenderer>();
            }

           
        }
#endif
    }
}