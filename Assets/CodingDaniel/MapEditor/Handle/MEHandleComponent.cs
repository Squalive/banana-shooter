using System;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.Interaction.Picking;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.Interaction.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using CodingDaniel.MapEditor.Interaction;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Handle
{
    [DefaultExecutionOrder(-90)]
    public class MEHandleComponent : MonoBehaviour, IHandleAppearance
    {


        [SerializeField] private ScriptableRendererData rendererData;

        private int _colorIndex = 0;
        
        [SerializeField]
        [FormerlySerializedAs("m_colors")]
        private GizmoPalette[] _colors = new GizmoPalette[1];
        public GizmoPalette Colors => _colors[_colorIndex];

        /// <summary>
        /// The palette the active theme resolves to. Cached on first read (and again in
        /// <see cref="Initialize"/>) so callers such as the outline renderer can read the colours
        /// without knowing how the theme array is indexed.
        /// </summary>
        private GizmoPalette _palette;
        public GizmoPalette Palette
        {
            get
            {
                if (_palette == null)
                {
                    _palette = Colors;
                }

                return _palette;
            }
        }

        [SerializeField]
        [FormerlySerializedAs("m_handleScale")]
        private float _handleScale = 1.0f;
        public float HandleScale
        {
            get { return _handleScale; }
            set { _handleScale = value; }
        }
        
        [SerializeField]
        [FormerlySerializedAs("m_selectionMargin")]
        private float _selectionMargin = 1;
        public float SelectionMargin
        {
            get { return _selectionMargin * _handleScale; }
            set { _selectionMargin = value; }
        }
        [SerializeField]
        [FormerlySerializedAs("m_selectionMarginPixels")]
        public float _selectionMarginPixels = 10;
        public float SelectionMarginPixels
        {
            get { return _selectionMarginPixels; }
            set { _selectionMarginPixels = value; }
        }

        [SerializeField]
        [FormerlySerializedAs("m_invertZAxis")]
        private bool _invertZAxis = false;
        public bool InvertZAxis
        {
            get { return _invertZAxis; }
            set { _invertZAxis = value; }
        }

        [SerializeField]
        [FormerlySerializedAs("m_positionHandleArrowOnly")]
        private bool _positionHandleArrowOnly = false;
        public bool PositionHandleArrowOnly
        {
            get { return _positionHandleArrowOnly; }
            set { _positionHandleArrowOnly = value; }
        }
        private float _oldHandleScale;
        private bool _oldInvertZAxis;
        private void Awake()
        {

            _oldHandleScale = _handleScale;
            _oldInvertZAxis = _invertZAxis;
            _palette = Colors;
            Initialize();
        }
        void Update()
        {
            if(_oldHandleScale != _handleScale || _oldInvertZAxis != _invertZAxis)
            {
                _oldHandleScale = _handleScale;
                _oldInvertZAxis = _invertZAxis;
                ApplySettings();
            }
        }
        void ApplySettings()
        {
            Cleanup();
            Initialize();
        }
        private GizmoMaterialSet _materials;
        private GizmoGeometryLibrary _geometry;
        private TransformHandleRenderer _renderer;
        private HandleHitTester _hitTester;

        /// <summary>
        /// Themes, geometry and behaviour are separate types now; the component owns them and forwards.
        /// Drawing and picking live in TransformHandleRenderer and HandleHitTester.
        /// </summary>
        private void Initialize()
        {
            if (rendererData != null)
            {
                foreach (ScriptableRendererFeature rendererFeature in rendererData.rendererFeatures)
                {
                    if (rendererFeature is MERenderSelection selection)
                    {
                        selection._settings.OutlineColor = Colors.ObjectSelectionColor;
                    }
                }
            }

            _materials = new GizmoMaterialSet(_handleScale);
            _geometry = new GizmoGeometryLibrary(Colors, _handleScale, _invertZAxis);
            _renderer = new TransformHandleRenderer(this, _geometry, _materials);
            _hitTester = new HandleHitTester(this);
        }

        private void Cleanup()
        {
            _renderer = null;
            _hitTester = null;

            if (_geometry != null)
            {
                _geometry.Dispose();
                _geometry = null;
            }

            if (_materials != null)
            {
                _materials.Dispose();
                _materials = null;
            }
        }

        // ----- IHandleAppearance ------------------------------------------------------------------
        // Explicit so the rendering/picking types read appearance through the interface while the
        // component keeps the public properties the rest of the editor already uses.

        GizmoPalette IHandleAppearance.Palette => Colors;
        float IHandleAppearance.HandleScale => _handleScale;
        float IHandleAppearance.SelectionMargin => SelectionMargin;
        float IHandleAppearance.SelectionMarginPixels => _selectionMarginPixels;
        bool IHandleAppearance.InvertZAxis => _invertZAxis;
        Transform IHandleAppearance.HostTransform => transform;
        bool IHandleAppearance.PositionHandleArrowOnly => _positionHandleArrowOnly;
        Vector3 IHandleAppearance.Forward => Forward;

        /// <summary>Handle-space forward, flipped when the Z axis is inverted.</summary>
        public Vector3 Forward
        {
            get { return _invertZAxis ? Vector3.back : Vector3.forward; }
        }
        // ----- forwarding API (unchanged signatures for existing callers) --------------------------

        public void DoPositionHandle(CommandBuffer commandBuffer, Camera camera, HandleDrawSettings settings, bool snapMode = false)
        {
            if (_renderer != null)
            {
                _renderer.DoPositionHandle(commandBuffer, camera, settings, snapMode);
            }
        }

        public void DoRotationHandle(CommandBuffer commandBuffer, Camera camera, HandleDrawSettings settings, bool cameraFacingBillboardMode = true)
        {
            if (_renderer != null)
            {
                _renderer.DoRotationHandle(commandBuffer, camera, settings, cameraFacingBillboardMode);
            }
        }

        public void DoScaleHandle(CommandBuffer commandBuffer, Camera camera, HandleDrawSettings settings)
        {
            if (_renderer != null)
            {
                _renderer.DoScaleHandle(commandBuffer, camera, settings);
            }
        }

        public HandleAxis HitTestPositionHandle(Camera camera, Ray ray, HandleDrawSettings settings, out float distance)
        {
            if (_hitTester == null)
            {
                distance = float.PositiveInfinity;
                return HandleAxis.None;
            }

            return _hitTester.HitTestPositionHandle(camera, ray, settings, out distance);
        }

        public HandleAxis HitTestRotationHandle(Camera camera, Ray ray, HandleDrawSettings settings, out float distance)
        {
            if (_hitTester == null)
            {
                distance = float.PositiveInfinity;
                return HandleAxis.None;
            }

            return _hitTester.HitTestRotationHandle(camera, ray, settings, out distance);
        }

        public HandleAxis HitTestScaleHandle(Camera camera, Ray ray, HandleDrawSettings settings, out float distance)
        {
            if (_hitTester == null)
            {
                distance = float.PositiveInfinity;
                return HandleAxis.None;
            }

            return _hitTester.HitTestScaleHandle(camera, ray, settings, out distance);
        }

        public static float GetScreenScale(Vector3 position, Camera camera)
        {
            return GraphicsUtility.GetScreenScale(position, camera);
        }
    }
}