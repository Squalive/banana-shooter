using System;
using CodingDaniel.MapEditor.Handle;
using UnityEngine;
using UnityEngine.InputSystem;

using CodingDaniel.MapEditor.Interaction;

namespace CodingDaniel.MapEditor.MEEditor
{
    public class METoolsInput : MonoBehaviour
    {
        private IME _editor;

        private void Awake()
        {
            _editor = MEBase.Instance;
        }
        
        private void Start()
        {
            _editor.Tools.Current = EditorTool.Move;
        }
        
        private void LateUpdate()
        {
            if(_editor.Tools.ActiveTool != null)
            {
                return;
            }

            // While the camera has the keyboard, Q and E are its descend/rise keys, not tool
            // shortcuts. Without this the same keypress would both move the camera and switch tools,
            // since CameraMovement clears Tools.IsViewing on the frame the control is released and
            // this runs afterwards in the same frame.
            CameraMovement cameraMovement = CameraMovement.Instance;
            if (cameraMovement != null && cameraMovement.IsControlling)
            {
                return;
            }

            bool isLocked = _editor.Tools.IsViewing;
            if (!isLocked)
            {
                if (ViewAction())
                {
                    _editor.Tools.Current = EditorTool.View;
                }
                else if (MoveAction())
                {
                    _editor.Tools.Current = EditorTool.Move;
                }
                else if (RotateAction())
                {
                    _editor.Tools.Current = EditorTool.Rotate;
                }
                else if (ScaleAction())
                {
                    _editor.Tools.Current = EditorTool.Scale;
                }
                else if(RectToolAction())
                {
                    _editor.Tools.Current = EditorTool.Rect;
                }

                if (PivotRotationAction())
                {
                    if (_editor.Tools.PivotRotation ==MEPivotRotation.Local)
                    {
                        _editor.Tools.PivotRotation = MEPivotRotation.Global;
                    }
                    else
                    {
                        _editor.Tools.PivotRotation = MEPivotRotation.Local;
                    }
                }
                if (PivotModeAction())
                {
                    if (_editor.Tools.PivotMode == MEPivotMode.Center)
                    {
                        _editor.Tools.PivotMode = MEPivotMode.Pivot;
                    }
                    else
                    {
                        _editor.Tools.PivotMode = MEPivotMode.Center;
                    }
                }
            }
        }

        /// <summary>
        /// Tool hotkeys only fire when no text field is focused. Polling the raw keyboard directly
        /// would also trigger them while the user is typing a map name or description.
        /// </summary>
        private static bool PressKey(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }

        /// <summary>View tool. Moved off Q, which now descends the camera.</summary>
        protected virtual bool ViewAction()
        {
            return PressKey(Key.V);
        }

        protected virtual bool MoveAction()
        {
            return PressKey(Key.W);
        }

        /// <summary>
        /// Rotate tool. Kept on E: the camera consumes E while it holds the keyboard, so the two
        /// only overlap if the editor is not being navigated.
        /// </summary>
        protected virtual bool RotateAction()
        {
            return PressKey(Key.E);
        }

        protected virtual bool ScaleAction()
        {
            return PressKey(Key.R);
        }

        protected virtual bool RectToolAction()
        {
            return PressKey(Key.T);
        }

        protected virtual bool PivotRotationAction()
        {
            return PressKey(Key.X);
        }

        protected virtual bool PivotModeAction()
        {
            return PressKey(Key.Z);
        }
    }
}
