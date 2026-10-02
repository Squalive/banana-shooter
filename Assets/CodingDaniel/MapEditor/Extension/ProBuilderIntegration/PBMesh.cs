using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Rendering;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public static class ProBuilderMeshOperationsExt
    {
        public static void Rebuild(this ProBuilderMesh mesh, IList<Vector3> positions, IList<Face> faces, IList<Vector2> textures)
        {
            mesh.Rebuild(positions, faces, textures, SharedVertex.GetSharedVerticesWithPositions(positions));
        }

        public static void Rebuild(this ProBuilderMesh mesh, IList<Vector3> positions, IList<Face> faces, IList<Vector2> textures, IList<SharedVertex> sharedVertices)
        {
            mesh.Clear();
            mesh.positions = positions;
            mesh.faces = faces;
            mesh.textures = textures;
            mesh.sharedVertices = sharedVertices;
            mesh.ToMesh();
            mesh.Refresh();
        }
    }

    public struct PBEdge
    {
        public int A;
        public int B;
        public int FaceIndex;

        internal PBEdge(Edge edge, int faceIndex)
        {
            A = edge.a;
            B = edge.b;
            FaceIndex = faceIndex;
        }

        public PBEdge(int a, int b, int faceIndex = -1)
        {
            A = a;
            B = b;
            FaceIndex = faceIndex;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PBEdge))
            {
                return false;
            }

            PBEdge other = (PBEdge)obj;
            return other.A == A && other.B == B;
        }

        public override int GetHashCode()
        {
            int hashcode = 23;
            hashcode = (hashcode * 37) + A;
            hashcode = (hashcode * 37) + B;
            return hashcode;
        }
    }

    [Serializable]
    public class PBFace
    {
        public int[] Indexes;
        public int SubmeshIndex;
        public int TextureGroup;
        public int SmoothingGroup;
        public bool IsManualUV;
        public PBAutoUnwrapSettings UnwrapSettings;

        public PBFace(Face face, bool recordUnwrapSettings)
        {
            Indexes = face.indexes.ToArray();
            SubmeshIndex = face.submeshIndex;
            TextureGroup = face.textureGroup;
            SmoothingGroup = face.smoothingGroup;
            IsManualUV = face.manualUV;
            if (recordUnwrapSettings)
            {   
                UnwrapSettings = face.uv;
            }
            else
            {
                UnwrapSettings = null;
            }
        }

        public PBFace()
        {
            
        }

        public Face ToFace()
        {
            Face face = new Face();
            face.SetIndexes(Indexes);
            face.submeshIndex = SubmeshIndex;
            face.smoothingGroup = SmoothingGroup;
            if(UnwrapSettings != null)
            {
                face.textureGroup = TextureGroup;
                face.uv = UnwrapSettings;
                face.manualUV = IsManualUV;
            }
            else
            {
                face.manualUV = IsManualUV;
            }
            return face;
        }
    }

    public delegate void PBMeshEvent();
    public delegate void PBMeshEvent<T>(T arg);
    public delegate void PBMeshEvent<T1, T2>(T1 arg1, T2 arg2);
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class PBMesh : MonoBehaviour
    {
        public static event PBMeshEvent<PBMesh> Initialized;
        public static event PBMeshEvent<PBMesh> Destroyed;
        
        public event PBMeshEvent<bool> Selected;
        public event PBMeshEvent Unselected;
        public event PBMeshEvent<bool, bool> Changed;
        
        private ProBuilderMesh _pbMesh;
        private MeshFilter _meshFilter;

        internal ProBuilderMesh ProBuilderMesh
        {
            get { return _pbMesh; }
        }

        public Mesh Mesh
        {
            get { return _meshFilter.sharedMesh; }
        }

        private PBFace[] _faces;
        public PBFace[] Faces
        {
            get
            {
                Init(this, Vector2.one);
                _faces = _pbMesh.faces.Select(f => new PBFace(f, true)).ToArray();
                return _faces;
            }
            set 
            {
                _faces = value; 
                if(_pbMesh != null)
                {
                    _pbMesh.faces = _faces != null ? _faces.Select(f => f.ToFace()).ToArray() : null;
                }
            }
        }

        public PBEdge[] Edges
        {
            get
            {
                Init(this, Vector2.one);
                List<PBEdge> edges = new List<PBEdge>();
                IList<Face> faces = _pbMesh.faces;
                for(int i = 0; i < faces.Count; ++i)
                {
                    ReadOnlyCollection<Edge> faceEdges = faces[i].edges;
                    for(int j = 0; j < faceEdges.Count; ++j)
                    {
                        edges.Add(new PBEdge(faceEdges[j], i));
                    }
                }
                return edges.ToArray();
            }
        }

        private Vector3[] _positions;
        public Vector3[] Positions
        {
            get
            {
                Init(this, Vector2.one);
                _positions = _pbMesh.positions.ToArray();
                return _positions;
            }
            set 
            {
                _positions = value; 
                if(_pbMesh != null)
                {
                    _pbMesh.positions = _positions;
                }
            }
        }

        private Vector2[] _textures;
        public Vector2[] Textures
        {
            get
            {
                Init(this, Vector2.one);
                _textures = _pbMesh.textures.ToArray();
                return _textures;
            }
            set
            {
                _textures = value;
                if(_pbMesh != null)
                {
                    _pbMesh.textures = _textures;
                }
            }
        }

        //internal ProBuilderMesh Mesh
        //{
        //    get { return _pbMesh; }
        //}

        public bool IsMarkedAsDestroyed
        {
            get;
            private set;
        }

        private void Awake()
        {
            Init(this, Vector2.one);
        }

        public static void Init(PBMesh mesh, Vector2 scale)
        {
            if(mesh._pbMesh != null)
            {
                return;
            }

            mesh._meshFilter = mesh.GetComponent<MeshFilter>();
            Mesh sourceMesh = mesh._meshFilter.sharedMesh;

            mesh._pbMesh = mesh.GetComponent<ProBuilderMesh>();
            if (mesh._pbMesh == null)
            {
                mesh._pbMesh = mesh.gameObject.AddComponent<ProBuilderMesh>();
                if (mesh._positions != null)
                {
                    Face[] faces = mesh._faces.Select(f => f.ToFace()).ToArray();
                    mesh._pbMesh.Rebuild(mesh._positions, faces, mesh._textures);

                    IList<Face> actualFaces = mesh._pbMesh.faces;
                    for (int i = 0; i < actualFaces.Count; ++i)
                    {
                        actualFaces[i].submeshIndex = mesh._faces[i].SubmeshIndex;
                    }

                    mesh._pbMesh.Refresh();
                    mesh._pbMesh.ToMesh();
                    
                    
                }
                else
                {
                    ImportMesh(sourceMesh, mesh._meshFilter, mesh._pbMesh, scale);
                }

                if(Initialized != null)
                {
                    Initialized(mesh);
                }
            }

            List<Color> colors = new List<Color>();

            for (int i = 0; i < mesh._pbMesh.positions.Count; i++)
            {
                colors.Add(Color.white);
            }

            mesh._pbMesh.colors = colors;
            //GK 10-25-2021 Always Center Pivot of new Probuilderized objects
            //mesh._pbMesh.CenterPivot(null);
        }

        public void DestroyImmediate()
        {
            if (Destroyed != null)
            {
                Destroyed(this);
            }
            DestroyImmediate(this);
        }

        private void OnDestroy()
        {
            if(_pbMesh != null)
            {
                Destroy(_pbMesh);
                _pbMesh = null;  
            }   
        }

        public void OnMarkAsDestroyed()
        {
            IsMarkedAsDestroyed = true;
        }

        public void OnMarkAsRestored()
        {
            IsMarkedAsDestroyed = false;
        }

        public MeshState GetState(bool recordUV)
        {
            return new MeshState(_pbMesh.positions.ToArray(), _pbMesh.faces.ToArray(), _pbMesh.textures.ToArray(), recordUV);
        }

        public void SetState(MeshState state)
        {
            _pbMesh.Rebuild(state.Positions, state.Faces.Select(f => f.ToFace()).ToArray(), state.Textures);
            RaiseChanged(false, true);
        }

        public bool CreateShapeFromPolygon(IList<Vector3> points, float extrude, bool flipNormals)
        {    
            ActionResult result = _pbMesh.CreateShapeFromPolygon(points, extrude, flipNormals);
            RaiseChanged(false, true);
            return result.ToBool();
        }

        public void Subdivide()
        {
            _pbMesh.Connect(_pbMesh.faces);
            _pbMesh.Refresh();
            _pbMesh.ToMesh();
            
            RaiseChanged(false, true);
        }

        public void CenterPivot()
        {
            _pbMesh.CenterPivot(null);

            RaiseChanged(false, true);
        }

        public void Clear()
        {
            _pbMesh.Clear();
            _pbMesh.Refresh();
            _pbMesh.ToMesh();

            MeshFilter filter = _pbMesh.GetComponent<MeshFilter>();
            filter.sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.zero);

            RaiseChanged(false, true);
        }

        public void Refresh(bool createMesh = true)
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if(filter != null)
            {
#if PROBUILDER_4_4_0_OR_NEWER
                //Do not allow probuilder 4.4.0 to re-use same mesh instance.
                //Required to duplicate object with ProBuilderMesh correctly.
                PropertyInfo meshProperty = _pbMesh.GetType().GetProperty("mesh", BindingFlags.NonPublic | BindingFlags.Instance);
                if(meshProperty == null)
                {
                    Debug.LogWarning("Unable to find mesh property");
                }
                else
                {
                    meshProperty.SetValue(_pbMesh, null);
                }
#endif
                if(createMesh)
                {
                    filter.sharedMesh = new Mesh();
                }
                _pbMesh.ToMesh();
                _pbMesh.Refresh();
            }

            RaiseChanged(false, true);
        }

        public void RefreshUV()
        {
            _pbMesh.textures = _textures;
            _pbMesh.Refresh(RefreshMask.UV);

            _pbMesh.ToMesh();
            _pbMesh.Refresh();
        }

        public void RaiseSelected(bool clear)
        {
            if(Selected != null)
            {
                Selected(clear);
            }
        }

        public void RaiseChanged(bool positionsOnly, bool forceUpdate)
        {
            if(Changed != null)
            {
                Changed(positionsOnly, forceUpdate);
            }
        }

        public void RaiseUnselected()
        {
            if(Unselected != null)
            {
                Unselected(); 
            }
        }

        public void BuildEdgeMesh(Mesh target, Color color, bool positionsOnly)
        {
            IList<Vector3> positions = _pbMesh.positions;

            int edgeIndex = 0;
            int edgeCount = 0;
            int faceCount = _pbMesh.faceCount;

            IList<Face> faces = _pbMesh.faces;
            for (int i = 0; i < faceCount; i++)
            {
                edgeCount += faces[i].edges.Count;
            }
            edgeCount = System.Math.Min(edgeCount, int.MaxValue / 2 - 1);

            int[] tris;
            Vector3[] vertices;
            if (positionsOnly)
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
                    
                    if (!positionsOnly)
                    {
                        tris[positionIndex + 0] = positionIndex + 0;
                        tris[positionIndex + 1] = positionIndex + 1;
                    }

                    edgeIndex++;
                }
            }

            if (!positionsOnly)
            {
                target.Clear();
                if(vertices.Length > ushort.MaxValue)
                {
                    target.indexFormat = IndexFormat.UInt32;
                }
                target.name = "EdgeMesh" + target.GetInstanceID();
                target.vertices = vertices.ToArray();
                Color[] colors = new Color[target.vertexCount];
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = color;
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

        public static PBMesh ProBuilderize(GameObject gameObject, bool hierarchy, bool localScaleToUvScale = false)
        {
            Vector3 scale = Vector3.one;
            if(localScaleToUvScale)
            {
                scale = gameObject.transform.localScale;
                float minScale = Mathf.Min(scale.x, scale.y, scale.z);
                scale = new Vector3(minScale, minScale);
            }
                
            return ProBuilderize(gameObject, hierarchy, scale);
        }

        public static PBMesh ProBuilderize(GameObject gameObject, bool hierarchy, Vector2 uvScale)
        {
            bool wasActive = false;
            if(uvScale != Vector2.one)
            {
                wasActive = gameObject.activeSelf;
                gameObject.SetActive(false);
            }
            
            if(hierarchy)
            {
                MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
                for(int i = 0; i < meshFilters.Length; ++i)
                {
                    if(meshFilters[i].GetComponent<PBMesh>() == null)
                    {
                        PBMesh pbMesh = meshFilters[i].gameObject.AddComponent<PBMesh>();
                        Init(pbMesh, uvScale);
                    }
                }

                if (uvScale != Vector2.one)
                {
                    gameObject.SetActive(wasActive);
                }

                return gameObject.GetComponent<PBMesh>();
            }
            else
            {
                PBMesh mesh = gameObject.GetComponent<PBMesh>();
                if (mesh != null)
                {
                    if (uvScale != Vector2.one)
                    {
                        gameObject.SetActive(wasActive);
                    }
                    return mesh;
                }

                mesh = gameObject.AddComponent<PBMesh>();
                Init(mesh, uvScale);
                if (uvScale != Vector2.one)
                {
                    gameObject.SetActive(wasActive);
                }
                return mesh;
            }
        }

        [Obsolete]
        public static void ImportMesh(ProBuilderMesh mesh)
        {
            MeshFilter filter = mesh.GetComponent<MeshFilter>();
            ImportMesh(filter.sharedMesh, filter, mesh, Vector2.one);
        }


        public static void ImportMesh(Mesh sourceMesh, ProBuilderMesh mesh)
        {
            MeshFilter filter = mesh.GetComponent<MeshFilter>();
            ImportMesh(sourceMesh, filter, mesh, Vector2.one);
        }


        private static readonly MeshImportSettings _defaultImportSettings = new MeshImportSettings() { smoothing = false };
        private static void ImportMesh(Mesh sourceMesh, MeshFilter filter, ProBuilderMesh mesh, Vector2 uvScale)
        {
            Renderer renderer = mesh.GetComponent<Renderer>();
            if (filter.sharedMesh == null)
            {
                filter.sharedMesh = new Mesh();
            }
            GameObject go = filter.gameObject;
            Material[] sourceMaterials = go.GetComponent<MeshRenderer>()?.sharedMaterials;

            try
            {
                var meshImporter = new MeshImporter(sourceMesh, sourceMaterials, mesh);
                meshImporter.Import(_defaultImportSettings);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed ProBuilderizing: " + go.name + "\n" + e.ToString());
            }
            int submeshCount = sourceMesh.subMeshCount;
            
            Dictionary<int, List<Face>> submeshIndexToFace = new Dictionary<int, List<Face>>();
            for(int i = 0; i < submeshCount; ++i)
            {
                submeshIndexToFace.Add(i, new List<Face>());
            }

            IList<Face> faces = mesh.faces;
            if(uvScale != Vector2.one)
            {
                AutoUnwrapSettings uv = AutoUnwrapSettings.defaultAutoUnwrapSettings;
                uv.scale = uvScale;
                for (int i = 0; i < mesh.faceCount; ++i)
                {
                    Face face = faces[i];
                    face.uv = uv;
                    //face.manualUV = true;
                    submeshIndexToFace[face.submeshIndex].Add(face);
                }
            }
            else
            {
                for (int i = 0; i < mesh.faceCount; ++i)
                {
                    Face face = faces[i];
                    //face.manualUV = true;
                    submeshIndexToFace[face.submeshIndex].Add(face);
                }
            }

            filter.sharedMesh = new Mesh();
            mesh.ToMesh();
            mesh.Refresh();

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < submeshCount && i < materials.Length; ++i)
            {
                List<Face> submeshFaces = submeshIndexToFace[i];
                Material material = materials[i];
                
                if (material != null)
                {
                    mesh.SetMaterial(submeshFaces, material);
                }
            }

            mesh.ToMesh();
            mesh.Refresh();
        }

        public bool UvTo3D(Vector2 uv, out Vector3 p3d)
        {
            Mesh mesh = _meshFilter.sharedMesh;
            int[] tris = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector2 u1 = uvs[tris[i]]; // get the triangle UVs
                Vector2 u2 = uvs[tris[i + 1]];
                Vector3 u3 = uvs[tris[i + 2]];
                // calculate triangle area - if zero, skip it
                float a = Area(u1, u2, u3);
                if (a == 0)
                {
                    continue;
                }
                // calculate barycentric coordinates of u1, u2 and u3
                // if anyone is negative, point is outside the triangle: skip it
                float a1 = Area(u2, u3, uv) / a;
                if (a1 < 0)
                {
                    continue;
                }

                float a2 = Area(u3, u1, uv) / a;
                if (a2 < 0)
                {
                    continue;
                }
                float a3 = Area(u1, u2, uv) / a;
                if (a3 < 0)
                {
                    continue;
                }
                // point inside the triangle - find mesh position by interpolation...
                p3d = a1 * verts[tris[i]] + a2 * verts[tris[i + 1]] + a3 * verts[tris[i + 2]];
                // and return it in world coordinates:
                p3d = transform.TransformPoint(p3d);
                return true;
            }
            // point outside any uv triangle: return Vector3.zero
            p3d = Vector3.zero;
            return false;
        }

        // calculate signed triangle area using a kind of "2D cross product":
        private float Area(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 v1 = p1 - p3;
            Vector2 v2 = p2 - p3;
            return (v1.x* v2.y - v1.y* v2.x)/2;
        }

        //GK add AutoUnwrapSettings
        public void SetAutoUnwrapSettings(AutoUnwrapSettings aus)
        {
            if (_pbMesh != null)
            {
                _pbMesh.textures = _textures;
                _pbMesh.Refresh(RefreshMask.UV);

                IList<Face> faces = _pbMesh.faces;
                for (int i = 0; i < _pbMesh.faceCount; ++i)
                {
                    Face face = faces[i];
                    aus.anchor = AutoUnwrapSettings.Anchor.LowerLeft;
                    face.uv = aus;
                    face.manualUV = false;
                }
                _pbMesh.ToMesh();
                _pbMesh.Refresh();
            }
        }

        public List<MapData.MeshObjectData.MaterialIndex> GetMaterialIndex()
        {
            List<MapData.MeshObjectData.MaterialIndex> materialIndexes = new List<MapData.MeshObjectData.MaterialIndex>();

            foreach (var material in GetComponent<MeshRenderer>().sharedMaterials)
            {
                int i = 0;
                bool flag = false;
                var id = material.GetInstanceID();
                
                foreach (var m in MapSaver.Instance.materials)
                {
                    if (m.GetInstanceID() ==id )
                    {
                        materialIndexes.Add(new MapData.MeshObjectData.MaterialIndex(false,i));
                        flag = true;
                        break;
                    }

                    i++;
                }

                if (!flag)
                {
                    i = 0;
                    foreach (var m in MapSaver.Instance.externalMaterials)
                    {
                        if (m.Item4.GetInstanceID() ==id )
                        {
                            materialIndexes.Add(new MapData.MeshObjectData.MaterialIndex(true,i));
                            flag = true;
                            break;
                        }

                        i++;
                    }
                }
            }

            return materialIndexes;
        }
    }
}
