using UnityEngine;

namespace Extensions
{
    public static class ColorExtensions
    {
        public static float Magnitude(this Color color)
        {
            return Mathf.Abs(color.r + color.g + color.b) / 3f;
        }
    }
}