using System;
using CodingDaniel.MapEditor.UI;
using Demo.Entity;
using Extensions;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Client;
using PlayerCameraController;
using UnityEngine;
using Cursor = UnityEngine.Cursor;

namespace Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        public static PlayerMovement Instance;

        public PlayerState currentPlayer;
    
        //Assingables
        public Transform playerCam;
        public Transform orientation;
        
        public GameObject playerSmokeFx;
    
        //Other
        private Rigidbody rb;

        //Rotation and look
        private float xRotation;
        public float sensitivity = 50f;
        internal float sensMultiplier = 1f;

        private IPlayerMovement _playerMovement;
        public IPlayerMovement PlayerMovementValue => _playerMovement;
        
        [SerializeField] private Movement movement = new Movement();
        [SerializeField] private NoClipMovement noClipMovement = new NoClipMovement();
    
        public int jumpLeft = 1;
        //Input
        public float x, y;
        public bool inWater;

        private float _fallSpeed;

        public float FallSpeed => _fallSpeed;

        private CapsuleCollider _collider;

        private InputManager _inputManager;
        
        void Awake()
        {
            Instance = this;

            rb = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();
            _playerHeight = _collider.bounds.size.y;
            player = GetComponentInParent<ClientPlayer>();
            _transform = transform;
            _demoPlayer = _transform.parent.GetComponent<DemoPlayer>();

            _inputManager = GameManager.InputManager;

            aimAssist = GameManager.Instance.setting.useController && Input.GetJoystickNames().Length>0;

            _playerMovement = movement;
            
            PlayerMovementValue.MyAwake(this,playerCam,orientation,playerSmokeFx,rb,_collider, _transform);
            
            SetPerks();
        }

        void EnableMovement()
        {
            PlayerMovementValue.MyAwake(this,playerCam,orientation,playerSmokeFx,rb,_collider, _transform);
            PlayerMovementValue.MyOnEnable();
        }

        void DisableMovement()
        {
            PlayerMovementValue.MyOnDisable();
            PlayerMovementValue.DeInitialize();
        }

        public CapsuleCollider GetCollider()
        {
            return _collider;
        }
        
        private void OnEnable()
        {
            sensitivity = GameManager.Instance.setting.sensitivity;
        
            PlayerMovementValue.MyOnEnable();
        
            if(PerkInGameMenu.Instance)
                PerkInGameMenu.Instance.Enable();
        
            GameManager.Instance.PlayerSpawn?.Invoke(MoveCamera.Instance.cam);
        
            if (NetworkManager.ClientGameMode == GameMode.GunGame)
            {
                GunGameWeapon.Instance.Open(player.WeaponLevel);
            }
        }

        public void DeInitialize()
        {
            PlayerMovementValue.DeInitialize();
        }
        public void SetPerks(bool change=false)
        {
            _readyToWallrun = PerkManager.Instance.HasPerk(Perk.WallRun);
            jumpFactor = PerkManager.Instance.HasPerk(Perk.Forg) ? 1.35f : 1f;
            perkMoveSpeedFactor = PerkManager.Instance.HasPerk(Perk.Nerd) ? 0.8f : 1f;
            perkMoveSpeedFactor *= PerkManager.Instance.HasPerk(Perk.Fat) ? 0.8f : 1f;

            if (change&&player != null)
            {
                player.SentPerks();
            }
        }
    
        private void OnDisable()
        {
            PlayerMovementValue.MyOnDisable();

            GameManager.Instance.PlayerDestroy?.Invoke();
            inWater = false;
            UnderWaterSfx.Instance.SetUnderWater(false);
            rb.drag = 0;
        }

        private void OnDestroy()
        {
            if(HitDetection.Instance)
                HitDetection.Instance.StopAllCoroutines();
        }

        void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if(HitDetection.Instance)
                HitDetection.Instance.SetProperty(this);
        }

        public void UpMovementSpeed()
        {
            moveSpeedFactor += 0.1f;
        }
        public void DownMovementSpeed()
        {
            moveSpeedFactor = 1;
            moveSpeedFactor = Mathf.Clamp(moveSpeedFactor, 1f, 1.5f);
        }
        private static int lastFrame = -1;
        private void FixedUpdate() {
            PlayerMovementValue.MyFixedUpdate();
        
            if ( lastFrame == Time.frameCount )
                return;
        
            if(player!=null)
                player.SendMovement(xRotation);

            lastFrame = Time.frameCount;
        }

        private void LateUpdate()
        {
            currentPlayer.SetVelocity(rb.velocity);
            PlayerMovementValue.MyLateUpdate();
        }

        private bool speed_a=false,speed_B=false;

        public void ChangeNoClip()
        {
            if (_playerMovement == movement)
            {
                DisableMovement();
                _playerMovement = noClipMovement;
            }
            else
            {
                DisableMovement();
                _playerMovement = movement;
            }
            EnableMovement();
        }

        private void Update()
        {
            _fallSpeed = rb.velocity.y;
       
            MyInput();
            if (NetworkManager.Instance.CantPlay()) return;
            PlayerMovementValue.MyUpdate();
            Look();

            if (!speed_a&&rb.velocity.magnitude > 60)
            {
                //faster faster yesssssssss
                VoiceLine.Instance.PlayVoice(VoiceKey.speed_A);
                speed_a = true;
            }

            if (!speed_B)
            {
                if (rb.velocity.magnitude < 0.1f)
                {
                    stayTimer += Time.deltaTime;
                }
                else
                {
                    stayTimer = 0;
                }

                if (stayTimer > 20)
                {
                    //what are you thinking,why don’t you move
                    speed_B = true;
                    VoiceLine.Instance.PlayVoice(VoiceKey.speed_B);
                
                }
            }
        
        
        }
    
        private bool moved = false;
        /// <summary>
        /// Find user input. Should put this in its own class but im lazy
        /// </summary>
        private void MyInput() {
            if (GameUIManager.Instance && GameUIManager.Instance.pause ||
                NetworkManager.Instance.CheckMultiplayerGameModeStarted() || NetworkManager.Instance.CantPlay())
            {
                x=0;
                y=0;
                return;
            }

            var input = _inputManager.FindAction("Move").ReadValue<Vector2>();
            x = input.x;
            y = input.y;
            if (TabHolder.Instance != null) return;
            if ( !moved&& (Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f))
            {
                moved = true;
                if (ClientPlayer.list.ContainsKey(NetworkManager.Instance.Client.Id)&&!ClientPlayer.list[NetworkManager.Instance.Client.Id].moved)
                {
                    ClientPlayer.list[NetworkManager.Instance.Client.Id].moved = true;
                }
            }
        }

        private float stayTimer = 0;


        
        private Transform _transform;

        private DemoPlayer _demoPlayer;

        public DemoPlayer DemoPlayer => _demoPlayer;

        public float moveSpeedFactor=1,jumpFactor=1;
        public float perkMoveSpeedFactor = 1f;

        
        private float actualWallRotation;

        private float wallRotationVel;
        public float desiredX { get; private set; }

        /// <summary>
        /// Vertical (pitch) look rotation of the local player, used to sync the
        /// demo/spectate player spine &amp; head so recorded players look up/down correctly.
        /// </summary>
        public float XRotation => xRotation;

        public bool aimAssist = false;
        [SerializeField]private LayerMask whatIsAssist;
        private void Look()
        {
            float multiplier = 1f;

            float offsetX = 0, offsetY = 0;

            if (aimAssist)
            {
                Vector3 camPos = playerCam.position;
                Vector3 myDir = playerCam.forward;
                Ray ray = new Ray(camPos,myDir);
                if (Physics.SphereCast(ray,1f,out var hit,1000f, whatIsAssist))
                {
                    multiplier = 0.5f;

                    Vector3 realDir = hit.transform.position - camPos;
                    float cross = Vector3.Cross(myDir, realDir).y;
                    offsetX = Mathf.Clamp(cross, -0.12f, 0.12f);
                    // if (Physics.Raycast(ray, out hit, 1000f, whatIsAssist))
                    // {
                    //     Debug.DrawLine(camPos,objPos,Color.red);
                    //     Debug.DrawLine(camPos,hit.point,Color.green);
                    //     // Debug.Log((hit.point).x-objPos.x);
                    //     offsetX = Mathf.Clamp(objPos.x-(hit.point).x, -.1f, .1f);
                    // }
                    // else
                    // {
                    //     Debug.DrawLine(camPos,objPos,Color.red);
                    //     Debug.DrawLine(camPos,hitPoint,Color.green);
                    //     // Debug.Log((hit.point).x-objPos.x);
                    //     offsetX = Mathf.Clamp(-hitPoint.x+objPos.x, -.25f, .25f);
                    // }
                }
            }
            // Debug.Log(Input.GetAxis("Mouse X") + " " + mouseLook.x);
            //mouseLook.x/20f
            float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier * multiplier;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier * multiplier;
        
            Recoil.Instance.CalculateRecoil();
            Vector3 recoilRot = Recoil.Instance.currentRotation;
            //Find current look rotation
            // Vector3 rot = playerCam.localRotation.eulerAngles;
            desiredX += mouseX + recoilRot.y + offsetX;
            FindWallRunRotation();
            actualWallRotation = Mathf.SmoothDamp(actualWallRotation, wallRunRotation, ref wallRotationVel, 0.15f);
            //Rotate, and also make sure we dont over- or under-rotate.
            xRotation -= mouseY;
            xRotation += recoilRot.x;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
       
            //Perform the rotations
            playerCam.rotation = Quaternion.Euler(xRotation, desiredX, 0);
            orientation.localRotation = Quaternion.Euler(0, desiredX, 0);
        }
        private void FindWallRunRotation()
        {
            if (!movement.WallRunning)
            {
                wallRunRotation = 0f;
                return;
            }

            var rotation = playerCam.rotation;
            float num = 0f;
            float current = rotation.eulerAngles.y;
        
            num = Vector3.SignedAngle(new Vector3(0f, 0f, 1f), movement.WallNormalVector, Vector3.up);
            if (Math.Abs(movement.WallNormalVector.x - 1f) < 0.1f)
            {
                num = 90f;
            }
            else if (Math.Abs(movement.WallNormalVector.x - -1f) < 0.1f)
            {
                num = 270f;
            }
            else if (Math.Abs(movement.WallNormalVector.z - 1f) < 0.1f)
            {
                num = 0f;
            }
            else if (Math.Abs(movement.WallNormalVector.z - -1f) < 0.1f)
            {
                num = 180f;
            }
            float num2 = Mathf.DeltaAngle(current, num);
            wallRunRotation = -num2 / 90f * 15f;
            if (!_readyToWallrun)
            {
                return;
            }
            if ((Mathf.Abs(wallRunRotation) < 4f && y > 0f && Math.Abs(x) < 0.1f) || (Mathf.Abs(wallRunRotation) > 22f && y < 0f && Math.Abs(x) < 0.1f))
            {
                if (!cancelling)
                {
                    cancelling = true;
                    CancelInvoke(nameof(CancelWallrun));
                    Invoke(nameof(CancelWallrun), 0.2f);
                }
            }
            else
            {
                cancelling = false;
                CancelInvoke(nameof(CancelWallrun));
            }
        }
        private float wallRunRotation;
        private bool cancelling;
        private void CancelWallrun()
        {
            Invoke(nameof(GetReadyToWallrun), 0.1f);
            rb.AddForce(movement.WallNormalVector * 600f);
            _readyToWallrun = false;
        }
        private void GetReadyToWallrun()
        {
            _readyToWallrun = PerkManager.Instance.perks.Contains(Perk.WallRun);
        }
        

        /// <summary>
        /// Find the velocity relative to where the player is looking
        /// Useful for vectors calculations regarding movement and limiting movement
        /// </summary>
        /// <returns></returns>
        public Vector2 FindVelRelativeToLook() {
            float lookAngle = orientation.eulerAngles.y;
            var velocity = rb.velocity;
            return VelocityExtensions.FindVelRelativeToLook(lookAngle, velocity);
        }


        private float _playerHeight;

        public float PlayerHeight => _playerHeight;
        
        public int maxJumpCount = 1;
        private void OnCollisionEnter(Collision other)
        {
            PlayerMovementValue.MyOnCollisionEnter(other);
        
        }

        
        private bool _readyToWallrun = false;
        public bool ReadyToWallRun => _readyToWallrun;
        
        /// <summary>
        /// Handle ground detection
        /// </summary>
        private void OnCollisionStay(Collision other) {
            PlayerMovementValue.MyOnCollisionStay(other);
        }
        public bool IsGrounded()
        {
            return movement.grounded;
        }
    
        public bool IsCrouching()
        {
            return movement.crouching;
        }
        public bool IsWallRunning()
        {
            return movement.WallRunning;
        }

        public bool IsJumping()
        {
            return movement.jumping;
        }

        public float GetWallRunRotation()
        {
            return actualWallRotation;
        }
        public Vector3 GetVelocity()
        {
            return rb.velocity;
        }

        public bool IsNoclip()
        {
            return PlayerMovementValue == noClipMovement;
        }

        public Rigidbody GetRb()
        {
            return rb;
        }
    
        public static Vector3 XZVector(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        public ClientPlayer player;
    
        
    }
}