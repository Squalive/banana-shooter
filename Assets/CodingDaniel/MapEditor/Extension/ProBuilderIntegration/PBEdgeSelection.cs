using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public struct Vector3Tuple
    {
        public Vector3 a;
        public Vector3 b;
        
        public Vector3Tuple(Vector3 a, Vector3 b)
        {
            this.a = a;
            this.b = b;
        }

        public bool Equals(Vector3Tuple other)
        {
            return (a == other.a && b == other.b) ||
                   (a == other.b && b == other.a);
        }

        public override bool Equals(object obj)
        {
            return obj is Vector3Tuple && Equals((Vector3Tuple)obj);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (a.GetHashCode() + b.GetHashCode());
                return hash;
            }
        }
    }
    public class PBEdgeSelection : MonoBehaviour
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
        private float _scale = 1.0f;

        [SerializeField]
        [FormerlySerializedAs("m_zTest")]
        private CompareFunction _zTest = CompareFunction.LessEqual;
        private Material _material;
        private readonly Dictionary<ProBuilderMesh, MeshFilter> _meshToSelection = new Dictionary<ProBuilderMesh, MeshFilter>();
        private readonly Dictionary<ProBuilderMesh, HashSet<Edge>> _meshToEdges = new Dictionary<ProBuilderMesh, HashSet<Edge>>();
        private readonly Dictionary<ProBuilderMesh, List<Edge>> _meshToEdgesList = new Dictionary<ProBuilderMesh, List<Edge>>();
        private readonly List<ProBuilderMesh> _meshes = new List<ProBuilderMesh>();
        private readonly List<PBMesh> _pbMeshes = new List<PBMesh>();
        private readonly Dictionary<Edge, List<int>> _edgeToSelection = new Dictionary<Edge, List<int>>();
        private readonly Dictionary<Edge, HashSet<Edge>> _coincidentEdges = new Dictionary<Edge, HashSet<Edge>>();

        private Vector3 _lastPosition;
        public Vector3 LastPosition
        {
            get { return LastMesh != null ? LastMesh.transform.TransformPoint(_lastPosition) : Vector3.zero; }
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
                if (_meshes.Count == 0)
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

        private int _selectedEdgesCount;
        public int EdgesCount
        {
            get { return _selectedEdgesCount; }
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

            _material = new Material(PBBuiltinMaterials.LinesMaterial);
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
                if (pbMesh != null)
                {
                    pbMesh.RaiseUnselected();
                }
            }

            _meshToSelection.Clear();
            _meshToEdges.Clear();
            _meshToEdgesList.Clear();
            _meshes.Clear();
            _pbMeshes.Clear();
            _edgeToSelection.Clear();
            _coincidentEdges.Clear();
        }

        public bool IsSelected(ProBuilderMesh mesh, Edge edge)
        {
            HashSet<Edge> edgeHs;
            if (_meshToEdges.TryGetValue(mesh, out edgeHs))
            {
                return edgeHs.Contains(edge);
            }
            return false;
        }
        

        public int GetEdgesCount(ProBuilderMesh mesh)
        {
            return _meshToEdgesList[mesh].Count;
        }

        public IList<Edge> GetEdges(ProBuilderMesh mesh)
        {
            List<Edge> edges;
            if(_meshToEdgesList.TryGetValue(mesh, out edges))
            {
                return edges;
            }
            return null;
        }

        public IList<Edge> GetCoincidentEdges(IEnumerable<Edge> edges)
        {
            HashSet<Edge> result = new HashSet<Edge>();
            if(edges == null)
            {
                return null;
            }
            foreach(Edge edge in edges)
            {
                if(_coincidentEdges.ContainsKey(edge))
                {
                    HashSet<Edge> coincidentEdges = _coincidentEdges[edge];
                    foreach (Edge coincident in coincidentEdges)
                    {
                        if (!result.Contains(coincident))
                        {
                            result.Add(coincident);
                        }
                    }
                }
                else
                {
                    result.Add(edge);
                }   
            }
            return result.ToArray();
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
                if(pbMesh != null)
                {
                    pbMesh.RaiseUnselected();
                }
            }

            _meshToSelection.Clear();
            _meshToEdges.Clear();
            _meshToEdgesList.Clear();
            _meshes.Clear();
            _pbMeshes.Clear();
            _edgeToSelection.Clear();
            _coincidentEdges.Clear();

            _selectedEdgesCount = 0;
            _centerOfMass = Vector3.zero;
            _lastNormal = Vector3.forward;
            _lastPosition = Vector3.zero;
        }

        public void Add(ProBuilderMesh mesh, IEnumerable<Edge> edges)
        {
            HashSet<Edge> edgesHs;
            List<Edge> edgesList;
            MeshFilter edgesSelection;

            _meshToSelection.TryGetValue(mesh, out edgesSelection);
            _meshToEdgesList.TryGetValue(mesh, out edgesList);
            if (!_meshToEdges.TryGetValue(mesh, out edgesHs))
            {
                edgesSelection = CreateEdgesGameObject(mesh);
                edgesSelection.transform.SetParent(mesh.transform, false);

                edgesHs = new HashSet<Edge>();
                edgesList = new List<Edge>();

                _meshToSelection.Add(mesh, edgesSelection);
                _meshToEdges.Add(mesh, edgesHs);
                _meshToEdgesList.Add(mesh, edgesList);
                _meshes.Add(mesh);

                PBMesh pbMesh = mesh.GetComponent<PBMesh>();
                if (pbMesh != null)
                {
                    pbMesh.RaiseSelected(true);
                }
                _pbMeshes.Add(pbMesh);
            }

            int vertexCount = mesh.vertexCount;
            Edge[] notSelectedEdges = edges.Where(edge => !edgesHs.Contains(edge) && vertexCount > edge.a && vertexCount > edge.b).ToArray();
            SetEdgesColor(mesh, edgesSelection, _selectedColor, notSelectedEdges);
            for (int i = 0; i < notSelectedEdges.Length; ++i)
            {
                edgesHs.Add(notSelectedEdges[i]);
                edgesList.Add(notSelectedEdges[i]);
            }

            if (notSelectedEdges.Length > 0)
            {
                _lastPosition = GetPosition(mesh, notSelectedEdges.Last());
                _lastNormal = GetNormal(mesh, notSelectedEdges.Last());

                for (int i = 0; i < notSelectedEdges.Length; i++)
                {
                    _selectedEdgesCount++;
                    if (_selectedEdgesCount == 1)
                    {
                        _centerOfMass = mesh.transform.TransformPoint(GetPosition(mesh, notSelectedEdges[i]));
                    }
                    else
                    {
                        _centerOfMass *= (_selectedEdgesCount - 1) / (float)_selectedEdgesCount;
                        _centerOfMass += mesh.transform.TransformPoint(GetPosition(mesh, notSelectedEdges[i])) / _selectedEdgesCount;
                    }
                }
            }
        }

        public void Remove(ProBuilderMesh mesh, IEnumerable<Edge> edges = null)
        {
            HashSet<Edge> edgesHs;
            if (_meshToEdges.TryGetValue(mesh, out edgesHs))
            {
                MeshFilter edgesSelection = _meshToSelection[mesh];
                List<Edge> edgesList = _meshToEdgesList[mesh];
                if (edges != null)
                {
                    Edge[] selectedEdges = edges.Where(i => edgesHs.Contains(i)).ToArray();
                    SetEdgesColor(mesh, edgesSelection, _color, selectedEdges);
                    for (int i = 0; i < selectedEdges.Length; ++i)
                    {
                        edgesHs.Remove(selectedEdges[i]);
                        edgesList.Remove(selectedEdges[i]);
                    }

                    UpdateCenterOfMassOnRemove(mesh, selectedEdges);
                }
                else
                {
                    UpdateCenterOfMassOnRemove(mesh, edgesList);

                    edgesHs.Clear();
                    edgesList.Clear();
                }

                if (edgesHs.Count == 0)
                {
                    _meshToEdges.Remove(mesh);
                    _meshToEdgesList.Remove(mesh);
                    _meshToSelection.Remove(mesh);

                    Destroy(edgesSelection.gameObject);

                    int meshIndex = _meshes.IndexOf(mesh);
                    if(meshIndex != -1)
                    {
                        _meshes.RemoveAt(meshIndex);

                        PBMesh pbMesh = _pbMeshes[meshIndex];
                        if (pbMesh != null)
                        {
                            pbMesh.RaiseUnselected();
                        }
                        _pbMeshes.RemoveAt(meshIndex);
                    }
                }

                if (edgesList.Count > 0)
                {
                    _lastPosition = GetPosition(mesh, edgesList.Last());
                    _lastNormal = GetNormal(mesh, edgesList.Last());
                }
                else
                {
                    if (LastMesh != null)
                    {
                        _lastPosition = GetPosition(LastMesh, _meshToEdgesList[LastMesh].Last());
                        _lastNormal = GetNormal(LastMesh, _meshToEdgesList[LastMesh].Last());
                    }
                    else
                    {
                        _lastPosition = Vector3.zero;
                        _lastPosition = Vector3.forward;
                    }
                }
            }
        }

        private void UpdateCenterOfMassOnRemove(ProBuilderMesh mesh,  IList<Edge> selectedEdges)
        {
            for (int i = selectedEdges.Count - 1; i >= 0; --i)
            {
                _selectedEdgesCount--;
                if (_selectedEdgesCount == 0)
                {
                    _centerOfMass = mesh.transform.position;
                }
                else if (_selectedEdgesCount == 1)
                {
                    _centerOfMass = mesh.transform.TransformPoint(GetPosition(mesh, selectedEdges[0]));
                }
                else
                {
                    _centerOfMass -= mesh.transform.TransformPoint(GetPosition(mesh, selectedEdges[i])) / (_selectedEdgesCount + 1);
                    _centerOfMass *= (_selectedEdgesCount + 1) / (float)_selectedEdgesCount;
                }
            }
        }

        public Vector3 GetPosition(ProBuilderMesh mesh, Edge edge)
        {
            Vertex[] vertices = mesh.GetVertices(new[] { edge.a, edge.b });
            return (vertices[0].position + vertices[1].position) * 0.5f;
        }

        public Vector3 GetNormal(ProBuilderMesh mesh, Edge edge)
        {
            Vertex[] vertices = mesh.GetVertices(new[] { edge.a, edge.b });
            return (vertices[0].normal + vertices[1].normal) * 0.5f;
        }

        private void SetEdgesColor(ProBuilderMesh mesh, MeshFilter selection, Color color, IEnumerable<Edge> edges)
        {
            Color[] colors = selection.sharedMesh.colors;
            foreach (Edge edge in edges)
            {
                if(_coincidentEdges.ContainsKey(edge))
                {
                    HashSet<Edge> coincidentEdges = _coincidentEdges[edge];
                    foreach (Edge coincidentEdge in coincidentEdges)
                    {
                        List<int> indices;
                        if(_edgeToSelection.TryGetValue(coincidentEdge, out indices))
                        {
                            for (int i = 0; i < indices.Count; ++i)
                            {
                                colors[indices[i]] = color;
                            }
                        }
                    }
                }
                else
                {
                    if(_edgeToSelection.ContainsKey(edge))
                    {
                        List<int> indices = _edgeToSelection[edge];
                        for (int i = 0; i < indices.Count; ++i)
                        {
                            colors[indices[i]] = color;
                        }
                    }
                }
            }
            selection.sharedMesh.colors = colors;
        }

        private MeshFilter CreateEdgesGameObject(ProBuilderMesh mesh)
        {
            GameObject edgesSelection = new GameObject("Edges");
            edgesSelection.hideFlags = HideFlags.DontSave;
            edgesSelection.layer = _editor.GraphicsLayer;

            MeshFilter meshFilter = edgesSelection.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = edgesSelection.AddComponent<MeshFilter>();
            }
            meshFilter.sharedMesh = new Mesh();

            MeshRenderer renderer = edgesSelection.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = edgesSelection.AddComponent<MeshRenderer>();
            }
            renderer.sharedMaterial = _material;
            FindCoincidentEdges(mesh);
            BuildEdgeMesh(mesh, meshFilter.sharedMesh, false);
            CopyTransform(mesh.transform, meshFilter.transform);
            return meshFilter;
        }

        public void FindCoincidentEdges(ProBuilderMesh mesh)
        {
            if (_coincidentEdges.Count != 0)
            {
                return;
            }

            Dictionary<Vector3Tuple, HashSet<Edge>> coordinateToEdges = new Dictionary<Vector3Tuple, HashSet<Edge>>();
            IList<Face> faces = mesh.faces;
            IList<Vector3> positions = mesh.positions;
            int faceCount = mesh.faceCount;
            for (int i = 0; i < faceCount; i++)
            {
                ReadOnlyCollection<Edge> edges = faces[i].edges;
                for (int n = 0; n < edges.Count; n++)
                {
                    Edge edge = edges[n];
                    Vector3Tuple tuple = new Vector3Tuple(positions[edge.a], positions[edge.b]);
                    HashSet<Edge> hs;
                    if (!coordinateToEdges.TryGetValue(tuple, out hs))
                    {
                        hs = new HashSet<Edge>();
                        coordinateToEdges.Add(tuple, hs);
                    }
                    if (!hs.Contains(edge))
                    {
                        hs.Add(edge);
                    }
                }
            }

            _coincidentEdges.Clear();
            foreach (HashSet<Edge> edges in coordinateToEdges.Values)
            {
                foreach (Edge edge in edges)
                {
                    _coincidentEdges.Add(edge, edges);
                }
            }
        }

        private void BuildEdgeMesh(ProBuilderMesh mesh, Mesh target, bool positionsOnly)
        {
            IList<Vector3> positions = mesh.positions;

            int edgeIndex = 0;
            int edgeCount = 0;
            int faceCount = mesh.faceCount;

            IList<Face> faces = mesh.faces;
            for (int i = 0; i < faceCount; i++)
            {
                edgeCount += faces[i].edges.Count;
            }
            edgeCount = System.Math.Min(edgeCount, int.MaxValue / 2 - 1);

            int[] tris;
            Vector3[] vertices;
            if(positionsOnly)
            {
                vertices = target.vertices;
                tris = null;
            }
            else
            {
                tris = new int[edgeCount * 2];
                vertices = new Vector3[edgeCount * 2];
                _edgeToSelection.Clear();
            }

            for (int i = 0; i < faceCount && edgeIndex < edgeCount; i++)
            {
                ReadOnlyCollection<Edge> edges = faces[i].edges;
                for (int n = 0; n < edges.Count && edgeIndex < edgeCount; n++)
                {
                    Edge edge = edges[n];

                    int positionIndex = edgeIndex * 2;

                    vertices[positionIndex + 0] = positions[edge.a];
                    vertices[positionIndex + 1] = positions[edge.b];

                    if(!positionsOnly)
                    {
                        tris[positionIndex + 0] = positionIndex + 0;
                        tris[positionIndex + 1] = positionIndex + 1;

                        List<int> list;
                        if (!_edgeToSelection.TryGetValue(edge, out list))
                        {
                            list = new List<int>();
                            _edgeToSelection.Add(edge, list);
                        }
                        list.Add(positionIndex + 0);
                        list.Add(positionIndex + 1);
                    }
                 
                    edgeIndex++;
                }
            }

            if(!positionsOnly)
            {
                target.Clear();
                target.name = "EdgeMesh" + target.GetInstanceID();
                target.vertices = vertices.ToArray();
                Color[] colors = new Color[target.vertexCount];
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = _color;
                }
                target.colors = colors;
                target.subMeshCount = 1;
                target.SetIndices(tris, MeshTopology.Lines, 0);
            }
            else
            {
                target.vertices = vertices.ToArray();
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
            foreach (KeyValuePair<ProBuilderMesh, HashSet<Edge>> kvp in _meshToEdges)
            {
                ProBuilderMesh mesh = kvp.Key;

                MeshFilter meshFilter = _meshToSelection[mesh];
                BuildEdgeMesh(mesh, meshFilter.sharedMesh, true);

                meshFilter.transform.position = mesh.transform.position;
            }

            _centerOfMass = centerOfMass;
            _lastNormal = LastMesh.transform.InverseTransformDirection(lastNormal.normalized);
            _lastPosition = LastMesh.transform.InverseTransformPoint(lastPosition);

        }

    }
}