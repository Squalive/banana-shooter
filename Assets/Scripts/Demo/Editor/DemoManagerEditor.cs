using Editor;
using UnityEditor;
using UnityEngine;

namespace Demo.Editor
{
    [CustomEditor(typeof(DemoManager))]
    public class DemoManagerEditor : BaseEditor<DemoManager>
    {
        protected override void OnInspectorGUIOverride()
        {
            if (DemoManager.Recording)
            {
                EditorGUILayout.LabelField("Recording...", EditorStyles.boldLabel);
            }
            
            GUILayout.Space(Space);
            
            EditorGUI.indentLevel = 0;
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Record & Save", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            if (GUILayout.Button("Record"))
            {
                Target.StartRecord(new GUID().ToString());
            }

            if (GUILayout.Button("Save"))
            {
                Target.Save();
            }

            if (DemoManager.Replaying)
            {
                if (GUILayout.Button("Skip 2 Second"))
                {
                    Target.SkipSeconds();
                }
            }

            GUILayout.EndVertical();
        }
    }
}