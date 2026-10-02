using System.Collections.Generic;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Graphics
{
    /// <summary>
    /// Interface to allow rendering "outline" selection in the runtime editor with a different material.
    /// 
    /// Material should render the full relevant area in opaque red rgba(1, 0, 0, 1)
    /// </summary>
    public interface ICustomOutlinePrepass
    {
        Renderer GetRenderer();
        Material GetOutlinePrepassMaterial();
    }
    public interface ICustomOutlineRenderersCache
    {
        List<ICustomOutlinePrepass> GetOutlineRendererItems();
    }
    public class MERenderSelection : ScriptableRendererFeature
    {
        [System.Serializable]
        public class RenderSelectionSettings
        {
            public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            public Material PrepassMaterial = null;
            public Material CompositeMaterial = null;
            public Color OutlineColor = new Color32(255, 128, 0, 255);

            /// <summary>Opacity of the band. Above 1 it saturates, which is what a crisp line wants.</summary>
            [Range(0.5f, 10f)]
            public float OutlineStength = 5;

            /// <summary>
            /// Width of the selection outline in screen pixels, measured outward from the silhouette.
            /// </summary>
            [Range(0.5f, 20f)]
            public float OutlineRadius = 3f;

            /// <summary>
            /// Tint of the outline drawn around whatever sits under the pointer. Applied at runtime
            /// from the editor palette; the value here is only the fallback until the editor boots.
            /// </summary>
            public Color HoverColor = new Color32(255, 160, 32, 255);

            /// <summary>Width of the hover outline in screen pixels.</summary>
            [Range(0.5f, 20f)]
            public float HoverRadius = 4f;
        }

        [SerializeField]
        [FormerlySerializedAs("m_settings")]
        public RenderSelectionSettings _settings = new RenderSelectionSettings();

        /// <summary>
        /// One outlined set of renderers. The selection and the hover each own a cache, a tint, a
        /// radius, their own render targets and their own material instances, and are drawn as
        /// independent passes.
        ///
        /// The materials are cloned per layer on purpose: a material's own texture bindings win over
        /// the ones a Blit supplies, so sharing an instance between layers lets one layer's textures
        /// leak into the other's draw.
        ///
        /// There is deliberately no blur stage. The ring is a dilation of the mask computed in the
        /// composite fragment from eight offset samples, which is uniform in width by construction
        /// and has no intermediate target whose format, texel size or ordering can go wrong.
        /// </summary>
        class OutlineLayer
        {
            public IRenderersCache Cache;
            public Color Color;
            public float Radius;

            /// <summary>Object mask: white on the layer's objects, black elsewhere.</summary>
            public RenderTexture Prepass;

            /// <summary>Copy of the scene colour the composite blends over.</summary>
            public RenderTexture Scene;

            private Material _prepassMaterial;
            private Material _compositeMaterial;

            private int _prepassId;
            private int _mainTexId;
            private int _outlineColorId;
            private int _outlineStrengthId;
            private int _outlineOffsetId;

            private int _width;
            private int _height;

            public OutlineLayer(Material prepassTemplate, Material compositeTemplate)
            {
                _prepassMaterial = Clone(prepassTemplate);
                _compositeMaterial = Clone(compositeTemplate);

                // The scene colour is supplied by the blit. A cloned material still carries whatever
                // _MainTex the asset had - and OutlineComposite.mat points at a built-in white texture
                // - which would otherwise win over the texture the blit provides and make the
                // composite read white instead of the scene.
                ClearMainTexture(_prepassMaterial);
                ClearMainTexture(_compositeMaterial);

                _prepassId = Shader.PropertyToID("_PrepassTex");
                _mainTexId = Shader.PropertyToID("_MainTex");

                _outlineColorId = Shader.PropertyToID("_OutlineColor");
                _outlineStrengthId = Shader.PropertyToID("_OutlineStrength");
                _outlineOffsetId = Shader.PropertyToID("_OutlineOffset");
            }

            private static Material Clone(Material template)
            {
                return template == null ? null : new Material(template);
            }

            private static void ClearMainTexture(Material material)
            {
                if (material != null && material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", null);
                }
            }

            public void EnsureResources(int width, int height)
            {
                if (Prepass != null && _width == width && _height == height)
                {
                    return;
                }

                Release();

                _width = width;
                _height = height;

                Prepass = CreateTarget(width, height, "MERenderSelection_Prepass");
                Scene = CreateTarget(width, height, "MERenderSelection_Scene");
            }

            private static RenderTexture CreateTarget(int width, int height, string name)
            {
                RenderTexture texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    name = name,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.Create();
                return texture;
            }

            public bool IsReady
            {
                get { return Prepass != null; }
            }

            /// <summary>
            /// Draws this layer's outline over <paramref name="sceneRT"/>. Every target and texture is
            /// set explicitly, so the result cannot depend on which target the previous layer left
            /// bound.
            /// </summary>
            public void Draw(CommandBuffer cmd, RenderTargetIdentifier sceneRT, float outlineStrength)
            {
                // Prepass: this layer's renderers only, into this layer's own target.
                cmd.SetRenderTarget(Prepass);
                cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 1));

                if (_prepassMaterial != null)
                {
                    DrawCache(cmd, _prepassMaterial);
                }

                cmd.SetRenderTarget(sceneRT);

                if (_compositeMaterial == null)
                {
                    return;
                }

                // Spool the scene colour off the swap chain first: the composite both samples the
                // scene and writes it, and a target cannot be read while it is being written.
                cmd.SetRenderTarget(Scene);
                cmd.Blit(sceneRT, Scene);

                // Composite draws the mask's outward band over the scene, reading both from this
                // layer's own material so neither can resolve to another layer's texture. The radius
                // is converted to a UV offset here, which keeps the shader free of any texel-size
                // uniform it would otherwise depend on Unity to populate.
                Vector2 offset = new Vector2(Radius / Mathf.Max(1, Prepass.width), Radius / Mathf.Max(1, Prepass.height));

                _compositeMaterial.SetTexture(_mainTexId, Scene);
                _compositeMaterial.SetTexture(_prepassId, Prepass);
                _compositeMaterial.SetColor(_outlineColorId, Color);
                _compositeMaterial.SetFloat(_outlineStrengthId, outlineStrength);
                _compositeMaterial.SetVector(_outlineOffsetId, offset);

                cmd.Blit(Scene, sceneRT, _compositeMaterial);
            }

            private void DrawCache(CommandBuffer cmd, Material prepassMaterial)
            {
                if (Cache == null)
                {
                    return;
                }

                IList<Renderer> renderers = Cache.Renderers;
                for (int i = 0; i < renderers.Count; ++i)
                {
                    Renderer renderer = renderers[i];
                    if (renderer != null && renderer.enabled && renderer.gameObject.activeSelf)
                    {
                        Material[] materials = renderer.sharedMaterials;

                        for (int j = 0; j < materials.Length; ++j)
                        {
                            if (materials[j] != null)
                            {
                                cmd.DrawRenderer(renderer, prepassMaterial, j);
                            }
                        }
                    }
                }
            }

            public void Release()
            {
                ReleaseTarget(ref Prepass);
                ReleaseTarget(ref Scene);
            }

            private static void ReleaseTarget(ref RenderTexture texture)
            {
                if (texture == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(texture);
                }
                else
                {
                    Object.DestroyImmediate(texture);
                }

                texture = null;
            }

            private static void ReleaseMaterial(ref Material material)
            {
                if (material == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(material);
                }
                else
                {
                    Object.DestroyImmediate(material);
                }

                material = null;
            }

            public void Dispose()
            {
                Release();

                ReleaseMaterial(ref _prepassMaterial);
                ReleaseMaterial(ref _compositeMaterial);
            }
        }

        class MERenderSelectionPass : ScriptableRenderPass
        {
            public RenderSelectionSettings Settings;

            private readonly List<OutlineLayer> _layers = new List<OutlineLayer>();

            /// <summary>How many of <see cref="_layers"/> the current frame is using.</summary>
            private int _layerCount;

            private RenderTargetIdentifier _cameraColorRT;

            /// <summary>Material templates from the feature settings; layers clone these.</summary>
            private Material _prepassMaterial;
            private Material _compositeMaterial;

            /// <summary>
            /// Starts a frame's layer set. Parameters are snapshotted rather than captured from the
            /// renderer cache, and the layers themselves are reused frame to frame so their render
            /// targets and materials are created once instead of churning.
            /// </summary>
            public void Setup(RenderTargetIdentifier camerColorRT, IRenderersCache selectionCache, Material prepassMaterial, Material compositeMaterial)
            {
                _cameraColorRT = camerColorRT;

                _prepassMaterial = prepassMaterial;
                _compositeMaterial = compositeMaterial;

                _layerCount = 0;
                AddLayer(selectionCache, Settings.OutlineColor, Settings.OutlineRadius);
            }

            public void AddLayer(IRenderersCache cache, Color color, float radius)
            {
                if (cache == null || cache.IsEmpty)
                {
                    return;
                }

                OutlineLayer layer;
                if (_layerCount < _layers.Count)
                {
                    layer = _layers[_layerCount];
                }
                else
                {
                    layer = new OutlineLayer(_prepassMaterial, _compositeMaterial);
                    _layers.Add(layer);
                }

                layer.Cache = cache;
                layer.Color = color;
                layer.Radius = radius;
                _layerCount++;
            }

            public bool HasLayers
            {
                get { return _layerCount > 0; }
            }

            public void DisposeLayers()
            {
                for (int i = 0; i < _layers.Count; ++i)
                {
                    _layers[i].Dispose();
                }

                _layers.Clear();
                _layerCount = 0;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor camDesc)
            {
                for (int i = 0; i < _layerCount; ++i)
                {
                    _layers[i].EnsureResources(camDesc.width, camDesc.height);
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("MERenderSelection");

                for (int i = 0; i < _layerCount; ++i)
                {
                    OutlineLayer layer = _layers[i];
                    if (layer.IsReady)
                    {
                        layer.Draw(cmd, _cameraColorRT, Settings.OutlineStength);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        private MERenderSelectionPass _scriptablePass;
        
        public override void Create()
        {
            _scriptablePass = new MERenderSelectionPass();
            _scriptablePass.Settings = _settings;
            _scriptablePass.renderPassEvent = _settings.RenderPassEvent;
        }

        protected override void Dispose(bool disposing)
        {
            _scriptablePass?.DisposeLayers();

            base.Dispose(disposing);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (OutlineManager.Instance == null) return;

            // The MapEditor scene can have more than one camera, and this pass instance carries a
            // single set of targets. Only the editor camera draws the outlines, so a second camera
            // cannot re-enqueue the pass with its own colour target on top of the first.
            if (MEBase.Instance == null || renderingData.cameraData.camera != MEBase.Instance.Camera) return;

            IRenderersCache renderersCache = OutlineManager.Instance._cache;
            IRenderersCache hoverCache = OutlineManager.Instance.HoverCache;

            // if(renderer!=null)Debug.Log(renderersCache.Renderers.Count);

            // Nothing selected and nothing hovered: skip the whole feature for the frame.
            if ((renderersCache == null || renderersCache.IsEmpty) && (hoverCache == null || hoverCache.IsEmpty))
            {
                return;
            }

            var src = renderer.cameraColorTarget;
            _scriptablePass.Setup(src, renderersCache, _settings.PrepassMaterial, _settings.CompositeMaterial);

            // The hover is a second layer on top of the selection, drawn with its own tint.
            if (hoverCache != null && !hoverCache.IsEmpty)
            {
                _scriptablePass.AddLayer(hoverCache, _settings.HoverColor, _settings.HoverRadius);
            }

            if (!_scriptablePass.HasLayers)
            {
                return;
            }

            renderer.EnqueuePass(_scriptablePass);
        }
    }



}