using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nanite.Editor
{
    [CustomEditor(typeof(ModelImporter))]
    [CanEditMultipleObjects]
    public class NaniteModelImporterEditor : UnityEditor.Editor
    {
        private ModelImporterEditor m_DefaultEditor;

        public void OnEnable()
        {
            if (m_DefaultEditor == null)
            {
                m_DefaultEditor = (ModelImporterEditor) CreateEditor(targets, typeof(ModelImporterEditor));
                m_DefaultEditor.InternalSetAssetImporterTargetEditor(this);
            }
        }

        public void OnDisable()
        {
            if (m_DefaultEditor != null)
            {
                m_DefaultEditor.OnDisable();
            }
        }
        
        private void OnDestroy()
        {
            m_DefaultEditor.OnEnable();
            DestroyImmediate(m_DefaultEditor);
            m_DefaultEditor = null;
        }
        
        internal override void PostSerializedObjectCreation()
        {
            base.PostSerializedObjectCreation();
            m_DefaultEditor.PostSerializedObjectCreation();
        }

        public override GUIContent GetPreviewTitle() => m_DefaultEditor.activeTab is ModelImporterClipEditor activeTab
            ? new GUIContent(activeTab.selectedClipName)
            : base.GetPreviewTitle();

        public override bool HasPreviewGUI() => m_DefaultEditor.HasPreviewGUI();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawingInspectorGUI?.Invoke(this);

            m_DefaultEditor.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }
        
        public static event Action<NaniteModelImporterEditor> DrawingInspectorGUI;
    }
}
