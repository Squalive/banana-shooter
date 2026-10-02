using System.Collections.Generic;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class MaterialPalette : MonoBehaviour
    {
        public List<Material> Materials = new List<Material>();

        public Material GetMaterialWithTexture(Texture2D texture)//texutre)
        {
            if(Materials == null)
            {
                return null;
            }

            for(int i = 0; i < Materials.Count; ++i)
            {
                Material material = Materials[i];
                if(material != null && material.MainTexture() == texture)
                {
                    return material;
                }
                /*
                else if (material != null && material.name == texture.name)//GK MainTexture().GetType() == typeof(Texture2D))
                {
                    return material;
                }*/
            }

            return null;
        }
    }
}
