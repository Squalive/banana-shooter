using System;
using System.Collections.Generic;
using UnityEngine;

namespace CodingDaniel.MapEditor.MECommon
{
    public interface IRenderersCache
    {
        event Action Refreshed;

        bool IsEmpty
        {
            get;
        }

        Material MaterialOverride
        {
            get;
            set;
        }

        IList<Renderer> Renderers
        {
            get;
        }

        void Add(Renderer[] renderers, bool forceRender = true, bool forceMatrixRecalculationPerRender = false);
        void Remove(Renderer[] renderers);

        void Add(Renderer renderer, bool forceRender = true, bool forceMatrixRecalcuationPerRender = false);
        void Remove(Renderer renderer);
        void Refresh();
        void Clear();
        void Destroy();
    }

    public class RenderersCache : MonoBehaviour, IRenderersCache
    {
        public event Action Refreshed;

        public bool IsEmpty
        {
            get { return _renderers.Count == 0;  }
        }

        public Material MaterialOverride
        {
            get;
            set;
        }

        private readonly List<Tuple<bool?, bool?>> _settingsBackup = new List<Tuple<bool?, bool?>>();
        private readonly List<Renderer> _renderers = new List<Renderer>();
        public IList<Renderer> Renderers
        {
            get { return _renderers; }
        }


        public void Add(Renderer[] renderers, bool forceRender = true, bool forceMatrixRecalculationPerRender = false)
        {
            for(int i = 0; i < renderers.Length; ++i)
            {
                Add(renderers[i], forceRender, forceMatrixRecalculationPerRender);
            }
        }

        public void Remove(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; ++i)
            {
                Remove(renderers[i]);
            }
        }

        public void Add(Renderer renderer, bool forceRender = true, bool forceMatrixRecalcuationPerRender = false)
        {
            bool isRendererEnabled = renderer.enabled;
            bool forceMatrixRecalculation = false;
            
            if (renderer is SkinnedMeshRenderer)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)renderer;
                forceMatrixRecalculation = skinnedMeshRenderer.forceMatrixRecalculationPerRender;
                if(forceMatrixRecalcuationPerRender)
                {
                    skinnedMeshRenderer.forceMatrixRecalculationPerRender = true;
                }
            }

            if (forceRender)
            {
                _renderers.Add(renderer);
                _settingsBackup.Add(new Tuple<bool?, bool?>(null, null));
            }
            else
            {
                if (!renderer.forceRenderingOff)
                {
                    renderer.enabled = false;
                    _renderers.Add(renderer);
                    _settingsBackup.Add(new Tuple<bool?, bool?>(isRendererEnabled, forceMatrixRecalculation));
                }   
            }

        }

        public void Remove(Renderer renderer)
        {
            int index = _renderers.IndexOf(renderer);
            if(index < 0)
            {
                return;
            }

            Tuple<bool?, bool?> settings = _settingsBackup[index];
            if(settings.Item2 != null)
            {
                if (renderer is SkinnedMeshRenderer)
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)renderer;
                    skinnedMeshRenderer.forceMatrixRecalculationPerRender = settings.Item2.Value;
                }

            }

            if(settings.Item1 != null)
            {
                renderer.enabled = settings.Item1.Value;
            }
            
            _renderers.RemoveAt(index);
            _settingsBackup.RemoveAt(index);
            
            
        }

        public void Refresh()
        {
            if (Refreshed != null)
            {
                Refreshed();
            }
        }

        public void Clear()
        {
            _renderers.Clear();
            _settingsBackup.Clear();
        }

        public void Destroy()
        {
            Destroy(this);
        }
    }
}
