using System;
using System.Collections.Generic;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.MECommon;
using UnityEngine;
using UnityEngine.Rendering;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.MEEditor
{
    public interface IMECamera
    {
        event Action<IMECamera> CommandBufferRefresh;

        Camera Camera
        {
            get;
        }

        CommandBuffer CommandBuffer
        {
            get;
        }

        CommandBuffer CommandBufferOverride
        {
            get;
            set;
        }
        IRenderersCache RenderersCache
        {
            get;
        }
        CameraEvent Event
        {
            get;
            set;
        }
        void RefreshCommandBuffer();
        void Destroy();
    }
    public class MECamera : MonoBehaviour,IMECamera
    {
        public event Action<IMECamera> CommandBufferRefresh;
        private Camera _camera;

        public Camera Camera => _camera;
        private CommandBuffer _commandBuffer;
        public CommandBuffer CommandBuffer
        {
            get { return _commandBufferOverride != null ? _commandBufferOverride :  _commandBuffer; }
        }

        private CommandBuffer _commandBufferOverride;
        public CommandBuffer CommandBufferOverride
        {
            get { return _commandBufferOverride; }
            set
            {
                _commandBufferOverride = value;
                if(_commandBufferOverride != null)
                {
                    RemoveCommandBuffer();
                }
                else
                {
                    CreateCommandBuffer();
                }
            }
        }
        private IMeshesCache _meshesCache;
        private bool _destroyMeshesCache;
        public IMeshesCache MeshesCache
        {
            get { return _meshesCache; }
            set 
            {
                DestroyMeshesCache();
                _meshesCache = value; 
            }
        }
        private void DestroyMeshesCache()
        {
            if (_destroyMeshesCache && _meshesCache != null)
            {
                _meshesCache.Destroy();
                _meshesCache = null;
            }
        }
        [SerializeField]
        [FormerlySerializedAs("m_cameraEvent")]
        private CameraEvent _cameraEvent = CameraEvent.BeforeImageEffects;

        private IRenderersCache _renderersCache;
        private bool _destroyRenderersCache;
        
        public IRenderersCache RenderersCache
        {
            get { return _renderersCache; }
            set 
            {
                DestroyRenderersCache();
                _renderersCache = value; 
            }
        }

        public CameraEvent Event
        {
            get { return _cameraEvent; }
            set
            {
                _cameraEvent = value;

                if(_commandBufferOverride == null)
                {
                    RemoveCommandBuffer();
                    CreateCommandBuffer();
                }
            }
        }
        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (_commandBufferOverride == null)
            {
                CreateCommandBuffer();
            }

            RefreshCommandBuffer();

            if (_renderersCache != null)
            {
                _renderersCache.Refreshed += OnRefresh;
            }
            
            if (_meshesCache != null)
            {
                _meshesCache.Refreshing += OnRefresh;
            }
            //
            // if (Created != null)
            // {
            //     Created(this);
            // }
        }
        private void OnRefresh()
        {
            RefreshCommandBuffer();
        }
        private void DestroyRenderersCache()
        {
            if (_destroyRenderersCache && _renderersCache != null)
            {
                _renderersCache.Destroy();
                _renderersCache = null;
            }
        }
        public void CreateMeshesCache()
        {
            DestroyMeshesCache();
            _meshesCache = gameObject.AddComponent<MeshesCache>();
            _destroyMeshesCache = true;
        }
        private void OnDestroy()
        {
            if (_renderersCache != null)
            {    
                _renderersCache.Refreshed -= OnRefresh;
                DestroyRenderersCache();
            }if (_meshesCache != null)
            {
                _meshesCache.Refreshing -= OnRefresh;
            }

            if (_camera != null)
            {
                RemoveCommandBuffer();
            }

        }
        private void CreateCommandBuffer()
        {
            if (_commandBuffer != null || _camera == null)
            {
                return;
            }
            _commandBuffer = new CommandBuffer();
            _commandBuffer.name = "MECameraCommandBuffer";
            _camera.AddCommandBuffer(_cameraEvent, _commandBuffer);
        }
        public void Destroy()
        {
            DestroyRenderersCache();
            Destroy(this);
        }
        private void RemoveCommandBuffer()
        {
            if (_commandBuffer == null)
            {
                return;
            }
            _camera.RemoveCommandBuffer(_cameraEvent, _commandBuffer);
            _commandBuffer = null;
        }
        
        public void RefreshCommandBuffer()
        {
            if(Camera == null)
            {
                return;
            }

            CommandBuffer commandBuffer;
            if(_commandBufferOverride == null)
            {
                if (_commandBuffer == null)
                {
                    return;
                }

                _commandBuffer.Clear();
                if (_cameraEvent == CameraEvent.AfterImageEffects || _cameraEvent == CameraEvent.AfterImageEffectsOpaque)
                {
                    _commandBuffer.ClearRenderTarget(true, false, Color.black);
                }

                commandBuffer = _commandBuffer;
            }
            else
            {
                commandBuffer = _commandBufferOverride;
            }
            
            if(_meshesCache != null)
            {
                IList<RenderMeshesBatch> batches = _meshesCache.Batches;
                for (int i = 0; i < batches.Count; ++i)
                {
                    RenderMeshesBatch batch = batches[i];
                    if (batch.Material == null)
                    {
                        continue;
                    }
            
                    if (batch.Material.enableInstancing)
                    {
                        for (int j = 0; j < batch.Mesh.subMeshCount; ++j)
                        {
                            if (batch.Mesh != null)
                            {
                                commandBuffer.DrawMeshInstanced(batch.Mesh, j, batch.Material, -1, batch.Matrices, batch.Matrices.Length);
                            }
                        }
                    }
                    else
                    {
                        Matrix4x4[] matrices = batch.Matrices;
                        for (int m = 0; m < matrices.Length; ++m)
                        {
                            for (int j = 0; j < batch.Mesh.subMeshCount; ++j)
                            {
                                if (batch.Mesh != null)
                                {
                                    commandBuffer.DrawMesh(batch.Mesh, matrices[m], batch.Material, j, -1);
                                }
                            }
                        }
                    }
                }
            }
            
            
            if (_renderersCache != null)
            {
                IList<Renderer> renderers = _renderersCache.Renderers;
                for (int i = 0; i < renderers.Count; ++i)
                {
                    Renderer renderer = renderers[i];
                    if(renderer == null)
                    {
                        continue;
                    }
                    Material[] materials = renderer.sharedMaterials;
                    for (int j = 0; j < materials.Length; ++j)
                    {
                        if(_renderersCache.MaterialOverride != null)
                        {
                            commandBuffer.DrawRenderer(renderer, _renderersCache.MaterialOverride, j, -1);
                        }
                        else
                        {
                            Material material = materials[j];
                            commandBuffer.DrawRenderer(renderer, material, j, -1);
                        }
                    }
                }
            }

            if (CommandBufferRefresh != null)
            {
                CommandBufferRefresh(this);
            }
        }
        
        public void CreateRenderersCache()
        {
            DestroyRenderersCache();
            _renderersCache = gameObject.AddComponent<RenderersCache>();
            _destroyRenderersCache = true;
        }
    }
}