using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using CodingDaniel.MapEditor.MECommon;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering.Universal;

namespace CodingDaniel.MapEditor.Extension.URP
{
    public class PBSelectionPickerRendererURP : PBSelectionPickerRenderer
    {
        private IRenderersCache _cache;
        public PBSelectionPickerRendererURP(IRenderersCache cache)
        {
            _cache = cache;
        }

        protected override void PrepareCamera(Camera renderCamera)
        {
            base.PrepareCamera(renderCamera);

            UniversalAdditionalCameraData cameraData = renderCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderShadows = false;

            cameraData.SetRenderer(1);
        }

        protected override void Render(Shader shader, string tag, Camera renderCam)
        {
            bool invertCulling = GL.invertCulling;

            GL.invertCulling = true;
            renderCam.projectionMatrix *= Matrix4x4.Scale(new Vector3(1, -1, 1));
            renderCam.Render();
            GL.invertCulling = invertCulling;

            _cache.Clear();
        }

        protected override void GenerateEdgePickingObjects(IList<ProBuilderMesh> selection, bool doDepthTest, out Dictionary<uint, SimpleTuple<ProBuilderMesh, Edge>> map, out GameObject[] depthObjects, out GameObject[] pickerObjects)
        {
            base.GenerateEdgePickingObjects(selection, doDepthTest, out map, out depthObjects, out pickerObjects);
            if(depthObjects != null)
            {
                _cache.Add(depthObjects.SelectMany(go => go.GetComponentsInChildren<Renderer>()).ToArray());
            }
            if(pickerObjects != null)
            {
                _cache.Add(pickerObjects.SelectMany(go => go.GetComponentsInChildren<Renderer>()).ToArray());
            }
        }

        protected override GameObject[] GenerateFacePickingObjects(IList<ProBuilderMesh> selection, out Dictionary<uint, SimpleTuple<ProBuilderMesh, Face>> map)
        {
            GameObject[] pickerObjects = base.GenerateFacePickingObjects(selection, out map);
            _cache.Add(pickerObjects.SelectMany(go => go.GetComponentsInChildren<Renderer>()).ToArray());
            return pickerObjects;
        }

        protected override void GenerateVertexPickingObjects(IList<ProBuilderMesh> selection, bool doDepthTest, out Dictionary<uint, SimpleTuple<ProBuilderMesh, int>> map, out GameObject[] depthObjects, out GameObject[] pickerObjects)
        {
            base.GenerateVertexPickingObjects(selection, doDepthTest, out map, out depthObjects, out pickerObjects);
            if(depthObjects != null)
            {
                _cache.Add(depthObjects.SelectMany(go => go.GetComponentsInChildren<Renderer>()).ToArray());
            }
            if(pickerObjects != null)
            {
                _cache.Add(pickerObjects.SelectMany(go => go.GetComponentsInChildren<Renderer>()).ToArray());
            }
        }
    }
}
