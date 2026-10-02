
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Handle
{
    public class Pointer : MonoBehaviour
    {
        private IME _editor;
        [SerializeField]
        [FormerlySerializedAs("m_clampToCameraPixelRect")]
        private bool _clampToCameraPixelRect = false;

        public virtual Ray Ray
        {
            get
            {
                Vector2 screenPoint = ScreenPoint;
                if(_clampToCameraPixelRect)
                {
                    Rect pixelRect = _editor.Camera.pixelRect;
                    if (!pixelRect.Contains(screenPoint))
                    {
                        return new Ray(Vector3.up * float.MaxValue, Vector3.up);
                    }
                }
                return _editor.Camera.ScreenPointToRay(screenPoint);
            }
        }

        public virtual Vector2 ScreenPoint => ScreenPointToViewPoint(Input.mousePosition);

        private RenderTextureCamera _renderTextureCamera;
        private CanvasScaler _canvasScaler;
        private Canvas _canvas;

        protected virtual void Awake()
        {
            Init();
        }

        protected virtual void Init()
        {
            _editor = MEBase.Instance;
            _canvas = GetComponentInParent<Canvas>();

            if (_editor.Camera != null)
            {
                _renderTextureCamera = _editor.Camera.GetComponent<RenderTextureCamera>();
                if (_renderTextureCamera != null)
                {
                    _canvasScaler = GetComponentInParent<CanvasScaler>();
                }
            }
        }

        private Vector2 ScreenPointToViewPoint(Vector2 screenPoint)
        {
            if (_renderTextureCamera == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return screenPoint;
            }

            Vector2 viewPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_renderTextureCamera.RectTransform, screenPoint, _renderTextureCamera.Canvas.worldCamera, out viewPoint);

            if(_canvasScaler != null)
            {
                if (_canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    viewPoint = screenPoint;
                }
                else
                {
                    viewPoint *= _canvasScaler.scaleFactor;
                }
            }
            
            return viewPoint;
        }

        public virtual bool WorldToScreenPoint(Vector3 worldPoint, Vector3 point, out Vector2 result)
        {
            result = _editor.Camera.WorldToScreenPoint(point);
            result = ScreenPointToViewPoint(result);
            return true;
        }

        public virtual bool XY(Vector3 worldPoint, out Vector2 result)
        {
            result = ScreenPoint;
            return true;
        }

        public virtual bool ToWorldMatrix(Vector3 worldPoint, out Matrix4x4 matrix)
        {
            matrix = _editor.Camera.cameraToWorldMatrix;
            return true;
        }

        public static implicit operator Ray(Pointer pointer)
        {
            return pointer.Ray;
        }
    }
}