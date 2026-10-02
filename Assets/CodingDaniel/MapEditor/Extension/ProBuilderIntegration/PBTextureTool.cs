using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class PBTextureTool
    {
        protected MeshSelection Selection;
        protected Vector3 InitialPosition;
        protected Quaternion InitialRotation;
        protected Matrix4x4 Matrix;
        protected Matrix4x4 MatrixInv;

        protected int[][] Indexes;
        protected Vector2[][] Origins;
        protected Vector2[][] FaceCenters;
        protected internal PBAutoUVConversion.UVTransform[][] UVTransforms;
        protected bool[][] IsManualUv;
        protected ProBuilderMesh[] Meshes;

        private Vector3 GetCenterOfMass(IList<Vector2> textures, Face face)
        {
            IList<int> indexes = face.indexes;
            Vector2 result = textures[indexes[0]];
            for (int i = 1; i < indexes.Count; ++i)
            {
                result += textures[indexes[i]];
            }
            result /= indexes.Count;
            return result;
        }


        public virtual void BeginDrag(MeshSelection selection, Vector3 initialPosition, Quaternion initialRotation)
        {
            Selection = selection.ToFaces(false, false);
            
            InitialPosition = initialPosition;
            InitialRotation = initialRotation;
            Matrix = Matrix4x4.TRS(InitialPosition, InitialRotation, Vector3.one);
            MatrixInv = Matrix.inverse;

            List<ProBuilderMesh> allMeshes = new List<ProBuilderMesh>();
            List<Vector2[]> allOrigins = new List<Vector2[]>();
            List<Vector2[]> allFaceCenters = new List<Vector2[]>();
            List<PBAutoUVConversion.UVTransform[]> allUVTransforms = new List<PBAutoUVConversion.UVTransform[]>();
            List<bool[]> allIsManualUv = new List<bool[]>();
            List<int[]> allIndexes = new List<int[]>();

            HashSet<int> indexes = new HashSet<int>();
            List<Vector2> origins = new List<Vector2>();
            List<Vector2> faceCenters = new List<Vector2>();
            List<PBAutoUVConversion.UVTransform> uvTransforms = new List<PBAutoUVConversion.UVTransform>();
            List<bool> isManualUv = new List<bool>();
            List<Face> faces = new List<Face>();

            foreach (KeyValuePair<GameObject, IList<int>> kvp in Selection.SelectedFaces)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                mesh.GetFaces(kvp.Value, faces);

                IList<Vector2> textures = mesh.textures;
               // IList<Vector4> tangents = mesh.tangents;

                for (int f = 0; f < faces.Count; ++f)
                {
                    Face face = faces[f];
                    IList<int> faceIndexes = face.indexes;
                    for (int i = 0; i < faceIndexes.Count; ++i)
                    {
                        int faceIndex = faceIndexes[i];
                        if (!indexes.Contains(faceIndex))
                        {
                            indexes.Add(faceIndex);
                            origins.Add(textures[faceIndex]);
                            faceCenters.Add(GetCenterOfMass(textures, face));

                            PBAutoUVConversion.UVTransform transform = PBAutoUVConversion.GetUVTransform(mesh, face);
                            if(!face.manualUV)
                            {
                                Vector2 scale =  transform.scale;
                                scale.x = -scale.x;
                                transform.scale = scale;
                            }

                            uvTransforms.Add(transform);
                        }
                    }

                    isManualUv.Add(face.manualUV);
                    face.manualUV = true;
                }

                allIndexes.Add(indexes.ToArray());
                allOrigins.Add(origins.ToArray());
                allFaceCenters.Add(faceCenters.ToArray());
                allUVTransforms.Add(uvTransforms.ToArray());
                allIsManualUv.Add(isManualUv.ToArray());
                allMeshes.Add(mesh);

                indexes.Clear();
                origins.Clear();
                faceCenters.Clear();
                uvTransforms.Clear();
                isManualUv.Clear();
                faces.Clear();
            }

            Indexes = allIndexes.ToArray();
            Origins = allOrigins.ToArray();
            FaceCenters = allFaceCenters.ToArray();
            UVTransforms = allUVTransforms.ToArray();
            IsManualUv = allIsManualUv.ToArray();
            Meshes = allMeshes.ToArray();
        }

        public virtual void Drag(Transform pivot)
        {
            Drag(pivot.position, pivot.rotation, pivot.localScale);
        }

        public virtual void Drag(Vector3 position, Quaternion rotation, Vector3 scale)
        {

        }

        public virtual void EndDrag(bool refreshMeshes = false)
        {
            List<Face> faces = new List<Face>();
            for (int m = 0; m < Meshes.Length; ++m)
            {
                ProBuilderMesh mesh = Meshes[m];
                bool[] isManualUv = IsManualUv[m];
                int[] indexes = Indexes[m];

                mesh.GetFaces(Selection.SelectedFaces[mesh.gameObject], faces);

                for (int i = 0; i < isManualUv.Length; ++i)
                {
                    faces[i].manualUV = isManualUv[i];
                }

                PBAutoUVConversion.SetAutoAndAlignUnwrapParamsToUVs(mesh, faces.Where(x => !x.manualUV));
                faces.Clear();
            }

            if(refreshMeshes)
            {
                for (int m = 0; m < Meshes.Length; ++m)
                {
                    ProBuilderMesh mesh = Meshes[m];
                    mesh.ToMesh();
                    mesh.Refresh();
                }
            }

            Indexes = null;
            Origins = null;
            FaceCenters = null;
            UVTransforms = null;
            IsManualUv = null;
            Meshes = null;
        }
    }
}