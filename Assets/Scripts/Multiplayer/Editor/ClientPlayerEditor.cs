using Editor;
using Multiplayer.Entity.Client;
using UnityEditor;
using UnityEngine;

namespace Multiplayer.Editor
{
    [CustomEditor(typeof(ClientPlayer))]
    public class ClientPlayerEditor : ToolEditor<ClientPlayer>
    {
        protected override void OnEnableOverride()
        {
            Parameters = new[] { "Base", "Art", "Weapons", "Misc"};
        }

        protected override void OnInspectorGUIOverride()
        {
            GUILayout.Space(Space);
            GUILayout.BeginVertical("SelectionRect");
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.isLocal)),
                new GUIContent("Is Local Player"));
            GUILayout.EndVertical();
            if (Target.isLocal)
            {
                EditorGUILayout.HelpBox("Local Player Will Have Less Properties to Fill", UnityEditor.MessageType.Info);
            }
            GUILayout.Space(Space);
            
            switch (Tool)
            {
                case 0: //Base
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    
                    EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.orientation)), new GUIContent("Client Player"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.player)), new GUIContent("Rigidbody"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.head)), new GUIContent("Head Transform"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.demoPlayer)), new GUIContent("Replay Player Component"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.playerState)), new GUIContent("Player State Component"));

                    if (!Target.isLocal)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.spine)), new GUIContent("Spine"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.bones)), new GUIContent("Bones", "For ragdolls to sync the bones"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.nameCanvas)), new GUIContent("Name Canvas"));
                    }
                    
                    GUILayout.EndVertical();
                    break;
                case 1: // Art
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    
                    EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.bananaObj)), new GUIContent("Banana Object"));

                    if (!Target.isLocal)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.animator)), new GUIContent("Player Main Animator"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.nameText)), new GUIContent("Name Text"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.nameTrans)), new GUIContent("Name Transform"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.eyes)), new GUIContent("Eyes Transform"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.fire)), new GUIContent("Fire Particle"));
                    }
                    
                    GUILayout.EndVertical();
                    
                    if (!Target.isLocal)
                    {
                        GUILayout.Space(Space);
                        
                        EditorGUI.indentLevel = 0;
                        GUILayout.BeginVertical("Box");
                    
                        EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.audioSource)), new GUIContent("The Original Audio Source"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.voice)), new GUIContent("The Voice Chat Audio Source"));
                        
                        GUILayout.EndVertical();
                        
                        GUILayout.Space(Space);
                        
                        EditorGUI.indentLevel = 0;
                        GUILayout.BeginVertical("Box");
                    
                        EditorGUILayout.LabelField("Light", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.Light)), new GUIContent("The Flash light"));
                        
                        GUILayout.EndVertical();
                        
                        GUILayout.Space(Space);
                    
                        EditorGUI.indentLevel = 0;
                        GUILayout.BeginVertical("Box");
                    
                        EditorGUILayout.LabelField("Cosmetics", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        
                        EditorGUILayout.LabelField("Default Cosmetics", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 2;
                        
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.daveHair)), new GUIContent("Dave Hair"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.clothes)), new GUIContent("Clothe"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.pant)), new GUIContent("Pant"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.models)), new GUIContent("Default Cosmetic Models"));
                        
                        GUILayout.Space(Space);
                        
                        EditorGUI.indentLevel = 1;
                        
                        EditorGUILayout.LabelField("Cosmetic Objects", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 2;
                        
                        // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.hatCosmetics)), new GUIContent("Hat Cosmetics"));
                        // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.faceCosmetics)), new GUIContent("Face Cosmetics"));
                        // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.shoeLCosmetics)), new GUIContent("Shoe L Cosmetics"));
                        // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.shoeRCosmetics)), new GUIContent("Shoe R Cosmetics"));
                        // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.hairCosmetics)), new GUIContent("Hair Cosmetics"));
                        // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.clothesCosmetics)), new GUIContent("Clothes Cosmetics"));
                        // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.pantCosmetics)), new GUIContent("Pant Cosmetics"));
                        
                        GUILayout.EndVertical();
                    }
                    
                    break;
                case 2: //Weapons
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    
                    EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.weapons)), new GUIContent("Weapon Objects"));

                    if (!Target.isLocal)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.clawKnifeAnim)), new GUIContent("Weapon Objects"));
                    }
                    
                    GUILayout.EndVertical();
                    break;
                case 3: //Misc
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    
                    EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.col)), new GUIContent("Collider"));

                    if (!Target.isLocal)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("interpolator"), new GUIContent("Interpolator"));
                    }
                    
                    GUILayout.EndVertical();
                    
                    if (!Target.isLocal)
                    {
                        GUILayout.Space(Space);
                        
                        EditorGUI.indentLevel = 0;
                        GUILayout.BeginVertical("Box");
                    
                        EditorGUILayout.LabelField("Ik", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.iks)), new GUIContent("iks"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.leftHandTarget)), new GUIContent("Left Hand Ik"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.rightHandTarget)), new GUIContent("Right Hand Ik"));

                        GUILayout.EndVertical();
                    }
                    
                    break;
            }
        }
    }
}
