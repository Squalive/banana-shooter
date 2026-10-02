using System;
using Manager;
using Menu;
using Multiplayer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement
{
    [Serializable]
    public class NoClipMovement : IPlayerMovement
    {
        public PlayerMovement PlayerMovement { get; set; }
        public Transform PlayerTransform { get; set; }
        public Transform PlayerCam { get; set; }
        public Transform Orientation { get; set; }
        public GameObject PlayerSmokeFx { get; set; }
        public Rigidbody Rb { get; set; }
        public CapsuleCollider Collider { get; set; }
        private float HorizontalInput => PlayerMovement.x;
        private float VerticalInput => PlayerMovement.y;

        private float _zInput;
        [SerializeField] private float moveSpeed = 10f;
        public void MyAwake(PlayerMovement playerMovement, Transform playerCam, Transform orientation, GameObject playerSmokeFx,
            Rigidbody rb, CapsuleCollider collider, Transform playerTransform)
        {
            PlayerTransform = playerTransform;
            PlayerMovement = playerMovement;
            PlayerCam = playerCam;
            Orientation = orientation;
            PlayerSmokeFx = playerSmokeFx;
            Rb = rb;
            Collider = collider;
            Rb.isKinematic = true;
        }

        public void MyOnEnable()
        {
            GameManager.InputManager.Player.Jump.started += StartJump;
            GameManager.InputManager.Player.Jump.canceled += StopZ;
            
            GameManager.InputManager.Player.Crouch.started += StartCrouch;
            GameManager.InputManager.Player.Crouch.canceled += StopZ;
        }

        public void MyOnDisable()
        {
            GameManager.InputManager.Player.Jump.started -= StartJump;
            GameManager.InputManager.Player.Jump.canceled -= StopZ;
            
            GameManager.InputManager.Player.Crouch.started -= StartCrouch;
            GameManager.InputManager.Player.Crouch.canceled -= StopZ;
        }

        public void MyFixedUpdate()
        {
            MovementCalculation();
        }

        public void MyUpdate()
        {
            
        }

        public void MyLateUpdate()
        {
            
        }

        public void MyOnCollisionEnter(Collision other)
        {
        }

        public void MyOnCollisionStay(Collision other)
        {
        }

        public void DeInitialize()
        {
            _zInput = 0;
        }

        void MovementCalculation()
        {
            float multiplier = 1f;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                multiplier = 2f;
            }
            
            Vector3 v = moveSpeed * VerticalInput * multiplier * Time.deltaTime * PlayerCam.forward;
            Vector3 h = moveSpeed * HorizontalInput * multiplier * Time.deltaTime * PlayerCam.right;
            Vector3 z = moveSpeed * _zInput * multiplier * Time.deltaTime * Vector3.up;

            Rb.MovePosition(PlayerTransform.position + v + h + z);
        }
        
        private void StopZ(InputAction.CallbackContext obj)
        {
            if (GameUIManager.Instance && GameUIManager.Instance.pause || NetworkManager.Instance.CantPlay())
            {
                _zInput = 0;
                return;
            }
            _zInput = 0;
        }

        private void StartJump(InputAction.CallbackContext obj)
        {
            if (GameUIManager.Instance && GameUIManager.Instance.pause || NetworkManager.Instance.CantPlay())
            {
                _zInput = 0;
                return;
            }
            _zInput = 1;
        }

        private void StartCrouch(InputAction.CallbackContext obj)
        {
            if (GameUIManager.Instance && GameUIManager.Instance.pause || NetworkManager.Instance.CantPlay())
            {
                _zInput = 0;
                return;
            }
            _zInput = -1;
        }
    }
}