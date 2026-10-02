using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using IRenderPipelineCameraUtility = CodingDaniel.MapEditor.Utils.IRenderPipelineCameraUtility;

namespace CodingDaniel.MapEditor.Graphics
{
    public class CameraUtilityURP : MonoBehaviour,IRenderPipelineCameraUtility
    {
        public static CameraUtilityURP Instance { private set; get; }
        public event Action<Camera, bool> PostProcessingEnabled;

        private void Awake()
        {            
            Instance = this;
        }

        public void RequiresDepthTexture(Camera camera, bool value)
        {
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            cameraData.requiresDepthTexture = value;
        }

        public bool IsPostProcessingEnabled(Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                return false;
            }
            return cameraData.renderPostProcessing;
        }

        public void EnablePostProcessing(Camera camera, bool value)
        {
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            cameraData.renderPostProcessing = value;

            if(PostProcessingEnabled != null)
            {
                PostProcessingEnabled(camera, value);
            }
        }

        public void Stack(Camera baseCamera, Camera overlayCamera)
        {
            UniversalAdditionalCameraData overlayData = overlayCamera.GetComponent<UniversalAdditionalCameraData>();
            if(overlayData == null)
            {
                overlayData = overlayCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            overlayData.renderType = CameraRenderType.Overlay;
            UniversalAdditionalCameraData baseData = baseCamera.GetComponent<UniversalAdditionalCameraData>();
            if(baseData == null)
            {
                baseData = baseCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            baseData.cameraStack.Add(overlayCamera);
            overlayCamera.clearFlags = CameraClearFlags.Depth;
        }

        public void SetBackgroundColor(Camera camera, Color color)
        {
            camera.backgroundColor = color;
        }

        public void ResetCullingMask(Camera camera)
        {
            camera.cullingMask = 0;
        }
    }
}
