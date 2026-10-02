using System;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;

namespace CodingDaniel.MapEditor.Handle
{
    [DefaultExecutionOrder(-60)]
    public class BaseHandleInput : MonoBehaviour
    {
        protected IME _editor;
        
        protected BaseHandle _handle;
        public virtual BaseHandle Handle
        {
            get { return _handle; }
            set { _handle = value; }
        }

        private void Start()
        {
            _editor = MEBase.Instance;
        }

        private void OnEnable()
        {
            if (_handle == null)
            {
                _handle = GetComponent<BaseHandle>();
            }

            if (BeginDragAction())
            {
                _handle.BeginDrag();
            }
        }
        
        protected virtual void Update()
        {
            if(_handle == null)
            {
                Destroy(this);
                return;
            }
            if(!_handle.enabled)
            {
                return;
            }

            if (BeginDragAction())
            {
                _handle.BeginDrag();
            }
            else if (EndDragAction())
            {
                _handle.EndDrag();
            }

            if(_handle != null && _handle.IsDragging)
            {
                _handle.UnitSnapping = UnitSnappingAction();
            }

            // Vertex snapping belongs to whichever tool declares it, so this component no longer needs a
            // subclass per tool (PositionHandleInput used to add it for the move tool only).
            if (_handle.SupportsVertexSnapping && _editor != null && _editor.Tools != null && !_editor.Tools.IsViewing)
            {
                if (BeginVertexSnappingAction())
                {
                    _handle.SetVertexSnapping(true);
                    if (VertexSnappingToggleAction())
                    {
                        _editor.Tools.IsSnapping = !_editor.Tools.IsSnapping;
                    }
                }
                else if (EndVertexSnappingAction())
                {
                    _handle.SetVertexSnapping(false);
                }
            }
        }
        
        protected virtual bool BeginDragAction()
        {
            return Input.GetMouseButtonDown(0);
        }

        protected virtual bool EndDragAction()
        {
            return Input.GetMouseButtonUp(0);
        }

        protected virtual bool UnitSnappingAction()
        {
            return Input.GetKey(KeyCode.LeftControl) ;
        }

        protected virtual bool BeginVertexSnappingAction()
        {
            return Input.GetKeyDown(KeyCode.V);
        }

        protected virtual bool EndVertexSnappingAction()
        {
            return Input.GetKeyUp(KeyCode.V);
        }

        protected virtual bool VertexSnappingToggleAction()
        {
            return Input.GetKey(KeyCode.LeftControl);
        }
    }
}
