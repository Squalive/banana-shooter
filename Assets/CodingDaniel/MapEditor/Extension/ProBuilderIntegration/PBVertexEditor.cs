using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class PBVertexEditor : PBBaseEditor
    {
        [HideInInspector]
        [FormerlySerializedAs("m_vertexSelection")]
        public PBVertexSelection _vertexSelection;
        private SceneSelection _selection = new SceneSelection();
        
        private int[][] _initialIndexes;
        private Vector3[][] _initialPositions;
        private Vector3 _initialPostion;
        private Quaternion _initialRotation;
        
        public override bool HasSelection
        {
            get { return _vertexSelection.VerticesCount > 0; }
        }

        public override Vector3 Position
        {
            get { return CenterMode ? _vertexSelection.CenterOfMass : _vertexSelection.LastPosition; }
            set { MoveTo(value); }
        }

        public override Vector3 Normal
        {
            get { return (!GlobalMode && _vertexSelection.LastMesh != null) ? _vertexSelection.LastMesh.transform.forward : Vector3.forward; }
        }


        public override Quaternion Rotation
        {
            get
            {
                if (GlobalMode || _vertexSelection.LastMesh == null)
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
                if (selection.SelectedFaces.TryGetValue(_vertexSelection.LastMesh.gameObject, out faceIndexes))
                {
                    if (faceIndexes.Count != 0)
                    {
                        return HandleUtility.GetRotation(_vertexSelection.LastMesh, _vertexSelection.LastMesh.faces[faceIndexes.Last()].distinctIndexes);
                    }
                }

                IList<int> vertices;
                if (!selection.SelectedIndices.TryGetValue(_vertexSelection.LastMesh.gameObject, out vertices) || vertices.Count == 0)
                {
                    return Quaternion.identity;
                }

                return HandleUtility.GetRotation(_vertexSelection.LastMesh, vertices);
            }
        }

        public override GameObject Target
        {
            get { return _vertexSelection.LastMesh != null ? _vertexSelection.LastMesh.gameObject : null; }
        }


        private void OnDestroy()
        {
            if (_vertexSelection != null)
            {
                Destroy(_vertexSelection);
            }
        }

        public override void SetSelection(MeshSelection selection)
        {
            _vertexSelection.Clear();
            _selection.Clear();

            if (selection != null)
            {
                selection = selection.ToVertices(false);
                foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedIndices)
                {
                    PBMesh pbMesh = kvp.Key.GetComponent<PBMesh>();
                    if (pbMesh.IsMarkedAsDestroyed)
                    {
                        continue;
                    }

                    _vertexSelection.Add(kvp.Key.GetComponent<ProBuilderMesh>(), kvp.Value);
                }
            }
        }

        public override MeshSelection GetSelection()
        {
            MeshSelection selection = new MeshSelection();

            foreach (ProBuilderMesh mesh in _vertexSelection.Meshes)
            {
                selection.SelectedIndices.Add(mesh.gameObject, _vertexSelection.GetVertices(mesh).ToArray());
            }
            if (selection.SelectedIndices.Count > 0)
            {
                return selection;
            }
            return null;
        }

        public override MeshSelection ClearSelection()
        {
            MeshSelection selection = new MeshSelection();

            foreach(ProBuilderMesh mesh in _vertexSelection.Meshes)
            {
                if(mesh != null && mesh.gameObject != null)
                {
                    selection.UnselectedIndices.Add(mesh.gameObject, _vertexSelection.GetVertices(mesh).ToArray());
                }
            }

            _vertexSelection.Clear();
            _selection.Clear();

            if(selection.UnselectedIndices.Count > 0)
            {
                return selection;
            }
            return null;
        }

        public override void Hover(Camera camera, Vector3 pointer)
        {
          
        }

        public override MeshSelection Select(Camera camera, Vector3 pointer, bool shift, bool ctrl, bool depthTest)
        {
            MeshSelection selection = null;
            GameObject pickedObject = PBUtility.PickObject(camera, pointer);
            float result = PBUtility.PickVertex(camera, pointer, 20, pickedObject, _vertexSelection.Meshes, depthTest, ref _selection);

            if(result != Mathf.Infinity)
            {
#if PROBUILDER_4_4_0_OR_NEWER
                int selectedVertex = _selection.vertexes[0];
#else
                int selectedVertex = _selection.vertex;
#endif
                if (_vertexSelection.IsSelected(_selection.mesh, selectedVertex))
                {
                    if(shift)
                    {
                        List<int> indices = _selection.mesh.GetCoincidentVertices(new[] { selectedVertex });
                        _vertexSelection.Remove(_selection.mesh, indices);
                        selection = new MeshSelection();
                        selection.UnselectedIndices.Add(_selection.mesh.gameObject, indices);
                    }
                    else
                    {
                        List<int> indices = _selection.mesh.GetCoincidentVertices(new[] { selectedVertex });

                        selection = ReadSelection();
                        selection.UnselectedIndices[_selection.mesh.gameObject] = selection.UnselectedIndices[_selection.mesh.gameObject].Where(i => i != selectedVertex).ToArray();
                        selection.SelectedIndices.Add(_selection.mesh.gameObject, indices);
                        _vertexSelection.Clear();
                        _vertexSelection.Add(_selection.mesh, indices);
                    }
                }
                else
                {
                    if(shift)
                    {
                        selection = new MeshSelection();
                    }
                    else
                    {
                        selection = ReadSelection();
                        _vertexSelection.Clear();
                    }

                    List<int> indices = _selection.mesh.GetCoincidentVertices(new[] { selectedVertex });

                    _vertexSelection.Add(_selection.mesh, indices);
                    selection.SelectedIndices.Add(_selection.mesh.gameObject, indices);
                }
            }
            else
            {
                if (!shift)
                {
                    selection = ReadSelection();
                    if (selection.UnselectedIndices.Count == 0)
                    {
                        selection = null;
                    }
                    _vertexSelection.Clear(); 
                }
            }
            return selection;
        }

        private MeshSelection ReadSelection()
        {
            MeshSelection selection = new MeshSelection();
            ProBuilderMesh[] meshes = _vertexSelection.Meshes.OrderBy(m => m == _vertexSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<int> indices = _vertexSelection.GetVertices(mesh);
                if (indices != null)
                {
                    selection.UnselectedIndices.Add(mesh.gameObject, indices.ToArray());
                }
            }
            return selection;
        }

        public override MeshSelection Select(Camera camera, Rect rect, Rect uiRootRect, GameObject[] gameObjects, bool depthTest, MeshEditorSelectionMode mode)
        {
            Dictionary<ProBuilderMesh, HashSet<int>> pickResult = PBUtility.PickVertices(camera, rect, uiRootRect, gameObjects, depthTest);
            if(pickResult.Count == 0)
            {
                return null;
            }

            MeshSelection selection = new MeshSelection();
            foreach (KeyValuePair<ProBuilderMesh, HashSet<int>> kvp in pickResult)
            {
                ProBuilderMesh mesh = kvp.Key;
                HashSet<int> sharedIndexes = kvp.Value;
                IList<SharedVertex> sharedVertices = mesh.sharedVertices;

                HashSet<int> indices = new HashSet<int>();
                foreach(int sharedIndex in sharedIndexes)
                {
                    SharedVertex sharedVertex = sharedVertices[sharedIndex];
                    for(int j = 0; j < sharedVertex.Count; ++j)
                    {
                        if(!indices.Contains(sharedVertex[j]))
                        {
                            indices.Add(sharedVertex[j]);
                        }
                    }    
                }

                if (mode == MeshEditorSelectionMode.Substract || mode == MeshEditorSelectionMode.Difference)
                {

                    IList<int> selected = mesh.GetCoincidentVertices(indices).Where(index => _vertexSelection.IsSelected(mesh, index)).ToArray();
                    selection.UnselectedIndices.Add(mesh.gameObject, selected);
                    _vertexSelection.Remove(mesh, selected);
                }

                if (mode == MeshEditorSelectionMode.Add || mode == MeshEditorSelectionMode.Difference)
                {
                    IList<int> notSelected = mesh.GetCoincidentVertices(indices).Where(index => !_vertexSelection.IsSelected(mesh, index)).ToArray();
                    selection.SelectedIndices.Add(mesh.gameObject, notSelected);
                    _vertexSelection.Add(mesh, notSelected);
                }
            }
            return selection;
        }

        public override MeshSelection Select(Material material)
        {
            MeshSelection selection = IMeshEditorExt.Select(material);
            selection = selection.ToVertices(false);
            
            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedIndices.ToArray())
            {
                IList<int> indices = kvp.Value;
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                for (int i = indices.Count - 1; i >= 0; i--)
                {
                    int index = indices[i];
                    if (_vertexSelection.IsSelected(mesh, index))
                    {
                        indices.Remove(index);
                    }
                }

                if (indices.Count == 0)
                {
                    selection.SelectedIndices.Remove(kvp.Key);
                }
                else
                {
                    _vertexSelection.Add(mesh, indices);
                }
            }
            if (selection.SelectedIndices.Count == 0)
            {
                return null;
            }
            return selection;
        }

        public override MeshSelection Unselect(Material material)
        {
            MeshSelection selection = IMeshEditorExt.Select(material);
            selection = selection.ToVertices(true);

            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.UnselectedIndices.ToArray())
            {
                IList<int> indices = kvp.Value;
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                for (int i = indices.Count - 1; i >= 0; i--)
                {
                    int index = indices[i];
                    if (!_vertexSelection.IsSelected(mesh, index))
                    {
                        indices.Remove(index);
                    }
                }

                if (indices.Count == 0)
                {
                    selection.UnselectedIndices.Remove(kvp.Key);
                }
                else
                {
                    _vertexSelection.Remove(mesh, indices);
                }
            }
            if (selection.UnselectedIndices.Count == 0)
            {
                return null;
            }
            return selection;
        }

        public override MeshEditorState GetState(bool recordUV)
        {
            MeshEditorState state = new MeshEditorState();
            ProBuilderMesh[] meshes = _vertexSelection.Meshes.OrderBy(m => m == _vertexSelection.LastMesh).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                state.State.Add(mesh.gameObject, new MeshState(mesh.positions.ToArray(), mesh.faces.ToArray(), mesh.textures.ToArray(), recordUV));
            }
            return state;
        }

        public override void SetState(MeshEditorState state)
        {   
            ProBuilderMesh[] meshes = state.State.Keys.Select(k => k.GetComponent<ProBuilderMesh>()).ToArray();
            foreach (ProBuilderMesh mesh in meshes)
            {
                IList<int> vertices = _vertexSelection.GetVertices(mesh);
                if(vertices != null)
                {
                    vertices = vertices.ToArray();
                    _vertexSelection.Remove(mesh, vertices);
                }
                
                MeshState meshState = state.State[mesh.gameObject];
                mesh.Rebuild(meshState.Positions, meshState.Faces.Select(f => f.ToFace()).ToArray(), meshState.Textures);

                if(vertices != null)
                {
                    _vertexSelection.Add(mesh, vertices);
                }
                
            }
        }

        

        public override void BeginMove()
        {
            base.BeginMove();
        }

        public override void EndMove()
        {
            base.EndMove();
            _vertexSelection.Meshes.RefreshColliders();
        }

        private void MoveTo(Vector3 to)
        {
            if (_vertexSelection.MeshesCount == 0)
            {
                return;
            }

            Vector3 from = Position;
            Vector3 offset = to - from;

            IEnumerable<ProBuilderMesh> meshes = _vertexSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                Vector3 localOffset = mesh.transform.InverseTransformVector(offset);
                IEnumerable<int> vertices = _vertexSelection.GetVertices(mesh);
                mesh.TranslateVertices(vertices, localOffset);

                mesh.ToMesh();
                mesh.Refresh();
            }

            ProBuilderMesh lastMesh = _vertexSelection.LastMesh;
            if (lastMesh != null)
            {
                _vertexSelection.Synchronize(
                    _vertexSelection.CenterOfMass + offset,
                    _vertexSelection.LastPosition + offset,
                    _vertexSelection.LastNormal);
            }

            RaisePBMeshesChanged(true);
        }

        public override void BeginRotate(Quaternion initialRotation)
        {
            _initialPostion = _vertexSelection.LastPosition;
            _initialPositions = new Vector3[_vertexSelection.MeshesCount][];
            _initialIndexes = new int[_vertexSelection.MeshesCount][];
            _initialRotation = Quaternion.Inverse(initialRotation);

            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _vertexSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                _initialIndexes[meshIndex] = mesh.GetCoincidentVertices(_vertexSelection.GetVertices(mesh)).ToArray();
                _initialPositions[meshIndex] = mesh.GetVertices(_initialIndexes[meshIndex]).Select(v => mesh.transform.TransformPoint(v.position)).ToArray();
                meshIndex++;
            }
        }

        public override void EndRotate()
        {
            _initialPositions = null;
            _initialIndexes = null;

#if PROBUILDER_4_4_0_OR_NEWER
            _vertexSelection.Meshes.RefreshColliders();
#endif
        }

        public override void Rotate(Quaternion rotation)
        {
            if (_vertexSelection.MeshesCount == 0)
            {
                return;
            }

            Vector3 center = Position;
            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _vertexSelection.Meshes;
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

           
            _vertexSelection.Synchronize(
                _vertexSelection.CenterOfMass,
                center + rotation * _initialRotation * (_initialPostion - center),
                rotation * _initialRotation * _vertexSelection.LastNormal);

            RaisePBMeshesChanged(true);
        }

        public override void BeginScale()
        {
            _initialPostion = _vertexSelection.LastPosition;
            _initialPositions = new Vector3[_vertexSelection.MeshesCount][];
            _initialIndexes = new int[_vertexSelection.MeshesCount][];

            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _vertexSelection.Meshes;
            foreach (ProBuilderMesh mesh in meshes)
            {
                _initialIndexes[meshIndex] = mesh.GetCoincidentVertices(_vertexSelection.GetVertices(mesh)).ToArray();
                _initialPositions[meshIndex] = mesh.GetVertices(_initialIndexes[meshIndex]).Select(v => mesh.transform.TransformPoint(v.position)).ToArray();
                meshIndex++;
            }
        }

        public override void EndScale()
        {
            _initialPositions = null;
            _initialIndexes = null;

#if PROBUILDER_4_4_0_OR_NEWER
            _vertexSelection.Meshes.RefreshColliders();
#endif
        }

        public override void Scale(Vector3 scale, Quaternion rotation)
        {
            if (_vertexSelection.MeshesCount == 0)
            {
                return;
            }

            Vector3 center = Position;
            int meshIndex = 0;
            IEnumerable<ProBuilderMesh> meshes = _vertexSelection.Meshes;

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

            _vertexSelection.Synchronize(
                _vertexSelection.CenterOfMass,
                center + rotation * Vector3.Scale(Quaternion.Inverse(rotation) * (_initialPostion - center), scale),
                rotation * _initialRotation * _vertexSelection.LastNormal);

            RaisePBMeshesChanged(true);
        }

        private void RaisePBMeshesChanged(bool positionsOnly)
        {
            foreach (PBMesh mesh in _vertexSelection.PBMeshes)
            {
                mesh.RaiseChanged(positionsOnly, false);
            }
        }

        public override void Delete()
        {
            MeshSelection selection = GetSelection();
            selection = selection.ToFaces(false, true);

            foreach(KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                mesh.DeleteFaces(kvp.Value);
                mesh.ToMesh();
                mesh.Refresh();
            }
        }
        
        public override void Merge()
        {
            ProBuilderMesh[] meshes = _vertexSelection.Meshes.OrderBy(m => m == _vertexSelection.LastMesh).ToArray();

            foreach (var mesh in meshes)
            {
                int index= mesh.MergeVertices(_vertexSelection.GetVertices(mesh).ToArray(), false);

                _vertexSelection.Clear();
                
                _vertexSelection.Add(mesh,new List<int> {index});
            }
        }
    }
}