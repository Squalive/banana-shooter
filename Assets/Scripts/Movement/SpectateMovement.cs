using System;
using System.Collections.Generic;

using Demo.Interface;
using EZCameraShake;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Client;
using PlayerCameraController;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using Weapon;

namespace Movement
{
    public enum EDoFMode
    {
        AutoFocus,
        ManualFocus,
        TargetFocus
    }

    public enum EPerspective : int
    {
        Spectate = 0,
        FirstPerson
    }

    public class SpectateMovement : MonoBehaviour
    {
        public static SpectateMovement Instance { get; private set; }

        public static Dictionary<string, CameraState> SavePoints = new();

        public CameraState CurrentState;
        public EPerspective Perspective { get;private set; }

        private InputAction _inputAction;

        private float _horizontal, _vertical;

        private Camera _camera;

        public Action<PlayerState> OnFirstPersonSpectatePlayer;
        public Action OnStopFirstPersonSpectate;

        [SerializeField] public MoveCamera moveCamera;

        [SerializeField] private Transform cameraTransform, cameraParent;

        [SerializeField] private float speed = 5f;
        public float moveSpeedMultiplier = 1f, lerpAmount = 1f, zoomSpeedMultiplier = 1f, zoomLerpAmount = 1f;

        public float sensitivity = 50f;

        private float _actualSpeed = 0;

        private Vector3 _desiredPos;

        public bool Spectating { private set; get; } = false;

        public bool Locked { set; get; } = false;

        public bool depthOfField = false;

        private DepthOfField _depthOfField;

        private Ray _raycast;
        private RaycastHit _hit;
        private bool _isHit;
        private float _hitDistance;

        [SerializeField] private float focusSpeed = 8f;

        private Quaternion _camRotation;

        public EDoFMode eDoFMode;

        private int _currentSelectedPlayer = -1;

        #region Target
        
        public ITarget Target { get; private set; }

        private int _boneIndex=0;

        private Transform TargetTransform { get; set; }

        public bool TargetRotationSync { get; private set; } = false;

        #endregion

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                _inputAction = GameManager.InputManager.FindAction("Move");

                sensitivity = GameManager.Instance.setting.sensitivity;
                _desiredPos = cameraTransform.localPosition;

                CurrentState = new CameraState(_desiredPos, 0, 0, 0);
                depthOfField = false;

                _camera = cameraTransform.GetComponentInChildren<Camera>();
                
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _depthOfField = GameManager.Instance.GetDepthOfField();
            
            cameraTransform.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Locked || !Spectating) return;

            if ( NetworkManager.Instance.CantPlay()) return;

            if (Perspective == EPerspective.Spectate)
            {
                Spectate();

                DepthOfFieldCalculation();
            }

            if (Mouse.current.leftButton.wasPressedThisFrame && Perspective == EPerspective.FirstPerson)
            {
                ChangePerspective(EPerspective.FirstPerson);
            }
            else if(Keyboard.current.vKey.wasPressedThisFrame)
            {
                LoopPerspective();
            }
        }

        private void LateUpdate()
        {
            CurrentState.Position = cameraTransform.localPosition;
            
            if (TargetTransform)
            {
                cameraParent.position = TargetTransform.position;
                if (TargetRotationSync)
                    cameraParent.rotation = TargetTransform.rotation;
            }
        }

        void Spectate()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            bool ctrl = Keyboard.current.leftCtrlKey.isPressed;
            bool holdRight = Mouse.current.rightButton.isPressed;

            if (Input.GetKeyDown(KeyCode.R))
            {
                CurrentState.Tilt = 0;
                CurrentState.Fov = CameraState.DefaultFov;
            }

            #region Spectate

            var input = _inputAction.ReadValue<Vector2>();
            _horizontal = input.x;
            _vertical = input.y;

            _actualSpeed = Input.GetKey(KeyCode.LeftShift) ? speed * 2f :
                Input.GetKey(KeyCode.Space) ? speed / 2 : speed;
            _actualSpeed *= moveSpeedMultiplier;
            
            _desiredPos += _actualSpeed * Time.fixedDeltaTime *
                           (cameraTransform.forward * _vertical + cameraTransform.right * _horizontal);
            
            if (Keyboard.current.eKey.isPressed)
            {
                _desiredPos += _actualSpeed * Time.fixedDeltaTime * Vector3.up;
            }

            if (Keyboard.current.qKey.isPressed)
            {
                _desiredPos -= _actualSpeed * Time.fixedDeltaTime * Vector3.up;
            }

            cameraTransform.localPosition =
                Vector3.Lerp(cameraTransform.localPosition, _desiredPos, Time.unscaledDeltaTime * 10f);

            #endregion

            #region Look

            float tiltInput = Input.GetKey(KeyCode.RightArrow) ? Input.GetKey(KeyCode.LeftArrow) ? 0 : -1 :
                Input.GetKey(KeyCode.LeftArrow) ? 1 : 0;

            if (holdRight)
            {
                CurrentState.SetFov(CurrentState.Fov -
                                    mouseY * zoomSpeedMultiplier * sensitivity * Time.fixedDeltaTime);
            }
            else
            {
                float lookMouseX = mouseX * sensitivity * Time.fixedDeltaTime;
                float lookMouseY = mouseY * sensitivity * Time.fixedDeltaTime;

                if (ctrl)
                {
                    tiltInput = mouseY;
                    lookMouseY = 0;
                }

                CurrentState.DesiredX += lookMouseX;

                //Rotate, and also make sure we dont over- or under-rotate.
                CurrentState.XRotation -= lookMouseY;

                CurrentState.XRotation = Mathf.Clamp(CurrentState.XRotation, -90, 90);

                _camRotation = Quaternion.Euler(CurrentState.XRotation, CurrentState.DesiredX, CurrentState.Tilt);
            }

            _camera.fieldOfView = Math.Abs(zoomLerpAmount - 1f) < 0.001f
                ? CurrentState.Fov
                : Mathf.Lerp(_camera.fieldOfView, CurrentState.Fov, Time.unscaledDeltaTime * 40f * zoomLerpAmount);

            //Perform the rotations
            cameraTransform.localRotation = Math.Abs(lerpAmount - 1f) < 0.001f
                ? _camRotation
                : Quaternion.Lerp(cameraTransform.localRotation, _camRotation,
                    Time.unscaledDeltaTime * 40f * lerpAmount);
            CurrentState.Tilt += tiltInput * sensitivity * Time.fixedDeltaTime * moveSpeedMultiplier;

            #endregion
        }

        void DepthOfFieldCalculation()
        {
            switch (eDoFMode)
            {
                case EDoFMode.AutoFocus:
                    _raycast = new Ray(cameraTransform.position, cameraTransform.forward * 100);

                    _isHit = false;

                    if (Physics.Raycast(_raycast, out _hit, 100f))
                    {
                        _isHit = true;
                        _hitDistance = _hit.distance;
                    }
                    else
                    {
                        if (_hitDistance < 100f)
                        {
                            _hitDistance++;
                        }
                    }

                    break;
                case EDoFMode.TargetFocus:
                    if(TargetTransform == null)
                        break;
                    var selfPosition = cameraTransform.position;
                    _raycast = new Ray(selfPosition, (TargetTransform.position - selfPosition).normalized * 100f);

                    _isHit = false;

                    if (Physics.Raycast(_raycast, out _hit, 100f))
                    {
                        _isHit = true;
                        _hitDistance = _hit.distance;
                    }
                    else
                    {
                        if (_hitDistance < 100f)
                        {
                            _hitDistance++;
                        }
                    }
                    break;
            }


            SetFocus();
        }

        void SetFocus()
        {
            _depthOfField.focusDistance.value = Mathf.Lerp(_depthOfField.focusDistance.value, _hitDistance,
                Time.unscaledDeltaTime * focusSpeed);
        }

        public bool TryToBindTarget()
        {
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit))
            {
                if (hit.transform.root.TryGetComponent(out ITarget target))
                {
                    Target = target;

                    _boneIndex = 0;
                    
                    SetTargetTransform(Target.Bones[_boneIndex]);
                    return true;
                }
            }

            return false;
        }

        private void SetTargetTransform(Transform t)
        {
            TargetTransform = t;
            Debug.Log("Current Target: " + t.name);

            Vector3 pos = t.InverseTransformPoint(cameraTransform.position);
            
            _desiredPos = pos;
            cameraTransform.localPosition = pos;
        }

        public void CycleBone()
        {
            _boneIndex++;

            if (Target != null)
            {
                _boneIndex = Math.Clamp(_boneIndex, 0, Target.Bones.Length - 1);
                
                SetTargetTransform(Target.Bones[_boneIndex]);
            }
        }

        public void BoneLs()
        {
            if (Target != null)
            {
                for (int i = 0; i < Target.Bones.Length; i++)
                {
                    Debug.Log($"{i}. {Target.Bones[i].name}");
                }
            }
        }

        public void SetBone(string boneName)
        {
            if (Target != null)
            {
                for (int i = 0; i < Target.Bones.Length; i++)
                {
                    if (String.Equals(Target.Bones[i].name, boneName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _boneIndex = i;
                        SetTargetTransform(Target.Bones[i]);
                        return;
                    }
                }
            }
        }
        // private void OnDrawGizmos()
        // {
        //     if (_isHit)
        //     {
        //         Gizmos.DrawSphere(_hit.point,0.1f);
        //         
        //         Debug.DrawRay(camera.position,camera.forward * _hitDistance);
        //     }
        //     else
        //     {
        //         Debug.DrawRay(camera.position,camera.forward * 100f);
        //     }
        // }

        public void StartSpect()
        {
            cameraTransform.position = Vector3.zero;
            _desiredPos = Vector3.zero;
            Spectating = true;
            Perspective = EPerspective.Spectate;

            cameraTransform.gameObject.SetActive(true);

            moveCamera.gameObject.SetActive(false);

            ListenerManager.Instance.SetCamera(cameraTransform);

            SetDof(depthOfField);

            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.gameSceneImportant.SetActive(false);
                GameUIManager.Instance.gameSceneMisc.SetActive(false);
            }

            if (!NetworkManager.Instance.CantPlay(false))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void StopSpect(bool keepFirstPersonCamera)
        {
            Spectating = false;

            cameraTransform.gameObject.SetActive(false);

            moveCamera.gameObject.SetActive(keepFirstPersonCamera);
            if (keepFirstPersonCamera)
            {
                ListenerManager.Instance.SetCamera(moveCamera.camTransform);

                if (PlayerState.LocalPlayer != null)
                {
                    SetToLocal(PlayerState.LocalPlayer);
                }
            }

            // Leaving spectate must not keep the watched player's scope on screen.
            if (WeaponManager.Instance)
                WeaponManager.Instance.ResetAim();

            _depthOfField.active = false;
            
            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.gameScene.SetActive(true);
                GameUIManager.Instance.gameSceneImportant.SetActive(true);
                GameUIManager.Instance.gameSceneMisc.SetActive(true);
            }
        }

        public void LoopPerspective()
        {
            int newPerspective = (int)Perspective + 1;

            if (newPerspective > 1) newPerspective = 0;
                
            ChangePerspective((EPerspective)newPerspective);
        }

        public void ChangePerspective(EPerspective perspective)
        {
            OnStopFirstPersonSpectate?.Invoke();
            
            Perspective = perspective;

            switch (Perspective)
            {
                case EPerspective.FirstPerson:
                    
                    LoopFirstPerson();
                    break;
                case EPerspective.Spectate:
                    //TODO: Set Audio listener transform
                    ListenerManager.Instance.SetCamera(cameraTransform);
                    moveCamera.DeInitialize();
                    
                    GameUIManager.Instance.gameSceneImportant.SetActive(false);
                    cameraTransform.gameObject.SetActive(true);
                    moveCamera.gameObject.SetActive(false);
                    
                    // //TODO: Reset Position
                    // cameraTransform.position = Vector3.zero;
                    // _desiredPos = Vector3.zero;
                    break;
            }
        }

        void LoopFirstPerson()
        {
            for (int i = 0; i < PlayerState.PlayerStates.Count; i++)
            {
                ++_currentSelectedPlayer;

                if (_currentSelectedPlayer >= PlayerState.PlayerStates.Count) _currentSelectedPlayer = 0;
                
                //Set Player
                if (!PlayerState.PlayerStates[_currentSelectedPlayer].selfControlled && PlayerState.PlayerStates[_currentSelectedPlayer].CanSpectate)
                {
                    GameUIManager.Instance.gameScene.SetActive(true);
                    GameUIManager.Instance.gameSceneImportant.SetActive(true);
                    
                    cameraTransform.gameObject.SetActive(false);
                    moveCamera.gameObject.SetActive(true);
                        
                    ListenerManager.Instance.SetCamera(moveCamera.camTransform);
                        
                    WeaponManager.Instance.InitializePlayer(PlayerState.PlayerStates[_currentSelectedPlayer]);
            
                    PlayerParticle.Instance.InitializePlayer(PlayerState.PlayerStates[_currentSelectedPlayer]);
                    SlideAudio.Instance.InitializePlayer(PlayerState.PlayerStates[_currentSelectedPlayer]);

                    PickInteractor.Instance.DeInitialize();
                    Interactor.Instance.DeInitialize();
                        
                    moveCamera.SetPlayer(PlayerState.PlayerStates[_currentSelectedPlayer]);
                        
                    OnFirstPersonSpectatePlayer?.Invoke(PlayerState.PlayerStates[_currentSelectedPlayer]);
                    
                    break;
                }
            }
        }

        public void SetToLocal(PlayerState ps)
        {
            OnStopFirstPersonSpectate?.Invoke();

            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.gameSceneImportant.SetActive(true);
                GameUIManager.Instance.gameSceneMisc.SetActive(true);
            }

            cameraTransform.gameObject.SetActive(false);
            moveCamera.gameObject.SetActive(true);
            
            ListenerManager.Instance.SetCamera(moveCamera.camTransform);
            
            moveCamera.SetPlayer(ps);
            
            WeaponManager.Instance.InitializePlayer(ps);
            
            PlayerParticle.Instance.InitializePlayer(ps);
            
            SlideAudio.Instance.InitializePlayer(ps);
            
            PickInteractor.Instance.InitializePlayer(ps);
            
            Interactor.Instance.InitializePlayer(ps);
        }

        public void AddNewSavePoint(string str)
        {
            CameraState cameraState = (CameraState)CurrentState.Clone();
            if (SavePoints.ContainsKey(str))
            {
                SavePoints[str] = cameraState;

                Debug.Log("SavePoint already exist, override!");
            }
            else
            {
                SavePoints.Add(str, cameraState);
            }
        }

        public void LoadSavePoint(string str)
        {
            if (SavePoints.TryGetValue(str, out var state))
            {
                cameraTransform.localPosition = state.Position;

                _desiredPos = state.Position;

                CurrentState.Fov = state.Fov;
                CurrentState.XRotation = state.XRotation;
                CurrentState.DesiredX = state.DesiredX;
                CurrentState.Tilt = state.Tilt;

                _camera.fieldOfView = CurrentState.Fov;

                _camRotation = Quaternion.Euler(CurrentState.XRotation, CurrentState.DesiredX, CurrentState.Tilt);

                cameraTransform.localRotation = _camRotation;
            }
            else
            {
                Debug.LogError("No save point entry found");
            }
        }

        public void CameraShake(float amplitude,float frequency,float duration)
        {
            CameraShaker.GetInstance("DebugCamera").ShakeOnce(amplitude, frequency, 0.1f, duration + 0.1f);
        }

        public void SetTargetRotationSync(bool flag)
        {
            TargetRotationSync = flag;
            if (!flag)
            {
                cameraParent.rotation = Quaternion.identity;
            }
        }

        public void SetDof(bool enable)
        {
            depthOfField = enable;

            _depthOfField.active = depthOfField;
        }

        public void SetDoFAperture(float aper)
        {
            _depthOfField.aperture.value = aper;
        }

        public void SetDoFBlur(float blur)
        {
            _depthOfField.focalLength.value = blur;
        }

        public bool GetDof()
        {
            return depthOfField;
        }

        public float GetDofAperture()
        {
            return _depthOfField.aperture.value;
        }

        public float GetDofBlur()
        {
            return _depthOfField.focalLength.value;
        }

        public float GetDofDistance()
        {
            return _depthOfField.focusDistance.value;
        }

        public void SetDofDistance(float distance)
        {
            _hitDistance = distance;
            _depthOfField.focusDistance.value = distance;
        }

        public void NudgeDofDistance(float nudge)
        {
            _hitDistance += nudge;
            _depthOfField.focusDistance.value = _hitDistance;
        }

        public void SetDofMode(int mode)
        {
            mode = Mathf.Min(mode, 2);

            mode = Mathf.Max(0, mode);

            eDoFMode = (EDoFMode)mode;
        }

        public int GetDofMode()
        {
            return (int)eDoFMode;
        }
    }

    
    public class CameraState : ICloneable
    {
        public static float DefaultFov = 75f, MaxFov = 130, MinFov = 5;
        
        public Vector3 Position;

        public float XRotation, DesiredX, Tilt, Fov = DefaultFov;

        public CameraState(Vector3 position, float xRotation, float desiredX, float tilt)
        {
            Position = position;
            XRotation = xRotation;
            DesiredX = desiredX;
            Tilt = tilt;
        }

        public void SetFov(float v)
        {
            Fov = Mathf.Clamp(v, MinFov, MaxFov);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
