using Demo;
using UnityEngine;

namespace Multiplayer.Entity.Client
{
    public class PlayerAnimation
    {
        private Animator _animator;
        
        private Vector3 _input,_inputInterpolate;
        
        private float _xRotation,_desiredXRotation;

        private Transform _spine;
        
        private bool Grounded { get; set; }
        private bool Crouch { get; set; }

        private static readonly int X = Animator.StringToHash("X");
        private static readonly int Y = Animator.StringToHash("Y");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int Crouch1 = Animator.StringToHash("Crouch");

        public PlayerAnimation(Animator animator, Transform spine)
        {
            _animator = animator;
            _spine = spine;
        }

        public void Reset()
        {
            _animator.SetFloat(X,0);
            _animator.SetFloat(Y,0);
            _animator.SetBool(Jump,false);
            _animator.SetBool(Crouch1,false);
            if(_animator.isActiveAndEnabled)
                _animator.Play("Blend Tree");
        }

        public void Update()
        {
            _inputInterpolate = Vector3.Slerp(_inputInterpolate, _input, Time.deltaTime * 15f);
            _desiredXRotation = Mathf.Lerp(_desiredXRotation, _xRotation, Time.deltaTime * 20f);
            
            _animator.SetFloat(X,_inputInterpolate.x);
            _animator.SetFloat(Y,_inputInterpolate.z);
            _animator.SetBool(Jump,!Grounded);
            _animator.SetBool(Crouch1,Crouch);

            if (DemoManager.Replaying && _animator.isActiveAndEnabled)
            {
                if (_animator.speed != 0f && DemoManager.ReplayPaused)
                {
                    _animator.Update(Time.deltaTime);
                    _animator.speed = 0f;
                }
                else if (_animator.speed == 0f && !DemoManager.ReplayPaused)
                    _animator.speed = 1f;
            }
        }

        public void LateUpdate()
        {
            _spine.localRotation = Quaternion.Euler(_desiredXRotation, 0, 0);
        }

        public void SetInput(Vector3 input)
        {
            if (_animator != null)
            {
                _animator.speed = 1f;
            }
            _input = input;
        }

        public void SetGround(bool ground)
        {
            if (Grounded != ground)
            {
                if (_animator != null)
                {
                    _animator.speed = 1f;
                }
                Grounded = ground;
            }
        }

        public void SetCrouch(bool crouch)
        {
            if (_animator != null)
            {
                _animator.speed = 1f;
            }
            Crouch = crouch;
        }

        public float GetXRotation()
        {
            return _xRotation;
        }

        public void SetXRotation(float xRotation)
        {
            _xRotation = xRotation;
        }
    }
}