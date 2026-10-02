using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Graphics
{
    [DefaultExecutionOrder(-90)]
    public class RenderTextureCamera : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("m_outputRoot")]
        private RectTransform _outputRoot = null;
        public RectTransform OutputRoot
        {
            get { return _outputRoot; }
            set
            {
                _outputRoot = value;
                _canvas = _outputRoot.GetComponentInParent<Canvas>();
                _canvasScaler = _outputRoot.GetComponentInParent<CanvasScaler>();
            }
        }

        [SerializeField]
        [FormerlySerializedAs("m_overlayMaterial")]
        private Material _overlayMaterial;
        public Material OverlayMaterial
        {
            get { return _overlayMaterial; }
            set
            {
                _overlayMaterial = value;
                if (_output != null)
                {
                    _output.material = _overlayMaterial;
                }
            }
        }

        [SerializeField]
        [FormerlySerializedAs("m_allowMSAA")]
        private bool _allowMSAA = true;
        public bool AllowMSAA
        {
            get { return _allowMSAA; }
            set
            {
                if (_output == null || _camera == null)
                {
                    return;
                }

                _allowMSAA = value;
                ResizeRenderTexture();
            }
        }

        [SerializeField]
        [FormerlySerializedAs("m_fullscreen")]
        private bool _fullscreen = true;
        public bool Fullscreen
        {
            get { return _fullscreen; }
            set
            {
                _fullscreen = value;
                ResizeOutput();
            }
        }

        private Camera _camera;
        public Camera Camera
        {
            get { return _camera; }
        }
        private RawImage _output;
        public RawImage Output
        {
            get { return _output; }
            set { _output = value; }
        }

        public RectTransform RectTransform
        {
            get { return _output.rectTransform; }
        }

        private Canvas _canvas;
        public Canvas Canvas
        {
            get { return _canvas; }
        }

        private CanvasScaler _canvasScaler;

        private int _screenWidth;
        private int _screenHeight;
        private Rect _outputRect;
        private Vector3 _position;
        private RenderTexture _texture;

        /// <summary>
        /// In certain cases (when using StandaloneFileBrowser for example) several TryResizeRenderTexture calls must be skipped to avoid glitches due to incorrect values returned by Screen.width and Screen.height
        /// </summary>
        private static int _skipFrames = 0;
        public static int SkipFrames
        {
            get { return _skipFrames; }
            set { _skipFrames = value; }
        }
        
        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (!_fullscreen)
            {
                _camera.rect = new Rect(0, 0, 1, 1);
            }

            GameObject outputGo = null;
            if (_output == null)
            {
                outputGo = new GameObject(_camera.name + " Output");
                outputGo.SetActive(false);

                _output = outputGo.AddComponent<RawImage>();
                _output.raycastTarget = false;

                if (_overlayMaterial != null)
                {
                    _output.material = _overlayMaterial;
                }

                RectTransform rt = outputGo.GetComponent<RectTransform>();
                rt.SetParent(_outputRoot, false);
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.pivot = Vector2.zero;
            }
            else
            {
                _outputRoot = _output.rectTransform;
            }


            _canvas = _outputRoot.GetComponentInParent<Canvas>();
            _canvasScaler = _outputRoot.GetComponentInParent<CanvasScaler>();

            ResizeRenderTexture();
            ResizeOutput();

            if (outputGo != null)
            {
                outputGo.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                if(_camera.targetTexture == _texture)
                {
                    _camera.targetTexture = null;
                }

                _texture.Release();
                _texture = null;
            }


            if (_output != null)
            {
                Destroy(_output.gameObject);
            }
        }

        private void LateUpdate()
        {
            TryResizeRenderTexture();
        }

        public bool TryResizeRenderTexture(bool canResizeOutput = true)
        {
            if (_skipFrames > 0)
            {
                _skipFrames--;
                return false;
            }

            if(_output == null)
            {
                return false;
            }

            bool resizeRenderTexture = _outputRect != _output.rectTransform.rect || _screenWidth != Screen.width || _screenHeight != Screen.height;
            bool resizeOutput = canResizeOutput && (resizeRenderTexture || _output.rectTransform.position != _position);

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                if (_output.uvRect != _camera.rect)
                {
                    resizeOutput = true;
                }
            }

            if (resizeRenderTexture)
            {
                ResizeRenderTexture();
            }

            if (resizeOutput)
            {
                ResizeOutput();
            }

            return resizeRenderTexture || resizeOutput;
        }

        public void ResizeRenderTexture()
        {
            int sizeX;
            int sizeY;

            if (_fullscreen)
            {
                sizeX = Screen.width;
                sizeY = Screen.height;
                ResizeRenderTexture(sizeX, sizeY);
            }
            else
            {
                if(_output != null)
                {
                    Rect rect = _output.rectTransform.rect;

                    Vector2 size = rect.size * ((_canvasScaler != null) ? _canvasScaler.scaleFactor : 1);
                    sizeX = Mathf.RoundToInt(size.x);
                    sizeY = Mathf.RoundToInt(size.y);
                    ResizeRenderTexture(sizeX, sizeY);
                }
            }
        }

        private void ResizeRenderTexture(int sizeX, int sizeY)
        {
            RenderTexture oldTexture = _texture;

            //UnityEngine.Camera:Render() Thread group size must be above zero fix ==> min size == 2 ?
            _texture = new RenderTexture(Mathf.Max(2, sizeX), Mathf.Max(2, sizeY), 24, RenderTextureFormat.ARGB32);
            _texture.name = _camera.name + " RenderTexture";
            _texture.filterMode = FilterMode.Point;
            _texture.antiAliasing = _allowMSAA ? Mathf.Max(1, RenderPipelineInfo.MSAASampleCount) : 1;

            _camera.targetTexture = _texture;
            _output.texture = _texture;

            _outputRect = _output.rectTransform.rect;
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;

            if (oldTexture != null)
            {
                oldTexture.Release();
            }
        }

        public void ResizeOutput()
        {
            if (_output == null)
            {
                return;
            }

            if (_fullscreen)
            {
                if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    if (_camera == null)
                    {
                        return;
                    }
                    _output.uvRect = _camera.rect;
                }
                else
                {
                    Vector2 p0;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_outputRoot, Vector2.zero, _canvas.worldCamera, out p0);

                    Vector2 p1;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_outputRoot, new Vector2(Screen.width, Screen.height), _canvas.worldCamera, out p1);

                    _output.rectTransform.anchoredPosition = p0;
                    _output.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Abs(p1.x - p0.x));
                    _output.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(p1.y - p0.y));
                }
            }
           
            _position = _output.rectTransform.position;
        }
    }
}