
using UnityEditor;
using UnityEngine;

namespace Map.Editor
{
    [CustomEditor(typeof(Map)),CanEditMultipleObjects]
    public class MapObjectEditor : UnityEditor.Editor
    {
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var item = (Map)target;

            if (item == null || item.texture ==null)
            {
                return null;
            }

            var texture = new Texture2D(width, height);
            EditorUtility.CopySerialized(item.texture,texture);
            return texture;
        }
    }
}