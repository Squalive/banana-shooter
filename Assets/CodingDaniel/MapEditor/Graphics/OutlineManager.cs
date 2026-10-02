using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.Interaction.Rendering;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using ExposeToEditor = CodingDaniel.MapEditor.MEEditor.ExposeToEditor;
using IRenderersCache = CodingDaniel.MapEditor.MECommon.IRenderersCache;
using Object = UnityEngine.Object;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Graphics
{
    public class OutlineManager : MonoBehaviour ,IOutlineManager
    {
        public static OutlineManager Instance { private set; get; }
        
        [FormerlySerializedAs("m_cache")]
        public IRenderersCache _cache;

        /// <summary>
        /// Renderers outlined because the pointer is over their object. Kept apart from
        /// <see cref="_cache"/> (the selection) so the two can be drawn with different tints, and
        /// added to the same renderer feature as an extra layer.
        /// </summary>
        public IRenderersCache HoverCache { get; private set; }

        [SerializeField]
        private RenderersCache _hoverCacheSource;

        private IME _editor;
        private IEnumerator _coUpdate;

        private readonly List<GameObject> _hoveredObjects = new List<GameObject>();
        private bool _hasHover;

        private IMESelection _selectionOverride;
        public IMESelection Selection
        {
            get
            {
                if (_selectionOverride != null)
                {
                    return _selectionOverride;
                }

                return _editor.Selection;
            }
            set
            {
                if (_selectionOverride != value)
                {
                    if (_selectionOverride != null)
                    {
                        _selectionOverride.SelectionChanged -= OnSelectionChanged;
                    }

                    _selectionOverride = value;
                    if (_selectionOverride == _editor.Selection)
                    {
                        _selectionOverride = null;
                    }

                    if (_selectionOverride != null)
                    {
                        _selectionOverride.SelectionChanged += OnSelectionChanged;
                    }
                }
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _cache = GetComponentInChildren<IRenderersCache>();

            if (_hoverCacheSource == null)
            {
                // A second cache for the same reason the selection has one: the renderer feature can
                // only read a list of renderers, so the hovered set is tracked separately.
                _hoverCacheSource = gameObject.AddComponent<RenderersCache>();
            }

            HoverCache = _hoverCacheSource;

            _editor = MEBase.Instance;

            ApplyOutlineColors();

            TryToAddRenderers(_cache, _editor.Selection);
            _editor.Selection.SelectionChanged += OnRuntimeEditorSelectionChanged;
            _editor.Object.Enabled += OnObjectEnabled;
            _editor.Object.Disabled += OnObjectDisabled;

        }

        /// <summary>
        /// Pushes the palette's outline tints into the renderer feature. Doing it here rather than in
        /// the handle component keeps one owner for the colours regardless of which component the
        /// renderer data happens to be wired onto.
        /// </summary>
        private void ApplyOutlineColors()
        {
            // Palette resolves its own theme (and caches it) on first read, so this is safe whatever
            // order the editor components happen to Awake in.
            GizmoPalette palette = MEBase.Instance != null && MEBase.Instance.Appearance != null
                ? MEBase.Instance.Appearance.Palette
                : null;

            if (palette == null)
            {
                // Nothing to apply: the tints already set on the renderer feature stand.
                return;
            }

            SetOutlineColors(palette.ObjectSelectionColor);
        }

        private static void SetOutlineColors(Color selectionColor)
        {
            MERenderSelection[] features;

            try
            {
                // The feature lives inside the renderer data asset, so it is not a scene object and
                // FindObjectsOfType would miss it. Unity's own lookup can refuse to touch objects that
                // are not available yet, hence the guard rather than an unhandled exception in Start.
                features = Resources.FindObjectsOfTypeAll<MERenderSelection>();
            }
            catch (Exception e)
            {
                Debug.LogWarning("MapEditor: could not apply the outline palette - " + e.Message);
                return;
            }

            for (int i = 0; i < features.Length; ++i)
            {
                MERenderSelection feature = features[i];
                if (feature == null || feature._settings == null)
                {
                    continue;
                }

                feature._settings.OutlineColor = selectionColor;
            }
        }

        /// <summary>
        /// Replaces the set of hovered objects. Passing null or an empty set clears the hover outline.
        /// Only exposed objects are outlined, matching what the selection can pick.
        /// </summary>
        public void SetHoveredObjects(IList<GameObject> gameObjects)
        {
            if (HoverCache == null)
            {
                return;
            }

            if (gameObjects == null || gameObjects.Count == 0)
            {
                ClearHoveredObjects();
                return;
            }

            // Diff against the previous set instead of rebuilding it: the cache is only touched when
            // the pointer actually moves onto a different object, so a stationary pointer over a big
            // decoration costs nothing per frame.
            List<GameObject> added = new List<GameObject>(gameObjects.Count);
            for (int i = 0; i < gameObjects.Count; ++i)
            {
                GameObject go = gameObjects[i];
                if (go != null && !_hoveredObjects.Contains(go))
                {
                    _hoveredObjects.Add(go);
                    added.Add(go);
                }
            }

            List<GameObject> removed = null;
            if (_hasHover && _hoveredObjects.Count > gameObjects.Count)
            {
                removed = new List<GameObject>();

                for (int i = _hoveredObjects.Count - 1; i >= 0; --i)
                {
                    GameObject go = _hoveredObjects[i];
                    if (go == null || !Contains(gameObjects, go))
                    {
                        removed.Add(go);
                        _hoveredObjects.RemoveAt(i);
                    }
                }
            }

            if (removed != null && removed.Count > 0)
            {
                TryToRemoveRenderers(HoverCache, removed.ToArray());
            }

            if (added.Count > 0)
            {
                TryToAddRenderers(HoverCache, added);
            }

            _hasHover = _hoveredObjects.Count > 0;
        }

        private static bool Contains(IList<GameObject> gameObjects, GameObject go)
        {
            for (int i = 0; i < gameObjects.Count; ++i)
            {
                if (gameObjects[i] == go)
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearHoveredObjects()
        {
            if (!_hasHover && _hoveredObjects.Count == 0)
            {
                return;
            }

            GameObject[] previous = _hoveredObjects.ToArray();
            _hoveredObjects.Clear();
            _hasHover = false;

            if (HoverCache != null)
            {
                TryToRemoveRenderers(HoverCache, previous);
            }
        }

        public bool HasHover
        {
            get { return _hasHover; }
        }

        private void OnDisable()
        {
            if(_editor != null)
            {
                if (_selectionOverride != null)
                {
                    OnSelectionChanged(_selectionOverride.Objects);
                }
                else
                {
                    OnRuntimeEditorSelectionChanged(_editor.Selection.Objects);
                }
            }
        }

        private void OnDestroy()
        {
            if (_editor != null)
            {
                if(_editor.Selection != null)
                {
                    _editor.Selection.SelectionChanged -= OnRuntimeEditorSelectionChanged;
                }

                if(_editor.Object != null)
                {
                    _editor.Object.Enabled -= OnObjectEnabled;
                    _editor.Object.Disabled -= OnObjectDisabled;
                }
            }

            if (_selectionOverride != null)
            {
                _selectionOverride.SelectionChanged -= OnSelectionChanged;
            }

            if (_coUpdate != null)
            {
                StopCoroutine(_coUpdate);
                _coUpdate = null;
            }
          
        }

        private void OnObjectEnabled(ExposeToEditor obj)
        {
            if(_coUpdate == null)
            {
                _coUpdate = CoUpdate();
                StartCoroutine(_coUpdate);
            }
        }

        private void OnObjectDisabled(ExposeToEditor obj)
        {
            if (_coUpdate == null && isActiveAndEnabled)
            {
                _coUpdate = CoUpdate();
                StartCoroutine(_coUpdate);
            }
        }

        private IEnumerator CoUpdate()
        {
            yield return null;

            if (_selectionOverride != null)
            {
                OnSelectionChanged(_selectionOverride.Objects);
            }
            else
            {
                OnRuntimeEditorSelectionChanged(_editor.Selection.Objects);
            }

            _coUpdate = null;
        }

        private void OnRuntimeEditorSelectionChanged(Object[] unselectedObject)
        {
            OnSelectionChanged(_editor.Selection, unselectedObject);
        }

        private void OnSelectionChanged(Object[] unselectedObjects)
        {
            OnSelectionChanged(_selectionOverride, unselectedObjects);
        }

        private void OnSelectionChanged(IMESelection selection, Object[] unselectedObjects)
        {
            TryToRemoveRenderers(_cache, unselectedObjects);
            TryToAddRenderers(_cache, selection);
        }

        private void TryToRemoveRenderers(IRenderersCache cache, Object[] unselectedObjects)
        {
            if (cache != null && unselectedObjects != null)
            {
                Renderer[] renderers = unselectedObjects.Select(go => go as GameObject).Where(go => go != null).SelectMany(go => go.GetComponentsInChildren<Renderer>(true)).ToArray();
                for (int i = 0; i < renderers.Length; ++i)
                {
                    Renderer renderer = renderers[i];
                    cache.Remove(renderer);
                }
            }
        }

        private void TryToAddRenderers(IRenderersCache cache, IMESelection selection)
        {
            if (cache != null && selection.GameObjects != null)
            {
                TryToAddRenderers(cache, selection.GameObjects);
            }
        }

        private void TryToAddRenderers(IRenderersCache cache, IList<GameObject> gameObjects)
        {
            if (cache == null || gameObjects == null)
            {
                return;
            }

            IList<Renderer> renderers = GetRenderers(gameObjects);
            for (int i = 0; i < renderers.Count; ++i)
            {
                Renderer renderer = renderers[i];
                cache.Add(renderer);
                // Debug.Log($"add {renderer.name}");
            }
        }

        private IList<GameObject> FilterSelection(IList<GameObject> gameObjects)
        {
            IList<GameObject> result = new List<GameObject>();

            for (int i = 0; i < gameObjects.Count; ++i)
            {
                GameObject go = gameObjects[i];
                if (go == null || go.IsPrefab() || (go.hideFlags & HideFlags.HideInHierarchy) != 0)
                {
                    continue;
                }

                ExposeToEditor exposed = go.GetComponent<ExposeToEditor>();
                if (exposed != null)
                {
                    result.Add(go);
                }
            }
            return result;
        }

        private IList<Renderer> GetRenderers(IList<GameObject> gameObjects)
        {
            List<Renderer> result = new List<Renderer>();

            gameObjects = FilterSelection(gameObjects);

            for (int i = 0; i < gameObjects.Count; ++i)
            {
                GameObject go = gameObjects[i];

                // includeInactive: the mirrored removal in TryToRemoveRenderers also uses true, so
                // both sides see the same renderer set and nothing can be left behind in the cache.
                foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                {
                    if ((renderer.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0)
                    {
                        result.Add(renderer);
                    }
                }
            }

            return result;
        }

        public void AddRenderers(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; ++i)
            {
                Renderer renderer = renderers[i];
                _cache.Add(renderer);
            }
        }

        public void RemoveRenderers(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; ++i)
            {
                Renderer renderer = renderers[i];
                _cache.Remove(renderer);
            }
        }

        public void RecreateCommandBuffer()
        {
        
        }

        public bool ContainsRenderer(Renderer renderer)
        {
            return _cache.Renderers.Contains(renderer);
        }
    }
}