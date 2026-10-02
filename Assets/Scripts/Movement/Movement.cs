using System;
using System.Collections;
using Audio;
using CodingDaniel.MapEditor.UI;
using EZCameraShake;
using Manager;
using Menu;
using Multiplayer;
using PlayerCameraController;
using Pool;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Movement
{
    [Serializable]
    public class Movement : IPlayerMovement
    {
        public PlayerMovement PlayerMovement { get; set; }
        public Transform PlayerTransform { get; set; }
        public Transform PlayerCam { get; set; }
        public Transform Orientation { get; set; }
        public GameObject PlayerSmokeFx { get; set; }
        public Rigidbody Rb { get; set; }
        public CapsuleCollider Collider { get; set; }

        //Movement
        [Header("Movement")]
        public float moveSpeed = 3250;
        public float maxSpeed = 16;
        public bool grounded;
        public LayerMask whatIsGround;
        
    
        public float counterMovement = 0.145f;
        private float _threshold = 0.01f;
        public float maxSlopeAngle = 45f;

        //Crouch & Slide
        [Header("Crouch & Slide")]
        private Vector3 _crouchScale = new Vector3(1f, 1.05f, 1f);

        private Vector3 _playerScale = new Vector3(1f, 1.5f, 1f);
        public float slideForce = 400;
        public float slideCounterMovement = 0.32f;

        //Jumping
        [Header("Jumping")]
        private bool _readyToJump = true;
        private float _jumpCooldown = 0.1f;
        public float jumpForce = 490f;
        
        //Input
        [Header("Input")]
        public bool jumping, crouching;
        private bool _canCrouch = true;
        
        //Sliding
        private Vector3 _normalVector = Vector3.up;
        private Vector3 _wallNormalVector;
        private Vector3 _groundPoint;

        //dash
        [Header("Dash")]
        private bool canDash = true;
        public float dashForce = 75f,dashDuration=0.1f;

        public float minDisHead = 2f;
        private float _distance;
        private RaycastHit _slopeHit;
        private Vector3 _lastMoveSpeed;
        private float _wallRunGravity = 1f;
        private bool ReadyToWallRun => PlayerMovement.ReadyToWallRun;

        private float HorizontalInput => PlayerMovement.x;
        private float VerticalInput => PlayerMovement.y;
        private bool UnderWater => PlayerMovement.inWater;
        private float MoveSpeedFactor => PlayerMovement.moveSpeedFactor;
        private float PerkMoveSpeedFactor => PlayerMovement.perkMoveSpeedFactor;
        private float JumpFactor => PlayerMovement.jumpFactor;

        public Vector3 WallNormalVector => _wallNormalVector;
        private int JumpLeft
        {
            get => PlayerMovement.jumpLeft;
            set => PlayerMovement.jumpLeft = value;
        }
        public void MyAwake(PlayerMovement playerMovement,Transform playerCam, Transform orientation, GameObject playerSmokeFx, Rigidbody rb,CapsuleCollider collider,Transform playerTransform)
        {
            PlayerTransform = playerTransform;
            PlayerMovement = playerMovement;
            PlayerCam = playerCam;
            Orientation = orientation;
            PlayerSmokeFx = playerSmokeFx;
            Rb = rb;
            Collider = collider;
            Rb.isKinematic = false;
        }
        public void MyOnEnable()
        {
            canDash = true;
            _readyToJump = true;
            _canCrouch = true;
            _wallRunning = false;
            grounded = false;
            crouching = false;
            jumping = false;

            GameManager.InputManager.Player.Jump.started += StartJump;
            GameManager.InputManager.Player.Jump.canceled += StopJump;
        
            GameManager.InputManager.Player.Crouch.started += StartCrouch;
            GameManager.InputManager.Player.Crouch.canceled += StopCrouch;
        
            GameManager.InputManager.Player.Dash.performed += Dash;

            PlayerMovement.currentPlayer.Grounded = grounded;
        }

        public void MyOnDisable()
        {
            GameManager.InputManager.Player.Jump.started -= StartJump;
            GameManager.InputManager.Player.Jump.canceled -= StopJump;
        
            GameManager.InputManager.Player.Crouch.started -= StartCrouch;
            GameManager.InputManager.Player.Crouch.canceled -= StopCrouch;

            GameManager.InputManager.Player.Dash.performed -= Dash;
            
            PlayerMovement.StopAllCoroutines();
        }

        public void MyFixedUpdate()
        {
            MovementCalculation();

            if (!PlayerMovement.inWater)
            {
                Rb.drag = 0;
            }
        }

        public void MyUpdate()
        {
            _lastMoveSpeed = PlayerMovement.XZVector(Rb.velocity);
            if (_needToStand && crouching)
            {
                if (!Physics.Raycast(PlayerTransform.position, new Vector3(0, 1f, 0), 2f, whatIsGround))
                {
                    _needToStand = false;
                    crouching = false;
                    if(PlayerMovement.player)PlayerMovement.player.SendStopCrouch();
                    PlayerTransform.localScale = _playerScale;
                    var position = PlayerTransform.position;
                    position = new Vector3(position.x, position.y +  0.45f, position.z);
                    PlayerTransform.position = position;
                    PlayerMovement.currentPlayer.IsCrouching = crouching;
                }
            }
        }

        public void MyLateUpdate()
        {
            WallRunningCalculation();
        }

        #region Movement

        private void MovementCalculation()
        {
            float x = HorizontalInput, y = VerticalInput;
            if (x==0 && y==0&&grounded&&Rb.velocity.sqrMagnitude < 1f)
            {
                Rb.velocity = Vector3.zero;
            }
        
            //Extra gravity
            Rb.AddForce(Time.fixedDeltaTime * 10 * Vector3.down);
        
            //Find actual velocity relative to where player is looking
            Vector2 mag = PlayerMovement.FindVelRelativeToLook();
            float xMag = mag.x, yMag = mag.y;
        
            //Counteract sliding and sloppy movement
            FootSteps();
            CounterMovement(x, y, mag);
            AirAcceleration();
            // if (GameUIManager.Instance && GameUIManager.Instance.pause || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || NetworkManager.Instance.CantPlay()) return;

            if(jumping && !UnderWater) Jump();
            //Set max speed
            float maxSpeed = this.maxSpeed*MoveSpeedFactor*PerkMoveSpeedFactor;

            bool isSlope = IsSlope();
            //If sliding down a ramp, add force down so player stays grounded and also builds speed
            if (crouching && grounded && _readyToJump) {
                Rb.AddForce(Time.deltaTime * 3000*MoveSpeedFactor*Vector3.down);
                if (isSlope)
                {
                    Rb.AddForce(moveSpeed*Time.deltaTime*0.5f*Orientation.forward);
                }
                return;
            }

        
        
            //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
            if (x > 0 && xMag > maxSpeed) x = 0;
            if (x < 0 && xMag < -maxSpeed) x = 0;
            if (y > 0 && yMag > maxSpeed) y = 0;
            if (y < 0 && yMag < -maxSpeed) y = 0;

            //Some multipliers
            float multiplier = 1f, multiplierV = 1f;
            float wallMultiplier = 1f,wallMultiplierV=1;
        
            // Movement in air
            if (!grounded) {
                multiplier = 0.5f;
                multiplierV = 0.5f;
            }

            if (_wallRunning)
            {
                wallMultiplierV = 0.3f;
                wallMultiplier = 0.3f;
            }
            if (_surfing)
            {
                multiplier = 0.6f;
                multiplierV = 0.3f;
            }
        
            // Movement while sliding
            // if (grounded && crouching) multiplierV = 0.5f;
            //Apply forces to move player
            if (UnderWater)
            {
                Rb.AddForce( y * moveSpeed * Time.deltaTime *MoveSpeedFactor*PerkMoveSpeedFactor* wallMultiplierV * wallMultiplier*PlayerCam.forward );
                Rb.AddForce( x * moveSpeed * Time.deltaTime *MoveSpeedFactor*wallMultiplier*PerkMoveSpeedFactor*PlayerCam.right );
            }
            else
            {
                if (!isSlope)
                {
                    Rb.AddForce( y * moveSpeed * Time.deltaTime * multiplier * multiplierV*MoveSpeedFactor*PerkMoveSpeedFactor* wallMultiplierV * wallMultiplier*Orientation.forward );
                    Rb.AddForce( x * moveSpeed * Time.deltaTime * multiplier*MoveSpeedFactor*wallMultiplier*PerkMoveSpeedFactor*Orientation.right );
                }
                else
                {
                    Vector3 dir = Orientation.forward*y;
                    dir = Vector3.ProjectOnPlane(dir, _slopeHit.normal);
                    Rb.AddForce( moveSpeed * Time.deltaTime * multiplier * multiplierV*PerkMoveSpeedFactor*dir); 
                    dir =  Orientation.right*x;
                    dir = Vector3.ProjectOnPlane(dir, _slopeHit.normal);
                    Rb.AddForce( moveSpeed * Time.deltaTime * multiplier*PerkMoveSpeedFactor*dir);
                }
            }
        }
        private void WallRunningCalculation()
        {
            if (WallRunning)
            {
                Rb.AddForce( Time.deltaTime * 16f*10f*-WallNormalVector);
                Rb.AddForce(Time.deltaTime * Rb.mass * 500f * _wallRunGravity*Vector3.up);
            }
        }
        private void CounterMovement(float x, float y, Vector2 mag) {
            if (!grounded || jumping) return;

            //Slow down sliding
            if (crouching&&Rb.velocity.magnitude>15f) {
                Rb.AddForce(moveSpeed * Time.deltaTime  * slideCounterMovement* -Rb.velocity.normalized);
                return;
            }

            //Counter movement
            if (Math.Abs(mag.x) > _threshold && Math.Abs(x) < 0.05f || (mag.x < -_threshold && x > 0) || (mag.x > _threshold && x < 0)) {
                Rb.AddForce(moveSpeed  * Time.deltaTime * -mag.x * counterMovement* Orientation.right);
            }
            if (Math.Abs(mag.y) > _threshold && Math.Abs(y) < 0.05f || (mag.y < -_threshold && y > 0) || (mag.y > _threshold && y < 0)) {
                Rb.AddForce(moveSpeed  * Time.deltaTime * -mag.y * counterMovement* Orientation.forward);
            }
            if (IsHoldingAgainstHorizontalVel(mag))
            {
                Rb.AddForce(moveSpeed  * 0.02f * (0f - mag.x) * counterMovement * 2f* Orientation.right);
            }
            if (IsHoldingAgainstVerticalVel(mag))
            {
                Rb.AddForce(moveSpeed* 0.02f * (0f - mag.y) * counterMovement * 2f * Orientation.forward );
            }
        
            var velocity = Rb.velocity;
            //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
            if (Mathf.Sqrt((Mathf.Pow(velocity.x, 2) + Mathf.Pow(velocity.z, 2))) > maxSpeed) {
                float fallspeed = velocity.y;
                Vector3 n = velocity.normalized * maxSpeed;
                velocity = new Vector3(n.x, fallspeed, n.z);
                Rb.velocity = velocity;
            }
        }
        private bool IsHoldingAgainstHorizontalVel(Vector2 vel)
        {
            if (!(vel.x < 0f - _threshold) || !(HorizontalInput > 0f))
            {
                if (vel.x > _threshold)
                {
                    return HorizontalInput < 0f;
                }
                return false;
            }
            return true;
        }

        private bool IsHoldingAgainstVerticalVel(Vector2 vel)
        {
            if (!(vel.y < 0f - _threshold) || !(VerticalInput > 0f))
            {
                if (vel.y > _threshold)
                {
                    return VerticalInput < 0f;
                }
                return false;
            }
            return true;
        }
        private void FootSteps()
        {
            if (!crouching  && grounded && !_wallRunning)
            {
                float num = 1.2f;
                float num2 = Rb.velocity.magnitude;
                if (num2 > 20f)
                {
                    num2 = 20f;
                }
                _distance += num2;
                if (_distance > 300f / num)
                {
                    AudioManager.Instance.PlayFootStep();
                    if(GameManager.Instance.setting.spawnParticle)
                    {
                        ObjectPooler.Instance.SpawnFromPool("PlayerWalkSmokeFx", _groundPoint,
                            Quaternion.Euler(-90, 0, 0));
                    
                    }
                    _distance = 0f;
                }

            
            }
        }
        bool IsSlope()
        {
            if (Physics.Raycast(PlayerTransform.position, Vector3.down, out _slopeHit, PlayerMovement.PlayerHeight / 2 + 0.5f))
            {
                if (_slopeHit.normal != Vector3.up) return true;
                else return false;
            }

            return false;
        }
        void AirAcceleration()
        {
            // if (grounded || !jumping) return;
            Vector3 wishDir = Orientation.forward * VerticalInput + Orientation.right * HorizontalInput;
            wishDir.Normalize();
            float currentVel = Vector3.Dot(Rb.velocity, wishDir);

            //10 = wishSpeed
            float addSpeed = Mathf.Clamp( maxAirSpeed - currentVel, 0, 13 * maxAirSpeed * Time.fixedDeltaTime);
            Vector3 accelerate = Rb.velocity + wishDir * addSpeed;
            Rb.velocity = accelerate;
        }

        public float maxAirSpeed = 2.5f;

        #endregion

        #region Collision
        
        private bool _cancellingGrounded,_cancellingWall; 
        private bool _surfing,_cancellingSurf;
        private bool _wallRunning=false;
        public bool WallRunning => _wallRunning;
        public bool onWall=false;

        private Coroutine _groundCoroutine;
        private Coroutine _wallCoroutine;
        private Coroutine _surfCoroutine;

        public void MyOnCollisionEnter(Collision other)
        {
            int layer = other.gameObject.layer;
        
            if (whatIsGround != (whatIsGround | (1 << layer)))
            {
                return;
            }

            for (int i = 0; i < other.contactCount; i++)
            {
                Vector3 normal = other.contacts[i].normal;
                if (IsFloor(normal))
                {
                    JumpLeft = PlayerMovement.maxJumpCount;
                    MoveCamera.Instance.BobOnce(new Vector3(0f, PlayerMovement.FallSpeed, 0f));
                    if ( PlayerMovement.FallSpeed< -12)
                    {
                        if(GameManager.Instance.setting.cameraShake)
                            CameraShaker.Instance.ShakeOnce(2, 5, 0.1f, 0.5f);
                        _groundPoint = other.contacts[0].point;
                        if (GameManager.Instance.setting.spawnParticle)
                        {
                            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = UnityEngine.Object
                                .Instantiate(PlayerSmokeFx, _groundPoint,
                                    Quaternion.LookRotation(PlayerTransform.position - _groundPoint))
                                .GetComponent<ParticleSystem>().velocityOverLifetime;
                            velocityOverLifetime.x = Rb.velocity.x * 2f;
                            velocityOverLifetime.z = Rb.velocity.z * 2f;
                        }
                    }

                
                    // AudioManager.Instance.Play("Landing");
                }
                float num = 1.3f;
                if (IsWall(normal) && layer!=LayerMask.NameToLayer("ClientPlayer")) 
                {
                    Vector3 normalized = _lastMoveSpeed.normalized;
                    Vector3 vector = PlayerTransform.position + Vector3.up * 1.6f;
                    //Debug.DrawLine(vector, vector + normalized * num, Color.blue, 10f);
                    if (!Physics.Raycast(PlayerTransform.position,Vector3.up,0.5f,whatIsGround)&&!Physics.Raycast(vector, normalized, num, whatIsGround) && Physics.Raycast(vector + normalized * num, Vector3.down, out var hitInfo, 3f, whatIsGround))
                    {
                        Vector3 vector2 = hitInfo.point + Vector3.up * PlayerMovement.PlayerHeight * 0.5f;
                        MoveCamera.Instance.vaultOffset += PlayerTransform.position - vector2;
                        PlayerTransform.position = vector2;
                        Rb.velocity = _lastMoveSpeed * 0.4f;
                        if (!_needToStand && !crouching)
                        {
                            if (Physics.Raycast(vector2, Vector3.up, out var hitGround, 2f, whatIsGround) && grounded)
                            {
                                if (hitGround.distance < minDisHead)
                                {
                                    _needToStand = true;
                                    crouching = true;
                                    if(PlayerMovement.player)PlayerMovement.player.SendStartCrouch();
                                    PlayerTransform.localScale = _crouchScale;
                                    var position = PlayerTransform.position;
                                    position = new Vector3(position.x, position.y - 0.65f, position.z);
                                    PlayerTransform.position = position;
                                    if (Rb.velocity.magnitude > 0.5f) {
                                        if (grounded) {
                                            Rb.AddForce(Orientation.forward * slideForce);
                                            AudioManager.Instance.PlayStartSlide();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        public void MyOnCollisionStay(Collision other)
        {
            //Make sure we are only checking for walkable layers
            int layer = other.gameObject.layer;
            if (whatIsGround != (whatIsGround | (1 << layer)))
            {
                return;
            }

            //Iterate through every collision in a physics update
            for (int i = 0; i < other.contactCount; i++)
            {
                _groundPoint = other.contacts[i].point;
                Vector3 normal = other.contacts[i].normal;
                //FLOOR
                if (IsFloor(normal)) {
                    if (!grounded && crouching)
                    {
                        AudioManager.Instance.PlayStartSlide();
                    }
                    // 
                    grounded = true;
                    PlayerMovement.currentPlayer.Grounded = grounded;
                    _cancellingGrounded = false;
                    _normalVector = normal;
                    _wallRunning = false;
                    _wallNormalVector = Vector3.zero;
                    if(_groundCoroutine!=null)
                        PlayerMovement.StopCoroutine(_groundCoroutine);
                    // PlayerMovement.CancelInvoke(nameof(StopGrounded));
                }
                if (IsWall(normal))
                {
                    if (Vector3.Angle(-normal, Orientation.forward) > 30f)
                    {
                        if (!onWall && PlayerMovement.ReadyToWallRun)
                        {
                            AudioManager.Instance.Play("StartWallRun");
                            AudioManager.Instance.Play("WallRun");
                        }

                        _normalVector = normal;
                        onWall = true;
                        _cancellingWall = false;
                        if(_wallCoroutine!=null)
                            PlayerMovement.StopCoroutine(_wallCoroutine);
                        // PlayerMovement.CancelInvoke(nameof(StopWall));
                        StartWallRun(normal);
                    }
               
                }
                if (IsSurf(normal))
                {
                    _surfing = true;
                    _cancellingSurf = false;
                    _normalVector = normal;
                    _wallNormalVector = Vector3.zero;
                    if(_surfCoroutine!=null)
                        PlayerMovement.StopCoroutine(_surfCoroutine);
                    // PlayerMovement.CancelInvoke(nameof(StopSurf));
                }
            }

            //Invoke ground/wall cancel, since we can't check normals with CollisionExit
            float delay = 3f;
            if (!_cancellingGrounded) {
                _cancellingGrounded = true;
                _groundCoroutine = PlayerMovement.StartCoroutine(StopGrounded(Time.deltaTime * delay));
            }
            if (!_cancellingWall)
            {
                _cancellingWall = true;
                _wallCoroutine = PlayerMovement.StartCoroutine(StopWall(Time.deltaTime * delay));
            }
            if (!_cancellingSurf) {
                _cancellingSurf = true;
                _surfCoroutine = PlayerMovement.StartCoroutine(StopSurf(Time.deltaTime * delay));
            }
        }

        public void DeInitialize()
        {
            StopCrouch(new InputAction.CallbackContext());
            StopJump(new InputAction.CallbackContext());
        }

        private void StartWallRun(Vector3 normal)
        {
            if (normal == _wallNormalVector) return;
            if (!grounded && ReadyToWallRun)
            {
                _wallNormalVector = normal;
                float num = 15f;
                if (!_wallRunning)
                {
                    var velocity = Rb.velocity;
                    float force = velocity.magnitude;
                    if (force > 3)
                    {
                        force = 1f;
                    }

                    if (force < 2)
                    {
                        force = 1.165f;
                    }

                    float offset = 0;
                    if (Rb.velocity.z < 0.1f)
                    {
                        offset = 1.4f;
                    }

                    velocity = new Vector3(velocity.x, 0f, velocity.z*force+offset);
                    Rb.velocity = velocity;
                    Rb.AddForce(Vector3.up * num, ForceMode.Impulse);
                    Rb.AddForce(-normal * 5, ForceMode.Impulse);
                    _wallRunning = true;
                }
            }
        }
        private bool IsFloor(Vector3 v) {
            float angle = Vector3.Angle(Vector3.up, v);
            return angle < maxSlopeAngle;
        }
        private IEnumerator StopGrounded(float time)
        {
            yield return new WaitForSeconds(time);
            grounded = false;
            PlayerMovement.currentPlayer.Grounded = grounded;
        }
        private bool IsWall(Vector3 v)
        {
            return Math.Abs(90f - Vector3.Angle(Vector3.up, v)) < 1f;
        }

        private IEnumerator StopWall(float time)
        {
            yield return new WaitForSeconds(time);
            onWall = false;
            _wallRunning = false;
        }
        
        private bool IsSurf(Vector3 v)
        {
            float num = Vector3.Angle(Vector3.up, v);
            if (num < 89f)
            {
                return num > maxSlopeAngle;
            }
            return false;
        }
        
        private IEnumerator StopSurf(float time)
        {
            yield return new WaitForSeconds(time);
            _surfing = false;
        }

        #endregion

        #region Input

        void StartJump(InputAction.CallbackContext obj)
        {
            if (GameUIManager.Instance &&GameUIManager.Instance.pause || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || NetworkManager.Instance.CantPlay()) return;
            jumping = true;
            // if (GameUIManager.Instance && GameUIManager.Instance.doubleJumpIndex < 1) return;
            if (((!grounded && JumpLeft > 0)  || _wallRunning) && _readyToJump) {
                _readyToJump = false;
                JumpLeft--;
                //Add jump forces
                Rb.AddForce( jumpForce * 1.5f*JumpFactor*Vector3.up);
                Rb.AddForce( jumpForce * 0.5f*JumpFactor*Vector3.up);
            
                //If jumping while falling, reset y velocity.
                Vector3 vel = Rb.velocity;
                if (Rb.velocity.y < 0.5f)
                    Rb.velocity = new Vector3(vel.x, 0, vel.z);
                else if (Rb.velocity.y > 0) 
                    Rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
                if (_wallRunning)
                {
                    _wallRunning = false;
                    Rb.AddForce(jumpForce * 1.5f*_normalVector);
                }
            
                PlayerMovement.StartCoroutine(ResetJump(_jumpCooldown));
                AudioManager.Instance.PlayJump();
                if (GameManager.Instance.setting.spawnParticle)
                {
                    ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = 
                        Object.Instantiate(PlayerSmokeFx, PlayerTransform.position, Quaternion.LookRotation(Vector3.up))
                            .GetComponent<ParticleSystem>().velocityOverLifetime;
                    var velocity = Rb.velocity;
                    velocityOverLifetime.x = velocity.x * 2f;
                    velocityOverLifetime.z = velocity.z * 2f;
                }
            }
        }
        private IEnumerator ResetJump(float time)
        {
            yield return new WaitForSeconds(time);
            _readyToJump = true;
        }
        public void StopJump(InputAction.CallbackContext obj)
        {
            jumping = false;
        }
        private void Jump() {
            if (grounded && _readyToJump) {
                _readyToJump = false;
                JumpLeft--;
            
                //Add jump forces
                Rb.AddForce(jumpForce * 1.5f*JumpFactor*Vector3.up);
                Rb.AddForce( jumpForce * 0.5f*JumpFactor*_normalVector);
            
                //If jumping while falling, reset y velocity.
                Vector3 vel = Rb.velocity;
                if (vel.y < 0.5f)
                    Rb.velocity = new Vector3(vel.x, 0, vel.z);
                else if (vel.y > 0) 
                    Rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

                PlayerMovement.StartCoroutine(ResetJump(_jumpCooldown));
                AudioManager.Instance.PlayJump();
                if (GameManager.Instance.setting.spawnParticle)
                {
                    ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = UnityEngine.Object
                        .Instantiate(PlayerSmokeFx, PlayerTransform.position, Quaternion.LookRotation(Vector3.up))
                        .GetComponent<ParticleSystem>().velocityOverLifetime;
                    velocityOverLifetime.x = vel.x * 2f;
                    velocityOverLifetime.z = vel.z * 2f;
                }
            }
        }
        private void StartCrouch(InputAction.CallbackContext obj)
        {
            if (GameUIManager.Instance &&GameUIManager.Instance.pause || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || NetworkManager.Instance.CantPlay()) return;
            if (!_canCrouch) return;
            _canCrouch = false;
            _needToStand = false;
            crouching = true;
            if(PlayerMovement.player)PlayerMovement.player.SendStartCrouch();
            PlayerTransform.localScale = _crouchScale;
            var position = PlayerTransform.position;
            PlayerTransform.position = new Vector3(position.x, position.y - 0.65f, position.z);
            if (Rb.velocity.magnitude > 0.5f) {
                if (grounded) {
                    Rb.AddForce(Orientation.forward * slideForce);
                    AudioManager.Instance.PlayStartSlide();
                }
            }

            PlayerMovement.StartCoroutine(ResetCanCrouch(.12f));
        
            if(PlayerMovement.DemoPlayer)
                PlayerMovement.DemoPlayer.NewCrouch(crouching);

            PlayerMovement.currentPlayer.IsCrouching = crouching;
        }

        IEnumerator ResetCanCrouch(float time)
        {
            yield return new WaitForSeconds(time);
            _canCrouch = true;
        }

        private bool _needToStand = false;
        public void StopCrouch(InputAction.CallbackContext obj)
        {
            if (GameUIManager.Instance &&GameUIManager.Instance.pause || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || NetworkManager.Instance.CantPlay()) return;
            if (!crouching) return;
            _needToStand = true;
            if (Physics.Raycast(PlayerTransform.position, Vector3.up, 2f, 1 << 3))
            {
                return;
            }

            _needToStand = false;
            crouching = false;
            if(PlayerMovement.player)PlayerMovement.player.SendStopCrouch();
            PlayerTransform.localScale = _playerScale;
            var position = PlayerTransform.position;
            PlayerTransform.position = new Vector3(position.x, position.y +  0.45f, position.z);
            if(PlayerMovement.DemoPlayer)
                PlayerMovement.DemoPlayer.NewCrouch(crouching);
            PlayerMovement.currentPlayer.IsCrouching = crouching;
        }
        #region Dash

        void Dash(InputAction.CallbackContext ctx)
        {
            if (TabHolder.Instance != null) return;
            if (GameUIManager.Instance.pause || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || NetworkManager.Instance.CantPlay() || !canDash || UpgradeInGameMenu.Instance.dashIndex<1) return;
            canDash = false;
            if (UpgradeInGameMenu.Instance) UpgradeInGameMenu.Instance.dashTimer = 0;
            AudioManager.Instance.Play("Dash");
            PlayerMovement.StartCoroutine(Dash());
            PlayerMovement.StartCoroutine(ReturnDash(2.5f));
        }
        
        IEnumerator Dash()
        {
            Rb.AddForce(PlayerCam.forward*dashForce,ForceMode.VelocityChange);

            yield return new WaitForSeconds(dashDuration);

            Rb.velocity *= 0.5f;
        }
        
        IEnumerator ReturnDash(float time)
        {
            yield return new WaitForSeconds(time);
            canDash = true;
        }

        #endregion

        


        #endregion
    }
}