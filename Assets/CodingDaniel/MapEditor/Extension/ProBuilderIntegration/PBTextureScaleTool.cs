using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class PBTextureScaleTool : PBTextureTool
    {
        public override void Drag(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if(Meshes == null)
            {
                return;
            }

            scale.x = 1f / scale.x;
            scale.y = 1f / scale.y;

            List<Face> faces = new List<Face>();
            for (int m = 0; m < Meshes.Length; ++m)
            {
                ProBuilderMesh mesh = Meshes[m];
                Vector2[] origins = Origins[m];
                Vector2[] centers = FaceCenters[m];
                int[] indexes = Indexes[m];

                Vector2[] textures = mesh.textures.ToArray();

                for (int i = 0; i < indexes.Length; ++i)
                {
                    int index = indexes[i];
                    textures[index] = centers[i] + Vector2.Scale(origins[i] - centers[i], scale);
                }

                mesh.textures = textures;
                mesh.GetFaces(Selection.SelectedFaces[mesh.gameObject], faces);

                mesh.RefreshUV(faces);
                faces.Clear();
            }
        }
    }
}