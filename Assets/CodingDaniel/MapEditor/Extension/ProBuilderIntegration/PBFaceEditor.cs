using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class PBFaceEditor : PBBaseEditor
    {
        [HideInInspector]
        [FormerlySerializedAs("m_faceSelection")]
        public PBFaceSelection _faceSelection;

        private int[][] _initialIndexes;
        private Vector3[][] _initialPositions;
        private Vector3 _initialPostion;
        private Quaternion _initialRotation;
        
        public override bool HasSelection
        {
            get { return _faceSelection.FacesCount > 0; }
        }

        public override Vector3 Position
        {
            get
            {
                if(UVEditingMode)
                {
                    return _faceSelection.LastPosition;
                }

                return CenterMode ? _faceSelection.CenterOfMass : _faceSelection.LastPosition;
            }
            set { MoveTo(value); }
        }

        public override Vector3 Normal
        {
            get
            {
                if(UVEditingMode)
                {
                    return _faceSelection.LastNormal;
                }

                return (CenterMode && _faceSelection.FacesCount > 1) ? Vector3.forward : _faceSelection.LastNormal;
            }
        }    
        
        public override Quaternion Rotation
        {
            get
            {
                if(_faceSelection.LastMesh == null || !UVEditingMode && CenterMode && _faceSelection.FacesCount > 1)
                {
                    return Quaternion.identity;
                }

                MeshSelection selection = GetSelection();
                if(selection == null)
                {
                    return Quaternion.identity;
                }
                IList<int> faces;
                if(!selection.SelectedFaces.TryGetValue(_faceSelection.LastMesh.gameObject, out faces))
                {
                    return Quaternion.identity;
                }

                if(faces == null || faces.Count == 0)
                {
                    return Quaternion.identity;
                }

                IList<int> distinctIndexes = _faceSelection.LastMesh.faces[faces.Last()].distinctIndexes;
                return HandleUtility.GetRotation(_faceSelection.LastMesh, distinctIndexes);
                //return HandleUtility.GetFaceRotation(_faceSelection.LastMesh, HandleOrientation.ActiveElement, new[] { _faceSelection.LastMesh.faces[faces.Last()] });
            }
        }

        public override GameObject Target
        {
            get { return _faceSelection.LastMesh != null ? _faceSelection.LastMesh.gameObject : null; }
        }


        private void OnDestroy()
        {
            if(_faceSelection != null)
            {
                Destroy(_faceSelection);
            }
        }

        public override void Hover(Camera camera, Vector3 pointer)
        {

        }

        private bool _isMoveInProgress;
        private Dictionary<ProBuilderMesh, IList<Face>> _meshes;
        public override void BeginMove()
        {
            _isMoveInProgress = true;
            _meshes = new Dictionary<ProBuilderMesh, IList<Face>>();
            IEnumerable<ProBuilderMesh> meshes = _faceSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Face> faces = new List<Face>();
                mesh.GetFaces(_faceSelection.GetFaces(mesh), faces);
                _meshes.Add(mesh, faces);
            }

            if(UVEditingMode)
            {
                _faceSelection.IsRendererEnabled = false;
            }
        }

        public override void EndMove()
        {
            _isMoveInProgress = false;
            _meshes = null;

            if (UVEditingMode)
            {
                _faceSelection.IsRendererEnabled = true;
            }

#if PROBUILDER_4_4_0_OR_NEWER
            _faceSelection.Meshes.RefreshColliders();
#endif
        }

        private void MoveTo(Vector3 to)
        {
            if (_faceSelection.MeshesCount == 0)
            {
                return;
            }

            Vector3 from = Position;
            Vector3 offset = to - from;

            bool wasMoveInProgress = _isMoveInProgress;
            if(!wasMoveInProgress)
            {
                BeginMove();
            }

            foreach(KeyValuePair<ProBuilderMesh, IList<Face>> kvp in _meshes)
            {
                ProBuilderMesh mesh = kvp.Key;
                Vector3 localOffset = mesh.transform.InverseTransformVector(offset);

                IList<Face> faces = kvp.Value;
                mesh.TranslateVertices(faces, localOffset);
                mesh.ToMesh();
                mesh.Refresh();
            }
            _faceSelection.Synchronize(
                _faceSelection.CenterOfMass + offset,
                _faceSelection.LastPosition + offset);

            RaisePBMeshesChanged(true);

            if(!wasMoveInProgress)
            {
                EndMove();
            }
        }

        public override void BeginRotate(Quaternion initialRotation)
        {
            _initialPostion = _faceSelection.LastPosition;
            _initialPositions = new Vector3[_faceSelection.MeshesCount][];
            _initialIndexes = new int[_faceSelection.MeshesCount][];
            _initialRotation = Quaternion.Inverse(initialRotation);

            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _faceSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                int[] indexes = _faceSelection.GetIndexes(mesh).ToArray();
                _initialIndexes[meshIndex] = mesh.GetCoincidentVertices(_faceSelection.GetIndexes(mesh)).ToArray();
                _initialPositions[meshIndex] = mesh.GetVertices(_initialIndexes[meshIndex]).Select(v => mesh.transform.TransformPoint(v.position)).ToArray();
                meshIndex++;
            }

            if (UVEditingMode)
            {
                _faceSelection.IsRendererEnabled = false;
            }
        }

        public override void EndRotate()
        {
            _initialPositions = null;
            _initialIndexes = null;

            if (UVEditingMode)
            {
                _faceSelection.IsRendererEnabled = true;
            }

#if PROBUILDER_4_4_0_OR_NEWER
            _faceSelection.Meshes.RefreshColliders();
#endif
        }

        public override void Rotate(Quaternion rotation)
        {
            if (_faceSelection.MeshesCount == 0)
            {
                return;
            }

            Vector3 center = Position;
            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _faceSelection.Meshes;
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

            _faceSelection.Synchronize(
                _faceSelection.CenterOfMass,
                center + rotation * _initialRotation * (_initialPostion - center));

            RaisePBMeshesChanged(true);
        }


        public override void BeginScale()
        {
            _initialPostion = _faceSelection.LastPosition;

            _initialPositions = new Vector3[_faceSelection.MeshesCount][];
            _initialIndexes = new int[_faceSelection.MeshesCount][];

            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _faceSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                _initialIndexes[meshIndex] = mesh.GetCoincidentVertices(_faceSelection.GetIndexes(mesh)).ToArray();
                _initialPositions[meshIndex] = mesh.GetVertices(_initialIndexes[meshIndex]).Select(v => mesh.transform.TransformPoint(v.position)).ToArray();
                meshIndex++;
            }

            if (UVEditingMode)
            {
                _faceSelection.IsRendererEnabled = false;
            }
        }

        public override void EndScale()
        {
            _initialPositions = null;
            _initialIndexes = null;

            if (UVEditingMode)
            {
                _faceSelection.IsRendererEnabled = true;
            }

#if PROBUILDER_4_4_0_OR_NEWER
            _faceSelection.Meshes.RefreshColliders();
#endif
        }

        public override void Scale(Vector3 scale, Quaternion rotation)
        {
            if (_faceSelection.MeshesCount == 0)
            {
                return;
            }

            Vector3 center = Position;
            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _faceSelection.Meshes;
            
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

            _faceSelection.Synchronize(
                _faceSelection.CenterOfMass,
                center + rotation * Vector3.Scale(Quaternion.Inverse(rotation) * (_initialPostion - center), scale));

            RaisePBMeshesChanged(true);
        }

        public override MeshSelection Select(Camera camera, Vector3 pointer, bool shift, bool ctrl, bool depthTest)
        {
            MeshSelection selection = null;
            MeshAndFace result = PBUtility.PickFace(camera, pointer);
            if(result.face != null)
            {
                if(ctrl)
                {
                    int submeshIndex = result.face.submeshIndex;
                    IList<Face> faces = result.mesh.faces;
                    List<int> sameMaterialFaces = new List<int>();

                    bool wasSelected = false;
                    bool wasUnselected = false;
                    
                    _faceSelection.BeginChange();
                    for (int i = 0; i < faces.Count; ++i)
                    {
                        Face face = faces[i];
                        if (face.submeshIndex == submeshIndex)
                        {
                            sameMaterialFaces.Add(i);

                            if (!_faceSelection.IsSelected(result.mesh, i))
                            {
                                _faceSelection.Add(result.mesh, i);
                                wasUnselected = true;
                            }
                            else
                            {
                                wasSelected = true;
                            }
                        }
                    }

                    if(wasSelected && !wasUnselected)
                    {
                        for(int i = 0; i < sameMaterialFaces.Count; ++i)
                        {
                            _faceSelection.Remove(result.mesh, sameMaterialFaces[i]);
                        }

                        selection = new MeshSelection();
                        selection.UnselectedFaces.Add(result.mesh.gameObject, sameMaterialFaces.ToArray());
                    }
                    else
                    {
                        selection = new MeshSelection();
                        selection.SelectedFaces.Add(result.mesh.gameObject, sameMaterialFaces.ToArray());
                    }

                    _faceSelection.EndChange();
                }
                else
                {
                    int faceIndex = result.mesh.faces.IndexOf(result.face);
                    if (_faceSelection.IsSelected(result.mesh, faceIndex))
                    {
                        if (shift)
                        {
                            _faceSelection.Remove(result.mesh, faceIndex);
                            selection = new MeshSelection();
                            selection.UnselectedFaces.Add(result.mesh.gameObject, new[] { faceIndex });
                        }
                        else
                        {
                            //selection = ReadSelection();
                            selection = null;
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
                            _faceSelection.Clear();
                        }

                        _faceSelection.Add(result.mesh, faceIndex);
                        selection.SelectedFaces.Add(result.mesh.gameObject, new[] { faceIndex });
                    }
                }
            }
            else
            {
                if (!shift)
                {
                    selection = ReadSelection();
                    if (selection.UnselectedFaces.Count == 0)
                    {
                        selection = null;
                    }
                    _faceSelection.Clear(); 
                }
            }
            return selection;
        }

        private MeshSelection ReadSelection()
        {
            MeshSelection selection = new MeshSelection();
            ProBuilderMesh[] meshes = _faceSelection.Meshes.OrderBy(m => m == _faceSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                if(mesh == null)
                {
                    continue;
                }

                IList<int> faces = _faceSelection.GetFaces(mesh);
                if (faces != null && faces.Count > 0)
                {
                    selection.UnselectedFaces.Add(mesh.gameObject, faces.ToArray());
                }
            }
            return selection;
        }

        public override MeshSelection Select(Camera camera, Rect rect, Rect uiRootRect, GameObject[] gameObjects, bool depthTest, MeshEditorSelectionMode mode)
        {
            MeshSelection selection = new MeshSelection();
            _faceSelection.BeginChange();

            Dictionary<ProBuilderMesh, HashSet<Face>> result = PBUtility.PickFaces(camera, rect, uiRootRect, gameObjects, depthTest);
            if (mode == MeshEditorSelectionMode.Add)
            {
                foreach (KeyValuePair<ProBuilderMesh, HashSet<Face>> kvp in result)
                {
                    ProBuilderMesh mesh = kvp.Key;
                    IList<Face> faces = mesh.faces;
                    IList<int> faceIndices = kvp.Value.Select(f => faces.IndexOf(f)).ToArray();
                    IList<int> notSelected = faceIndices.Where(f => !_faceSelection.IsSelected(mesh, f)).ToArray();
                    foreach (int face in notSelected)
                    {
                        _faceSelection.Add(mesh, face);
                    }

                    selection.SelectedFaces.Add(mesh.gameObject, notSelected);
                }
            }
            else if(mode == MeshEditorSelectionMode.Substract)
            {
                foreach (KeyValuePair<ProBuilderMesh, HashSet<Face>> kvp in result)
                {
                    ProBuilderMesh mesh = kvp.Key;
                    IList<Face> faces = mesh.faces;
                    IList<int> faceIndices = kvp.Value.Select(f => faces.IndexOf(f)).ToArray();
                    IList<int> selected = faceIndices.Where(f => _faceSelection.IsSelected(mesh, f)).ToArray();
                    foreach (int face in selected)
                    {
                        _faceSelection.Remove(mesh, face);
                    }
                    selection.UnselectedFaces.Add(mesh.gameObject, selected);
                }
            }
            else if(mode == MeshEditorSelectionMode.Difference)
            {
                foreach (KeyValuePair<ProBuilderMesh, HashSet<Face>> kvp in result)
                {
                    ProBuilderMesh mesh = kvp.Key;
                    IList<Face> faces = mesh.faces;
                    IList<int> faceIndices = kvp.Value.Select(f => faces.IndexOf(f)).ToArray();

                    IList<int> selected = faceIndices.Where(f => _faceSelection.IsSelected(mesh, f)).ToArray();
                    IList<int> notSelected = faceIndices.Where(f => !_faceSelection.IsSelected(mesh, f)).ToArray();

                    foreach (int face in selected)
                    {
                        _faceSelection.Remove(mesh, face);
                    }

                    foreach(int face in notSelected)
                    {
                        _faceSelection.Add(mesh, face);
                    }

                    selection.UnselectedFaces.Add(mesh.gameObject, selected);
                    selection.SelectedFaces.Add(mesh.gameObject, notSelected);
                }
            }

            _faceSelection.EndChange();

            if(selection.SelectedFaces.Count == 0 && selection.UnselectedFaces.Count == 0)
            {
                selection = null;
            }

            return selection;
        }

        public override MeshSelection Select(Material material)
        {
            MeshSelection selection = IMeshEditorExt.Select(material);

            _faceSelection.BeginChange();
            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces.ToArray())
            {
                IList<int> faces = kvp.Value;
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                for(int i = faces.Count - 1; i >= 0; i--)
                {
                    int face = faces[i];
                    if(_faceSelection.IsSelected(mesh, face))
                    {
                        faces.Remove(face);
                    }
                    else
                    {
                        _faceSelection.Add(mesh, face);
                    }
                }

                if(faces.Count == 0)
                {
                    selection.SelectedFaces.Remove(kvp.Key);
                }
            }
            _faceSelection.EndChange();
            if(selection.SelectedFaces.Count == 0)
            {
                return null;
            }
            return selection;
        }

        public override MeshSelection Unselect(Material material)
        {
            MeshSelection selection = IMeshEditorExt.Select(material);
            selection.Invert();

            _faceSelection.BeginChange();
            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.UnselectedFaces.ToArray())
            {
                IList<int> faces = kvp.Value;
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                for (int i = faces.Count - 1; i >= 0; i--)
                {
                    int face = faces[i];
                    if (!_faceSelection.IsSelected(mesh, face))
                    {
                        faces.Remove(face);
                    }
                    else
                    {
                        _faceSelection.Remove(mesh, face);
                    }
                }

                if (faces.Count == 0)
                {
                    selection.UnselectedFaces.Remove(kvp.Key);
                }
            }
            _faceSelection.EndChange();
            if (selection.UnselectedFaces.Count == 0)
            {
                return null;
            }
            return selection;
        }

        public override void SetSelection(MeshSelection selection)
        {
            if (_faceSelection != null)
            {
                _faceSelection.Clear();
            }

            if (selection != null)
            {
                selection = selection.ToFaces(false);

                _faceSelection.BeginChange();

                foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces)
                {
                    ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                    if(mesh == null)
                    {
                        continue;
                    }

                    PBMesh pbMesh = kvp.Key.GetComponent<PBMesh>();
                    if (pbMesh.IsMarkedAsDestroyed)
                    {
                        continue;
                    }
                    foreach (int face in kvp.Value)
                    {
                        _faceSelection.Add(mesh, face);
                    }
                }

                _faceSelection.EndChange();
            }
        }

        public override MeshSelection GetSelection()
        {
            MeshSelection selection = new MeshSelection();

            foreach (ProBuilderMesh mesh in _faceSelection.Meshes)
            {
                selection.SelectedFaces.Add(mesh.gameObject, _faceSelection.GetFaces(mesh).ToArray());
            }
            if (selection.SelectedFaces.Count > 0)
            {
                return selection;
            }
            return null;
        }

        public override MeshSelection ClearSelection()
        {
            MeshSelection meshSelection = null;
            if (_faceSelection != null)
            {
                meshSelection = ReadSelection();
                _faceSelection.Clear();
            }
            return meshSelection;
        }

        public override MeshEditorState GetState(bool recordUV)
        {
            MeshEditorState state = new MeshEditorState();
            ProBuilderMesh[] meshes = _faceSelection.Meshes.OrderBy(m => m == _faceSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                state.State.Add(mesh.gameObject, new MeshState(mesh.positions.ToArray(), mesh.faces.ToArray(), mesh.textures.ToArray(), recordUV));
            }
            return state;
        }

        public override void SetState(MeshEditorState state)
        {
            _faceSelection.BeginChange();

            ProBuilderMesh[] meshes = state.State.Keys.Select(k => k.GetComponent<ProBuilderMesh>()).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<int> faces = _faceSelection.GetFaces(mesh).ToArray();
                for (int i = 0; i < faces.Count; ++i)
                {
                    _faceSelection.Remove(mesh, faces[i]);
                }

                MeshState meshState = state.State[mesh.gameObject];
                mesh.Rebuild(meshState.Positions, meshState.Faces.Select(f => f.ToFace()).ToArray(), meshState.Textures);

                for (int i = 0; i < faces.Count; ++i)
                {
                    _faceSelection.Add(mesh, faces[i]);
                }
            }

            _faceSelection.EndChange();
        }

        public override void Extrude(float distance)
        {
            _faceSelection.BeginChange();
            
            ProBuilderMesh[] meshes = _faceSelection.Meshes.OrderBy(m => m == _faceSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<int> faceIndexes = _faceSelection.GetFaces(mesh).ToArray();
                for (int i = 0; i < faceIndexes.Count; ++i)
                {
                    _faceSelection.Remove(mesh, faceIndexes[i]);
                }

                IList<Face> faces = new List<Face>();
                mesh.GetFaces(faceIndexes, faces);
                mesh.Extrude(faces, ExtrudeMethod.FaceNormal, distance);

                mesh.ToMesh();
                mesh.Refresh();
                
                for (int i = 0; i < faceIndexes.Count; ++i)
                {
                    _faceSelection.Add(mesh, faceIndexes[i]);
                }
            }

            _faceSelection.EndChange();

            if (distance != 0.0f)
            {
                _faceSelection.Synchronize(
                    _faceSelection.GetCenterOfMass(),
                    _faceSelection.LastPosition + _faceSelection.LastNormal * distance);
            }

            RaisePBMeshesChanged(false);
        }

        public override void InsertFace()
        {
            _faceSelection.BeginChange();

            ProBuilderMesh[] meshes = _faceSelection.Meshes.OrderBy(m => m == _faceSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<int> faceIndexes = _faceSelection.GetFaces(mesh).ToArray();
                for (int i = 0; i < faceIndexes.Count; ++i)
                {
                    _faceSelection.Remove(mesh, faceIndexes[i]);
                }

                IList<Face> faces = new List<Face>();
                mesh.GetFaces(faceIndexes, faces);

                // Extract the unique vertices that define the selected faces
                HashSet<Vertex> uniqueVertices = new HashSet<Vertex>();
                foreach (Face face in faces)
                {
                    var indices = face.distinctIndexes;
                    for (int i = 0; i < indices.Count; i++)
                        uniqueVertices.Add(mesh.GetVertices()[indices[i]]);
                }
                // Create new faces to insert into the selected faces
                int numSelectedFaces = faces.Count;
                Face[] newFaces = new Face[numSelectedFaces];
                Vector3[][] newVertices = new Vector3[numSelectedFaces][];
                Color[][] newColors = new Color[numSelectedFaces][];
                Vector2[][] newUVs = new Vector2[numSelectedFaces][];
                int[][] newSharedIndexes = new int[numSelectedFaces][];
                HashSet<int> usedVertices = new HashSet<int>();
                for (int i = 0; i < numSelectedFaces; i++)
                {
                    // Get the vertices of the selected face and create a new set of vertices that define the shape of the new face
                    Face selectedFace = faces[i];
                    int numFaceVertices = selectedFace.distinctIndexes.Count;
                    Vector3[] newFaceVertices = new Vector3[numFaceVertices];

                    for (int j = 0; j < numFaceVertices; j++)
                    {
                        int vertexIndex = selectedFace.distinctIndexes[j];
                        
                        if (!uniqueVertices.Contains(mesh.GetVertices()[vertexIndex]))
                        {
                            // This vertex is not part of the original face, so add it to the new face
                            newFaceVertices[j] = mesh.GetVertices()[vertexIndex].position;
                        }
                        else
                        {
                            // This vertex is part of the original face, so create a new vertex for the new face
                            Vertex vertex = mesh.GetVertices()[vertexIndex];
                            int newIndex = mesh.GetVertices().Length + usedVertices.Count;
                            var l = mesh.GetVertices().ToList();
                            l.Add(new Vertex(vertex));
                            mesh.SetVertices(l.ToArray());
                            usedVertices.Add(newIndex);
                            newFaceVertices[j] = mesh.GetVertices()[newIndex].position;
                        }
                    }

                    // Create a new face using the new vertices, and set its material index to the same as the original face
                    Face newFace = new Face(selectedFace.distinctIndexes);
                    newFaces[i] = newFace;
                    newVertices[i] = new Vector3[newFace.distinctIndexes.Count];
                    newColors[i] = new Color[newFace.distinctIndexes.Count];
                    newUVs[i] = new Vector2[newFace.distinctIndexes.Count];
                    newSharedIndexes[i] = new int[newFace.distinctIndexes.Count];

                    for (int j = 0; j < newFace.distinctIndexes.Count; j++)
                    {
                        // Set the new vertices, colors, UVs, and shared indexes for the new face
                        int vertexIndex = newFace.distinctIndexes[j];
                        newVertices[i][j] = mesh.GetVertices()[vertexIndex].position;
                        newColors[i][j] = mesh.colors[vertexIndex];
                        newUVs[i][j] = mesh.textures[vertexIndex];
                        Dictionary<int, int> sharedVertexLookup = new Dictionary<int, int>();
                        SharedVertex.GetSharedVertexLookup(mesh.sharedVertices, sharedVertexLookup);
                        newSharedIndexes[i][j] = sharedVertexLookup[vertexIndex];
                    }
                }


                mesh.AppendFaces(newVertices, newColors, newUVs, newFaces, newSharedIndexes);

                for (int i = 0; i < faceIndexes.Count; ++i)
                {
                    _faceSelection.Add(mesh, faceIndexes[i]);
                }

                mesh.ToMesh();
                mesh.Refresh();
            }

            _faceSelection.EndChange();

            _faceSelection.Synchronize(
                _faceSelection.GetCenterOfMass(),
                _faceSelection.LastPosition);

            RaisePBMeshesChanged(false);
        }
        public override void Delete()
        {
            ProBuilderMesh[] meshes = _faceSelection.Meshes.OrderBy(m => m == _faceSelection.LastMesh).ToArray();   
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Face> faces = new List<Face>();
                mesh.GetFaces(_faceSelection.GetFaces(mesh), faces);
                mesh.DeleteFaces(faces);
                mesh.ToMesh();
                mesh.Refresh();
            }
        }

        public override void Subdivide()
        {
            ProBuilderMesh[] meshes = _faceSelection.Meshes.OrderBy(m => m == _faceSelection.LastMesh).ToArray();

            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Face> faces = new List<Face>();
                mesh.GetFaces(_faceSelection.GetFaces(mesh), faces);
                ConnectElements.Connect(mesh, faces);
                mesh.ToMesh();
                mesh.Refresh();
            }
        }

        public override void Merge()
        {
            ProBuilderMesh[] meshes = _faceSelection.Meshes.OrderBy(m => m == _faceSelection.LastMesh).ToArray();

            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<Face> faces = new List<Face>();
                mesh.GetFaces(_faceSelection.GetFaces(mesh), faces);
                Merge(mesh, faces);
                mesh.ToMesh();
                mesh.Refresh();
            }
        }

        private static Face Merge(ProBuilderMesh target, IEnumerable<Face> faces)
        {
            int mergedCount = faces != null ? faces.Count() : 0;

            if (mergedCount < 1)
                return null;

            Face first = faces.First();

            Face mergedFace = new Face();
            mergedFace.SetIndexes(faces.SelectMany(x => x.indexes).ToArray());
            mergedFace.submeshIndex = first.submeshIndex;
            mergedFace.uv = first.uv;
            mergedFace.smoothingGroup = first.smoothingGroup;
            mergedFace.textureGroup = first.textureGroup;
            mergedFace.manualUV = first.manualUV;

            Face[] rebuiltFaces = new Face[target.faces.Count - mergedCount + 1];

            int n = 0;

            HashSet<Face> skip = new HashSet<Face>(faces);

            foreach (Face f in target.faces)
            {
                if (!skip.Contains(f))
                    rebuiltFaces[n++] = f;
            }

            rebuiltFaces[n] = mergedFace;

            target.faces = rebuiltFaces;

            CollapseCoincidentVertices(target, new Face[] { mergedFace });

            return mergedFace;
        }

        /// <summary>
        /// Condense co-incident vertex positions per-face. vertices must already be marked as shared in the sharedIndexes
        /// array to be considered. This method is really only useful after merging faces.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="faces"></param>
        internal static void CollapseCoincidentVertices(ProBuilderMesh mesh, IEnumerable<Face> faces)
        {
            Dictionary<int, int> lookup = new Dictionary<int, int>();
            SharedVertex.GetSharedVertexLookup(mesh.sharedVertices, lookup);
            Dictionary<int, int> matches = new Dictionary<int, int>();

            foreach (Face face in faces)
            {
                matches.Clear();

                int[] indexes = face.indexes.ToArray();
                for (int i = 0; i < indexes.Length; i++)
                {
                    int common = lookup[face.indexes[i]];

                    if (matches.ContainsKey(common))
                        indexes[i] = matches[common];
                    else
                        matches.Add(common, indexes[i]);
                }
                face.SetIndexes(indexes);

                face.Reverse();
                face.Reverse();
            }

            MeshValidation.RemoveUnusedVertices(mesh);
        }

        private void RaisePBMeshesChanged(bool positionsOnly)
        {
            foreach (PBMesh mesh in _faceSelection.PBMeshes)
            {
                mesh.RaiseChanged(positionsOnly, false);
            }
        }
    }
}