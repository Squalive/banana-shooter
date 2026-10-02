using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public enum MeshEditorSelectionMode
    {
        Add,
        Substract,
        Difference
    }

    public class MeshEditorState
    {
        internal readonly Dictionary<GameObject, MeshState> State = new Dictionary<GameObject, MeshState>();

        public IEnumerable<PBMesh> GetMeshes()
        {
            return State.Select(kvp => kvp.Key.GetComponent<PBMesh>()).Where(pbMesh => pbMesh != null);
        }

        public void Apply()
        {
            foreach (KeyValuePair<GameObject, MeshState> kvp in State)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                if(mesh == null)
                {
                    continue;
                }
                MeshState meshState = kvp.Value;
                mesh.Rebuild(meshState.Positions, meshState.Faces.Select(f => f.ToFace()).ToArray(), meshState.Textures);
            }
        }
    }

    public class MeshState
    {
        public readonly IList<Vector3> Positions;
        public readonly IList<PBFace> Faces;
        public readonly IList<Vector2> Textures;

        public MeshState(IList<Vector3> positions, IList<Face> faces, IList<Vector2> textures, bool recordUV)
        {
            Positions = positions;
            Faces = faces.Select(f => new PBFace(f, recordUV)).ToList();
            Textures = textures;
        }
    }

    public static class ProBuilderExt
    {
        public static void GetFaces(this ProBuilderMesh mesh, IList<int> faceIndexes, IList<Face> faces)
        {
            IList<Face> allFaces = mesh.faces;
            for(int i = 0; i < faceIndexes.Count; ++i)
            {
                Face face = allFaces[faceIndexes[i]];
                faces.Add(face);
            }
        }

        /// <summary>
        /// This method required to fix "invalid" behavior of ProBuilder 4.4.0 where MeshCollider does not work after ProBuilderMesh update
        /// </summary>
        /// <param name="mesh"></param>
        internal static void RefereshColliders(this ProBuilderMesh mesh)
        {
            foreach(MeshCollider collider in mesh.GetComponents<MeshCollider>())
            {
                collider.enabled = false;
                collider.enabled = true;
            }
        }

        internal static void RefreshColliders(this IEnumerable<ProBuilderMesh> meshes)
        {
            foreach(ProBuilderMesh mesh in meshes)
            {
                mesh.RefereshColliders();
            }
        }
    }

    public class MeshSelection
    {
        internal Dictionary<GameObject, IList<int>> SelectedFaces = new Dictionary<GameObject, IList<int>>();
        internal Dictionary<GameObject, IList<int>> UnselectedFaces = new Dictionary<GameObject, IList<int>>();

        internal Dictionary<GameObject, IList<Edge>> SelectedEdges = new Dictionary<GameObject, IList<Edge>>();
        internal Dictionary<GameObject, IList<Edge>> UnselectedEdges = new Dictionary<GameObject, IList<Edge>>();

        internal Dictionary<GameObject, IList<int>> SelectedIndices = new Dictionary<GameObject, IList<int>>();
        internal Dictionary<GameObject, IList<int>> UnselectedIndices = new Dictionary<GameObject, IList<int>>();

        public bool HasFaces
        {
            get { return SelectedFaces.Count != 0 || UnselectedFaces.Count != 0; }
        }

        public bool HasEdges
        {
            get { return SelectedEdges.Count != 0 || UnselectedEdges.Count != 0; }
        }

        public bool HasVertices
        {
            get { return SelectedIndices.Count != 0 || UnselectedIndices.Count != 0; }
        }

        public IEnumerable<PBMesh> GetSelectedMeshes()
        {
            IEnumerable<GameObject> meshes = SelectedFaces.Select(kvp => kvp.Key).Union(SelectedEdges.Select(kvp => kvp.Key)).Union(SelectedIndices.Select(kvp => kvp.Key));
            return meshes.Select(m => m.GetComponent<PBMesh>()).Where(pbMesh => pbMesh != null);
        }

        public IEnumerable<int> GetFaces(PBMesh mesh)
        {
            return SelectedFaces[mesh.gameObject];
        }

        public void SetFaces(PBMesh mesh, IEnumerable<int> faces)
        {
            SelectedFaces[mesh.gameObject] = faces.ToList();
        }

        public IEnumerable<int> GetEdges(PBMesh mesh)
        {
            PBEdge[] edges = mesh.Edges;
            return SelectedEdges[mesh.gameObject].Select(e => Array.IndexOf(edges, new PBEdge(e, -1)));
        }

        public void SetEdges(PBMesh mesh, IEnumerable<PBEdge> edges)
        {
            SelectedEdges[mesh.gameObject] = edges.Select(e => new Edge(e.A, e.B)).ToArray();
        }

        public IEnumerable<int> GetIndices(PBMesh mesh)
        {
            return SelectedIndices[mesh.gameObject];
        }

        public void SetIndices(PBMesh mesh, IEnumerable<int> indices)
        {
            SelectedIndices[mesh.gameObject] = indices.ToArray();
        }

        public MeshSelection()
        {

        }

        public MeshSelection(MeshSelection selection)
        {
            SelectedFaces = selection.SelectedFaces.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            UnselectedFaces = selection.UnselectedFaces.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            SelectedEdges = selection.SelectedEdges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            UnselectedEdges = selection.UnselectedEdges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            SelectedIndices = selection.SelectedIndices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            UnselectedIndices = selection.UnselectedIndices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public MeshSelection Invert()
        {
            var temp1 = SelectedFaces;
            SelectedFaces = UnselectedFaces;
            UnselectedFaces = temp1;

            var temp2 = SelectedEdges;
            SelectedEdges = UnselectedEdges;
            UnselectedEdges = temp2;

            var temp3 = SelectedIndices;
            SelectedIndices = UnselectedIndices;
            UnselectedIndices = temp3;

            return this;
        }

        public MeshSelection ToFaces(bool invert, bool partial = false)
        {
            MeshSelection selection = new MeshSelection(this);
            if (selection.HasEdges)
            {
                selection.EdgesToFaces(invert, partial);
            }
            else if (selection.HasVertices)
            {
                selection.VerticesToFaces(invert, partial);
            }
            else
            {
                selection.SelectedEdges.Clear();
                selection.UnselectedEdges.Clear();
                selection.SelectedIndices.Clear();
                selection.UnselectedIndices.Clear();
            }
            return selection;
        }

        public MeshSelection ToEdges(bool invert, bool partial = false)
        {
            MeshSelection selection = new MeshSelection(this);
            if (selection.HasFaces)
            {
                selection.FacesToEdges(invert);
            }
            else if (selection.HasVertices)
            {
                selection.VerticesToEdges(invert, partial);
            }
            else
            {
                selection.SelectedFaces.Clear();
                selection.UnselectedFaces.Clear();
                selection.SelectedIndices.Clear();
                selection.UnselectedIndices.Clear();
            }
            return selection;
        }

        public MeshSelection ToVertices(bool invert)
        {
            MeshSelection selection = new MeshSelection(this);
            if (selection.HasFaces)
            {
                selection.FacesToVertices(invert);
            }
            else if(selection.HasEdges)
            {
                selection.EdgesToVertices(invert);
            }
            else
            {
                selection.SelectedFaces.Clear();
                selection.UnselectedFaces.Clear();
                selection.SelectedEdges.Clear();
                selection.UnselectedEdges.Clear();
            }
            return selection;
        }

        private void FacesToVertices(bool invert)
        {
            SelectedIndices.Clear();
            UnselectedIndices.Clear();

            foreach(KeyValuePair<GameObject, IList<int>> kvp in invert ? UnselectedFaces : SelectedFaces)
            {
                ProBuilderMesh mesh;
                List<int> indices;
                GetCoindicentIndices(kvp, out mesh, out indices);

                SelectedIndices.Add(mesh.gameObject, indices);
            }

            foreach(KeyValuePair<GameObject, IList<int>> kvp in invert ? SelectedFaces : UnselectedFaces)
            {
                ProBuilderMesh mesh;
                List<int> indices;
                GetCoindicentIndices(kvp, out mesh, out indices);

                UnselectedIndices.Add(mesh.gameObject, indices);
            }

            SelectedEdges.Clear();
            UnselectedEdges.Clear();
            SelectedFaces.Clear();
            UnselectedFaces.Clear();
        }

        private void VerticesToFaces(bool invert, bool partial)
        {
            SelectedFaces.Clear();
            UnselectedFaces.Clear();

            foreach (KeyValuePair<GameObject, IList<int>> kvp in invert ? UnselectedIndices : SelectedIndices)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                HashSet<int> indicesHs = new HashSet<int>(mesh.GetCoincidentVertices(kvp.Value));
                List<int> faces = GetFaces(mesh, indicesHs, !partial);

                if (faces.Count > 0)
                {
                    SelectedFaces.Add(mesh.gameObject, faces);
                }
            }

            foreach (KeyValuePair<GameObject, IList<int>> kvp in invert ? SelectedIndices : UnselectedIndices)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                HashSet<int> indicesHs = new HashSet<int>(mesh.GetCoincidentVertices(kvp.Value));
                List<int> faces = GetFaces(mesh, indicesHs, !partial);

                if (faces.Count > 0)
                {
                    UnselectedFaces.Add(mesh.gameObject, faces);
                }
            }


            SelectedEdges.Clear();
            UnselectedEdges.Clear();
            SelectedIndices.Clear();
            UnselectedIndices.Clear();
        }

        private void FacesToEdges(bool invert)
        {
            SelectedEdges.Clear();
            UnselectedEdges.Clear();

            foreach (KeyValuePair<GameObject, IList<int>> kvp in invert ? UnselectedFaces : SelectedFaces)
            {
                ProBuilderMesh mesh;
                HashSet<Edge> edgesHs;
                GetEdges(kvp, out mesh, out edgesHs);
                SelectedEdges.Add(mesh.gameObject, edgesHs.ToArray());
            }

            foreach (KeyValuePair<GameObject, IList<int>> kvp in invert ? SelectedFaces : UnselectedFaces)
            {
                ProBuilderMesh mesh;
                HashSet<Edge> edgesHs;
                GetEdges(kvp, out mesh, out edgesHs);
                UnselectedEdges.Add(mesh.gameObject, edgesHs.ToArray());
            }


            SelectedFaces.Clear();
            UnselectedFaces.Clear();
            SelectedIndices.Clear();
            UnselectedIndices.Clear();
        }

        private void EdgesToFaces(bool invert, bool partial)
        {
            SelectedFaces.Clear();
            UnselectedFaces.Clear();

            foreach (KeyValuePair<GameObject, IList<Edge>> kvp in invert ? UnselectedEdges : SelectedEdges)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                HashSet<Edge> edgesHs = new HashSet<Edge>(kvp.Value);
                List<int> faces = GetFaces(mesh, edgesHs, !partial);

                if (faces.Count > 0)
                {
                    SelectedFaces.Add(mesh.gameObject, faces);
                }
            }

            foreach (KeyValuePair<GameObject, IList<Edge>> kvp in invert ? SelectedEdges : UnselectedEdges)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                HashSet<Edge> edgesHs = new HashSet<Edge>(kvp.Value);
                List<int> faces = GetFaces(mesh, edgesHs, !partial);

                if (faces.Count > 0)
                {
                    UnselectedFaces.Add(mesh.gameObject, faces);
                }
            }

            SelectedEdges.Clear();
            UnselectedEdges.Clear();
            SelectedIndices.Clear();
            UnselectedIndices.Clear();
        }

        private void EdgesToVertices(bool invert)
        {
            SelectedIndices.Clear();
            UnselectedIndices.Clear();

            foreach (KeyValuePair<GameObject, IList<Edge>> kvp in invert ? UnselectedEdges : SelectedEdges)
            {
                ProBuilderMesh mesh;
                List<int> indices;
                GetCoindicentIndices(kvp, out mesh, out indices);

                SelectedIndices.Add(mesh.gameObject, indices);
            }

            foreach (KeyValuePair<GameObject, IList<Edge>> kvp in invert ? SelectedEdges : UnselectedEdges)
            {
                ProBuilderMesh mesh;
                List<int> indices;
                GetCoindicentIndices(kvp, out mesh, out indices);

                UnselectedIndices.Add(mesh.gameObject, indices);
            }

            SelectedEdges.Clear();
            UnselectedEdges.Clear();
            SelectedFaces.Clear();
            UnselectedFaces.Clear();
        }

        private void VerticesToEdges(bool invert, bool partial)
        {
            SelectedEdges.Clear();
            UnselectedEdges.Clear();

            foreach (KeyValuePair<GameObject, IList<int>> kvp in invert ? UnselectedIndices : SelectedIndices)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                HashSet<int> indicesHs = new HashSet<int>(mesh.GetCoincidentVertices(kvp.Value));
                List<Edge> edges = GetEdges(mesh, indicesHs, !partial);

                if (edges.Count > 0)
                {
                    SelectedEdges.Add(mesh.gameObject, edges);
                }
            }

            foreach (KeyValuePair<GameObject, IList<int>> kvp in invert ? SelectedIndices : UnselectedIndices)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                HashSet<int> indicesHs = new HashSet<int>(mesh.GetCoincidentVertices(kvp.Value));
                List<Edge> edges = GetEdges(mesh, indicesHs, !partial);

                if (edges.Count > 0)
                {
                    UnselectedEdges.Add(mesh.gameObject, edges);
                }
            }

            SelectedIndices.Clear();
            UnselectedIndices.Clear();
            SelectedFaces.Clear();
            UnselectedFaces.Clear();
        }

        private static List<int> GetFaces(ProBuilderMesh mesh, HashSet<int> indicesHs, bool all)
        {
            IList<Face> allFaces = mesh.faces;
            List<int> faces = new List<int>();
            for (int i = 0; i < allFaces.Count; ++i)
            {
                Face face = allFaces[i];
                if (all)
                {
                    if (face.indexes.All(index => indicesHs.Contains(index)))
                    {
                        faces.Add(i);
                    }
                }
                else
                {
                    if (face.indexes.Any(index => indicesHs.Contains(index)))
                    {
                        faces.Add(i);
                    }
                }   
            }

            return faces;
        }

        private static List<int> GetFaces(ProBuilderMesh mesh, HashSet<Edge> edgesHs, bool all)
        {
            IList<Face> allFaces = mesh.faces;
            List<int> faces = new List<int>();
            for (int i = 0; i < allFaces.Count; ++i)
            {
                Face face = allFaces[i];

                if(all)
                {
                    if (face.edges.All(edge => edgesHs.Contains(edge)))
                    {
                        faces.Add(i);
                    }
                }
                else
                {
                    if (face.edges.Any(edge => edgesHs.Contains(edge)))
                    {
                        faces.Add(i);
                    }
                }
                
            }
            return faces;
        }

        private static List<Edge> GetEdges(ProBuilderMesh mesh, HashSet<int> indicesHs, bool all)
        {
            IList<Face> allFaces = mesh.faces;
            HashSet<Edge> edgesHs = new HashSet<Edge>();
            for (int i = 0; i < allFaces.Count; ++i)
            {
                Face face = allFaces[i];
                ReadOnlyCollection<Edge> edges = face.edges;
                for(int e = 0; e < edges.Count; ++e)
                {
                    Edge edge = edges[e];
                    if(!edgesHs.Contains(edge))
                    {
                        if(all)
                        {
                            if (indicesHs.Contains(edge.a) && indicesHs.Contains(edge.b))
                            {
                                edgesHs.Add(edge);
                            }
                        }
                        else
                        {
                            if (indicesHs.Contains(edge.a) || indicesHs.Contains(edge.b))
                            {
                                edgesHs.Add(edge);
                            }
                        }
                        
                    }
                }
            }

            return edgesHs.ToList();
        }

        private static void GetEdges(KeyValuePair<GameObject, IList<int>> kvp, out ProBuilderMesh mesh, out HashSet<Edge> edgesHs)
        {
            mesh = kvp.Key.GetComponent<ProBuilderMesh>();
            edgesHs = new HashSet<Edge>();
            IList<Face> faces = new List<Face>();
            mesh.GetFaces(kvp.Value, faces);
            for (int i = 0; i < faces.Count; ++i)
            {
                ReadOnlyCollection<Edge> edges = faces[i].edges;
                for (int e = 0; e < edges.Count; ++e)
                {
                    if (!edgesHs.Contains(edges[e]))
                    {
                        edgesHs.Add(edges[e]);
                    }
                }
            }
        }

        private static void GetCoindicentIndices(KeyValuePair<GameObject, IList<int>> kvp, out ProBuilderMesh mesh, out List<int> indices)
        {
            mesh = kvp.Key.GetComponent<ProBuilderMesh>();
            IList<Face> faces = new List<Face>();
            mesh.GetFaces(kvp.Value, faces);
            indices = new List<int>();
            mesh.GetCoincidentVertices(faces, indices);
        }

        private static void GetCoindicentIndices(KeyValuePair<GameObject, IList<Edge>> kvp, out ProBuilderMesh mesh, out List<int> indices)
        {
            mesh = kvp.Key.GetComponent<ProBuilderMesh>();
            IList<Edge> edges = kvp.Value;
            indices = new List<int>();
            mesh.GetCoincidentVertices(edges, indices);
        }
    }

    public interface IMeshEditor
    {
        int GraphicsLayer
        {
            get;
            set;
        }

        bool HasSelection
        {
            get;
        }

        bool CenterMode
        {
            get;
            set;
        }

        bool GlobalMode
        {
            get;
            set;
        }

        bool UVEditingMode
        {
            get;
            set;
        }

        Vector3 Position
        {
            get;
            set;
        }

        Vector3 Normal
        {
            get;
        }

        Quaternion Rotation
        {
            get;
        }

        GameObject Target
        {
            get;
        }

        void Hover(Camera camera, Vector3 pointer);
        void Extrude(float distance = 0.0f);
        void Delete();
        void Subdivide();
        void Merge();
        // void Combine();
        MeshSelection SelectHoles();
        void FillHoles();
        void Bridge();
        void InsertEdgeLoop();
        void InsertFace();
        MeshSelection Select(Camera camera, Vector3 pointer, bool shift, bool ctrl, bool depthTest);
        MeshSelection Select(Camera camera, Rect rect, Rect uiRootRect, GameObject[] gameObjects, bool depthTest, MeshEditorSelectionMode mode);
        MeshSelection Select(Material material);
        MeshSelection Unselect(Material material);

        void SetSelection(MeshSelection selection);
        MeshSelection GetSelection();
        MeshSelection ClearSelection();
        
        MeshEditorState GetState(bool recordUV);
        void SetState(MeshEditorState state);

        void BeginMove();
        void EndMove();

        void BeginRotate(Quaternion initialRotation);
        void Rotate(Quaternion rotation);
        void EndRotate();

        void BeginScale();
        void Scale(Vector3 scale, Quaternion rotation);
        void EndScale();
    }

    public static class IMeshEditorExt
    {
        public static MeshSelection Select(Material material)
        {
            MeshSelection selection = new MeshSelection();
            ProBuilderMesh[] meshes = UnityEngine.Object.FindObjectsOfType<ProBuilderMesh>();
            foreach (ProBuilderMesh mesh in meshes)
            {
                Renderer renderer = mesh.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                int index = Array.IndexOf(materials, material);
                if (index < 0)
                {
                    continue;
                }

                List<int> selectedFaces = new List<int>();
                IList<Face> faces = mesh.faces;
                for (int i = 0; i < faces.Count; ++i)
                {
                    Face face = faces[i];
                    if (face.submeshIndex == index)
                    {
                        selectedFaces.Add(i);
                    }
                }

                if (selectedFaces.Count > 0)
                {
                    selection.SelectedFaces.Add(mesh.gameObject, selectedFaces);
                }
            }
            return selection;
        }

    }
}