
using Multiplayer.Interface;
using UnityEditor;
using UnityEngine;

namespace Multiplayer.Editor
{
    [CustomEditor(typeof(NetworkServerManager)), CanEditMultipleObjects]
    public class NetworkServerManagerEditor : UnityEditor.Editor
    {
        private NetworkServerManager _networkServer;

        private void OnEnable()
        {
            _networkServer = (NetworkServerManager)target;
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_networkServer.serverPlayerPrefab)), new GUIContent("Server Player", "Spawn this when players join"));
            GUILayout.EndVertical();
            
            GUILayout.Space(space);
            
            EditorGUI.indentLevel = 0;
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Weapons", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_networkServer.weaponInfo)), new GUIContent("Weapon Infos", "All the weapons in the game"));
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
