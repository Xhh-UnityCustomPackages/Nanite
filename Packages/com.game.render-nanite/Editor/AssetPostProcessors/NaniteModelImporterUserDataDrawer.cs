using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Nanite.Editor
{
    internal static class NaniteModelImporterUserDataDrawer
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            //池
            var wrapperPool = new ObjectPool<NaniteModelUserDataWrapper>(
                () =>
                {
                    var wrapper = ScriptableObject.CreateInstance<NaniteModelUserDataWrapper>();
                    wrapper.hideFlags = HideFlags.DontSave;
                    return wrapper;
                },
                w => w.Settings ??= new NaniteModelSettings(),
                actionOnDestroy: Object.DestroyImmediate
            );
            
            
            NaniteModelImporterEditor.DrawingInspectorGUI += editor =>
            {
                Object[] targets = editor.targets;
                var wrappers = new Object[targets.Length];
                
                for (int i = 0; i < targets.Length; i++)
                {
                    var wrapper = wrapperPool.Get();
                    string userData = ((ModelImporter) targets[i]).userData;
                    NaniteModelSettings.Deserialize(userData, wrapper.Settings);
                    wrappers[i] = wrapper;
                }
                
                var serializedObject = new SerializedObject(wrappers);
                serializedObject.Update();
                SerializedProperty property = serializedObject.FindProperty(nameof(NaniteModelUserDataWrapper.Settings));
                EditorGUILayout.PropertyField(property);
                serializedObject.ApplyModifiedProperties();

                if (GUILayout.Button("Extract Materials"))
                {
                    
                }

                serializedObject.Dispose();
                
                
                EditorGUILayout.Space();
            };
        }
    }
}