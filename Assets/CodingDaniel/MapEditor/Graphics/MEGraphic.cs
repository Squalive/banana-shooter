using System;
using System.Collections.Generic;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodingDaniel.MapEditor.Graphics
{
    public interface IMEGraphic
    {
        void RegisterCamera(Camera camera);
        void UnregisterCamera(Camera camera);
        IMECamera GetOrCreateCamera(Camera camera, CameraEvent cameraEvent);
        IMECamera CreateCamera(Camera camera, CameraEvent cameraEvent, bool renderersCache = false);
        IMeshesCache CreateSharedMeshesCache(CameraEvent cameraEvent);
        void DestroySharedMeshesCache(IMeshesCache cache);
    }
    [DefaultExecutionOrder(-60)]
    public class MEGraphic : MonoBehaviour , IMEGraphic
    {

        private Dictionary<Camera, Dictionary<CameraEvent, MECamera>> _cameras = new Dictionary<Camera, Dictionary<CameraEvent, MECamera>>();
        private class Data
        {
            public MonoBehaviour MonoBehaviour;
            public List<MECamera> MEECameras;
            public CameraEvent Event;
            public Data(MonoBehaviour behaviour, CameraEvent cameraEvent, List<MECamera> cameras)
            {
                MonoBehaviour = behaviour;
                Event = cameraEvent;
                MEECameras = cameras;
            }
        }
        
        private readonly Dictionary<IMeshesCache, Data> _meshesCache = new Dictionary<IMeshesCache, Data>();
        private void Awake()
        {

            
            RegisterCamera(Camera.main);
        }

        public void RegisterCamera(Camera camera)
        {
            foreach (KeyValuePair<IMeshesCache, Data> kvp in _meshesCache)
            {
                IMeshesCache cache = kvp.Key;
                Data data = kvp.Value;
                CreateMECamera(camera.gameObject, data.Event, cache, data.MEECameras);
            }
            
            if (!_cameras.ContainsKey(camera))
            {
                _cameras.Add(camera, new Dictionary<CameraEvent, MECamera>());
            }
        }

        public void UnregisterCamera(Camera camera)
        {
            foreach (KeyValuePair<IMeshesCache, Data> kvp in _meshesCache)
            {
                Data data = kvp.Value;
                DestroyRTECameras(camera, data);
            }
            
            
            Dictionary<CameraEvent, MECamera> rteCameras;
            if(_cameras.TryGetValue(camera, out rteCameras))
            {
                foreach(IMECamera meCamera in rteCameras.Values)
                {
                    meCamera.Destroy();
                }
                _cameras.Remove(camera);
            }
        }

        public IMECamera GetOrCreateCamera(Camera camera, CameraEvent cameraEvent)
        {
            Dictionary<CameraEvent, MECamera> rteCameras;
            if (!_cameras.TryGetValue(camera, out rteCameras))
            {
                return null;
            }

            MECamera rteCamera;
            if(!rteCameras.TryGetValue(cameraEvent, out rteCamera))
            {
                rteCamera = _CreateCamera(camera, cameraEvent,  true);
                rteCameras.Add(cameraEvent, rteCamera);
            }

            return rteCamera;
        }

        public IMECamera CreateCamera(Camera camera, CameraEvent cameraEvent, bool createRenderersCache = false)
        {
            return _CreateCamera(camera, cameraEvent, createRenderersCache);
        }
        
        private MECamera _CreateCamera(Camera camera, CameraEvent cameraEvent, bool createRenderersCache)
        {
            bool wasActive = camera.gameObject.activeSelf;
            camera.gameObject.SetActive(false);
            MECamera rteCamera = camera.gameObject.AddComponent<MECamera>();
            rteCamera.Event = cameraEvent;

            if (createRenderersCache)
            {
                rteCamera.CreateRenderersCache();
            }

            camera.gameObject.SetActive(wasActive);
            return rteCamera;
        }
        
        public IMeshesCache CreateSharedMeshesCache(CameraEvent cameraEvent)
        {
            MeshesCache cache = gameObject.AddComponent<MeshesCache>();
            cache.RefreshMode = CacheRefreshMode.Manual;

            List<MECamera> rteCameras = new List<MECamera>();
            foreach (Camera camera in _cameras.Keys)
            {
                CreateMECamera(camera.gameObject, cameraEvent, cache, rteCameras);
            }

            _meshesCache.Add(cache, new Data(cache, cameraEvent, rteCameras));
            return cache;
        }
        
        private static void CreateMECamera(GameObject camera, CameraEvent cameraEvent, IMeshesCache cache, List<MECamera> rteCameras)
        {
            bool wasActive = camera.gameObject.activeSelf;
            camera.SetActive(false);

            MECamera rteCamera = camera.AddComponent<MECamera>();
            rteCamera.Event = cameraEvent;
            rteCamera.MeshesCache = cache;
            rteCameras.Add(rteCamera);

            camera.SetActive(wasActive);
        }
        
        public void DestroySharedMeshesCache(IMeshesCache cache)
        {
            Data tuple;
            if (_meshesCache.TryGetValue(cache, out tuple))
            {
                Destroy(tuple.MonoBehaviour);
                for (int i = 0; i < tuple.MEECameras.Count; ++i)
                {
                    Destroy(tuple.MEECameras[i]);
                }
                _meshesCache.Remove(cache);
            }
        }
        
        private static void DestroyRTECameras(Camera camera, Data data)
        {
            List<MECamera> rteCameras = data.MEECameras;
            for (int i = rteCameras.Count - 1; i >= 0; i--)
            {
                MECamera rteCamera = rteCameras[i];
                if (rteCamera != null && rteCamera.gameObject == camera.gameObject)
                {
                    Destroy(rteCameras[i]);
                    rteCameras.RemoveAt(i);
                }
            }
        }
    }
}
