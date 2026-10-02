
using Editor;
using UnityEditor;
using UnityEngine;

namespace MapEditor.Editor
{
    [CustomEditor(typeof(MapBound)), CanEditMultipleObjects]
    public class MapBoundEditor : ToolEditor<MapBound>
    {
        protected override void OnEnableOverride()
        {
            Parameters = new[] { "Base", "Hill"};
        }

        protected override void OnInspectorGUIOverride()
        {
            switch (Tool)
            {
                case 0:
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Spawn Point", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.spawnPos)), new GUIContent("Spawn Point", "The spawn point of the map"));
                    GUILayout.EndVertical();
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Bound", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.center)), new GUIContent("Center", "The origin of the map"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.size)), new GUIContent("Size", "The size of the map"));
                    GUILayout.EndVertical();
                    break;
                case 1:
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Hill Spawn", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.hills)), new GUIContent("Hills", "The hills spawn point"));
                    GUILayout.EndVertical();
                    break;
            }
        }
    }
}
