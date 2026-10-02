using System;
using Manager.Interface;
using UnityEngine;

namespace Manager
{
    public class EnableGui : IGui
    {
        public void DrawText(string text, Color color, Rect rect, int fontSize, FontStyle fontStyle, Action onClick= null)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                padding = new RectOffset(2, 0, 0, 2),
                fontStyle = fontStyle,
                normal =
                {
                    textColor = color
                },
                hover =
                {
                    textColor = color
                },
                active =
                {
                    textColor = color
                }
            };
            // style.hover.background = MakeTexture(Color.cyan);
            // style.active.background = MakeTexture(Color.gray);
            //
            // GUI.Label(rect, text,style);

            // GUILayout.Label(text,rect, style);

            if (GUI.Button(rect, text, style))
            {
                onClick?.Invoke();
            }
        }
    }
}