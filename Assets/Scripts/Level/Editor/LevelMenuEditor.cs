using UnityEditor;
using UnityEngine;

namespace Level.Editor
{
    [CustomEditor(typeof(LevelMenu))]
    public class LevelMenuEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LevelMenu levelMenu = (LevelMenu) target;

            if (GUILayout.Button("Display"))
            {
                levelMenu.Display();
            }
            
            if (GUILayout.Button("UnDisplay"))
            {
                levelMenu.UnDisplay();
            }
            
            if (GUILayout.Button("Add Visual XP"))
            {
                levelMenu.AddVisualXp();
            }
        }
    }
}