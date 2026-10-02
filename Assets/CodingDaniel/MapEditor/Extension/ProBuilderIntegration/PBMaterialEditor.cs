using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public struct FaceToSubmeshIndex
    {
        public ProBuilderMesh Mesh;
        public int FaceIndex;
        public int SubmeshIndex;

        public FaceToSubmeshIndex(ProBuilderMesh mesh, int faceIndex, int submeshIndex)
        {
            Mesh = mesh;
            SubmeshIndex = submeshIndex;
            FaceIndex = faceIndex;
        }
    }

    public class MeshMaterialsState
    {
        public List<FaceToSubmeshIndex> FaceToSubmeshIndex;
        public Dictionary<ProBuilderMesh, object[]> Materials;
        public MeshMaterialsState()
        {
            FaceToSubmeshIndex = new List<FaceToSubmeshIndex>();
            Materials = new Dictionary<ProBuilderMesh, object[]>();
        }

        public MeshMaterialsState(List<FaceToSubmeshIndex> faceToSubeshIndex, Dictionary<ProBuilderMesh, object[]> materials)
        {
            FaceToSubmeshIndex = faceToSubeshIndex;
            Materials = materials;
        }

        public bool Erase(object oldReference, object newReference)
        {
            foreach(object[] materials in Materials.Values)
            {
                for(int i = 0; i < materials.Length; ++i)
                {
                    if(materials[i] == oldReference)
                    {
                        materials[i] = newReference;
                    }
                }
            }
            return false;
        }
    }
    
    public class ApplyMaterialResult
    {
        public MeshMaterialsState OldState;
        public MeshMaterialsState NewState;

        public ApplyMaterialResult(MeshMaterialsState oldState, MeshMaterialsState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public ApplyMaterialResult()
        {
            OldState = new MeshMaterialsState();
            NewState = new MeshMaterialsState();
        }
    }
    public interface IMaterialEditor
    {
        event Action MaterialsApplied;

        ApplyMaterialResult ApplyMaterial(Material material, MeshSelection selection, Camera camera, Vector3 mousePosition);
        ApplyMaterialResult ApplyMaterial(Material material, MeshSelection selection);
        ApplyMaterialResult ApplyMaterial(Material material, GameObject gameObject, int submeshIndex);
        void ApplyMaterials(MeshMaterialsState state);
    }
    public class PBMaterialEditor : MonoBehaviour,IMaterialEditor
    {
         public event Action MaterialsApplied;

        public ApplyMaterialResult ApplyMaterial(Material material, MeshSelection selection)
        {
            if(selection == null)
            {
                return null;
            }

            selection = selection.ToFaces(false, false);

            MeshMaterialsState oldState = new MeshMaterialsState();
            MeshMaterialsState newState = new MeshMaterialsState();

            List<Face> faces = new List<Face>();
            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();

                AddAllFacesToState(oldState, mesh);
                AddMaterialsToState(oldState, mesh);

                faces.Clear();
                mesh.GetFaces(kvp.Value, faces);
                
                mesh.SetMaterial(faces, material);
                mesh.Refresh();
                mesh.ToMesh();

                RemoveUnusedMaterials(mesh);
                mesh.Refresh();
                mesh.ToMesh();

                GetFaceToSubmeshIndexes(newState.FaceToSubmeshIndex, kvp, mesh);
                AddMaterialsToState(newState, mesh);
            }

            if (MaterialsApplied != null)
            {
                MaterialsApplied();
            }

            if (oldState.FaceToSubmeshIndex.Count == 0 && newState.FaceToSubmeshIndex.Count == 0)
            {
                return null;
            }

            return new ApplyMaterialResult(oldState, newState);
        }

        private static void AddMaterialsToState(MeshMaterialsState state, ProBuilderMesh mesh)
        {
            Renderer renderer = mesh.GetComponent<Renderer>();
            Material[] materials;
            if (renderer != null)
            {
                materials = renderer.sharedMaterials.ToArray();
            }
            else
            {
                materials = new Material[0];
            }
            state.Materials.Add(mesh, materials);
        }

        private static void GetFaceToSubmeshIndexes(List<FaceToSubmeshIndex> result, KeyValuePair<GameObject, IList<int>> kvp, ProBuilderMesh mesh)
        {
            IList<Face> faces = mesh.faces;
            foreach (int faceIndex in kvp.Value)
            {
                result.Add(new FaceToSubmeshIndex(mesh, faceIndex, faces[faceIndex].submeshIndex));
            }
        }

        public ApplyMaterialResult ApplyMaterial(Material material, MeshSelection selection, Camera camera, Vector3 mousePosition)
        {
            MeshAndFace meshAndFace = PBUtility.PickFace(camera, mousePosition);
            if(meshAndFace.mesh != null && meshAndFace.face != null)
            {
                MeshMaterialsState oldState = new MeshMaterialsState();
                MeshMaterialsState newState = new MeshMaterialsState();

                int faceIndex = meshAndFace.mesh.faces.IndexOf(meshAndFace.face);

                IList<int> faceIndexes;
                if(selection != null && selection.SelectedFaces.TryGetValue(meshAndFace.mesh.gameObject, out faceIndexes))
                {
                    if(faceIndexes.Contains(faceIndex))
                    {
                        return ApplyMaterial(material, selection);
                    }
                }

                AddAllFacesToState(oldState, meshAndFace.mesh);
                AddMaterialsToState(oldState, meshAndFace.mesh);

                meshAndFace.mesh.SetMaterial(new[] { meshAndFace.face }, material);                
                meshAndFace.mesh.Refresh();
                meshAndFace.mesh.ToMesh();

                RemoveUnusedMaterials(meshAndFace.mesh);
                meshAndFace.mesh.Refresh();
                meshAndFace.mesh.ToMesh();

                FaceToSubmeshIndex newFaceToIndex = new FaceToSubmeshIndex(meshAndFace.mesh, meshAndFace.mesh.faces.IndexOf(meshAndFace.face), meshAndFace.face.submeshIndex);
                newState.FaceToSubmeshIndex.Add(newFaceToIndex);
                AddMaterialsToState(newState, meshAndFace.mesh);

                if (MaterialsApplied != null)
                {
                    MaterialsApplied();
                }

                return new ApplyMaterialResult(oldState, newState);
            }
            return null;
        }

        public ApplyMaterialResult ApplyMaterial(Material material, GameObject gameObject, int submeshIndex)
        {
            MeshMaterialsState oldState = new MeshMaterialsState();
            MeshMaterialsState newState = new MeshMaterialsState();
            ProBuilderMesh[] meshes = gameObject.GetComponentsInChildren<ProBuilderMesh>(true);
            for(int i = 0; i < meshes.Length; ++i)
            {
                ProBuilderMesh mesh = meshes[i];
                AddAllFacesToState(oldState, mesh);
                AddMaterialsToState(oldState, mesh);

                if(submeshIndex < 0)
                {
                    mesh.SetMaterial(mesh.faces, material);
                }
                else
                {
                    mesh.SetMaterial(mesh.faces.Where(f => f.submeshIndex == submeshIndex), material);
                }
                
                mesh.Refresh();
                mesh.ToMesh();

                RemoveUnusedMaterials(mesh);
                mesh.Refresh();
                mesh.ToMesh();

                AddAllFacesToState(newState, mesh);
                AddMaterialsToState(newState, mesh);
            }

            if (MaterialsApplied != null)
            {
                MaterialsApplied();
            }

            if (oldState.FaceToSubmeshIndex.Count == 0 && newState.FaceToSubmeshIndex.Count == 0)
            {
                return null;
            }

            return new ApplyMaterialResult(oldState, newState);
        }

        private static void AddAllFacesToState(MeshMaterialsState state, ProBuilderMesh mesh)
        {
            IList<Face> faces = mesh.faces;
            for (int j = 0; j < faces.Count; ++j)
            {
                state.FaceToSubmeshIndex.Add(new FaceToSubmeshIndex(mesh, j, faces[j].submeshIndex));
            }
        }

        public void ApplyMaterials(MeshMaterialsState state)
        {
            foreach(KeyValuePair<ProBuilderMesh, object[]> kvp in state.Materials)
            {
                ProBuilderMesh mesh = kvp.Key;
                Renderer renderer = mesh.GetComponent<Renderer>();
                if(renderer != null)
                {
                    Material[] materials = new Material[kvp.Value.Length];
                    for(int i = 0; i < materials.Length; ++i)
                    {
                        if(kvp.Value[i] is Material)
                        {
                            materials[i] = (Material)kvp.Value[i];
                        }
                    }

                    renderer.sharedMaterials = materials;
                }
            }

            IList<FaceToSubmeshIndex> faceToSubmeshIndexes = state.FaceToSubmeshIndex;
            Dictionary<ProBuilderMesh, Renderer> meshToRenderer = new Dictionary<ProBuilderMesh, Renderer>();
            Dictionary<ProBuilderMesh, Dictionary<int, List<Face>>> meshToFaces = new Dictionary<ProBuilderMesh, Dictionary<int, List<Face>>>();
            for(int i = 0; i < faceToSubmeshIndexes.Count; ++i)
            {
                FaceToSubmeshIndex faceToSubmeshIndex = faceToSubmeshIndexes[i];
                ProBuilderMesh mesh = faceToSubmeshIndex.Mesh;
                
                if(!meshToRenderer.ContainsKey(mesh))
                {
                    Renderer renderer = mesh.GetComponent<Renderer>();
                    meshToRenderer.Add(mesh, renderer);
                }

                Dictionary<int, List<Face>> indexToFaces;
                if(!meshToFaces.TryGetValue(mesh, out indexToFaces))
                {
                    indexToFaces = new Dictionary<int, List<Face>>();
                    meshToFaces.Add(mesh, indexToFaces);
                }

                List<Face> faceList;
                if(!indexToFaces.TryGetValue(faceToSubmeshIndex.SubmeshIndex, out faceList))
                {
                    faceList = new List<Face>();
                    indexToFaces.Add(faceToSubmeshIndex.SubmeshIndex, faceList);
                }

                Face face = mesh.faces[faceToSubmeshIndex.FaceIndex];
                faceList.Add(face);
            }

            foreach(KeyValuePair<ProBuilderMesh, Dictionary<int, List<Face>>> meshToFace in meshToFaces)
            {
                ProBuilderMesh mesh = meshToFace.Key;
                Dictionary<int, List<Face>> indexToFaces = meshToFace.Value;
                Renderer renderer = meshToRenderer[mesh];

                foreach(KeyValuePair<int, List<Face>> kvp in indexToFaces)
                {
                    int submeshIndex = kvp.Key;
                    List<Face> faceList = kvp.Value;

                    Material material = renderer.sharedMaterials[submeshIndex];
                    mesh.SetMaterial(faceList, material);
                }

                mesh.Refresh();
                mesh.ToMesh();
            }

            if (MaterialsApplied != null)
            {
                MaterialsApplied();
            }
        }

        private void RemoveUnusedMaterials(ProBuilderMesh mesh)
        {
            Renderer renderer = mesh.GetComponent<Renderer>();
            List<Material> materials = renderer.sharedMaterials.ToList();
            HashSet<int> submeshIndices = new HashSet<int>();
            IList<Face> faces = mesh.faces;
            for(int i = 0; i < faces.Count; ++i)
            {
                Face face = faces[i];
                if(!submeshIndices.Contains(face.submeshIndex))
                {
                    submeshIndices.Add(face.submeshIndex);
                }
            }

            for(int i = materials.Count - 1; i >= 0; --i)
            {
                if(!submeshIndices.Contains(i))
                {
                    materials.RemoveAt(i);
                    for(int f = 0; f < faces.Count; ++f)
                    {
                        if(faces[f].submeshIndex > i)
                        {
                            faces[f].submeshIndex--;
                        }
                    }
                }
            }

            renderer.sharedMaterials = materials.ToArray();
        }
    }
}
