using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class PBTextureMoveTool : PBTextureTool
    {
        private static float vector3Magnitude = Vector3.one.magnitude;

        public override void Drag(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (Meshes == null)
            {
                return;
            }

            Vector3 lp = MatrixInv.MultiplyPoint(position);

            Vector2 delta = new Vector2(-lp.x, -lp.y);

            List<Face> faces = new List<Face>();

            for(int m = 0; m < Meshes.Length; ++m)
            {
                ProBuilderMesh mesh = Meshes[m];
                Vector2[] origins = Origins[m];
                var uvTransforms = UVTransforms[m];
                int[] indexes = Indexes[m];

                Vector2[] textures = mesh.textures.ToArray();
                IList<Vector4> tangents = mesh.tangents;

                // Account for object scale
                delta *= vector3Magnitude / mesh.transform.lossyScale.magnitude;

                for (int i = 0; i < indexes.Length; ++i)
                {
                    int index = indexes[i];
                    var uvTransform = uvTransforms[i];
                    textures[index] = origins[i] + new Vector2(tangents[index].w * delta.x / uvTransform.scale.x, delta.y / uvTransform.scale.y);
                }

                mesh.textures = textures;
                mesh.GetFaces(Selection.SelectedFaces[mesh.gameObject], faces);
               
                mesh.RefreshUV(faces);
                faces.Clear();
            }
        }
    }
}