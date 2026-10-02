
using System;
using Audio;
using Manager;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Server;
using Pool;
using TMPro;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Demo.Entity
{
    public class DemoPlayer : DemoEntity
    {
        // public PlayerWeaponManager PlayerWeapon;
        
        private PlayerAnimation _playerAnimation;

        public PlayerAnimation PlayerAnimation => _playerAnimation;

        [SerializeField] public PlayerState playerState;

        [SerializeField] private Animator animator;

        [SerializeField] private LayerMask whatIsGround;

        [SerializeField] private AudioSource audioSource;

        [SerializeField] private TextMeshProUGUI nameText;

        [SerializeField] private Transform head, spine;
        bool Grounded { get; set; }
        public bool Crouch { get; set; }
        
        public bool Dead { get; private set; }

        private InventoryManager.CosmeticIndex _cosmeticIndex;

        private Vector3 _groundCheck;
        private float _playerHeight;

        #region Interpolation

        private float _movementThreshold = 0.2f;

        private Vector3 _to;

        private Quaternion _desiredRot, _desiredHeadRot;

        private float _walkDistance;

        #endregion

        private void Start()
        {
            
            _playerAnimation = new PlayerAnimation(animator, spine);
        }

        private void Update()
        {
            if (DemoManager.Replaying)
            {
                float lerp = Time.deltaTime / (0.02f / Time.timeScale);
                
                // lerp = Mathf.Max(0.02f, lerp);
                
                InterpolatePosition(lerp);
                InterpolateRotation(lerp);

                _playerHeight = Crouch ? ServerPlayer.CrouchSize : ServerPlayer.NormalSize;

                _groundCheck = selfTrans.position + new Vector3(0, -_playerHeight / 2f, 0);
                Grounded = Physics.CheckSphere(_groundCheck, .5f, whatIsGround);
                
                playerState.Grounded = Grounded;
                
                _playerAnimation.SetGround(Grounded);

                _playerAnimation.Update();
            }
        }

        private void LateUpdate()
        {
            if (DemoManager.Replaying)
            {
                _playerAnimation.LateUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (DemoManager.Replaying)
            {
                FootSteps();
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;

                Gizmos.DrawWireSphere(_groundCheck, 0.5f);
            }
        }

        public override void Spawn(int id, params object[] obj)
        {
            base.Spawn(id, obj);

            string username = (string)obj[0];
            ulong steamId = (ulong)obj[1];

            int currentWeaponIndex = (int)obj[2];
            short[] weaponIndexes = (short[])obj[3];
            InventoryManager.CosmeticIndex cosmeticIndex = (InventoryManager.CosmeticIndex)obj[4];
            bool isInfected = (bool)obj[5];
            ushort[] weaponSkinIndex = cosmeticIndex.weaponIndex;

            _cosmeticIndex = cosmeticIndex;

            if (DemoManager.Replaying)
            {
                rb.useGravity = false;
                rb.isKinematic = true;

                Vector3 position = GetTransform().position;

                _to = position;

                _desiredRot = Quaternion.identity;

                nameText.SetText(username);

                playerState.SetValues(false, steamId, username, Team.Rebel, true);
                
                playerState.SetPlayerCosmetics(cosmeticIndex);
                
                playerState.InitializeWeaponManager( audioSource);
                
                // PlayerWeapon = new PlayerWeaponManager(false, weapons, leftHandTarget, rightHandTarget, iks, playerState, clawKnifeAnim, audioSource, playerState.weaponHolder, head);
                playerState.WeaponManager.UpdateWeapons(weaponIndexes, currentWeaponIndex, weaponSkinIndex);

                playerState.IsInfected = isInfected;
                playerState.SetInfect();
            }
        }

        private void FootSteps()
        {
            if (playerState.Health > 0 && !Crouch && Grounded)
            {
                float num = 1.2f;
                float num2 = rb.velocity.magnitude;
                if (num2 > 20f)
                {
                    num2 = 20f;
                }

                _walkDistance += num2;
                if (_walkDistance > 240f / num)
                {
                    int range = Random.Range(0, PrefabManager.Instance.walksSound.Length - 1);
                    audioSource.PlayOneShot(PrefabManager.Instance.walksSound[range]);
                    if (GameManager.Instance.setting.spawnParticle)
                    {
                        ObjectPooler.Instance.SpawnFromPool("PlayerWalkSmokeFx", _groundCheck,
                            Quaternion.Euler(-90, 0, 0));
                    }

                    _walkDistance = 0f;
                }
            }
        }

        private void InterpolatePosition(float lerpAmount)
        {
            Vector3 from = GetTransform().position;

            if (_to != from)
            {
                selfTrans.position = Vector3.Lerp(from, _to, lerpAmount);
            }
        }

        private void InterpolateRotation(float lerpAmount)
        {
            selfTrans.rotation = Quaternion.Lerp(selfTrans.rotation, _desiredRot, lerpAmount);
            head.localRotation = Quaternion.Slerp(head.localRotation, _desiredHeadRot, lerpAmount);
        }

        public void NewPosition(Vector3 position)
        {
            _to = position;

            Vector3 from = GetTransform().position;

            Vector3 dir = _to - from;
            _playerAnimation.SetInput(dir.magnitude < 0.01f
                ? Vector3.zero
                : orientation.InverseTransformDirection(dir) * 4f);
        }

        public void NewRotation(float rot, float headRot)
        {
            float deltaY = GetHeadRotation() - headRot;
            float deltaX = AngleUtils.WrapAngle(rot) - AngleUtils.WrapAngle(selfTrans.localEulerAngles.y);

            float deltaThreshold = 3.5f * Time.timeScale;
            
            deltaY = Mathf.Clamp(deltaY * 0.1f,-deltaThreshold, deltaThreshold);
            deltaX = Mathf.Clamp(deltaX * 0.1f,-deltaThreshold, deltaThreshold);

            _desiredRot = Quaternion.Euler(0, rot, 0);

            _desiredHeadRot = Quaternion.Euler(headRot, 0, 0);
            
            playerState.SetDelta(deltaX,deltaY);

            _playerAnimation.SetXRotation(headRot);
        }

        public void NewVelocity(Vector3 velocity)
        {
            // GetRb().velocity = velocity;
            
            playerState.SetVelocity(velocity);
        }

        /// <summary>
        /// Use for replaying
        /// </summary>
        /// <param name="crouch"></param>
        public void SetCrouch(bool crouch)
        {
            Crouch = crouch;

            playerState.IsCrouching = crouch;

            if (crouch && Grounded)
            {
                AudioManager.Instance.SoundEffect3D("start_slide", selfTrans.position, 0.8f);
            }
            
            _playerAnimation.SetCrouch(crouch);
        }

        /// <summary>
        /// Use for recording
        /// </summary>
        /// <param name="crouch"></param>
        public void NewCrouch(bool crouch)
        {
            Crouch = crouch;
            
            DemoManager.Instance.AddPlayerCrouch(Id,crouch);
        }

        public override bool IsPlayer()
        {
            return true;
        }
        
        public float GetHeadRotation()
        {
            return AngleUtils.WrapAngle(head.localEulerAngles.x);
        }

        public void Respawn()
        {
            Dead = false;
            selfTrans.gameObject.SetActive(true);
            playerState.MaxHealth = 100;
            playerState.Health = playerState.MaxHealth;
            playerState.Respawn();
            playerState.SetInfect();
            playerState.WeaponManager.UpdateWeapons(playerState.WeaponManager.WeaponIndexes,playerState.WeaponManager.CurrentWeaponIndex,playerState.WeaponManager.WeaponSkinIndexes);
            _playerAnimation.Reset();
        }

        public void SetDead(DemoPlayerData playerData, DemoPlayerData.StateData stateData)
        {
            Dead = true;
            playerState.Health = 0;
            selfTrans.gameObject.SetActive(false);
            // playerState.SetHealth(0, playerState.Health, false, selfTrans.position, Vector3.up, true, false);
            bool attackerIsLocal = playerData.id != stateData.attackerId;
            if (DemoManager.Instance.TryGetEntity(stateData.attackerId,out var entity) && entity.IsPlayer())
            {
                attackerIsLocal &= ((DemoPlayer)entity).playerState.IsLocal;
            }
            else
            {
                attackerIsLocal = false;
            }
            playerState.Dead(stateData.headShot,stateData.wallbang,stateData.weaponIndex,attackerIsLocal);
            playerState.SpawnRagdoll(false);
        }
    }
}