using System.Collections.Generic;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;

using CodingDaniel.MapEditor.Interaction;

namespace CodingDaniel.MapEditor.Handle
{
    [DefaultExecutionOrder(-89)]
    public class MEHitTester : MonoBehaviour
    {
        protected readonly List<BaseHandle> _handles = new List<BaseHandle>();

        protected BaseHandle _selectedHandle;
        protected HandleAxis _selectedAxis;
        protected IME _editor;

        private void Awake()
        {
            _editor = MEBase.Instance;
        }

        public virtual void Add(BaseHandle handle)
        {
            if(!_handles.Contains(handle))
            {
                _handles.Add(handle);
            }
        }

        public virtual void Remove(BaseHandle handle)
        {
            _handles.Remove(handle);
        }

        public virtual HandleAxis GetSelectedAxis(BaseHandle handle)
        {
            if(_selectedHandle == null)
            {
                return HandleAxis.None;
            }

            if(_selectedHandle != handle)
            {
                return HandleAxis.None;
            }

            return _selectedAxis;
            
        }

        protected virtual void Update()
        {
            HitTestAll();
        }

        private void HitTestAll()
        {
            _selectedHandle = null;
            _selectedAxis = HandleAxis.None;

            float minDistance = float.PositiveInfinity;
            for (int i = 0; i < _handles.Count; ++i)
            {
                BaseHandle handle = _handles[i];

                float distance;
                HandleAxis selectedAxis = handle.HitTest(out distance);
                // Debug.Log(selectedAxis);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    _selectedAxis = selectedAxis;
                    _selectedHandle = handle;
                }
            }
        }
    }
}
