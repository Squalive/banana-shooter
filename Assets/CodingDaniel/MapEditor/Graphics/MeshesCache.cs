using System;
using System.Collections.Generic;
using UnityEngine;

namespace CodingDaniel.MapEditor.Graphics
{
    public enum CacheRefreshMode
    {
        Manual,
        OnTransformChange,
        Always,
    }
    public interface IMeshesCache
    {
        event Action Refreshing;

        CacheRefreshMode RefreshMode
        {
            get;
            set;
        }

        bool IsEmpty
        {
            get;
        }

        IList<RenderMeshesBatch> Batches
        {
            get;
        }

        void AddBatch(Mesh mesh, Material material, Matrix4x4[] matrices);
        void RemoveBatch(Mesh mesh);

        void Add(Mesh mesh, Transform transform);
        void SetMaterial(Mesh mesh, Material material);
        void Remove(Mesh mesh, Transform transform);

        void Refresh(bool batchesOnly = false, int maxBatchSize = 128);
        void Clear();

        void Destroy();
    }
    
    public class RenderMeshesBatch
    {
        public readonly Mesh Mesh;
        public readonly Material Material;
        
        protected Matrix4x4[] _matrices;
        public Matrix4x4[] Matrices
        {
            get { return _matrices; }
        }

        public RenderMeshesBatch(Mesh mesh, Material material, Matrix4x4[] matrices)
        {
            Mesh = mesh;
            Material = material;
            _matrices = matrices;
        }

        public virtual void Refresh()
        {

        }
    }
    public class MeshesCache : MonoBehaviour,IMeshesCache
    {
        public event Action Refreshing;

        public class RenderTransformedMeshesBatch : RenderMeshesBatch
        {
            public readonly List<Transform> Transforms = new List<Transform>();

            public RenderTransformedMeshesBatch(Mesh mesh, Material material) : base(mesh, material, new Matrix4x4[0])
            {

            }

            public override void Refresh()
            {
                for (int i = Transforms.Count - 1; i >= 0; --i)
                {
                    if (Transforms[i] == null)
                    {
                        Transforms.RemoveAt(i);
                    }
                }

                if (Transforms.Count != _matrices.Length)
                {
                    _matrices = new Matrix4x4[Transforms.Count];
                }

                for (int i = Transforms.Count - 1; i >= 0; --i)
                {
                    Transform transform = Transforms[i];
                    _matrices[i] = transform.localToWorldMatrix;
                }
            }
        }

        private CacheRefreshMode _refreshMode;
        public CacheRefreshMode RefreshMode
        {
            get
            {
                return _refreshMode;
            }
            set
            {
                _refreshMode = value;
                enabled = _refreshMode != CacheRefreshMode.Manual;
            }
        }

        public bool IsEmpty
        {
            get { return _batches.Count == 0; }
        }

        
        private readonly List<RenderMeshesBatch> _batches = new List<RenderMeshesBatch>();
        public IList<RenderMeshesBatch> Batches
        {
            get { return _batches; }
        }

        private class PRS
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LocalScale;
        }

        private List<PRS> _prs = new List<PRS>();
        private List<Transform> _transforms = new List<Transform>();
        private Dictionary<Mesh, Tuple<Material, List<Transform>>> _meshToData = new Dictionary<Mesh, Tuple<Material, List<Transform>>>();
        private readonly Dictionary<Mesh, RenderMeshesBatch> _meshToBatch = new Dictionary<Mesh, RenderMeshesBatch>();
        private void Awake()
        {
            enabled = _refreshMode != CacheRefreshMode.Manual;
        }

        private void Update()
        {
            if(_refreshMode == CacheRefreshMode.Always)
            {
                Refresh(true);
            }
            else
            {
                for (int i = 0; i < _transforms.Count; ++i)
                {
                    Transform t = _transforms[i];
                    if(t != null)
                    {
                        PRS prs = _prs[i];
                        if (prs.Position != t.position || prs.Rotation != t.rotation || prs.LocalScale != t.localScale)
                        {
                            prs.Position = t.position;
                            prs.Rotation = t.rotation;
                            prs.LocalScale = t.localScale;
                            Refresh(true);
                            break;
                        }
                    }
                }
            }
        }

        public void AddBatch(Mesh mesh, Material material, Matrix4x4[] matrices)
        {
            _meshToBatch.Add(mesh, new RenderMeshesBatch(mesh, material, matrices));
        }

        public void RemoveBatch(Mesh mesh)
        {
            _meshToBatch.Remove(mesh);
        }
   
        public void Add(Mesh mesh, Transform transform)
        {
            Tuple<Material, List<Transform>> data;
            if (!_meshToData.TryGetValue(mesh, out data))
            {
                data = new Tuple<Material, List<Transform>>(null, new List<Transform>());
                _meshToData.Add(mesh, data);
            }

            _transforms.Add(transform);
            _prs.Add(new PRS { Position = Vector3.one * float.NaN });
            data.Item2.Add(transform);
        }

        public void SetMaterial(Mesh mesh, Material material)
        {
            if (_meshToData.ContainsKey(mesh))
            {
                _meshToData[mesh] = new Tuple<Material, List<Transform>>(material, _meshToData[mesh].Item2);
            }
            else
            {
                _meshToData.Add(mesh, new Tuple<Material, List<Transform>>(material, new List<Transform>()));
            }
        }

        public void Remove(Mesh mesh, Transform transform)
        {
            Tuple<Material, List<Transform>> data;
            if (_meshToData.TryGetValue(mesh, out data))
            {
                data.Item2.Remove(transform);
                int index = _transforms.IndexOf(transform);
                if(index >= 0)
                {
                    _transforms.RemoveAt(index);
                    _prs.RemoveAt(index);
                }
                if (data.Item2.Count == 0)
                {
                    _meshToData.Remove(mesh);
                }
            }
        }

        public void Clear()
        {
            _meshToData.Clear();
            _batches.Clear();
        }

        public void Destroy()
        {
            Destroy(this);
        }

        public void Refresh(bool batchesOnly = false, int maxBatchSize = 128)
        {
            if(batchesOnly)
            {
                RefreshBatches();
                return;
            }

            _batches.Clear();

            foreach (KeyValuePair<Mesh, Tuple<Material, List<Transform>>> kvp in _meshToData)
            {
                if(kvp.Key == null)
                {
                    continue;
                }

                Tuple<Material, List<Transform>> data = kvp.Value;

                RenderTransformedMeshesBatch batch = new RenderTransformedMeshesBatch(kvp.Key, data.Item1);
                _batches.Add(batch);

                int index = 0;
                for (int i = 0; i < data.Item2.Count; ++i)
                {
                    if (index == maxBatchSize)
                    {
                        batch = new RenderTransformedMeshesBatch(kvp.Key, data.Item1);
                        _batches.Add(batch);
                        index = 0;
                    }

                    batch.Transforms.Add(data.Item2[i]);
                    index++;
                }
            }

            foreach(KeyValuePair<Mesh, RenderMeshesBatch> kvp in _meshToBatch)
            {
                if (kvp.Key == null)
                {
                    continue;
                }

                _batches.Add(kvp.Value);
            }

            RefreshBatches();
        }

        private void RefreshBatches()
        {
            for (int i = 0; i < _batches.Count; ++i)
            {
                _batches[i].Refresh();
            }

            if (Refreshing != null)
            {
                Refreshing();
            }
        }
    }
}