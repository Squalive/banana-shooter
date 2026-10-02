using System;
using Editor;
using Manager;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cosmetic.Editor
{
    [CustomEditor(typeof(CosmeticItem)),CanEditMultipleObjects]
    public class CosmeticItemEditor : ToolEditor<CosmeticItem>
    {
        protected override void OnEnableOverride()
        {
            Parameters = new[] { "Base", "Art" };
        }

        protected override void OnInspectorGUIOverride()
        {
            // Undo.RecordObject(Target,"Cosmetic changed");
            
            switch (Tool)
            {
                case 0: //Base
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("FrameBox");
                    EditorGUILayout.LabelField("Definition", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.itemdefid)), new GUIContent("Itemdefid", "The definition id of the item"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.displayName)), new GUIContent("Display Name", "The name which will be displayed to players"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.description)), new GUIContent("Description", "The description of the item"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.tag)), new GUIContent("Tag", "The tag of the item (most likely will use on particle items)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.index)), new GUIContent("Index", "The index of the item"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.type)), new GUIContent("Type", "The type of this item"));
                    if (Target.type != CosmeticItem.Type.None && Target.type != CosmeticItem.Type.Rag &&
                        Target.type != CosmeticItem.Type.Other && Target.type != CosmeticItem.Type.Particle)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.inventoryType)), new GUIContent("Inventory Type", "The inventory of this item (To define whether its weapons or cosmetics)"));
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.rarity)), new GUIContent("Rarity", "The rarity of this item"));

                    GUILayout.EndVertical();
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("FrameBox");
                    EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.canApplyParticle)), new GUIContent("Particles Applable", "Is this item particle applable?"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.relatedItem)), new GUIContent("Related Items", "Related Items"));
                    GUILayout.EndVertical();
                    break;
                case 1:
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("FrameBox");
                    EditorGUILayout.LabelField("Art", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.icon)), new GUIContent("Icon", "The icon of the item"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.mesh)), new GUIContent("Mesh", "The mesh of the item"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.materials)), new GUIContent("Materials", "The Materials of the item"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.alpha)), new GUIContent("Alpha", "The alpha of the item"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 0;
                    EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.size)), new GUIContent("Size", "The size of the item"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.offset)), new GUIContent("Offset", "The offset of the item"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.defaultRotation)), new GUIContent("Default Rotation", "The offset of the item"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.sizeMultiplier)), new GUIContent("Size Multiplier", "The offset of the item"));
                    GUILayout.EndVertical();
                    break;
            }
            
            GUILayout.BeginHorizontal("FrameBox");
            

            if (GUILayout.Button("Showcase"))
            {
                ShowcaseManager showcaseManager = FindObjectOfType<ShowcaseManager>();
                
                if(showcaseManager)
                    showcaseManager.Showcase(Target);
            }

            if (GUILayout.Button("Divide Size"))
            {
                Target.size /= 2f;
            }
            
            GUILayout.EndHorizontal();
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var item = (CosmeticItem)target;

            if (item == null || item.icon ==null)
            {
                return null;
            }

            var texture = new Texture2D(width, height);
            EditorUtility.CopySerialized(item.icon,texture);
            return texture;
        }
    }
}
