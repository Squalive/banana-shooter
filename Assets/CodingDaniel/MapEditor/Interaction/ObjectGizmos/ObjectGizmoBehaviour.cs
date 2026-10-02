using System.Collections.Generic;
using System.Reflection;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.UI.Component;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.Rendering;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Interaction.ObjectGizmos
{
    /// <summary>
    /// The single interpreter for every object gizmo.
    ///
    /// Replaces BaseGizmo and its whole subclass tree (SphereGizmo, ConeGizmo, BoxGizmo,
    /// PointLightGizmo, SpotlightGizmo, DirectionalLightGizmo, AudioSourceGizmo, DecalGizmo),
    /// the LightGizmo dispatcher and the BaseGizmoInput companion component. What each gizmo is and
    /// does now comes from an <see cref="ObjectGizmoDefinition"/>.
    ///
    /// Execution order is -55, which is where the old LightGizmo/AudioSourceGizmo ran and is later
    /// than BaseHandleInput(-60) - so when a transform handle and a gizmo are both under the pointer,
    /// the handle still wins the Tools.ActiveTool slot exactly as before.
    /// </summary>
    [DefaultExecutionOrder(-55)]
    public class ObjectGizmoBehaviour : MonoBehaviour
    {
        [FormerlySerializedAs("m_definitionId")]
        [SerializeField] private string _definitionId;
        [FormerlySerializedAs("m_enableUndo")]
        [SerializeField] private bool _enableUndo = true;
        [FormerlySerializedAs("m_selectionMargin")]
        [SerializeField] private float _selectionMargin = 10f;
        [FormerlySerializedAs("m_gridSize")]
        [SerializeField] private float _gridSize = 1f;
        [FormerlySerializedAs("m_unitSnapKey")]
        [SerializeField] private KeyCode _unitSnapKey = KeyCode.LeftControl;

        /// <summary>The transform the gizmo draws around. Defaults to the owning component's transform.</summary>
        public Transform Target;

        private Component _component;
        private ObjectGizmoDefinition _definition;
        private IME _editor;
        private IMECamera _meCamera;

        private MaterialPropertyBlock _lineProperties;
        private MaterialPropertyBlock _handleProperties;
        private MaterialPropertyBlock _selectionProperties;

        private Vector3[] _handlePositions;
        private Vector3[] _handleNormals;
        private Vector3[] _coneHandlePositions;
        private Vector3[] _coneHandleNormals;

        private bool _isDragging;
        private bool _endDragPending;
        private int _dragIndex;
        private Plane _dragPlane;
        private Vector3 _prevPoint;
        private Vector3 _normal;
        private Matrix4x4 _handlesTransform;
        private Matrix4x4 _handlesInverseTransform;

        private Vector3 _prevPosition;
        private Quaternion _prevRotation;
        private Vector3 _prevScale;

        private Vector3 _prevCamPosition;
        private Quaternion _prevCamRotation;
        private bool _prevCamOrthographic;

        // ----- public surface (what ComponentMenu and the panels use) ----------------------------

        public IMECamera MECamera
        {
            get { return _meCamera; }
        }

        public bool IsDragging
        {
            get { return _isDragging; }
        }

        public string DefinitionId
        {
            get { return _definition != null ? _definition.Id : _definitionId; }
        }

        /// <summary>
        /// Ensures the GameObject owning <paramref name="component"/> has the gizmo for it, and returns it.
        /// Returns null when no definition applies. Replaces the per-type
        /// GetComponent-or-AddComponent blocks in ComponentMenu and the LightGizmo dispatcher.
        /// </summary>
        public static ObjectGizmoBehaviour Attach(Component component)
        {
            if (component == null)
            {
                return null;
            }

            ObjectGizmoDefinition definition = ObjectGizmoRegistry.Resolve(component);
            if (definition == null)
            {
                return null;
            }

            ObjectGizmoBehaviour behaviour = component.gameObject.GetComponent<ObjectGizmoBehaviour>();
            if (behaviour == null)
            {
                behaviour = component.gameObject.AddComponent<ObjectGizmoBehaviour>();
            }

            behaviour.Bind(component, definition);
            return behaviour;
        }

        /// <summary>Rebuilds the camera command buffer so panel edits show up immediately.</summary>
        public void Refresh()
        {
            if (_meCamera != null)
            {
                _meCamera.RefreshCommandBuffer();
            }
        }

        /// <summary>Kept so existing panel code keeps working; colours come from the definition now.</summary>
        public void ResetObject()
        {
            ApplyColors();
            Refresh();
        }

        // ----- lifetime ---------------------------------------------------------------------------

        private void Awake()
        {
            _lineProperties = new MaterialPropertyBlock();
            _handleProperties = new MaterialPropertyBlock();
            _selectionProperties = new MaterialPropertyBlock();

            _handlePositions = GizmoUtility.GetHandlesPositions();
            _handleNormals = GizmoUtility.GetHandlesNormals();
            _coneHandlePositions = GizmoUtility.GetConeHandlesPositions();
            _coneHandleNormals = GizmoUtility.GetConeHandlesNormals();

            _editor = MEBase.Instance;

            if (_definition == null)
            {
                ObjectGizmoDefinition definition;
                Component component = ObjectGizmoRegistry.FindComponent(gameObject, out definition);
                if (component == null || definition == null)
                {
                    Destroy(this);
                    return;
                }

                Bind(component, definition);
            }
        }

        private void OnEnable()
        {
            SubscribeCamera();
            SubscribeUndo(true);
        }

        private void OnDisable()
        {
            SubscribeCamera(false);
            SubscribeUndo(false);
            EndDrag();
        }

        private void OnDestroy()
        {
            if (_editor != null && _editor.Tools != null && ReferenceEquals(_editor.Tools.ActiveTool, this))
            {
                _editor.Tools.ActiveTool = null;
            }

            if (_meCamera != null)
            {
                _meCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
            }

            SubscribeUndo(false);
        }

        // ----- binding ----------------------------------------------------------------------------

        private void Bind(Component component, ObjectGizmoDefinition definition)
        {
            if (component == null || definition == null)
            {
                return;
            }

            if (_isDragging)
            {
                EndDrag();
            }

            _component = component;
            _definition = definition;
            _definitionId = definition.Id;
            Target = component.transform;

            ApplyColors();
            EnsureCamera();
            Refresh();
        }

        private void ApplyColors()
        {
            if (_definition == null || _lineProperties == null)
            {
                return;
            }

            _lineProperties.SetColor("_Color", _definition.Colors.Line);
            _handleProperties.SetColor("_Color", _definition.Colors.Handle);
            _selectionProperties.SetColor("_Color", _definition.Colors.Selection);
        }

        private void EnsureCamera()
        {
            Camera sceneCamera = SceneCamera;
            if (sceneCamera == null)
            {
                return;
            }

            if (_meCamera == null)
            {
                IMEGraphic graphics = _editor.Graphics;
                if (graphics != null)
                {
                    _meCamera = graphics.GetOrCreateCamera(sceneCamera, CameraEvent.AfterImageEffectsOpaque);
                }

                if (_meCamera == null)
                {
                    _meCamera = sceneCamera.gameObject.AddComponent<MECamera>();
                    _meCamera.Event = CameraEvent.AfterImageEffectsOpaque;
                }
            }

            if (_meCamera != null)
            {
                _prevPosition = transform.position;
                _prevRotation = transform.rotation;
                _prevScale = transform.localScale;
                SnapshotCamera();
                SubscribeCamera();
            }
        }

        private void SubscribeCamera()
        {
            if (_meCamera == null)
            {
                return;
            }

            _meCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
            _meCamera.CommandBufferRefresh += OnCommandBufferRefresh;
        }

        private void SubscribeCamera(bool subscribe)
        {
            if (_meCamera == null)
            {
                return;
            }

            _meCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
            if (subscribe)
            {
                _meCamera.CommandBufferRefresh += OnCommandBufferRefresh;
            }
        }

        private void SubscribeUndo(bool subscribe)
        {
            if (_editor == null || _editor.Undo == null)
            {
                return;
            }

            _editor.Undo.StateChanged -= OnUndoStateChanged;
            _editor.Undo.UndoCompleted -= OnUndoStateChanged;
            _editor.Undo.RedoCompleted -= OnUndoStateChanged;

            if (subscribe)
            {
                _editor.Undo.StateChanged += OnUndoStateChanged;
                _editor.Undo.UndoCompleted += OnUndoStateChanged;
                _editor.Undo.RedoCompleted += OnUndoStateChanged;
            }
        }

        private void OnUndoStateChanged()
        {
            Refresh();
        }

        private void SnapshotCamera()
        {
            if (_meCamera == null || _meCamera.Camera == null)
            {
                return;
            }

            Transform cameraTransform = _meCamera.Camera.transform;
            _prevCamPosition = cameraTransform.position;
            _prevCamRotation = cameraTransform.rotation;
            _prevCamOrthographic = _meCamera.Camera.orthographic;
        }

        // ----- per-frame --------------------------------------------------------------------------

        private void Update()
        {
            if (_definition == null || _component == null)
            {
                return;
            }

            // A component that changes shape (Light.type) gets re-resolved, which is what the old
            // LightGizmo dispatcher did in its own Update.
            RefreshDefinitionIfComponentChanged();

            if (_endDragPending)
            {
                _endDragPending = false;
                EndDrag();
            }

            HandlePointerInput();

            if (_isDragging)
            {
                ApplyDrag();
            }
            else if (_meCamera != null &&
                     (_prevPosition != transform.position ||
                      _prevRotation != transform.rotation ||
                      _prevScale != transform.localScale))
            {
                _prevPosition = transform.position;
                _prevRotation = transform.rotation;
                _prevScale = transform.localScale;
                Refresh();
            }
        }

        private void LateUpdate()
        {
            if (SceneCamera == null)
            {
                Destroy(this);
                return;
            }

            if (_isDragging || _definition == null || !_definition.RefreshOnCameraChanged)
            {
                return;
            }

            if (_meCamera == null || _meCamera.Camera == null)
            {
                return;
            }

            Camera camera = _meCamera.Camera;
            Transform cameraTransform = camera.transform;
            if (_prevCamPosition != cameraTransform.position ||
                _prevCamRotation != cameraTransform.rotation ||
                _prevCamOrthographic != camera.orthographic)
            {
                SnapshotCamera();
                Refresh();
            }
        }

        private void RefreshDefinitionIfComponentChanged()
        {
            if (!(_component is Light))
            {
                return;
            }

            ObjectGizmoDefinition resolved = ObjectGizmoRegistry.Resolve(_component);
            if (resolved != null && !ReferenceEquals(resolved, _definition))
            {
                Bind(_component, resolved);
            }
        }

        // ----- input ------------------------------------------------------------------------------

        private void HandlePointerInput()
        {
            // Preserves BaseGizmoInput's sequencing: a mouse-up ends the drag at the start of the
            // following frame, so the last frame with the button down still applies the drag.
            if (Input.GetMouseButtonDown(0))
            {
                BeginDrag();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _endDragPending = true;
            }
        }

        private void BeginDrag()
        {
            if (_definition == null || !_definition.AllowDrag)
            {
                return;
            }

            Camera sceneCamera = SceneCamera;
            if (sceneCamera == null || Target == null || _editor == null || _editor.Tools == null)
            {
                return;
            }

            if (_editor.Tools.IsViewing || _editor.Tools.ActiveTool != null)
            {
                return;
            }

            if (_editor.Pointer == null)
            {
                return;
            }

            Vector2 pointer = _editor.Pointer.ScreenPoint;
            _dragIndex = Hit(pointer, HandlePositions, HandleNormals);
            if (_dragIndex < 0)
            {
                return;
            }

            _handlesTransform = HandlesTransform;
            _handlesInverseTransform = HandlesTransformInverse;
            _dragPlane = GetDragPlane();
            if (!GetPointOnDragPlane(pointer, out _prevPoint))
            {
                return;
            }

            _normal = HandleNormals[_dragIndex].normalized;
            _isDragging = true;
            _editor.Tools.ActiveTool = this;

            if (_enableUndo)
            {
                BeginRecord();
            }
        }

        private void EndDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;

            bool wasRecording = _editor != null && _editor.Undo != null && _editor.Undo.IsRecording;
            if (_enableUndo && _editor != null && _editor.Undo != null)
            {
                if (!wasRecording)
                {
                    _editor.Undo.BeginRecord();
                }

                EndRecord();

                if (!wasRecording)
                {
                    _editor.Undo.EndRecord();
                }
            }

            if (_editor != null && _editor.Tools != null && ReferenceEquals(_editor.Tools.ActiveTool, this))
            {
                _editor.Tools.ActiveTool = null;
            }

            // The old OnDrop refreshed every other live gizmo so sibling visuals followed the change.
            foreach (ObjectGizmoBehaviour other in FindObjectsOfType<ObjectGizmoBehaviour>())
            {
                if (!ReferenceEquals(other, this))
                {
                    other.Refresh();
                }
            }

            Refresh();
        }

        private void ApplyDrag()
        {
            if (_editor == null || _editor.Pointer == null)
            {
                return;
            }

            Vector3 point;
            if (GetPointOnDragPlane(_editor.Pointer.ScreenPoint, out point))
            {
                Vector3 offset = _handlesInverseTransform.MultiplyVector(point - _prevPoint);
                offset = Vector3.Project(offset, _normal);

                if (Input.GetKey(_unitSnapKey) || (_editor.Tools != null && _editor.Tools.UnitSnapping))
                {
                    Vector3 gridOffset = Vector3.zero;
                    if (Mathf.Abs(offset.x * 1.5f) >= _gridSize)
                    {
                        gridOffset.x = _gridSize * Mathf.Sign(offset.x);
                    }

                    if (Mathf.Abs(offset.y * 1.5f) >= _gridSize)
                    {
                        gridOffset.y = _gridSize * Mathf.Sign(offset.y);
                    }

                    if (Mathf.Abs(offset.z * 1.5f) >= _gridSize)
                    {
                        gridOffset.z = _gridSize * Mathf.Sign(offset.z);
                    }

                    if (gridOffset != Vector3.zero && ApplyOffset(_dragIndex, gridOffset))
                    {
                        _prevPoint = point;
                        Refresh();
                    }
                }
                else if (ApplyOffset(_dragIndex, offset))
                {
                    _prevPoint = point;
                    Refresh();
                }
            }
        }

        /// <summary>Applies one drag step. Returns false when the gesture should not advance.</summary>
        private bool ApplyOffset(int index, Vector3 offset)
        {
            switch (_definition.Geometry)
            {
                case ObjectGizmoGeometry.Sphere:
                    {
                        // Math.Sign (not Mathf.Sign) matches the original: a zero dot product means no change.
                        int sign = System.Math.Sign(Vector3.Dot(offset, HandleNormals[index]));
                        float radius = Radius + offset.magnitude * sign;
                        if (radius < 0f)
                        {
                            WritePrimary(0f);
                            return false;
                        }

                        WritePrimary(radius);
                        return true;
                    }

                case ObjectGizmoGeometry.Cone:
                    {
                        int sign = System.Math.Sign(Vector3.Dot(offset.normalized, HandleNormals[index]));
                        if (index == 0)
                        {
                            WritePrimary(Height + offset.magnitude * sign);
                        }
                        else
                        {
                            WriteSecondary(Radius + offset.magnitude * sign);
                        }

                        return true;
                    }

                default:
                    return false;
            }
        }

        private void WritePrimary(float value)
        {
            if (_definition.WritePrimary != null)
            {
                _definition.WritePrimary(_component, value);
            }

            PushValuesToUi();
        }

        private void WriteSecondary(float value)
        {
            if (_definition.WriteSecondary != null)
            {
                _definition.WriteSecondary(_component, value);
            }

            PushValuesToUi();
        }

        private void PushValuesToUi()
        {
            ComponentMenu menu = ComponentMenu.Instance;
            if (menu == null || _component == null || _definition == null || _definition.UiBindings.Length == 0)
            {
                return;
            }

            Dictionary<string, BaseProperty> properties;
            if (!menu.properties.TryGetValue(_component, out properties) || properties == null)
            {
                return;
            }

            for (int i = 0; i < _definition.UiBindings.Length; ++i)
            {
                ObjectGizmoDefinition.UiBinding binding = _definition.UiBindings[i];
                BaseProperty property;
                if (binding != null && properties.TryGetValue(binding.Key, out property) && property != null)
                {
                    property.UpdateValue(binding.Current(_component));
                }
            }
        }

        // ----- undo ------------------------------------------------------------------------------

        private void BeginRecord()
        {
            if (_editor == null || _editor.Undo == null || _component == null || _definition == null)
            {
                return;
            }

            MemberInfo[] members = _definition.UndoMembers(_component);
            if (members == null)
            {
                return;
            }

            for (int i = 0; i < members.Length; ++i)
            {
                if (members[i] != null)
                {
                    _editor.Undo.BeginRecordValue(_component, members[i]);
                }
            }
        }

        private void EndRecord()
        {
            if (_editor == null || _editor.Undo == null || _component == null || _definition == null)
            {
                return;
            }

            MemberInfo[] members = _definition.UndoMembers(_component);
            if (members == null)
            {
                return;
            }

            for (int i = 0; i < members.Length; ++i)
            {
                if (members[i] != null)
                {
                    _editor.Undo.EndRecordValue(_component, members[i]);
                }
            }
        }

        // ----- geometry --------------------------------------------------------------------------

        private Camera SceneCamera
        {
            get { return _editor != null ? _editor.Camera : null; }
        }

        private Vector3 LocalCenter
        {
            get { return _definition != null ? _definition.LocalCenter(_component) : Vector3.zero; }
        }

        private float Radius
        {
            get { return _definition != null && _definition.Radius != null ? _definition.Radius(_component) : 0f; }
        }

        private float Height
        {
            get { return _definition != null && _definition.Height != null ? _definition.Height(_component) : 0f; }
        }

        private Bounds LocalBounds
        {
            get
            {
                return _definition != null && _definition.LocalBounds != null
                    ? _definition.LocalBounds(_component)
                    : new Bounds(Vector3.zero, Vector3.one);
            }
        }

        private Vector3 FlattenScale
        {
            get
            {
                Vector3 scale = Target != null ? Target.lossyScale : Vector3.one;
                return Vector3.one * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            }
        }

        private Vector3 ConeScale
        {
            get
            {
                float radius = Mathf.Max(Mathf.Abs(Radius), 0.001f);
                return new Vector3(radius, radius, 1f);
            }
        }

        private bool IsCone
        {
            get { return _definition != null && _definition.Geometry == ObjectGizmoGeometry.Cone; }
        }

        private Vector3[] HandlePositions
        {
            get { return IsCone ? _coneHandlePositions : _handlePositions; }
        }

        private Vector3[] HandleNormals
        {
            get { return IsCone ? _coneHandleNormals : _handleNormals; }
        }

        private Matrix4x4 HandlesTransform
        {
            get
            {
                if (_definition == null || Target == null)
                {
                    return Matrix4x4.identity;
                }

                switch (_definition.Geometry)
                {
                    case ObjectGizmoGeometry.Sphere:
                        return Matrix4x4.TRS(Target.TransformPoint(LocalCenter), Target.rotation, FlattenScale * Radius);

                    case ObjectGizmoGeometry.Cone:
                        return Matrix4x4.TRS(Target.TransformPoint(Vector3.forward * Height), Target.rotation, ConeScale);

                    case ObjectGizmoGeometry.Box:
                        {
                            Bounds bounds = LocalBounds;
                            return Matrix4x4.TRS(Target.TransformPoint(bounds.center), Target.rotation,
                                Vector3.Scale(bounds.extents, Target.lossyScale));
                        }

                    case ObjectGizmoGeometry.DirectionalLight:
                        return Target.localToWorldMatrix;

                    default:
                        return Matrix4x4.identity;
                }
            }
        }

        private Matrix4x4 HandlesTransformInverse
        {
            get
            {
                if (Target == null)
                {
                    return Matrix4x4.identity;
                }

                if (_definition != null && _definition.Geometry == ObjectGizmoGeometry.Sphere)
                {
                    return Matrix4x4.TRS(Target.position, Target.rotation, FlattenScale).inverse;
                }

                return Matrix4x4.TRS(Target.position, Target.rotation, Target.lossyScale).inverse;
            }
        }

        // ----- picking ---------------------------------------------------------------------------

        private int Hit(Vector2 pointer, Vector3[] vertices, Vector3[] normals)
        {
            Camera sceneCamera = SceneCamera;
            if (sceneCamera == null || vertices == null || normals == null)
            {
                return -1;
            }

            float minMagnitude = float.MaxValue;
            int index = -1;

            for (int i = 0; i < vertices.Length; ++i)
            {
                Vector3 normal = HandlesTransform.MultiplyVector(normals[i]);
                Vector3 vertexWorld = HandlesTransform.MultiplyPoint(vertices[i]);

                // Skip handles pointing away from the camera.
                if (Mathf.Abs(Vector3.Dot((sceneCamera.transform.position - vertexWorld).normalized, normal.normalized)) > 0.999f)
                {
                    continue;
                }

                if (!HitOverride(i))
                {
                    continue;
                }

                Vector2 vertexScreen = sceneCamera.WorldToScreenPoint(vertexWorld);
                float distance = (vertexScreen - pointer).magnitude;
                if (distance < minMagnitude && distance <= _selectionMargin * GizmoUtility.HandleScale)
                {
                    minMagnitude = distance;
                    index = i;
                }
            }

            return index;
        }

        private bool HitOverride(int index)
        {
            if (IsCone && index == 0 && Mathf.Abs(Radius) < 0.0001f)
            {
                return false;
            }

            return true;
        }

        private Plane GetDragPlane()
        {
            Vector3 toCamera = SceneCamera.transform.position - _handlesTransform.MultiplyPoint(HandlePositions[_dragIndex]);
            Vector3 position = _handlesTransform.MultiplyPoint(Vector3.zero);
            return new Plane(toCamera.normalized, position);
        }

        private bool GetPointOnDragPlane(Vector3 screenPosition, out Vector3 point)
        {
            Camera sceneCamera = SceneCamera;
            if (sceneCamera == null)
            {
                point = Vector3.zero;
                return false;
            }

            Ray ray = sceneCamera.ScreenPointToRay(screenPosition);
            float distance;
            if (_dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        // ----- drawing ---------------------------------------------------------------------------

        private void OnCommandBufferRefresh(IMECamera camera)
        {
            if (camera == null || camera.CommandBuffer == null || _definition == null || _component == null || Target == null)
            {
                return;
            }

            switch (_definition.Geometry)
            {
                case ObjectGizmoGeometry.Sphere:
                    DrawSphere(camera);
                    break;

                case ObjectGizmoGeometry.Cone:
                    DrawCone(camera);
                    break;

                case ObjectGizmoGeometry.DirectionalLight:
                    GizmoUtility.DrawDirectionalLight(camera.CommandBuffer, camera.Camera, Target.position,
                        Target.rotation, Vector3.one, _lineProperties);
                    break;

                case ObjectGizmoGeometry.Box:
                    {
                        Bounds bounds = LocalBounds;
                        GizmoUtility.DrawWireCube(camera.CommandBuffer, bounds, Target.TransformPoint(bounds.center),
                            Target.rotation, Target.lossyScale, _lineProperties);
                        break;
                    }
            }
        }

        private void DrawSphere(IMECamera camera)
        {
            Vector3 scale = Target.lossyScale * Radius;
            scale = Vector3.one * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            GizmoUtility.DrawCubeHandles(camera.CommandBuffer, Target.TransformPoint(LocalCenter), Target.rotation, scale, _handleProperties);
            GizmoUtility.DrawWireSphere(camera.CommandBuffer, camera.Camera, Target.TransformPoint(LocalCenter), Target.rotation, scale, _lineProperties);

            if (_isDragging)
            {
                GizmoUtility.DrawSelection(camera.CommandBuffer,
                    HandlesTransform.MultiplyPoint(LocalCenter + HandlePositions[_dragIndex]),
                    Target.rotation, FlattenScale, _selectionProperties);
            }
        }

        private void DrawCone(IMECamera camera)
        {
            // The old ConeGizmo drew its handles with the line property block, so HandlesColor never
            // reached them; the draggable handles use the handle block now.
            GizmoUtility.DrawConeHandles(camera.CommandBuffer, Target.TransformPoint(Vector3.forward * Height),
                Target.rotation, ConeScale, _handleProperties);
            GizmoUtility.DrawWireCone(camera.CommandBuffer, Height, Radius, Target.position, Target.rotation,
                Vector3.one, _lineProperties);

            if (_isDragging)
            {
                GizmoUtility.DrawSelection(camera.CommandBuffer,
                    Target.TransformPoint(Vector3.forward * Height + Vector3.Scale(HandlePositions[_dragIndex], ConeScale)),
                    Target.rotation, ConeScale, _selectionProperties);
            }
        }
    }
}