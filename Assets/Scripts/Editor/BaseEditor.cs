
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public abstract class BaseEditor<T> : UnityEditor.Editor where T : Object
    {
        protected T Target;
        protected float Space = 10f;

        private void OnEnable()
        {
            Target = (T) target;

            OnEnableOverride();
        }

        public override void OnInspectorGUI()
        {
            if (target == null)
            {
                return;
            }
            EditorGUI.BeginChangeCheck();

            OnInspectorGUIOverride();
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target,$"Changed {target.name}");
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
            }
        }

        protected virtual void OnInspectorGUIOverride()
        {
            
        }

        protected virtual void OnEnableOverride()
        {
            
        }
    }
}
