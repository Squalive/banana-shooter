
using System;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityObject = UnityEngine.Object;
namespace CodingDaniel.MapEditor.Handle
{
    public class SelectionPicker
    {
        private Action<FilteringArgs> _filterCallback;
        public SelectionPicker(Action<FilteringArgs> filterCallback = null)
        {
            _filterCallback = filterCallback;
        }

        protected virtual Renderer[] GetRenderers()
        {
            return UnityObject.FindObjectsOfType<Renderer>().Where(r => r.hideFlags == HideFlags.None && r.isVisible).ToArray();
        }

        protected virtual HashSet<Renderer> FilterObjects(FilteringArgs filteringArgs, IEnumerable<Renderer> renderers)
        {
            HashSet<Renderer> selection = new HashSet<Renderer>();
            foreach (Renderer rend in renderers)
            {
                if (!selection.Contains(rend))
                {
                    if (_filterCallback != null)
                    {
                        filteringArgs.Object = rend.gameObject;
                        _filterCallback(filteringArgs);
                        if (!filteringArgs.Cancel)
                        {
                            selection.Add(rend);
                        }
                        filteringArgs.Reset();
                    }
                    else
                    {
                        selection.Add(rend);
                    }
                }
            }

            return selection;
        }

        public IEnumerable<Renderer> PixelPerfectDepthTest(Renderer[] renderers, Bounds bounds)
        {
            Canvas canvas = BoxSelection.Instance.Canvas;

            RectTransform sceneOutput = canvas.GetComponent<RectTransform>();

            Rect rect = SelectionBoundsToSelectionRect(MEBase.Instance.Camera, bounds, canvas, sceneOutput);
            return PickRenderersInRect(MEBase.Instance.Camera, rect, renderers, Mathf.RoundToInt(canvas.pixelRect.width), Mathf.RoundToInt(canvas.pixelRect.height));
        }

        public Renderer[] Pick(Renderer[] renderers = null, bool filterObjects = true)
        {
            return Pick(renderers, new Bounds(MEBase.Instance.Pointer.ScreenPoint, Vector2.zero), filterObjects);
        }

        public Renderer[] Pick(Renderer[] renderers, Bounds bounds, bool filterObjects = true)
        {
            if (renderers == null)
            {
                renderers = GetRenderers();
            }

            IEnumerable<Renderer> selection = PixelPerfectDepthTest(renderers, bounds);
            if (filterObjects)
            {
                FilteringArgs filteringArgs = new FilteringArgs();
                return FilterObjects(filteringArgs, selection).ToArray();
            }

            return selection.ToArray();
        }

        public Color32[] BeginPick(out Vector2Int texSize, Renderer[] renderers = null)
        {
            if (renderers == null)
            {
                renderers = GetRenderers();
            }

            Canvas canvas = BoxSelection.Instance.Canvas;
            return Render(MEBase.Instance.Camera, renderers, new Vector2Int(Mathf.RoundToInt(canvas.pixelRect.width), Mathf.RoundToInt(canvas.pixelRect.height)), out texSize);
        }

        public Renderer[] EndPick(Color32[] texPixels, Vector2Int texSize, Renderer[] renderers = null)
        {
            if (renderers == null)
            {
                renderers = UnityObject.FindObjectsOfType<Renderer>().Where(r => r.hideFlags == HideFlags.None && r.isVisible).ToArray(); ;
            }

            Bounds bounds = new Bounds(MEBase.Instance.Pointer.ScreenPoint, Vector2.zero);
            return EndPick(texPixels, texSize, renderers, bounds);
        }

        private Renderer[] EndPick(Color32[] texPixels, Vector2Int texSize, Renderer[] renderers, Bounds bounds)
        {
            Canvas canvas = BoxSelection.Instance.GetComponentInParent<Canvas>();
            RectTransform sceneOutput = canvas.GetComponent<RectTransform>();

            Rect selectionRect = SelectionBoundsToSelectionRect(MEBase.Instance.Camera, bounds, canvas, sceneOutput);
            return PickRenderersInRect(MEBase.Instance.Camera, selectionRect, renderers, texPixels, texSize);
        }

        private static Rect SelectionBoundsToSelectionRect(Camera sceneCamera, Bounds bounds, Canvas canvas, RectTransform sceneOutput)
        {
            Vector2 min = bounds.min;
            Vector2 max = bounds.max;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(sceneOutput, min, canvas.worldCamera, out min);
            min.y = sceneOutput.rect.height - min.y;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(sceneOutput, max, canvas.worldCamera, out max);
            max.y = sceneOutput.rect.height - max.y;

            /*quick fix for ui scale issue. TODO: replace with better solution*/
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                min *= scaler.scaleFactor;
                max *= scaler.scaleFactor;
            }

            Rect rect = new Rect(new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y)), new Vector2(Mathf.Max(Mathf.Abs(max.x - min.x), 1), Mathf.Max(Mathf.Abs(max.y - min.y), 1)));
            rect.x += sceneCamera.pixelRect.x;
            rect.y += canvas.pixelRect.height - (sceneCamera.pixelRect.y + sceneCamera.pixelRect.height);
            return rect;
        }

        private static RenderTextureFormat _renderTextureFormat;
        private static bool _initialized;
        private static RenderTextureFormat[] _preferredFormats = new RenderTextureFormat[]
        {
            RenderTextureFormat.ARGB32,
            RenderTextureFormat.ARGBFloat,
        };

        private static RenderTextureFormat RenderTextureFormat
        {
            get
            {
                Init();
                return _renderTextureFormat;
            }
        }

        private static TextureFormat TextureFormat { get { return TextureFormat.ARGB32; } }
        private static Shader _objectSelectionShader;
        private static Shader ObjectSelectionShader
        {
            get
            {
                Init();
                return _objectSelectionShader;
            }
        }

        private static void Init()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _objectSelectionShader = Shader.Find("CodingDaniel/MEHandles/BoxSelectionShader");

            for (int i = 0; i < _preferredFormats.Length; i++)
            {
                if (SystemInfo.SupportsRenderTextureFormat(_preferredFormats[i]))
                {
                    _renderTextureFormat = _preferredFormats[i];
                    break;
                }
            }
        }

        public static Color32[] Render(Camera camera, Renderer[] renderers, Vector2Int reqiestedTexSize, out Vector2Int texSize)
        {
            for (int i = 0; i < renderers.Length; ++i)
            {
                Renderer renderer = renderers[i];
                if (renderer.isPartOfStaticBatch)
                {
                    continue;
                }
                Material[] materials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; ++materialIndex)
                {
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(propertyBlock, materialIndex);
                    propertyBlock.SetColor("_SelectionColor", EncodeRGBA((uint)i + 1));
                    renderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
            }

            Texture2D tex = Render(camera, ObjectSelectionShader, renderers, reqiestedTexSize.x, reqiestedTexSize.y);
            Color32[] texPizels = tex.GetPixels32();
            texSize = new Vector2Int(tex.width, tex.height);
            UnityObject.DestroyImmediate(tex);
            return texPizels;
        }

        public static Renderer[] PickRenderersInRect(Camera camera, Rect selectionRect, Renderer[] renderers, Color32[] texPixels, Vector2Int texSize)
        {
            selectionRect.width /= camera.rect.width;
            selectionRect.height /= camera.rect.height;
            selectionRect.x = (selectionRect.x - camera.pixelRect.x) / camera.rect.width;
            selectionRect.y = (selectionRect.y - (texSize.y - (camera.pixelRect.y + camera.pixelRect.height))) / camera.rect.height;

            int ox = System.Math.Max(0, Mathf.FloorToInt(selectionRect.x));
            int oy = System.Math.Max(0, Mathf.FloorToInt(texSize.y - selectionRect.y - selectionRect.height));

            int width = Mathf.FloorToInt(selectionRect.width);
            int height = Mathf.FloorToInt(selectionRect.height);

            List<Renderer> selectedRenderers = new List<Renderer>();
            HashSet<int> used = new HashSet<int>();

            for (int y = oy; y < System.Math.Min(oy + height, texSize.y); y++)
            {
                for (int x = ox; x < System.Math.Min(ox + width, texSize.x); x++)
                {
                    int index = (int)DecodeRGBA(texPixels[y * texSize.x + x]) - 1;
                    if (index < 0 || index >= renderers.Length)
                    {
                        continue;
                    }

                    if (used.Add(index))
                    {
                        Renderer selectedRenderer = renderers[index];
                        selectedRenderers.Add(selectedRenderer);

                        if (selectedRenderers.Count == renderers.Length)
                        {
                            return selectedRenderers.ToArray();
                        }
                    }
                }
            }

            return selectedRenderers.ToArray();
        }

        public static Renderer[] PickRenderersInRect(
            Camera camera,
            Rect selectionRect,
            Renderer[] renderers,
            int renderTextureWidth = -1,
            int renderTextureHeight = -1)
        {

            Vector2Int texSize;
            Color32[] pixels = Render(camera, renderers, new Vector2Int(renderTextureWidth, renderTextureHeight), out texSize);
            return PickRenderersInRect(camera, selectionRect, renderers, pixels, texSize);
        }


        private static uint DecodeRGBA(Color32 color)
        {
            uint r = color.r;
            uint g = color.g;
            uint b = color.b;

            if (System.BitConverter.IsLittleEndian)
            {
                return r << 16 | g << 8 | b;
            }

            return r << 24 | g << 16 | b << 8;
        }

        private static Color32 EncodeRGBA(uint hash)
        {
            if (System.BitConverter.IsLittleEndian)
                return new Color32(
                    (byte)(hash >> 16 & 0xFF),
                    (byte)(hash >> 8 & 0xFF),
                    (byte)(hash & 0xFF),
                    (byte)(255));
            else
                return new Color32(
                    (byte)(hash >> 24 & 0xFF),
                    (byte)(hash >> 16 & 0xFF),
                    (byte)(hash >> 8 & 0xFF),
                    (byte)(255));
        }

        private static Texture2D Render(
            Camera camera,
            Shader shader,
            Renderer[] renderers,
            int width = -1,
            int height = -1)
        {

            bool autoSize = width < 0 || height < 0;

            int _width = autoSize ? (int)camera.pixelRect.width : width;
            int _height = autoSize ? (int)camera.pixelRect.height : height;

            GameObject go = new GameObject();
            Camera renderCam = go.AddComponent<Camera>();
            renderCam.CopyFrom(camera);

            renderCam.renderingPath = RenderingPath.Forward;
            renderCam.enabled = false;
            renderCam.clearFlags = CameraClearFlags.SolidColor;
            renderCam.backgroundColor = Color.white;

            IRenderPipelineCameraUtility cameraUtility = CameraUtilityURP.Instance;
            if (cameraUtility != null)
            {
                cameraUtility.ResetCullingMask(renderCam);
                cameraUtility.EnablePostProcessing(renderCam, false);
                cameraUtility.SetBackgroundColor(renderCam, Color.white);
            }
            else
            {
                renderCam.cullingMask = 0;
            }

            renderCam.allowHDR = false;
            renderCam.allowMSAA = false;
            renderCam.forceIntoRenderTexture = true;

            float aspect = renderCam.aspect;
            renderCam.rect = new Rect(Vector2.zero, Vector2.one);
            renderCam.aspect = aspect;

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor()
            {
                width = _width,
                height = _height,
                colorFormat = RenderTextureFormat,
                autoGenerateMips = false,
                depthBufferBits = 16,
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = false,
                memoryless = RenderTextureMemoryless.None,
                sRGB = true,
                useMipMap = false,
                volumeDepth = 1,
                msaaSamples = 1
            };
            RenderTexture rt = RenderTexture.GetTemporary(descriptor);

            RenderTexture prev = RenderTexture.active;
            renderCam.targetTexture = rt;
            RenderTexture.active = rt;

            Material replacementMaterial = new Material(shader);

            IMEGraphic graphics = MEBase.Instance.Graphics;
            IMECamera rteCamera = graphics.CreateCamera(renderCam, CameraEvent.AfterForwardAlpha, true);
            rteCamera.RenderersCache.MaterialOverride = replacementMaterial;
            rteCamera.Camera.name = "BoxSelectionCamera";
            foreach (Renderer renderer in renderers)
            {
                if (renderer.isPartOfStaticBatch)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; ++i)
                {
                    if (materials[i] != null)
                    {
                        rteCamera.RenderersCache.Add(renderer);
                    }
                }
            }
            rteCamera.RefreshCommandBuffer();

            if (RenderPipelineInfo.Type != RPType.Standard)
            {
                bool invertCulling = GL.invertCulling;
                GL.invertCulling = true;
                renderCam.projectionMatrix *= Matrix4x4.Scale(new Vector3(1, -1, 1));
                renderCam.Render();
                GL.invertCulling = invertCulling;
            }
            else
            {
                renderCam.Render();
            }

            Texture2D img = new Texture2D(_width, _height, TextureFormat, false, false);
            img.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            img.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            UnityObject.DestroyImmediate(go);
            UnityObject.Destroy(replacementMaterial);

            rteCamera.Destroy();
            //System.IO.File.WriteAllBytes("Assets/box_selection.png", img.EncodeToPNG());

            return img;
        }
    }
}