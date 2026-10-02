using System;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.UI;
using UnityEngine;

namespace CodingDaniel.MapEditor.Handle
{
    public class BoxSelectionInput : MonoBehaviour
    {
        private BoxSelection _boxSelection;
        private IME _editor;

        private void Start()
        {
            _boxSelection = GetComponent<BoxSelection>();

            _editor = _boxSelection.Editor;
        }
        private bool _pointerPressed = false;
        
        private void LateUpdate()
        {
            if (TabHolder.Instance.UsingUI()) return;
            if (!Input.GetMouseButton(0))
            {
                if(_pointerPressed)
                {
                    _pointerPressed = false;
                    _boxSelection.EndSelect();
                }
            }

            if(!_boxSelection.enabled)
            {
                return;
            }

            if (_editor.Tools.ActiveTool != null &&_editor.Tools.ActiveTool != _boxSelection)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0)){
                _pointerPressed = true;
                _boxSelection.BeginSelect();
            }
           
        }
    }
}
