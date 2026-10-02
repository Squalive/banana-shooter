
using System;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public static class PBBuiltinMaterials
    {
        public const string pointShader = "CodingDaniel/MEHandles/PointBillboard";
        public const string dotShader = "CodingDaniel/MEBuilder/VertexShader";
        public const string lineShader = "CodingDaniel/MEBuilder/LineBillboard";

        private static Material _FacePickerMaterial;
        private static Material _VertexPickerMaterial;
        private static Material _EdgePickerMaterial;
        private static Shader _SelectionPickerShader;

        private static float _scaleMultiplicator = 1.0f;
        public static float ScaleMultiplicator
        {
            get { return _scaleMultiplicator; }
            set
            {
                _scaleMultiplicator = value;
                Shader.SetGlobalFloat("_PBScaleMultiplicator", _scaleMultiplicator);
            }
        }

        private static Material _defaultMaterial;

        /// <summary>
        /// Material given to newly created shapes, and the one the palette starts from.
        ///
        /// The prototype material must live inside a Resources folder for this lookup to succeed -
        /// it is not shipped with the ProBuilder package, so there is no package copy to fall back
        /// on. If it cannot be found at all, a white URP Lit material is generated instead, because
        /// returning null here leaves every new object with no material at all.
        /// </summary>
        public static Material DefaultMaterial
        {
            get
            {
                if(_defaultMaterial == null)
                {
                    _defaultMaterial = Resources.Load<Material>(DefaultMaterialName);

                    if (_defaultMaterial == null)
                    {
                        _defaultMaterial = CreateFallbackMaterial();
                    }
                }

                return _defaultMaterial;
            }
        }

        private const string DefaultMaterialName = "Prototype_512x512_White";

        private static Material CreateFallbackMaterial()
        {
            Shader shader = Shader.Find(RenderPipelineInfo.DefaultShaderName);

            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader);
            material.name = DefaultMaterialName;
            material.Color(Color.white);
            return material;
        }
 
        private static Material _linesMaterial;
        public static Material LinesMaterial
        {
            get
            {
                if(_linesMaterial == null)
                {
                    _linesMaterial = new Material(Shader.Find(lineShader));
                }
                return _linesMaterial;
            }
        }

        private static Material _pointsMaterial;
        public static Material PointsMaterial
        {
            get
            {
                if (_pointsMaterial == null)
                {
                    string vertShader = geometryShadersSupported ?
                        pointShader :
                        dotShader;

                    _pointsMaterial = new Material(Shader.Find(vertShader));
                }
                return _pointsMaterial;
            }

        }

        private static bool _GeometryShadersSupported;
        public static bool geometryShadersSupported
        {
            get
            {
                Init();
                return _GeometryShadersSupported;
            }
        }

        internal static Shader selectionPickerShader
        {
            get
            {
                Init();
                return _SelectionPickerShader;
            }
        }

        internal static Material facePickerMaterial
        {
            get
            {
                Init();
                return _FacePickerMaterial;
            }
        }

        internal static Material vertexPickerMaterial
        {
            get
            {
                Init();
                return _VertexPickerMaterial;
            }
        }
        
        internal static Material edgePickerMaterial
        {
            get
            {
                Init();
                return _EdgePickerMaterial;
            }
        }

        private static bool _IsInitialized;
        static void Init()
        {
            if (_IsInitialized)
                return;

            _IsInitialized = true;

            try
            {
                //UnityEngine.ProBuilder.BuiltinMaterials.cs 4.2.3 has mistakes in lines 106 and 233 causing NullReferenceException throw when trying to access defaultMaterial with UniversalRenderPipeline enabled(also see Face constructor at line 217)
                //Following line calls BuiltinMaterials.defaultMaterial which in turn raises NullReferenceExeption enclosed in a try catch... and this prevents unhandled exeption in future.
                var defaultMaterial = BuiltinMaterials.defaultMaterial;
            }
            catch(Exception e)
            {
                Debug.LogWarning(e);
            }


            _GeometryShadersSupported = SystemInfo.supportsGeometryShaders;
            //Debug.Log("Geometry Shaders Support: " + _GeometryShadersSupported);

            _SelectionPickerShader = Shader.Find("CodingDaniel/MEBuilder/SelectionPicker");

            if ((_FacePickerMaterial = Resources.Load<Material>("Materials/PBFacePicker")) == null)
            {
                _FacePickerMaterial = new Material(Shader.Find("CodingDaniel/MEBuilder/FacePicker"));
            }

            if ((_VertexPickerMaterial = Resources.Load<Material>("Materials/PBVertexPicker")) == null)
            {
                _VertexPickerMaterial = new Material(Shader.Find("CodingDaniel/MEBuilder/VertexPicker"));
            }

            if ((_EdgePickerMaterial = Resources.Load<Material>("Materials/PBEdgePicker")) == null)
            {                
                _EdgePickerMaterial = new Material(Shader.Find("CodingDaniel/MEBuilder/EdgePicker"));
            }
        }
    }
}