
using System;
using System.Linq;
using CodingDaniel.MapEditor.UI;
using CodingDaniel.MapEditor.Utils;
using Manager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodingDaniel.MapEditor.MEEditor
{
    public class CameraMovement : MonoBehaviour
    {
        public static CameraMovement Instance { private set; get; }
        private bool isRotating=false,isMoving=false;

        [SerializeField] private float speed=12f;
        public float sensitivity = 150;

        /// <summary>
        /// Movement speed for the vertical (rise/descend) keys, as a multiplier of <see cref="speed"/>.
        /// </summary>
        [SerializeField] private float verticalSpeed = 1f;

        /// <summary>
        /// True for as long as the camera owns the mouse and keyboard (looking, panning or orbiting),
        /// and for the rest of the frame the control is released. Tool hotkeys consult this so a key
        /// that doubles as a camera key cannot also switch tools.
        /// </summary>
        public bool IsControlling { get; private set; }

        private Vector3 desiredPos;

        private Transform _transform;

        private Camera _camera;

        private float desiredX, xRotation;
        public Texture2D ViewTexture;
        public Texture2D MoveTexture;
        public Texture2D FreeMoveTexture;
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _transform = transform;
            _camera = Camera.main;
            desiredPos = _transform.position;
        }

        private void OnEnable()
        {
            GameManager.InputManager.MapEditor.Look.started += StartLooking;
            GameManager.InputManager.MapEditor.Look.canceled += StopLooking;
            
            GameManager.InputManager.MapEditor.Pan.started += StartPanning;
            GameManager.InputManager.MapEditor.Pan.canceled += StopPanning;
        }

        private void OnDisable()
        {
            GameManager.InputManager.MapEditor.Look.started -= StartLooking;
            GameManager.InputManager.MapEditor.Look.canceled -= StopLooking;
            
            GameManager.InputManager.MapEditor.Pan.started -= StartPanning;
            GameManager.InputManager.MapEditor.Pan.canceled -= StopPanning;
        }

        #region Input
        
        private void StartPanning(InputAction.CallbackContext obj)
        {
            if (TabHolder.Instance.UsingUI()) return;
            isMoving = true;
            CursorHelper.Instance.SetCursor(FreeMoveTexture, Vector2.one * 0.5f, CursorMode.Auto);
        }
        
        private void StopPanning(InputAction.CallbackContext obj)
        {
            isMoving = false;
            CursorHelper.Instance.SetCursor(null, Vector2.one * 0.5f, CursorMode.Auto);
        }

        private void StopLooking(InputAction.CallbackContext obj)
        {
            looking = false;
            CursorHelper.Instance.SetCursor(null, Vector2.one * 0.5f, CursorMode.Auto);
        }

        private void StartLooking(InputAction.CallbackContext obj)
        {
            if (TabHolder.Instance.UsingUI()) return;
            looking = true;
            CursorHelper.Instance.SetCursor(MoveTexture, Vector2.one * 0.5f, CursorMode.Auto);
        }

        private void ScrollMove()
        {
            if (TabHolder.Instance.UsingUI()) return;
            float s = Mathf.Clamp(Input.GetAxis("Mouse ScrollWheel")*100f,-10,10f);
            // desiredPos += speed*Time.deltaTime*s*_transform.forward ;
            var size = speed * Time.deltaTime * s*_transform.forward;
            
            //Add the vector to the final pos
            desiredPos += size;
        }

        /// <summary>
        /// Rise and descend on E and Q.
        ///
        /// Movement is along the camera's own up axis, matching how the horizontal keys move along
        /// its forward and right axes. World up is deliberately not used: it would make E stall as
        /// the camera pitches toward straight down and invert once past it.
        /// </summary>
        private void VerticalMove()
        {
            float vertical = 0f;

            if (Input.GetKey(KeyCode.E))
            {
                vertical += 1f;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                vertical -= 1f;
            }

            if (vertical != 0f)
            {
                float multiplier = Input.GetKey(KeyCode.LeftShift) ? 3 : 1;
                desiredPos += _transform.up * (vertical * speed * verticalSpeed * multiplier * Time.deltaTime);
            }
        }

        private void StopRotate()
        {
            if (isRotating)
            {
                CursorHelper.Instance.SetCursor(null, Vector2.one * 0.5f, CursorMode.Auto);
                isRotating = false;
                return;
            }
        }

        private void StartRotate()
        {
            // pivot = _transform.position + _transform.forward * 10f;
            if (!isRotating)
            {
                CursorHelper.Instance.SetCursor(ViewTexture, Vector2.one * 0.5f, CursorMode.Auto);
            }
            isRotating = true;
        }


        #endregion


        void RotateAroundPivot()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
            {
                StartRotate();
            }
            else if (Input.GetKey(KeyCode.LeftAlt) && !Input.GetMouseButton(0))
            {
                StopRotate();
            }
            else if(!Input.GetKey(KeyCode.LeftAlt) && !Input.GetMouseButtonDown(0))
            {
                StopRotate();
            }

            if (isRotating)
            {
                Vector3 angle = 5f*sensitivity * Time.deltaTime*(-Input.GetAxisRaw("Mouse Y")*_transform.right +Input.GetAxisRaw("Mouse X")*_transform.up);

                var pos = _transform.position;
               
                desiredPos = RotatePointAroundPivot(pos,pivot , angle);

                var rot = Quaternion.LookRotation(pivot-desiredPos);

                Vector3 euler = rot.eulerAngles;

                if (Mathf.Abs(euler.x - 90) < 4f || Mathf.Abs(euler.x - 270) < 4f)
                {
                    return;
                }
                
                _transform.position  = desiredPos;
                _transform.rotation = rot;

                xRotation = euler.x;
                desiredX = euler.y;
            }
        }

        private float _pivotDis=0;

        private bool looking = false;
        void Look()
        {
            if (looking)
            {
                float sensMultiplier = 2f;
                float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime*sensMultiplier;
                float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime*sensMultiplier;
        
                //Find current look rotation
                // Vector3 rot = _transform.localRotation.eulerAngles;
                desiredX += mouseX;
                //Rotate, and also make sure we dont over- or under-rotate.
                xRotation -= mouseY;
                // xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                //Perform the rotations
                _transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);

                pivot = _transform.position + _transform.forward * _pivotDis;

                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");

                float multiplier = Input.GetKey(KeyCode.LeftShift) ? 3 : 1;

                desiredPos += speed * Time.deltaTime *multiplier * (_transform.forward * v + _transform.right * h);

                VerticalMove();
            }
        }

        private void Update()
        {
            RotateAroundPivot();

            Vector3 p = desiredPos;
            if (isMoving && !isRotating)
            {
                float v = -Input.GetAxis("Mouse X");
                float h = -Input.GetAxis("Mouse Y");
                
                desiredPos += 4*speed*Time.deltaTime*(_transform.right*v+_transform.up*h);
            }
            
            Look();
            
            pivot += (desiredPos - p);
            var position = _transform.position;
            _pivotDis = Vector3.Distance(pivot, position);
            
            
            ScrollMove();

            position = Vector3.Lerp(position, desiredPos, Time.deltaTime * 15f);
            _transform.position = position;


            MEBase.Instance.Tools.IsViewing = isMoving||isRotating|| looking;

            // Held for the remainder of this frame so a tool hotkey pressed on the same frame the
            // camera control ends is still treated as a camera key rather than a tool switch.
            IsControlling = isMoving || isRotating || looking;

            if (TabHolder.Instance.UsingUI()) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                Focus();
            }
        }
        void Focus(FocusMode focusMode = FocusMode.Selected)
        {
            Transform[] transforms;
            if (focusMode == FocusMode.Selected)
            {
                if (MEBase.Instance.Selection.ActiveTransform == null)
                {
                    return;
                }

                if ((MEBase.Instance.Selection.ActiveTransform.gameObject.hideFlags & HideFlags.DontSave) != 0 || MEBase.Instance.Selection.ActiveGameObject.IsPrefab())
                {
                    return;
                }

                transforms = MEBase.Instance.Selection.GameObjects.Select(go => go.transform).ToArray();
            }
            else
            {
                transforms = MEBase.Instance.Object.Get(true).SelectMany(e => e.GetComponentsInChildren<Transform>()).Where(r => r.gameObject.activeInHierarchy).ToArray();
            }

            Bounds bounds = TransformUtility.CalculateBounds(transforms);
            if (bounds.extents == Vector3.zero)
            {
                bounds.extents = Vector3.one * 0.5f;
            }
            float objSize = Mathf.Max(bounds.extents.y, bounds.extents.x, bounds.extents.z) * 2.0f;
            Focus(bounds.center, objSize);
            
            // if (focusMode == FocusMode.Selected || focusMode == FocusMode.Default)
            // {
            //     if (Selection.activeTransform != null)
            //     {
            //         MEBase.Instance.secondaryPivotTransform.position = Selection.activeTransform.position;
            //     }
            // }
            // else
            // {
            //     SecondaryPivotTransform.position = bounds.center;
            // }
        }

        public void Focus(Vector3 objPosition, float objSize)
        {
            pivot = objPosition;
            _pivotDis = Vector3.Distance(pivot, _transform.position);
            float distance;
            float fov = MEBase.Instance.Camera.fieldOfView * Mathf.Deg2Rad;
            distance = Mathf.Abs(objSize / Mathf.Sin(fov / 2.0f)) * 2f;
            // if (ChangeOrthographicSizeOnly && IsOrthographic)
            // {
            //     distance = _orbitDistance;
            // }
            // else
            // {
            //     
            // }

            desiredPos = objPosition - _transform.forward * distance;
            // Focus(distance, objSize);
        }
        private Vector3 pivot = Vector3.zero;
        
        
        
        static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }
    }
}
