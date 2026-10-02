using System;
using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;
using UnityEngine.Rendering;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Graphics
{
    public class SceneGridVisual : MonoBehaviour
    {
        public static SceneGridVisual Instance { get; private set; }
        private Mesh _grid0Mesh;
        private Mesh _grid1Mesh;
        private Material _grid0Material;
        private Material _grid1Material;

        [FormerlySerializedAs("m_gridOffset")]
        [SerializeField] private Vector3 _gridOffset = new Vector3(0f, 0.01f, 0f);

        private float _gridSize = 0.5f;
        
        public float SizeOfGrid
        {
            get { return _gridSize; }
            set
            {
                if (_gridSize != value)
                {
                    _gridSize = value;
                    Rebuild();
                }
            }
        }
        [SerializeField]
        [FormerlySerializedAs("m_alpha")]
        private float _alpha = 1.0f;
        public float Alpha
        {
            get { return _alpha; }
            set { _alpha = Mathf.Clamp01(value); }
        }
        private bool _zTest = true;
        public bool ZTest
        {
            get { return _zTest; }
            set
            {
                if(_zTest != value)
                {
                    _zTest = value;
                    Rebuild();
                }
            }
        }
        private MECamera _meCamera;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            Init();
        }

        private void OnDisable()
        {
            if(_meCamera != null)
            {
                _meCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
            }
            Destroy(_meCamera);
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Init()
        {
            if (Camera.main != null) _meCamera = Camera.main.gameObject.AddComponent<MECamera>();
            _meCamera.Event = CameraEvent.AfterForwardAlpha;
            _meCamera.CommandBufferRefresh += OnCommandBufferRefresh;

            Rebuild();
            _meCamera.RefreshCommandBuffer();
        }
        private void Update()
        {
            if (_meCamera.CommandBufferOverride == null)
            {
                _meCamera.RefreshCommandBuffer();
            }
        }
        void Rebuild()
        {
            Cleanup();
            _grid0Material = CreateGridMaterial(0.5f, _zTest);
            _grid1Material = CreateGridMaterial(0.5f, _zTest);

            _grid0Mesh = CreateGridMesh(MEBase.Instance.Appearance.Colors.GridColor, _gridSize);
            _grid1Mesh = CreateGridMesh(MEBase.Instance.Appearance.Colors.GridColor, _gridSize);
        }
        Mesh CreateGridMesh(Color color, float spacing, int linesCount = 150)
        {
            int count = linesCount / 2;

            Mesh mesh = new Mesh();
            mesh.name = "Grid " + spacing;

            int index = 0;
            int[] indices = new int[count * 8];
            Vector3[] vertices = new Vector3[count * 8];
            Color[] colors = new Color[count * 8];
            
            for(int i = -count; i < count; ++i)
            {
                vertices[index] = new Vector3(i * spacing, 0, -count * spacing);
                vertices[index + 1] = new Vector3(i * spacing, 0, count * spacing);

                vertices[index + 2] = new Vector3(-count * spacing, 0, i * spacing);
                vertices[index + 3] = new Vector3(count * spacing, 0, i * spacing);

                indices[index] = index;
                indices[index + 1] = index + 1;
                indices[index + 2] = index + 2;
                indices[index + 3] = index + 3;

                colors[index] = colors[index + 1] = colors[index + 2] = colors[index + 3] = color;

                index += 4;
            }

            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.colors = colors;

            return mesh;
        }
        private void Cleanup()
        {
            if (_grid0Material != null)
            {
                Destroy(_grid0Material);
            }
            if (_grid1Material != null)
            {
                Destroy(_grid1Material);
            }
            if (_grid0Mesh != null)
            {
                Destroy(_grid0Mesh);
            }
            if (_grid1Mesh != null)
            {
                Destroy(_grid1Mesh);
            }
        }
        
        private void OnCommandBufferRefresh(IMECamera obj)
        {
            float h = GetCameraOffset();
            h = Mathf.Abs(h);
            h = Mathf.Max(1, h);
            float scale = MathfHelper.CountOfDigits(h);
            float fadeDistance = h * 10;

            float alpha0 = GetAlpha(0, h, scale);
            float alpha1 = GetAlpha(1, h, scale);

            SetGridAlpha(_grid0Material, alpha0 * _alpha, fadeDistance);
            SetGridAlpha(_grid1Material, alpha1 * _alpha, fadeDistance);

            float pow0 = Mathf.Pow(10, scale - 1);
            float pow1 = Mathf.Pow(10, scale);

            Matrix4x4 grid0 =
                transform.localToWorldMatrix * Matrix4x4.TRS(GetGridPostion(pow0), Quaternion.identity, Vector3.one * pow0);
            Matrix4x4 grid1 =
                transform.localToWorldMatrix * Matrix4x4.TRS(GetGridPostion(pow1), Quaternion.identity, Vector3.one * pow1);

            CommandBuffer commandBuffer = _meCamera.CommandBuffer;
            commandBuffer.DrawMesh(_grid0Mesh, grid0, _grid0Material, 0, 0);
            commandBuffer.DrawMesh(_grid1Mesh, grid1, _grid1Material, 0, 0);
        }

        private Vector3 GetGridPostion(float spacing)
        {
            Vector3 position = _meCamera.Camera.transform.position;
            position = transform.InverseTransformPoint(position);

            spacing *= _gridSize;

            position.x = Mathf.Floor(position.x / spacing) * spacing;
            position.z = Mathf.Floor(position.z / spacing) * spacing;
            position.y = 0;

            position += _gridOffset;

            return position;
        }

        private void SetGridAlpha(Material gridMaterial, float alpha, float fadeDistance)
        {
            Color color = gridMaterial.GetColor("_GridColor");
            color.a = alpha;
            gridMaterial.SetColor("_GridColor", color);
            gridMaterial.SetFloat("_FadeDistance", fadeDistance);

            if (_meCamera.Camera.orthographic)
            {
                gridMaterial.SetFloat("_CameraSize", _meCamera.Camera.orthographicSize);
            }
        }

        private Material CreateGridMaterial(float scale, bool zTest)
        {
            Shader shader =  Shader.Find("CodingDaniel/MEHandles/Grid");
            Material material = new Material(shader);

            material.SetColor("_GridColor", MEBase.Instance.Appearance.Colors.GridColor);
            material.SetFloat("_ZTest", zTest ? (float)CompareFunction.LessEqual : (float)CompareFunction.Always);
            
            return material;
        }

        private float GetCameraOffset()
        {
            if (_meCamera.Camera.orthographic)
            {
                return _meCamera.Camera.orthographicSize;
            }

            Vector3 position = _meCamera.Camera.transform.position;
            position = transform.InverseTransformPoint(position);
            return position.y;
        }

        private float GetAlpha(int grid, float h, float scale)
        {
            float nextSpacing = Mathf.Pow(10, scale);
            if (grid == 0)
            {
                float spacing = Mathf.Pow(10, scale - 1);
                return 1.0f - (h - spacing) / (nextSpacing - spacing);
            }

            float nextNextSpacing = Mathf.Pow(10, scale + 1);
            return (h * 10 - nextSpacing) / (nextNextSpacing - nextSpacing);
        }
    }
}