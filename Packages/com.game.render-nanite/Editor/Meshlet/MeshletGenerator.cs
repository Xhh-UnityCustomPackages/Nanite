using System;
using Nanite.Runtime;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Nanite.Editor
{
    public class MeshletGenerator : OdinEditorWindow
    {
        [MenuItem("Window/Nanite/Meshlet Generator")]
        public static void ShowWindow()
        {
            GetWindow<MeshletGenerator>("Meshlet Generator");
        }

        [ShowInInspector] 
        [HideLabel] 
        private BuildSettings m_Settings = new BuildSettings();

        [ShowInInspector] 
        private Mesh m_SelectedMesh;
        
        [ShowInInspector]
        [FolderPath]
        private string m_SavePath = "Assets/";
        private string m_AssetName;
        private bool m_ProcessingMesh;

        protected override void OnEnable()
        {
            m_Settings.EnableFuse = true;
            m_Settings.EnableOpt = true;
            m_Settings.EnableRemap = true;
            m_Settings.MaxVertices = 64;
            m_Settings.MaxTriangles = 64;
            m_Settings.ConeWeight = 0.5f;
        }
        

        // [Button]
        // private void GenerateMeshlets()
        // {
        //     if (m_ProcessingMesh) return;
        //     if (!m_SelectedMesh) return;
        //
        //     m_ProcessingMesh = true;
        //
        //     try
        //     {
        //         m_AssetName = m_SelectedMesh.name;
        //         
        //         // Get mesh data
        //         var vertices = m_SelectedMesh.vertices;
        //         var triangles = m_SelectedMesh.triangles;
        //
        //         // Convert triangles to uint[]
        //         var uintTriangles = new uint[triangles.Length];
        //         for (var i = 0; i < triangles.Length; i++)
        //         {
        //             uintTriangles[i] = (uint)triangles[i];
        //         }
        //
        //         // Process the mesh
        //         var collection = MeshOptimizer.ProcessMesh(uintTriangles, vertices, m_Settings);
        //
        //         // Create and save the asset
        //         var asset = CreateInstance<MeshletAsset>();
        //         asset.Collection = collection;
        //         asset.SourceMesh = m_SelectedMesh;
        //         var fullPath = System.IO.Path.Combine(m_SavePath, $"{m_AssetName}.asset");
        //         AssetDatabase.CreateAsset(asset, fullPath);
        //         AssetDatabase.SaveAssets();
        //         EditorUtility.FocusProjectWindow();
        //         Selection.activeObject = asset;
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogError($"Error generating meshlets: {ex}");
        //     }
        //     finally
        //     {
        //         m_ProcessingMesh = false;
        //     }
        // }
        //
        // [Button]
        // void CalcArraySize()
        // {
        //     UIntPtr maxMeshlets = MeshOptimizer.meshopt_buildMeshletsBound((UIntPtr)m_SelectedMesh.triangles.Length, (UIntPtr)m_Settings.MaxVertices, (UIntPtr)m_Settings.MaxTriangles);
        //
        //     int maxMeshletsInt = (int)maxMeshlets.ToUInt32();
        //     Debug.LogError(maxMeshletsInt);
        // }
    }
}