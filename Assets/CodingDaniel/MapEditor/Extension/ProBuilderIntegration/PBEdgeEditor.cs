
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class PBEdgeEditor : PBBaseEditor
    {
        [HideInInspector]
        [FormerlySerializedAs("m_edgeSelection")]
        public PBEdgeSelection _edgeSelection;
        private SceneSelection _selection = new SceneSelection();
    
        public override bool HasSelection
        {
            get { return _edgeSelection.EdgesCount > 0; }
        }

        public override Vector3 Position
        {
            get { return CenterMode ? _edgeSelection.CenterOfMass : _edgeSelection.LastPosition; }
            set { MoveTo(value); }
        }

        public override Vector3 Normal
        {
            get { return (!GlobalMode && _edgeSelection.LastMesh != null) ? _edgeSelection.LastMesh.transform.forward : Vector3.forward; }
        }

        public override Quaternion Rotation
        {
            get
            {
                if (GlobalMode || _edgeSelection.LastMesh == null)
                {
                    return Quaternion.identity;
                }

                MeshSelection selection = GetSelection();
                if (selection == null)
                {
                    return Quaternion.identity;
                }

                selection = selection.ToFaces(false, false);

                IList<int> faceIndexes;
                if (selection.SelectedFaces.TryGetValue(_edgeSelection.LastMesh.gameObject, out faceIndexes))
                {
                    if (faceIndexes.Count != 0)
                    {
                        return HandleUtility.GetRotation(_edgeSelection.LastMesh, _edgeSelection.LastMesh.faces[faceIndexes.Last()].distinctIndexes);
                    }
                }

                IList<Edge> edges;
                if (!selection.SelectedEdges.TryGetValue(_edgeSelection.LastMesh.gameObject, out edges) || edges.Count == 0)
                {
                    return Quaternion.identity;
                }

                Face face = PBUtility.GetFace(_edgeSelection.LastMesh, edges.Last());
                if (face == null)
                {
                    return Quaternion.identity;
                }

                return HandleUtility.GetRotation(_edgeSelection.LastMesh, face.distinctIndexes);
            }
        }

        public override GameObject Target
        {
            get { return _edgeSelection.LastMesh != null ? _edgeSelection.LastMesh.gameObject : null; }
        }


        private void OnDestroy()
        {
            if (_edgeSelection != null)
            {
                Destroy(_edgeSelection);
            }
        }

        public override void SetSelection(MeshSelection selection)
        {
            _edgeSelection.Clear();
            _selection.Clear();

            if (selection != null)
            {
                selection = selection.ToEdges(false);

                foreach (KeyValuePair<GameObject, IList<Edge>> kvp in selection.SelectedEdges)
                {
                    PBMesh pbMesh = kvp.Key.GetComponent<PBMesh>();
                    if(pbMesh.IsMarkedAsDestroyed)
                    {
                        continue;
                    }

                    _edgeSelection.Add(kvp.Key.GetComponent<ProBuilderMesh>(), kvp.Value);
                }
            }
        }

        public override MeshSelection GetSelection()
        {
            MeshSelection selection = new MeshSelection();

            foreach (ProBuilderMesh mesh in _edgeSelection.Meshes)
            {
                selection.SelectedEdges.Add(mesh.gameObject, _edgeSelection.GetEdges(mesh).ToArray());
            }
            if (selection.SelectedEdges.Count > 0)
            {
                return selection;
            }
            return null;
        }

        public override MeshSelection ClearSelection()
        {
            MeshSelection selection = new MeshSelection();

            foreach (ProBuilderMesh mesh in _edgeSelection.Meshes)
            {
                if(mesh == null || mesh.gameObject == null)
                {
                    continue;
                }

                selection.UnselectedEdges.Add(mesh.gameObject, _edgeSelection.GetEdges(mesh).ToArray());
            }

            _edgeSelection.Clear();
            _selection.Clear();

            if (selection.UnselectedEdges.Count > 0)
            {
                return selection;
            }
            return null;
        }

        public override MeshSelection SelectHoles()
        {
            MeshSelection selection = new MeshSelection();
            foreach (ProBuilderMesh mesh in _edgeSelection.Meshes)
            {
                HashSet<int> indexes = new HashSet<int>();
                for (int i = 0; i < mesh.vertexCount; ++i)
                {
                    indexes.Add(i);
                }

                List<List<Edge>> holes = PBElementSelection.FindHoles(mesh, indexes);
                selection.SelectedEdges.Add(mesh.gameObject, _edgeSelection.GetCoincidentEdges(holes.SelectMany(e => e).Where(e => !_edgeSelection.IsSelected(mesh, e))));
                _edgeSelection.Add(mesh, selection.SelectedEdges[mesh.gameObject]);
            }

            if (!selection.HasEdges)
            {
                return null;
            }

            return selection;
        }

        
        IEnumerable<Edge> GetEdgeRing(ProBuilderMesh pb, IEnumerable<Edge> edges)
        {
            List<WingedEdge> wings = WingedEdge.GetWingedEdges(pb);
            
            //Get the edge look up
            Dictionary<int, int> sharedVertexLookup = new Dictionary<int, int>();
            SharedVertex.GetSharedVertexLookup(pb.sharedVertices, sharedVertexLookup);
            List<EdgeLookup> edgeLookup = EdgeLookup.GetEdgeLookup(edges, sharedVertexLookup).ToList();
            edgeLookup = edgeLookup.Distinct().ToList();

            Dictionary<Edge, WingedEdge> wings_dic = new Dictionary<Edge, WingedEdge>();

            for (int i = 0; i < wings.Count; i++)
                if (!wings_dic.ContainsKey(wings[i].edge.common))
                    wings_dic.Add(wings[i].edge.common, wings[i]);

            //Check Multiple used edges
            HashSet<EdgeLookup> used = new HashSet<EdgeLookup>();

            for (int i = 0, c = edgeLookup.Count; i < c; i++)
            {
                WingedEdge we;

                if (!wings_dic.TryGetValue(edgeLookup[i].common, out we) || used.Contains(we.edge))
                    continue;

                WingedEdge cur = we;

                while (cur != null)
                {
                    if (!used.Add(cur.edge)) break;
                    cur = EdgeRingNext(cur);
                    if (cur != null && cur.opposite != null) cur = cur.opposite;
                }

                cur = EdgeRingNext(we.opposite);
                if (cur != null && cur.opposite != null) cur = cur.opposite;

                // run in both directions
                while (cur != null)
                {
                    if (!used.Add(cur.edge)) break;
                    cur = EdgeRingNext(cur);
                    if (cur != null && cur.opposite != null) cur = cur.opposite;
                }
            }

            return used.Select(x => x.local);
        }
        WingedEdge EdgeRingNext(WingedEdge edge)
        {
            if (edge == null)
                return null;

            WingedEdge next = edge.next, prev = edge.previous;
            int i = 0;

            while (next != prev && next != edge)
            {
                next = next.next;

                if (next == prev)
                    return null;

                prev = prev.previous;

                i++;
            }

            if (i % 2 == 0 || next == edge)
                next = null;

            return next;
        }
        public override void FillHoles()
        {
            int filled = 0;

            foreach (ProBuilderMesh mesh in _edgeSelection.Meshes)
            {
                HashSet<int> indexes = new HashSet<int>();
                for (int i = 0; i < mesh.vertexCount; ++i)
                {
                    indexes.Add(i);
                }
                List<List<Edge>> holes = PBElementSelection.FindHoles(mesh, indexes);

                mesh.ToMesh();

                List<WingedEdge> wings = WingedEdge.GetWingedEdges(mesh);
                HashSet<Face> appendedFaces = new HashSet<Face>();

                foreach (List<Edge> hole in holes)
                {
                    List<int> holeIndexes;
                    Face face;

                    if (!hole.All(e => _edgeSelection.IsSelected(mesh, e)))
                    {
                        continue;
                    }

                    //if (wholePath)
                    //{
                    //    // if selecting whole path and in edge mode, make sure the path contains
                    //    // at least one complete edge from the selection.
                    //    if (!hole.Any(x => common.Contains(x.edge.common.a) &&
                    //            common.Contains(x.edge.common.b)))
                    //        continue;

                    //    holeIndexes = hole.Select(x => x.edge.local.a).ToList();
                    //    face = AppendElements.CreatePolygon(mesh, holeIndexes, false);
                    //}
                    //else
                    {
                        //IEnumerable<WingedEdge> selected = hole.Where(x => common.Contains(x.edge.common.a));
                        //holeIndexes = selected.Select(x => x.edge.local.a).ToList();

                        //holeIndexes = hole.Select(x => x.edge.local.a).ToList();
                        //face = AppendElements.CreatePolygon(mesh, holeIndexes, true);

                        holeIndexes = hole.Select(x => x.a).ToList();
                        face = AppendElements.CreatePolygon(mesh, holeIndexes, true);
                    }

                    if (face != null)
                    {
                        filled++;
                        appendedFaces.Add(face);
                    }
                }

                mesh.SetSelectedFaces(appendedFaces);

                wings = WingedEdge.GetWingedEdges(mesh);

                // make sure the appended faces match the first adjacent face found
                // both in winding and face properties
                foreach (var appendedFace in appendedFaces)
                {
                    var wing = wings.FirstOrDefault(x => x.face == appendedFace);

                    if (wing == null)
                        continue;

                    using (var it = new WingedEdgeEnumerator(wing))
                    {
                        while (it.MoveNext())
                        {
                            if (it.Current == null)
                                continue;

                            var currentWing = it.Current;
                            var oppositeFace = it.Current.opposite != null ? it.Current.opposite.face : null;

                            if (oppositeFace != null && !appendedFaces.Contains(oppositeFace))
                            {
                                currentWing.face.submeshIndex = oppositeFace.submeshIndex;
                                currentWing.face.uv = new AutoUnwrapSettings(oppositeFace.uv);
                                PBSurfaceTopology.ConformOppositeNormal(currentWing.opposite);
                                break;
                            }
                        }
                    }
                }

                mesh.ToMesh();
                mesh.Refresh();
            }
        }

        public override MeshSelection Select(Camera camera, Vector3 pointer, bool shift, bool ctrl, bool depthTest)
        {
            MeshSelection selection = null;
            GameObject pickedObject = PBUtility.PickObject(camera, pointer);
            float result = PBUtility.PickEdge(camera, pointer, 20, pickedObject, _edgeSelection.Meshes, depthTest, ref _selection);

            
            if (!float.IsPositiveInfinity(result))
            {
#if PROBUILDER_4_4_0_OR_NEWER
            Edge selectedEdge = _selection.edges[0];
#else
                Edge selectedEdge = _selection.edge;
#endif

                if (_edgeSelection.IsSelected(_selection.mesh, selectedEdge))
                {
                    if (shift)
                    {
                        _edgeSelection.FindCoincidentEdges(_selection.mesh);

                        IList<Edge> edges = _edgeSelection.GetCoincidentEdges(new[] { selectedEdge });
                        _edgeSelection.Remove(_selection.mesh, edges);
                        selection = new MeshSelection();
                        selection.UnselectedEdges.Add(_selection.mesh.gameObject, edges);
                    }
                    else
                    {
                        _edgeSelection.FindCoincidentEdges(_selection.mesh);

                        IList<Edge> edges = _edgeSelection.GetCoincidentEdges(new[] { selectedEdge });
                        selection = ReadSelection();
                        selection.UnselectedEdges[_selection.mesh.gameObject] = selection.UnselectedEdges[_selection.mesh.gameObject].Where(e => e != selectedEdge).ToArray();
                        selection.SelectedEdges.Add(_selection.mesh.gameObject, edges);
                        _edgeSelection.Clear();
                        _edgeSelection.Add(_selection.mesh, edges);
                    }
                }
                else
                {
                    if (shift)
                    {
                        selection = new MeshSelection();
                    }
                    else
                    {
                        selection = ReadSelection();
                        _edgeSelection.Clear();
                    }

                    _edgeSelection.FindCoincidentEdges(_selection.mesh);

                    
                    IList<Edge> edges = _edgeSelection.GetCoincidentEdges(new[] { selectedEdge });

                    _edgeSelection.Add(_selection.mesh, edges);
                    selection.SelectedEdges.Add(_selection.mesh.gameObject, edges);
                }
            }
            else
            {
                if (!shift)
                {
                    selection = ReadSelection();
                    if (selection.UnselectedEdges.Count == 0)
                    {
                        selection = null;
                    }
                    _edgeSelection.Clear();
                }
            }
            return selection;
        }

        private MeshSelection ReadSelection()
        {
            MeshSelection selection = new MeshSelection();
            ProBuilderMesh[] meshes = _edgeSelection.Meshes.OrderBy(m => m == _edgeSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Edge> edges = _edgeSelection.GetEdges(mesh);
                if (edges != null)
                {
                    selection.UnselectedEdges.Add(mesh.gameObject, edges.ToArray());
                }
            }
            return selection;
        }

        public override MeshSelection Select(Camera camera, Rect rect, Rect uiRootRect, GameObject[] gameObjects, bool depthTest, MeshEditorSelectionMode mode)
        {
            Dictionary<ProBuilderMesh, HashSet<Edge>> pickResult = PBUtility.PickEdges(camera, rect, uiRootRect, gameObjects, depthTest);
            if (pickResult.Count == 0)
            {
                return null;
            }

            MeshSelection selection = new MeshSelection();
            foreach (KeyValuePair<ProBuilderMesh, HashSet<Edge>> kvp in pickResult)
            {
                ProBuilderMesh mesh = kvp.Key;

                _edgeSelection.FindCoincidentEdges(mesh);
                IList<Edge> edges = _edgeSelection.GetCoincidentEdges(kvp.Value);

                IList<Edge> selected = edges.Where(edge => _edgeSelection.IsSelected(mesh, edge)).ToArray();
                IList<Edge> notSelected = edges.Where(edge => !_edgeSelection.IsSelected(mesh, edge)).ToArray();

                if (mode == MeshEditorSelectionMode.Substract || mode == MeshEditorSelectionMode.Difference)
                {
                    selection.UnselectedEdges.Add(mesh.gameObject, selected);
                    _edgeSelection.Remove(mesh, selected);
                }

                if (mode == MeshEditorSelectionMode.Add || mode == MeshEditorSelectionMode.Difference)
                {
                    selection.SelectedEdges.Add(mesh.gameObject, notSelected);
                    _edgeSelection.Add(mesh, notSelected);
                }
            }
            return selection;
        }

        public override MeshSelection Select(Material material)
        {
            MeshSelection selection = IMeshEditorExt.Select(material);
            selection = selection.ToEdges(false, false);

            foreach (KeyValuePair<GameObject, IList<Edge>> kvp in selection.SelectedEdges.ToArray())
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                _edgeSelection.FindCoincidentEdges(mesh);
                IList<Edge> edges = _edgeSelection.GetCoincidentEdges(kvp.Value).ToList();

                for (int i = edges.Count - 1; i >= 0; i--)
                {
                    Edge edge = edges[i];
                    if (_edgeSelection.IsSelected(mesh, edge))
                    {
                        edges.Remove(edge);
                    }
                }

                if (edges.Count == 0)
                {
                    selection.SelectedEdges.Remove(kvp.Key);
                }
                else
                {
                    _edgeSelection.Add(mesh, edges);
                }
            }
            if (selection.SelectedEdges.Count == 0)
            {
                return null;
            }
            return selection;
        }

        public override MeshSelection Unselect(Material material)
        {
            MeshSelection selection = IMeshEditorExt.Select(material);
            selection = selection.ToEdges(true, false);

            foreach (KeyValuePair<GameObject, IList<Edge>> kvp in selection.UnselectedEdges.ToArray())
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                _edgeSelection.FindCoincidentEdges(mesh);
                IList<Edge> edges = _edgeSelection.GetCoincidentEdges(kvp.Value).ToList();

                for (int i = edges.Count - 1; i >= 0; i--)
                {
                    Edge edge = edges[i];
                    if (!_edgeSelection.IsSelected(mesh, edge))
                    {
                        edges.Remove(edge);
                    }
                }

                if (edges.Count == 0)
                {
                    selection.UnselectedEdges.Remove(kvp.Key);
                }
                else
                {
                    _edgeSelection.Remove(mesh, edges);
                }
            }
            if (selection.UnselectedEdges.Count == 0)
            {
                return null;
            }
            return selection;
        }

        public override void BeginMove()
        {
            base.BeginMove();
        }

        public override void EndMove()
        {
            base.EndMove();
#if PROBUILDER_4_4_0_OR_NEWER
        _edgeSelection.Meshes.RefreshColliders();
#endif
        }
        private void MoveTo(Vector3 to)
        {
            if (_edgeSelection.MeshesCount == 0)
            {
                return;
            }

            Vector3 from = Position;
            Vector3 offset = to - from;

            IEnumerable<ProBuilderMesh> meshes = _edgeSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                Vector3 localOffset = mesh.transform.InverseTransformVector(offset);
                IEnumerable<Edge> edges = _edgeSelection.GetEdges(mesh);
                mesh.TranslateVertices(edges, localOffset);

                mesh.ToMesh();
                mesh.Refresh();
            }

            ProBuilderMesh lastMesh = _edgeSelection.LastMesh;
            if (lastMesh != null)
            {
                _edgeSelection.Synchronize(
                    _edgeSelection.CenterOfMass + offset,
                    _edgeSelection.LastPosition + offset,
                    _edgeSelection.LastNormal);
            }

            RaisePBMeshesChanged(true);
        }

        private int[][] _initialIndexes;
        private Vector3[][] _initialPositions;
        private Vector3 _initialPostion;
        private Quaternion _initialRotation;
    
        public override void BeginRotate(Quaternion initialRotation)
        {
            _initialPostion = _edgeSelection.LastPosition;
            _initialPositions = new Vector3[_edgeSelection.MeshesCount][];
            _initialIndexes = new int[_edgeSelection.MeshesCount][];
            _initialRotation = Quaternion.Inverse(initialRotation);

            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _edgeSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Edge> edges = _edgeSelection.GetCoincidentEdges(_edgeSelection.GetEdges(mesh));

                _initialIndexes[meshIndex] = mesh.GetCoincidentVertices(edges.Select(e => e.a).Union(edges.Select(e => e.b))).ToArray();
                _initialPositions[meshIndex] = mesh.GetVertices(_initialIndexes[meshIndex]).Select(v => mesh.transform.TransformPoint(v.position)).ToArray();
                meshIndex++;
            }
        }

        public override void EndRotate()
        {
            _initialPositions = null;
            _initialIndexes = null;
#if PROBUILDER_4_4_0_OR_NEWER
        _edgeSelection.Meshes.RefreshColliders();
#endif
        }

        public override void Rotate(Quaternion rotation)
        {
            if (_edgeSelection.MeshesCount == 0)
            {
                return;
            }

            Vector3 center = Position;
            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _edgeSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Vector3> positions = mesh.positions.ToArray();
                Vector3[] initialPositions = _initialPositions[meshIndex];
                int[] indexes = _initialIndexes[meshIndex];

                for (int i = 0; i < initialPositions.Length; ++i)
                {
                    Vector3 position = initialPositions[i];
                    position = center + rotation * _initialRotation * (position - center);
                    position = mesh.transform.InverseTransformPoint(position);
                    positions[indexes[i]] = position;
                }

                mesh.positions = positions;
                mesh.Refresh();
                mesh.ToMesh();
                meshIndex++;
            }

            _edgeSelection.Synchronize(
                _edgeSelection.CenterOfMass,
                center + rotation * _initialRotation * (_initialPostion - center),
                rotation * _initialRotation * _edgeSelection.LastNormal);

            RaisePBMeshesChanged(true);
        }


        public override void BeginScale()
        {
            _initialPostion = _edgeSelection.LastPosition;
            _initialPositions = new Vector3[_edgeSelection.MeshesCount][];
            _initialIndexes = new int[_edgeSelection.MeshesCount][];

            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _edgeSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Edge> edges = _edgeSelection.GetCoincidentEdges(_edgeSelection.GetEdges(mesh));

                _initialIndexes[meshIndex] = mesh.GetCoincidentVertices(edges.Select(e => e.a).Union(edges.Select(e => e.b))).ToArray();
                _initialPositions[meshIndex] = mesh.GetVertices(_initialIndexes[meshIndex]).Select(v => mesh.transform.TransformPoint(v.position)).ToArray();

                meshIndex++;
            }
        }

        public override void EndScale()
        {
            _initialPositions = null;
            _initialIndexes = null;
#if PROBUILDER_4_4_0_OR_NEWER
        _edgeSelection.Meshes.RefreshColliders();
#endif
        }

        public override void Scale(Vector3 scale, Quaternion rotation)
        {
            if (_edgeSelection.MeshesCount == 0)
            {
                return;
            }

            Vector3 center = Position;
            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _edgeSelection.Meshes;

            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Vector3> positions = mesh.positions.ToArray();
                Vector3[] initialPositions = _initialPositions[meshIndex];
                int[] indexes = _initialIndexes[meshIndex];

                for (int i = 0; i < initialPositions.Length; ++i)
                {
                    Vector3 position = initialPositions[i];
                    position = center + rotation * Vector3.Scale(Quaternion.Inverse(rotation) * (position - center), scale);
                    position = mesh.transform.InverseTransformPoint(position);
                    positions[indexes[i]] = position;
                }

                mesh.positions = positions;
                mesh.Refresh();
                mesh.ToMesh();
                meshIndex++;
            }

            _edgeSelection.Synchronize(
                _edgeSelection.CenterOfMass,
                center + rotation * Vector3.Scale(Quaternion.Inverse(rotation) * (_initialPostion - center), scale),
                rotation * _initialRotation * _edgeSelection.LastNormal);

            RaisePBMeshesChanged(true);
        }

        public override MeshEditorState GetState(bool recordUV)
        {
            MeshEditorState state = new MeshEditorState();
            ProBuilderMesh[] meshes = _edgeSelection.Meshes.OrderBy(m => m == _edgeSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                state.State.Add(mesh.gameObject, new MeshState(mesh.positions.ToArray(), mesh.faces.ToArray(), mesh.textures.ToArray(), recordUV));
            }
            return state;
        }

        public override void SetState(MeshEditorState state)
        {
            ProBuilderMesh[] meshes = state.State.Keys.Select(k => k.GetComponent<ProBuilderMesh>()).ToArray();
            _edgeSelection.Clear();
            foreach (ProBuilderMesh mesh in meshes)
            {
                // IList<Edge> edges = _edgeSelection.GetCoincidentEdges(_edgeSelection.GetEdges(mesh));
                // if(edges != null)
                // {
                //     edges = edges.ToArray();
                //     _edgeSelection.Remove(mesh, edges);
                // }
            
                MeshState meshState = state.State[mesh.gameObject];
                mesh.Rebuild(meshState.Positions, meshState.Faces.Select(f => f.ToFace()).ToArray(), meshState.Textures);

                // if(edges != null)
                // {
                //     _edgeSelection.Add(mesh, edges);
                // }
            }
        }

        public override void Bridge()
        {
            ProBuilderMesh mesh = _edgeSelection.Meshes.OrderBy(m => m == _edgeSelection.LastMesh).FirstOrDefault();

            if (mesh != null)
            {
                IList<Edge> e = _edgeSelection.GetEdges(mesh);
                HashSet<Face> appendedFaces = new HashSet<Face>();
                if (e.Count == 2)
                {
                    var face = mesh.Bridge(e[0], e[1], false);

                    appendedFaces.Add(face);
                    
                    mesh.SetSelectedFaces(appendedFaces);

                    List<WingedEdge> wings = WingedEdge.GetWingedEdges(mesh);

                    // make sure the appended faces match the first adjacent face found
                    // both in winding and face properties
                    foreach (var appendedFace in appendedFaces)
                    {
                        var wing = wings.FirstOrDefault(x => x.face == appendedFace);

                        if (wing == null)
                            continue;

                        using (var it = new WingedEdgeEnumerator(wing))
                        {
                            while (it.MoveNext())
                            {
                                if (it.Current == null)
                                    continue;

                                var currentWing = it.Current;
                                var oppositeFace = it.Current.opposite != null ? it.Current.opposite.face : null;

                                if (oppositeFace != null && !appendedFaces.Contains(oppositeFace))
                                {
                                    currentWing.face.submeshIndex = oppositeFace.submeshIndex;
                                    currentWing.face.uv = new AutoUnwrapSettings(oppositeFace.uv);
                                    PBSurfaceTopology.ConformOppositeNormal(currentWing.opposite);
                                    break;
                                }
                            }
                        }
                    }

                    mesh.ToMesh();
                    mesh.Refresh();
                }
                else
                {
                    return;
                }
            }
            
            
        }

        public override void InsertEdgeLoop()
        {
            ProBuilderMesh[] meshes = _edgeSelection.Meshes.OrderBy(m => m == _edgeSelection.LastMesh).ToArray();
            foreach (var mesh in meshes)
            {
                IList<Edge> e = _edgeSelection.GetEdges(mesh);
                Edge[] edges = mesh.Connect(GetEdgeRing(mesh, e)).item2;

                if (edges != null)
                {
                    
                    // mesh.SetSelectedEdges(edges);
                    mesh.ToMesh();
                    mesh.Refresh();
                    
                    MeshSelection selection = ReadSelection();
                    _edgeSelection.Clear();
                    _edgeSelection.FindCoincidentEdges(mesh);
                    IList<Edge> es = _edgeSelection.GetCoincidentEdges(edges);

                    _edgeSelection.Add(mesh, es);
                    selection.SelectedEdges.Add(mesh.gameObject, es);
                    
                    ProBuilderTool.Instance.SetSelection(this);
                }
            } 
        }
        public override void Subdivide()
        {
            ProBuilderMesh[] meshes = _edgeSelection.Meshes.OrderBy(m => m == _edgeSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Edge> edges = _edgeSelection.GetEdges(mesh);
                ConnectElements.Connect(mesh, edges);

                mesh.Refresh();
                mesh.ToMesh();
            }
        }

        public override void Extrude(float distance = 0)
        {
            ProBuilderMesh[] meshes = _edgeSelection.Meshes.OrderBy(m => m == _edgeSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Edge> edges = _edgeSelection.GetEdges(mesh).ToList();
                for(int i = edges.Count - 1; i >= 0; i--)
                {
                    Edge currentEdge = edges[i];

                    bool hasCoEdges = false;
                    IList<Edge> coEdges =  _edgeSelection.GetCoincidentEdges(new[] { currentEdge });
                    for(int j = 0; j < coEdges.Count; ++j)
                    {
                        Edge coEdge = coEdges[j];
                        if(coEdge == currentEdge)
                        {
                            continue;
                        }

                        if(edges.Contains(coEdge))
                        {
                            hasCoEdges = true;
                            break;
                        }
                    }

                    if(hasCoEdges)
                    {
                        edges.RemoveAt(i);
                    }
                }
                _edgeSelection.Remove(mesh);

                Edge[] newEdges = mesh.Extrude(edges, distance, false, true);

                mesh.ToMesh();
                mesh.Refresh();

                _edgeSelection.Add(mesh, newEdges);
            }

            if (distance != 0.0f)
            {
                _edgeSelection.Synchronize(
                    _edgeSelection.CenterOfMass + _edgeSelection.LastNormal * distance,
                    _edgeSelection.LastPosition + _edgeSelection.LastNormal * distance,
                    _edgeSelection.LastNormal);
            }

            RaisePBMeshesChanged(false);
        }

        private void RaisePBMeshesChanged(bool positionsOnly)
        {
            foreach (PBMesh mesh in _edgeSelection.PBMeshes)
            {
                mesh.RaiseChanged(positionsOnly, false);
            }
        }

        public override void Delete()
        {
            MeshSelection selection = GetSelection();
            selection = selection.ToFaces(false, true);

            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                mesh.DeleteFaces(kvp.Value);
                mesh.ToMesh();
                mesh.Refresh();
            }
        }
        
        
    }
}