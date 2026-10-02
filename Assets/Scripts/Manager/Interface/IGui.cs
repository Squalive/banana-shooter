using System;
using UnityEngine;

namespace Manager.Interface
{
    public interface IGui
    {
        void DrawText(string text, Color color, Rect rect, int fontSize, FontStyle fontStyle, Action onClick= null);
    }
}