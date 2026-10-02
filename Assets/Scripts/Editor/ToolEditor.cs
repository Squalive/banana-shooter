using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor
{
    public class ToolEditor<T> : BaseEditor<T> where T : Object
    {
        protected int Tool;

        protected string[] Parameters = Array.Empty<string>();

        public override void OnInspectorGUI()
        {
            if (target == null)
            {
                return;
            }
            EditorGUI.BeginChangeCheck();

            Tool = GUILayout.Toolbar(Tool, Parameters);
            
            OnInspectorGUIOverride();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target,$"Changed {target.name}");
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}