using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class PBTextureRotateTool : PBTextureTool
    {
        public override void Drag(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (Meshes == null)
            {
                return;
            }

            float angle = (Quaternion.Inverse(InitialRotation) * rotation).eulerAngles.z;

            List<Face> faces = new List<Face>();
            for (int m = 0; m < Meshes.Length; ++m)
            {
                ProBuilderMesh mesh = Meshes[m];
                Vector2[] origins = Origins[m];
                Vector2[] centers = FaceCenters[m];
                var uvTransforms = UVTransforms[m];
                int[] indexes = Indexes[m];

                Vector2[] textures = mesh.textures.ToArray();
                IList<Vector4> tangents = mesh.tangents;

                for (int i = 0; i < indexes.Length; ++i)
                {
                    int index = indexes[i];
                    textures[index] = centers[i] + PBMath.RotateAroundPoint(origins[i] - centers[i], Vector2.zero, angle * tangents[index].w);
                }

                mesh.textures = textures;
                mesh.GetFaces(Selection.SelectedFaces[mesh.gameObject], faces);

                mesh.RefreshUV(faces);
                faces.Clear();
            }
        }
    }
}