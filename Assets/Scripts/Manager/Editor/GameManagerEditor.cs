
using Editor;
using UnityEditor;
using UnityEngine;

namespace Manager.Editor
{
    [CustomEditor(typeof(GameManager)),CanEditMultipleObjects]
    public class GameManagerEditor : ToolEditor<GameManager>
    {
        protected override void OnEnableOverride()
        {
            Parameters = new[] { "Settings", "Infos", "LayerMask", "Misc" };
        }

        protected override void OnInspectorGUIOverride()
        {
            switch (Tool)
            {
                case 0:
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.setting)), new GUIContent("Settings", "Setting"));
                    GUILayout.EndVertical();
                    
                    GUILayout.Space(Space);

                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Intro Theme", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    Target.introTheme = GUILayout.Toolbar(Target.introTheme, new[] { "Original", "Gray", "Blue", "Pink", "Dark", "Brown", "Red", "Milk"});
                    GUILayout.EndVertical();
            
                    GUILayout.Space(Space);

                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Ragdoll Limitation", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.ragdollLimited)), new GUIContent("Maximum Ragdoll Count", "A limitation of how many ragdolls can be in the scene at the same time"));
                    GUILayout.EndVertical();
                    break;
                case 1:
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Maps", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.maps)), new GUIContent("Maps", "A list of the available maps to be tracked for the 'World Travel' achievement"));
                    GUILayout.EndVertical();
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Upgrade", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.upgradeDetaileds)), new GUIContent("Upgrade Details", "A list of the available upgrades can be used in game"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.upgrades)), new GUIContent("Upgrades", "The upgrades the current user is using"));
                    GUILayout.EndVertical();
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Power Upgrade", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.powerDetails)), new GUIContent("Power Upgrade Details", "A list of the available power upgrades can be used in game"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.power)), new GUIContent("Power Upgrade", "The power upgrade the current user is using"));
                    GUILayout.EndVertical();
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Throwable", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.tacticalProp)), new GUIContent("Throwable Type", "A list of the available throwables can be used in game"));
                    GUILayout.EndVertical();
                    
                    break;
                case 2:
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.whatIsGround)), new GUIContent("Ground Layer", "To determine what is Ground in the game"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.lagCompensationHitboxLayer)), new GUIContent("Lag Compensation Hitbox Layer", "To determine what is the lag compensation hitbox in the game"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.serverPlayer)), new GUIContent("Server Player Layer", "To determine what is server player in the game"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.flashBangHitLayer)), new GUIContent("Flashbang Layer", "To determine what is flashbangable in the game"));
                    GUILayout.EndVertical();
                    break;
                case 3:
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Global Properties", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.volume)), new GUIContent("Global Volume", "The main volume"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.earRingingClip)), new GUIContent("Ear ringing Audio", "Play when the player gets flashbanged"));
                    GUILayout.EndVertical();
                    break;
            }
        }
    }
}
