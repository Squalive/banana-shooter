using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class FaceList
    {
        public readonly Dictionary<int, int> Indexes;
        public readonly List<int> Faces;
        public readonly List<int[]> FaceIndexes;
        public readonly List<Face> SelectionFaces;

        public FaceList()
        {
            Indexes = new Dictionary<int, int>();
            Faces = new List<int>();
            FaceIndexes = new List<int[]>();
            SelectionFaces = new List<Face>();
        }
    }
    public class PBFaceSelection : MonoBehaviour
    {
        private ProBuilderMesh _selectionMesh;
        private readonly Dictionary<ProBuilderMesh, MeshFilter> _meshToSelection = new Dictionary<ProBuilderMesh, MeshFilter>();
        private readonly Dictionary<ProBuilderMesh, Dictionary<int, Face>> _faceToSelectionFace = new Dictionary<ProBuilderMesh, Dictionary<int, Face>>();
        private readonly Dictionary<Face, ProBuilderMesh> _selectionFaceToMesh = new Dictionary<Face, ProBuilderMesh>();
        private readonly List<Vector3> _selectionVertices = new List<Vector3>();
        private readonly List<Face> _selectionFaces = new List<Face>();
        private readonly Dictionary<ProBuilderMesh, FaceList> _meshToFaces = new Dictionary<ProBuilderMesh, FaceList>();
        private readonly List<PBMesh> _pbMeshes = new List<PBMesh>();
        private readonly Dictionary<Edge, HashSet<Edge>> _coincidentEdges = new Dictionary<Edge, HashSet<Edge>>();
        private bool _isChanging;

        [SerializeField]
        [FormerlySerializedAs("m_color")]
        private Color _color = Color.yellow;
        [SerializeField]
        private Color edgeColor = new Color(0.33f, 0.33f, 0.33f);
        private Material _material,edgeMaterial;

        private Vector3 _lastPosition;
        public Vector3 LastPosition
        {
            get { return transform.TransformPoint(_lastPosition); }
        }

        [FormerlySerializedAs("m_lastNormal")]
        public Vector3 _lastNormal = Vector3.forward;
        public Vector3 LastNormal
        {
            get { return transform.TransformDirection(_lastNormal.normalized); }
        }

        private ProBuilderMesh _lastMesh;
        public ProBuilderMesh LastMesh
        {
            get { return _lastMesh; }
        }

        private Vector3 _centerOfMass;
        public Vector3 CenterOfMass
        {
            get { return transform.TransformPoint(_centerOfMass); }
        }

        public int FacesCount
        {
            get { return _selectionFaces.Count; }
        }

        public int MeshesCount
        {
            get { return _meshToFaces.Count; }
        }

        public IEnumerable<ProBuilderMesh> Meshes
        {
            get { return _meshToFaces.Keys; }
        } 

        public IEnumerable<PBMesh> PBMeshes
        {
            get { return _pbMeshes; }
        }

        private PBBaseEditor _editor;

        private MeshRenderer _renderer;
        public bool IsRendererEnabled
        {
            get { return _renderer.enabled; }
            set { _renderer.enabled = value; }
        }
        [SerializeField]
        [FormerlySerializedAs("m_scale")]
        private float _scale = 1.0f;

        [SerializeField]
        [FormerlySerializedAs("m_zTest")]
        private CompareFunction _zTest = CompareFunction.LessEqual;
        private void Awake()
        {
            _editor = GetComponent<PBBaseEditor>();

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if(meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            meshFilter.sharedMesh = new Mesh();

            _renderer = gameObject.GetComponent<MeshRenderer>();
            if(_renderer == null)
            {
                _renderer = gameObject.AddComponent<MeshRenderer>();
            }

            _material = new Material(Shader.Find("CodingDaniel/MEBuilder/FaceHighlight"));
            _material.SetFloat("_Dither", 0.0f);
            _material.SetInt("_HandleZTest", (int)CompareFunction.LessEqual);
            _material.color = new Color(_color.r, _color.g, _color.b, 0.5f);

            _renderer.sharedMaterial = _material;

            gameObject.AddComponent<PBMesh>();
            gameObject.layer = _editor.GraphicsLayer;
            _selectionMesh = GetComponent<ProBuilderMesh>();   
            
            edgeMaterial = new Material(PBBuiltinMaterials.LinesMaterial);
            edgeMaterial.SetColor("_Color", Color.white);
            edgeMaterial.SetInt("_HandleZTest", (int)_zTest);
            edgeMaterial.SetFloat("_Scale", _scale);
        }

        private void OnDestroy()
        {
            if(_material != null)
            {
                Destroy(_material);
            }
            _selectionMesh = null;
            Clear();
        }

        public bool IsSelected(ProBuilderMesh mesh, int face)
        {
            Dictionary<int, Face> faceToSelection;
            if(!_faceToSelectionFace.TryGetValue(mesh, out faceToSelection))
            {
                return false;
            }

            return faceToSelection.ContainsKey(face);
        }

        public void BeginChange()
        {
            _isChanging = true;
        }

        public void EndChange()
        {
            if(_isChanging)
            {
                RebuildSelectionMesh();
                _isChanging = false;
            }
        }

        public void Add(ProBuilderMesh mesh, int faceIndex)
        {
            Face face = mesh.faces[faceIndex];

            Dictionary<int, Face> faceToSelection;
            if (_faceToSelectionFace.TryGetValue(mesh, out faceToSelection))
            {
                if (faceToSelection.ContainsKey(faceIndex))
                {
                    return;
                }
            }
            else
            {
                faceToSelection = new Dictionary<int, Face>();
                _faceToSelectionFace.Add(mesh, faceToSelection);
            }
            

            int[] indices = new int[face.indexes.Count];
            for(int i = 0; i < indices.Length; ++i)
            {
                indices[i] = _selectionVertices.Count + i;
            }

            Face selectionFace = new Face();
            selectionFace.SetIndexes(indices);
            faceToSelection.Add(faceIndex, selectionFace);
            _selectionFaceToMesh.Add(selectionFace, mesh);

            IList<int> indexes = face.indexes;
            Vertex[] vertices = mesh.GetVertices(indexes);
            for(int i = 0; i < vertices.Length; ++i)
            {
                _selectionVertices.Add(transform.InverseTransformPoint(mesh.transform.TransformPoint(vertices[i].position)));
            }

            _selectionFaces.Add(selectionFace);
            if(!_isChanging)
            {
                RebuildSelectionMesh();
            }
            
            FaceList faceList;
            if(!_meshToFaces.TryGetValue(mesh, out faceList))
            {
                faceList = new FaceList();
                _meshToFaces.Add(mesh, faceList);

                PBMesh pbMesh = mesh.GetComponent<PBMesh>();
                if(pbMesh != null)
                {
                    pbMesh.RaiseSelected(false);
                }
                _pbMeshes.Add(pbMesh);
            }

            for (int i = 0; i < indexes.Count; ++i)
            {
                int index = indexes[i];
                if (!faceList.Indexes.ContainsKey(index))
                {
                    faceList.Indexes.Add(index, 1);
                }
                else
                {
                    faceList.Indexes[index]++;
                }
            }

            faceList.Faces.Add(faceIndex);
            faceList.FaceIndexes.Add(face.indexes.ToArray());
            faceList.SelectionFaces.Add(selectionFace);

            _lastMesh = mesh;
            _lastPosition = GetCenterOfMass(selectionFace);
            _lastNormal = GetNormal(selectionFace);

            if (_selectionFaces.Count == 1)
            {
                _centerOfMass = _lastPosition;
            }
            else
            {
                _centerOfMass *= (_selectionFaces.Count - 1) / (float)_selectionFaces.Count;
                _centerOfMass += _lastPosition / _selectionFaces.Count;
            }

            EdgeVisualization(mesh);
        }

        void EdgeVisualization(ProBuilderMesh mesh)
        {
            MeshFilter edgesSelection;
            
            if (!_meshToSelection.TryGetValue(mesh, out edgesSelection))
            {
                edgesSelection = CreateEdgesGameObject(mesh);
                edgesSelection.transform.SetParent(mesh.transform, false);
                _meshToSelection.Add(mesh, edgesSelection);
                PBMesh pbMesh = mesh.GetComponent<PBMesh>();
                if (pbMesh != null)
                {
                    pbMesh.RaiseSelected(true);
                }
                _pbMeshes.Add(pbMesh);
                // Debug.Log("New mesh to the selection");
            }
        }
        public void Remove(ProBuilderMesh mesh, int faceIndex)
        {
            // _meshToSelection.Clear();
            
            Face selectionFace;
            Dictionary<int, Face> faceToSelection;
            if (_faceToSelectionFace.TryGetValue(mesh, out faceToSelection))
            {
                if (!faceToSelection.TryGetValue(faceIndex, out selectionFace))
                {
                    return;
                }
            }
            else
            {
                return;
            }

            Face face = mesh.faces[faceIndex];

            faceToSelection.Remove(faceIndex);
            if(faceToSelection.Count == 0)
            {
                _faceToSelectionFace.Remove(mesh);
            }

            _selectionFaceToMesh.Remove(selectionFace);

            FaceList faceList = _meshToFaces[mesh];
            IList<int> indexes = face.indexes;
            for (int i = 0; i < indexes.Count; ++i)
            {
                int index = indexes[i];
                if (faceList.Indexes.ContainsKey(index))
                {
                    faceList.Indexes[index]--;
                    if (faceList.Indexes[index] == 0)
                    {
                        faceList.Indexes.Remove(index);
                    }
                }
            }

            
            int flidx = faceList.Faces.IndexOf(faceIndex);
            faceList.Faces.RemoveAt(flidx);
            faceList.FaceIndexes.RemoveAt(flidx);
            faceList.SelectionFaces.Remove(selectionFace);
            
            if(faceList.Faces.Count == 0)
            {
                if (_meshToSelection.TryGetValue(mesh, out var filter))
                {
                    if (filter != null) Destroy(filter.gameObject);
                    _meshToSelection.Remove(mesh);
                }
                _meshToFaces.Remove(mesh);

                PBMesh pbMesh = mesh.GetComponent<PBMesh>();
                if(pbMesh != null)
                {
                    pbMesh.RaiseUnselected();
                }
                
                _pbMeshes.Remove(pbMesh);
            }

            Vector3 removedFaceCenterOfMass = GetCenterOfMass(selectionFace);
            int[] indices = selectionFace.distinctIndexes.OrderByDescending(i => i).ToArray();
            for(int i = 0; i < indices.Length; ++i)
            {
                _selectionVertices.RemoveAt(indices[i]);
            }

            int selectionFaceIndex = _selectionFaces.IndexOf(selectionFace);
            int count = selectionFace.indexes.Count;

            _selectionFaces.RemoveAt(selectionFaceIndex);
            for(int i = selectionFaceIndex; i < _selectionFaces.Count; ++i)
            {
                _selectionFaces[i].ShiftIndexes(-count);
            }

            if(!_isChanging)
            {
                RebuildSelectionMesh();
            }

            if(_selectionFaces.Count == 0)
            {
                _centerOfMass = Vector3.zero;
                _lastPosition = Vector3.zero;
                _lastNormal = Vector3.forward;
                _lastMesh = null;
            }
            else if (_selectionFaces.Count == 1)
            {
                _centerOfMass = GetCenterOfMass(_selectionFaces[0]);
                _lastPosition = _centerOfMass;
                _lastNormal = GetNormal(_selectionFaces[0]);
                _lastMesh = _selectionFaceToMesh[_selectionFaces[0]];
            }
            else
            {
                _centerOfMass -= removedFaceCenterOfMass / (_selectionFaces.Count + 1);
                _centerOfMass *= (_selectionFaces.Count + 1) / (float)_selectionFaces.Count;
                _lastPosition = GetCenterOfMass(_selectionFaces.Last());
                _lastNormal = GetNormal(_selectionFaces.Last());
                _lastMesh = _selectionFaceToMesh[_selectionFaces.Last()];
            }
        }

        public void Clear()
        {
            for(int i = 0; i < _pbMeshes.Count; ++i)
            {
                PBMesh pbMesh = _pbMeshes[i];
                
                ProBuilderMesh mesh = pbMesh.GetComponent<ProBuilderMesh>();
                if (mesh == null) continue;
                if (_meshToSelection.TryGetValue(mesh, out var filter))
                {
                    if(filter != null)
                    {
                        Destroy(filter.gameObject);
                    }
                }
                
                if(pbMesh != null)
                {
                    pbMesh.RaiseUnselected();
                }
            }

            _meshToSelection.Clear();
            _selectionVertices.Clear();
            _selectionFaces.Clear();
            _faceToSelectionFace.Clear();
            _selectionFaceToMesh.Clear();
            _meshToFaces.Clear();
            _pbMeshes.Clear();
            RebuildSelectionMesh();
        }

        public Vector3 GetCenterOfMass()
        {
            Vector3 centerOfMass = GetCenterOfMass(_selectionFaces[0]);
            for(int i = 1; i < _selectionFaces.Count; ++i)
            {
                Face face = _selectionFaces[i];
                centerOfMass += GetCenterOfMass(face);
            }

            return centerOfMass / _selectionFaces.Count;
        }

        private Vector3 GetCenterOfMass(Face face)
        {
            IList<int> indexes = face.indexes;
            Vector3 result = _selectionVertices[indexes[0]];
            for(int i = 1; i < indexes.Count; ++i)
            {
                result += _selectionVertices[indexes[i]];
            }
            result /= indexes.Count;
            return result;
        }

        private Vector3 GetNormal(Face face)
        {
            IList<int> indexes = face.indexes;

            return Math.Normal(
                _selectionVertices[indexes[0]],
                _selectionVertices[indexes[1]],
                _selectionVertices[indexes[2]]);
        }

        public IList<int> GetFaces(ProBuilderMesh mesh)
        {
            FaceList faces;
            if(_meshToFaces.TryGetValue(mesh, out faces))
            {
                return faces.Faces;
            }

            return new int[0];
        }

        public IEnumerable<int> GetIndexes(ProBuilderMesh mesh)
        {
            return _meshToFaces[mesh].Indexes.Keys;
        }

        public void Synchronize(Vector3 centerOfMass, Vector3 lastPosition)
        {
            foreach (KeyValuePair<ProBuilderMesh, FaceList> kvp in _meshToFaces)
            {
                ProBuilderMesh mesh = kvp.Key;
                FaceList faces = kvp.Value;

                for (int f = 0; f < faces.Faces.Count; ++f)
                {
                    int[] faceIndexes = faces.FaceIndexes[f];
                    Face selectionFace = faces.SelectionFaces[f];

                    Vertex[] vertices = mesh.GetVertices(faceIndexes);
                    IList<int> selectionIndexes = selectionFace.indexes;
                    for (int i = 0; i < vertices.Length; ++i)
                    {
                        int selectionIndex = selectionIndexes[i];
                        _selectionVertices[selectionIndex] =
                            transform.InverseTransformPoint(
                                mesh.transform.TransformPoint(vertices[i].position));
                    }
                }
                if (_meshToSelection.TryGetValue(mesh, out var meshFilter))
                {
                    if (meshFilter != null)
                    {
                        BuildEdgeMesh(mesh, meshFilter.sharedMesh, true);

                        meshFilter.transform.position = mesh.transform.position;
                    }
                }
            }

            _centerOfMass = transform.InverseTransformPoint(centerOfMass);
            _lastPosition = transform.InverseTransformPoint(lastPosition);
            _lastNormal = _selectionFaces.Count == 0 ? Vector3.forward : GetNormal(_selectionFaces[^1]); // transform.InverseTransformDirection(lastNormal.normalized);
            RebuildSelectionMesh();
        }

        private void RebuildSelectionMesh()
        {
            if(_selectionMesh == null)
            {
                return;
            }
            _selectionMesh.RebuildWithPositionsAndFaces(_selectionVertices, _selectionFaces);
            _selectionMesh.ToMesh();
            _selectionMesh.Refresh();
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
            renderer.sharedMaterial = edgeMaterial;
            FindCoincidentEdges(mesh);
            BuildEdgeMesh(mesh, meshFilter.sharedMesh, false);
            // CopyTransform(mesh.transform, meshFilter.transform);
            return meshFilter;
        }
        
        void FindCoincidentEdges(ProBuilderMesh mesh)
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
            }

            for (int i = 0; i < faceCount && edgeIndex < edgeCount; i++)
            {
                ReadOnlyCollection<Edge> edges = faces[i].edges;
                for (int n = 0; n < edges.Count && edgeIndex < edgeCount; n++)
                {
                    Edge edge = edges[n];

                    int positionIndex = edgeIndex * 2;

                    if(vertices.Length > positionIndex)
                    {
                        vertices[positionIndex + 0] = positions[edge.a];
                    }
                    
                    if(vertices.Length > positionIndex + 1)
                    {
                        vertices[positionIndex + 1] = positions[edge.b];
                    }

                    if(!positionsOnly)
                    {
                        tris[positionIndex + 0] = positionIndex + 0;
                        tris[positionIndex + 1] = positionIndex + 1;
                        
                        // List<int> list;
                        // if (!_edgeToSelection.TryGetValue(edge, out list))
                        // {
                        //     list = new List<int>();
                        //     _edgeToSelection.Add(edge, list);
                        // }
                        // list.Add(positionIndex + 0);
                        // list.Add(positionIndex + 1);
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
                    colors[i] = edgeColor;
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
    }
}