using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class PBVertexSelection : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("m_color")]
        private Color _color = new Color(0.33f, 0.33f, 0.33f);
        [SerializeField]
        [FormerlySerializedAs("m_selectedColor")]
        private Color _selectedColor = Color.yellow;
        [SerializeField]
        [FormerlySerializedAs("m_hoverColor")]
        private Color _hoverColor = new Color(1, 1, 0, 0.75f);

        [SerializeField]
        [FormerlySerializedAs("m_scale")]
        private float _scale = 4.5f;

        [SerializeField]
        [FormerlySerializedAs("m_zTest")]
        public CompareFunction _zTest = CompareFunction.LessEqual;
        private Material _material;
        private readonly Dictionary<ProBuilderMesh, MeshFilter> _meshToSelection = new Dictionary<ProBuilderMesh, MeshFilter>();
        private readonly Dictionary<ProBuilderMesh, HashSet<int>> _meshToIndices = new Dictionary<ProBuilderMesh, HashSet<int>>();
        private readonly Dictionary<ProBuilderMesh, List<int>> _meshToIndicesList = new Dictionary<ProBuilderMesh, List<int>>();
        private readonly List<ProBuilderMesh> _meshes = new List<ProBuilderMesh>();
        private readonly List<PBMesh> _pbMeshes = new List<PBMesh>();
        private readonly int[] _hoveredIndices = new int[] { -1 };
        private ProBuilderMesh _hoveredMesh;

        private Vector3 _lastPosition;
        public Vector3 LastPosition
        {
            get { return LastMesh != null ? LastMesh.transform.TransformPoint(_lastPosition) : Vector3.zero; }
        }

        public Vector3 LastPositionLocal
        {
            get { return LastMesh != null ? _lastPosition : Vector3.zero; }
        }

        [FormerlySerializedAs("m_lastNormal")]
        public Vector3 _lastNormal = Vector3.forward;
        public Vector3 LastNormal
        {
            get { return LastMesh != null ? LastMesh.transform.TransformDirection(_lastNormal.normalized) : Vector3.zero; }
        }

        public ProBuilderMesh LastMesh
        {
            get
            {
                if(_meshes.Count == 0)
                {
                    return null;
                }

                return _meshes.Last();
            }
        }

        private Vector3 _centerOfMass;
        public Vector3 CenterOfMass
        {
            get { return _centerOfMass; }
        }

        private int _selectedVerticesCount;
        public int VerticesCount
        {
            get { return _selectedVerticesCount; }
        }

        public int MeshesCount
        {
            get { return _meshes.Count; }
        }

        public IEnumerable<ProBuilderMesh> Meshes
        {
            get { return _meshes; }
        }

        public IEnumerable<PBMesh> PBMeshes
        {
            get { return _pbMeshes; }
        }

        private bool IsGeometryShadersSupported
        {
            get { return PBBuiltinMaterials.geometryShadersSupported; }
        }

        private PBBaseEditor _editor;
        private void Awake()
        {
            
            _editor = GetComponent<PBBaseEditor>();

            _material = new Material(PBBuiltinMaterials.PointsMaterial);
            _material.SetColor("_Color", Color.white);
            _material.SetInt("_HandleZTest", (int)_zTest);
            _material.SetFloat("_Scale", _scale); 
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }

            for(int i = 0; i < _pbMeshes.Count; ++i)
            {
                PBMesh pbMesh = _pbMeshes[i];
                if(pbMesh != null)
                {
                    pbMesh.RaiseUnselected();
                }
            }

            _meshToSelection.Clear();
            _meshToIndices.Clear();
            _meshToIndicesList.Clear();
            _meshes.Clear();
            _pbMeshes.Clear();
        }

        public bool IsSelected(ProBuilderMesh mesh, int index)
        {
            HashSet<int> indicesHs;
            if(_meshToIndices.TryGetValue(mesh, out indicesHs))
            {
                return indicesHs.Contains(index);
            }
            return false;
        }

        public int GetVerticesCount(ProBuilderMesh mesh)
        {
            return _meshToIndicesList[mesh].Count;
        }

        public IList<int> GetVertices(ProBuilderMesh mesh)
        {
            List<int> result;
            if(_meshToIndicesList.TryGetValue(mesh, out result))
            {
                return result;
            }

            return null;
        }

        public void Clear()
        {
            for (int i = 0; i < _meshes.Count; ++i)
            {
                ProBuilderMesh mesh = _meshes[i];
                MeshFilter filter = _meshToSelection[mesh];
                if(filter != null)
                {
                    Destroy(filter.gameObject);
                }
                
                PBMesh pbMesh = _pbMeshes[i];
                if (pbMesh != null)
                {
                    pbMesh.RaiseUnselected();
                }
            }

            _meshToSelection.Clear();
            _meshToIndices.Clear();
            _meshToIndicesList.Clear();
            _meshes.Clear();
            _pbMeshes.Clear();

            _selectedVerticesCount = 0;
            _centerOfMass = Vector3.zero;
            _lastNormal = Vector3.forward;
            _lastPosition = Vector3.zero;
        }

        public void Hover(ProBuilderMesh mesh, int index)
        {
            if(_hoveredMesh != null)
            {
                Leave();
            }

            HashSet<int> indicesHs;
            MeshFilter vertices;
            _meshToSelection.TryGetValue(mesh, out vertices);
            if (!_meshToIndices.TryGetValue(mesh, out indicesHs))
            {
                return;
            }

            _hoveredMesh = mesh;
            _hoveredIndices[0] = index;

            if (indicesHs.Contains(_hoveredIndices[0]))
            {    
                SetVerticesColor(mesh, vertices, _selectedColor, _hoveredIndices);
            }
            else
            {
                SetVerticesColor(mesh, vertices, _hoverColor, _hoveredIndices);
            }
        }

        public void Leave()
        {
            if (_hoveredMesh == null) 
            {
                return;
            }

            HashSet<int> indicesHs;
            MeshFilter vertices;
            _meshToSelection.TryGetValue(_hoveredMesh, out vertices);
            if (!_meshToIndices.TryGetValue(_hoveredMesh, out indicesHs))
            {
                return;
            }

            if (indicesHs.Contains(_hoveredIndices[0]))
            {
                SetVerticesColor(_hoveredMesh, vertices, _selectedColor, _hoveredIndices);
            }
            else
            {
                SetVerticesColor(_hoveredMesh, vertices, _color, _hoveredIndices);
            }
            _hoveredIndices[0] = -1;
            _hoveredMesh = null;
        }

        public void Add(ProBuilderMesh mesh, IEnumerable<int> indices)
        {
            HashSet<int> indicesHs;
            List<int> indicesList;
            MeshFilter vertices;
            _meshToSelection.TryGetValue(mesh, out vertices);
            _meshToIndicesList.TryGetValue(mesh, out indicesList);
            if (!_meshToIndices.TryGetValue(mesh, out indicesHs))
            {
                vertices = CreateVerticesGameObject(mesh, null);
                //vertices.transform.SetParent(transform, false);
                vertices.transform.SetParent(mesh.transform, false);

                indicesHs = new HashSet<int>();
                indicesList = new List<int>();

                _meshToSelection.Add(mesh, vertices);
                _meshToIndices.Add(mesh, indicesHs);
                _meshToIndicesList.Add(mesh, indicesList);
                _meshes.Add(mesh);

                PBMesh pbMesh = mesh.GetComponent<PBMesh>();
                if (pbMesh != null)
                {
                    pbMesh.RaiseSelected(false);
                }
                _pbMeshes.Add(pbMesh);
            }

            int[] notSelectedIndices = indices.Where(i => !indicesHs.Contains(i)).ToArray();
            SetVerticesColor(mesh, vertices, _selectedColor, notSelectedIndices); 
            for(int i = 0; i < notSelectedIndices.Length; ++i)
            {
                indicesHs.Add(notSelectedIndices[i]);
                indicesList.Add(notSelectedIndices[i]);
            }

            if(notSelectedIndices.Length > 0)
            {
                Vertex[] notSelectedVertices = mesh.GetVertices(notSelectedIndices);
                _lastPosition = notSelectedVertices.Last().position;
                _lastNormal = notSelectedVertices.Last().normal;

                for(int i = 0; i < notSelectedIndices.Length; i++)
                {
                    _selectedVerticesCount++;
                    if (_selectedVerticesCount == 1)
                    {
                        _centerOfMass = mesh.transform.TransformPoint(notSelectedVertices[i].position);
                    }
                    else
                    {
                        _centerOfMass *= (_selectedVerticesCount - 1) / (float)_selectedVerticesCount;
                        _centerOfMass += mesh.transform.TransformPoint(notSelectedVertices[i].position) / _selectedVerticesCount;
                    }
                }
            }
        }

        public void Remove(ProBuilderMesh mesh, IEnumerable<int> indices = null)
        {
            HashSet<int> indicesHs;
            if (_meshToIndices.TryGetValue(mesh, out indicesHs))
            {
                MeshFilter vertices = _meshToSelection[mesh];
                List<int> indicesList = _meshToIndicesList[mesh];
                if (indices != null)
                {
                    int[] selectedIndices = indices.Where(i => indicesHs.Contains(i)).ToArray();
                    SetVerticesColor(mesh, vertices, _color, selectedIndices);
                    for (int i = 0; i < selectedIndices.Length; ++i)
                    {
                        indicesHs.Remove(selectedIndices[i]);
                        indicesList.Remove(selectedIndices[i]);
                    }

                    Vertex[] selectedVertices = mesh.GetVertices(selectedIndices);
                    UpdateCenterOfMassOnRemove(mesh.transform, selectedVertices);
                }
                else
                {
                    Vertex[] selectedVertices = mesh.GetVertices(indicesList);
                    UpdateCenterOfMassOnRemove(mesh.transform, selectedVertices);

                    indicesHs.Clear();
                    indicesList.Clear();
                }

                if(indicesHs.Count == 0)
                {
                    _meshToIndices.Remove(mesh);
                    _meshToIndicesList.Remove(mesh);
                    _meshToSelection.Remove(mesh);
                    
                    Destroy(vertices.gameObject);

                    int index = _meshes.IndexOf(mesh);
                    if(index != -1)
                    {
                        _meshes.RemoveAt(index);
                        PBMesh pbMesh = _pbMeshes[index];
                        if (pbMesh != null)
                        {
                            pbMesh.RaiseUnselected();
                        }
                        _pbMeshes.RemoveAt(index);
                    }
                }

                if (indicesList.Count > 0)
                {
                    Vertex[] lastVertex = mesh.GetVertices(new[] { indicesList.Last() });
                    _lastPosition = lastVertex[0].position;
                    _lastNormal = lastVertex[0].normal;
                }
                else
                {
                    if(LastMesh != null)
                    {
                        Vertex[] lastVertex = LastMesh.GetVertices(new[] { _meshToIndicesList[LastMesh].Last() });
                        _lastPosition = lastVertex[0].position;
                        _lastNormal = lastVertex[0].normal;
                    }
                    else
                    {
                        _lastPosition = Vector3.zero;
                        _lastPosition = Vector3.forward;
                    }
                }
            }
        }

        private void UpdateCenterOfMassOnRemove(Transform mesh, IList<Vertex> selectedVertices)
        {
            for (int i = selectedVertices.Count - 1; i >= 0; --i)
            {
                _selectedVerticesCount--;
                if (_selectedVerticesCount == 0)
                {
                    _centerOfMass = mesh.position;
                }
                else if (_selectedVerticesCount == 1)
                {
                    _centerOfMass = mesh.TransformPoint(selectedVertices[0].position);
                }
                else
                {
                    _centerOfMass -= mesh.TransformPoint(selectedVertices[i].position) / (_selectedVerticesCount + 1);
                    _centerOfMass *= (_selectedVerticesCount + 1) / (float)_selectedVerticesCount;
                }
            }
        }

        private void SetVerticesColor(ProBuilderMesh mesh, MeshFilter vertices, Color color, IEnumerable<int> indices)
        {
            if (IsGeometryShadersSupported)
            {
                List<int> coincident = new List<int>();
                foreach(int index in indices)
                {
                    mesh.GetCoincidentVertices(index, coincident);
                    Color[] colors = vertices.sharedMesh.colors;
                    for (int i = 0; i < coincident.Count; ++i)
                    {
                        colors[coincident[i]] = color;
                    }
                    vertices.sharedMesh.colors = colors;
                    coincident.Clear();
                }       
            }
            else
            {
                List<int> coincident = new List<int>();
                foreach (int index in indices)
                {
                    mesh.GetCoincidentVertices(index, coincident);
                    Color[] colors = vertices.sharedMesh.colors;
                    for (int i = 0; i < coincident.Count; ++i)
                    {
                        colors[coincident[i] * 4] = color;
                        colors[coincident[i] * 4 + 1] = color;
                        colors[coincident[i] * 4 + 2] = color;
                        colors[coincident[i] * 4 + 3] = color;
                    }
                    vertices.sharedMesh.colors = colors;
                    coincident.Clear();
                }
            }
        }

        private MeshFilter CreateVerticesGameObject(ProBuilderMesh mesh, IList<int> indices)
        {
            GameObject vertices = new GameObject("Vertices");
            vertices.hideFlags = HideFlags.DontSave;
            vertices.layer = _editor.GraphicsLayer;

            MeshFilter meshFilter = vertices.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = vertices.AddComponent<MeshFilter>();
            }
            meshFilter.sharedMesh = new Mesh();

            MeshRenderer renderer = vertices.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = vertices.AddComponent<MeshRenderer>();
            }
            renderer.sharedMaterial = _material;

            if (indices == null)
            {
                indices = new int[mesh.vertexCount];
                for (int i = 0; i < indices.Count; ++i)
                {
                    indices[i] = i;
                }
            }

            BuildVertexMesh(mesh, meshFilter.sharedMesh, indices);
            CopyTransform(mesh.transform, meshFilter.transform);
            return meshFilter;
        }

        private void BuildVertexMesh(ProBuilderMesh mesh, Mesh target, IList<int> indexes)
        {
            if (IsGeometryShadersSupported)
            {
                PBUtility.BuildVertexMesh(mesh.positions, _color, target, indexes);
            }
            else
            {
                PBUtility.BuildVertexMeshLegacy(mesh.positions, _color, target, indexes);
            }
        }

     
        private static void CopyTransform(Transform src, Transform dst)
        {
            //dst.position = src.position;
            //dst.rotation = src.rotation;
            //dst.localScale = src.localScale;
        }

        public void Synchronize(Vector3 centerOfMass, Vector3 lastPosition, Vector3 lastNormal)
        {
            foreach (KeyValuePair<ProBuilderMesh, List<int>> kvp in _meshToIndicesList)
            {
                ProBuilderMesh mesh = kvp.Key;

                MeshFilter meshFilter = _meshToSelection[mesh];
                BuildVertexMesh(mesh, meshFilter.sharedMesh, null);

                meshFilter.transform.position = mesh.transform.position;
            }

            _centerOfMass = centerOfMass;
            _lastNormal = LastMesh.transform.InverseTransformDirection(lastNormal.normalized);  
            _lastPosition = LastMesh.transform.InverseTransformPoint(lastPosition);    
        }
    }
}