using System;
using UnityEngine;

namespace CodingDaniel.MapEditor.Utils
{
    public class CursorHelper : MonoBehaviour
    {
        public static CursorHelper Instance { private set; get; }

        private void Awake()
        {
            Instance = this;
        }

        private Texture2D _defaultCursorTexture;
        private Vector2 _defaultCursorHotspot;
        public Texture2D DefaultCursorTexture
        {
            get 
            {
                return _defaultCursorTexture;
            }
        }
        public Vector2 DefaultCursorHotspot
        {
            get 
            {
                return _defaultCursorHotspot;
            }
        }
        private Texture2D _texture;
        public bool SetCursor(Texture2D texture, Vector2 hotspot, CursorMode mode)
        {
            if (texture != null)
            {
                hotspot = new Vector2(texture.width * hotspot.x, texture.height * hotspot.y);
            }
            else
            {
                texture = DefaultCursorTexture;
                if (texture != null)
                {
                    hotspot = new Vector2(texture.width * DefaultCursorHotspot.x, texture.height * DefaultCursorHotspot.y);
                }
            }

            if (_texture != texture)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Cursor.SetCursor(texture, hotspot, mode);
                _texture = texture;
                return true;
            }

            return false;
        }
    }
}
