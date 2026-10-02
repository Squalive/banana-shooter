
using UnityEditor;
using UnityEngine;

namespace Multiplayer.Editor
{
    [CustomEditor(typeof(NetworkManager)), CanEditMultipleObjects]
    public class NetworkManagerEditor : UnityEditor.Editor
    {
        private NetworkManager _network;

        private void OnEnable()
        {
            _network = (NetworkManager)target;
        }

        public override void OnInspectorGUI()
        {
            if (target == null)
                return;

            float space = 10f;

            EditorGUI.indentLevel = 0;
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_network.playerPrefab)), new GUIContent("Client Player", "The prefab which the local player sees"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_network.localPlayerPrefab)), new GUIContent("Local Player", "The local player prefab"));
            EditorGUILayout.HelpBox("Make sure there are only one Local Player in the scene", UnityEditor.MessageType.Info);
            GUILayout.EndVertical();
            
            GUILayout.Space(space);

            EditorGUI.indentLevel = 0;
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Weapons", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_network.weaponInfo)), new GUIContent("Weapon Infos", "All the weapons in the game"));
            GUILayout.EndVertical();

            GUILayout.Space(space);
            
            EditorGUI.indentLevel = 0;
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Custom", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_network.testMode)), new GUIContent("Test Mode", "Enable this to test new features"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_network.language)), new GUIContent("Locales", "All the locales make sure dont change the order"));
            if (_network.language.Length > 0)
            {
                EditorGUILayout.HelpBox("Make sure dont change the order", UnityEditor.MessageType.Warning);
            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
