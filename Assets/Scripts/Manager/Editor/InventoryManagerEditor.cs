using System;
using Cosmetic;
using UnityEditor;
using UnityEngine;

namespace Manager.Editor
{
    [CustomEditor(typeof(InventoryManager)),CanEditMultipleObjects]
    public class InventoryManagerEditor : UnityEditor.Editor
    {
        private InventoryManager _inventory;

        private void OnEnable()
        {
            _inventory = (InventoryManager) target;
        }

        public override void OnInspectorGUI()
        {
            if (target == null)
                return;

            float space = 10f;
            
            EditorGUI.indentLevel = 0;
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Cosmetics", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_inventory.cosmeticItems)), new GUIContent("Cosmetic Items", "All the cosmetics the game contains"));
            GUILayout.EndVertical();
            
            GUILayout.Space(space);
            
            EditorGUI.indentLevel = 0;
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Particles", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_inventory.itemsPrefabs)), new GUIContent("Item Prefabs", "All the particles the game contains"));
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("Check Items Missing"))
            {
                foreach (var cosmeticItem in _inventory.cosmeticItems)
                {
                    if(cosmeticItem.type == CosmeticItem.Type.MenuScene || cosmeticItem.type == CosmeticItem.Type.Particle)
                        continue;
                    
                    string path = AssetDatabase.GetAssetPath(cosmeticItem.GetInstanceID());
                    if (cosmeticItem.mesh == null)
                    {
                        Debug.Log($"{path}'s mesh was missing");
                    }

                    if (cosmeticItem.icon == null)
                    {
                        Debug.Log($"{path}'s icon was missing");
                    }

                    foreach (var mat in cosmeticItem.materials)
                    {
                        if (mat == null)
                        {
                            Debug.Log($"{path}'s materials was missing");
                            break;
                        }
                    }
                }
            }
            GUILayout.EndVertical();
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
