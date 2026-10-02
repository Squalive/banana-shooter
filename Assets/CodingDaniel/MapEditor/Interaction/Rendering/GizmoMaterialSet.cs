using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodingDaniel.MapEditor.Interaction.Rendering
{
    /// <summary>
    /// The materials the transform handles draw with.
    ///
    /// These were created inline by MEHandleComponent.Initialize and destroyed by its Cleanup, which
    /// made a 1,200 line component responsible for shader lookups. They now have an owner that exists
    /// only to hold and dispose them.
    /// </summary>
    public sealed class GizmoMaterialSet : IDisposable
    {
        public Material Lines { get; private set; }
        public Material LinesClip { get; private set; }
        public Material LinesClipUsingClipPlane { get; private set; }
        public Material LinesBillboard { get; private set; }
        public Material ShapesZTest { get; private set; }
        public Material ShapesZTest2 { get; private set; }
        public Material ShapesZTest3 { get; private set; }
        public Material ShapesZTest4 { get; private set; }
        public Material ShapesZTestOffset { get; private set; }
        public Material UnlitColor { get; private set; }

        public GizmoMaterialSet(float handleScale)
        {
            Lines = CreateLineMaterial("CodingDaniel/MEBuilder/LineBillboard", handleScale);
            LinesClip = CreateLineMaterial("CodingDaniel/MEHandles/LineBillboardClip", handleScale);
            LinesBillboard = CreateLineMaterial("CodingDaniel/MEBuilder/LineBillboard", handleScale);

            LinesClipUsingClipPlane = new Material(Shader.Find("CodingDaniel/MEHandles/VertexColorClipUsingClipPlane"));
            LinesClipUsingClipPlane.color = Color.white;

            ShapesZTest = CreateShapeMaterial(new Color(1f, 1f, 1f, 1f));
            ShapesZTestOffset = CreateShapeMaterial(new Color(1f, 1f, 1f, 1f));
            ShapesZTestOffset.SetFloat("_OFactors", -1.0f);
            ShapesZTestOffset.SetFloat("_OUnits", -1.0f);

            ShapesZTest2 = CreateShapeMaterial(new Color(1f, 1f, 1f, 0f));
            ShapesZTest3 = CreateShapeMaterial(new Color(1f, 1f, 1f, 0f));
            ShapesZTest4 = CreateShapeMaterial(new Color(1f, 1f, 1f, 0f));

            UnlitColor = new Material(Shader.Find("Hidden/MECommon/UnlitColor"));
        }

        private static Material CreateLineMaterial(string shaderName, float handleScale)
        {
            Material material = new Material(Shader.Find(shaderName));
            material.color = Color.white;
            material.SetFloat("_Scale", handleScale);
            return material;
        }

        private static Material CreateShapeMaterial(Color color)
        {
            Material material = new Material(Shader.Find("CodingDaniel/MEHandles/Shape"));
            material.color = color;
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.SetFloat("_ZWrite", 1.0f);
            return material;
        }

        public void Dispose()
        {
            Destroy(Lines);
            Destroy(LinesClip);
            Destroy(LinesClipUsingClipPlane);
            Destroy(LinesBillboard);
            Destroy(ShapesZTest);
            Destroy(ShapesZTest2);
            Destroy(ShapesZTest3);
            Destroy(ShapesZTest4);
            Destroy(ShapesZTestOffset);
            Destroy(UnlitColor);

            Lines = null;
            LinesClip = null;
            LinesClipUsingClipPlane = null;
            LinesBillboard = null;
            ShapesZTest = null;
            ShapesZTest2 = null;
            ShapesZTest3 = null;
            ShapesZTest4 = null;
            ShapesZTestOffset = null;
            UnlitColor = null;
        }

        private static void Destroy(Material material)
        {
            if (material != null)
            {
                UnityEngine.Object.Destroy(material);
            }
        }
    }
}
