using System;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class GUIStyleViewer : EditorWindow
    {
        Vector2 _scrollPos = Vector2.zero;

        private string _search = String.Empty;

        private GUIStyle _textStyle;

        private static GUIStyleViewer _window;

        [MenuItem("Tool/GUIStyleViewer", false, 10)]
        private static void OpenStyleViewer()
        {
            _window = GetWindow<GUIStyleViewer>(false, "Built-In GUI Style");
        }

        private void OnGUI()
        {
            if (_textStyle == null)
            {
                _textStyle = new GUIStyle("HeaderLabel")
                {
                    fontSize = 25
                };
            }
            
            GUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label("Results: ",_textStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Search:");
            _search = EditorGUILayout.TextField(_search);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal("PopupCurveSwatchBackground");
            GUILayout.Label("Showcase",_textStyle, GUILayout.Width(300));
            GUILayout.Label("Name",_textStyle,GUILayout.Width(300));
            GUILayout.EndHorizontal();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            foreach (var style in GUI.skin.customStyles)
            {
                if (style.name.ToLower().Contains(_search.ToLower()))
                {
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal("PopupCurveSwatchBackground");
                    if (GUILayout.Button(style.name, style, GUILayout.Width(300)))
                    {
                        EditorGUIUtility.systemCopyBuffer = style.name;
                        Debug.Log($"Copied {style.name} To System Buffer");
                    }
                    EditorGUILayout.SelectableLabel(style.name,GUILayout.Width(300));
                    GUILayout.EndHorizontal();
                }
            }
            
            GUILayout.EndScrollView();
        }
    }
}