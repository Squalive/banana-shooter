using Menu;
using Movement;
using Multiplayer.Entity.Client;
using TMPro;
using UnityEngine;
using Weapon;

namespace PlayerCameraController
{
    public class MoveCamera : MonoBehaviour
    {
        public static MoveCamera Instance { get; private set; }

        [SerializeField] private PlayerState playerState;
        [SerializeField] Transform player;
        [SerializeField] public Transform camTransform;
        public Camera cam;

        #region Bob

        private Vector3 _bobOffset;
        private Vector3 _desiredBob;
        private readonly float _bobSpeed = 15f;
        private readonly float _bobMultiplier = 1f;

        #endregion
        private float _desiredTilt;
        public Vector3 vaultOffset;

        private float _tilt;

        private Transform _transform;

        private Vector3 _rot;
        private void Awake()
        {
            Instance = this;
            _transform = transform;
        }

        private void Start()
        {
            gameObject.SetActive(false);
        }


        private void LateUpdate()
        {
            UpdateBob();
            _transform.position = player.position + _bobOffset + vaultOffset;

            if (PlayerMovement.Instance && PlayerMovement.Instance.IsWallRunning())
            {
                _desiredTilt = PlayerMovement.Instance.GetWallRunRotation();
            }
            else
            {
                if (playerState.IsCrouching && playerState.GetVelocity().magnitude>8f)
                {
                    _desiredTilt = 6f;
                }
                else
                {
                    _desiredTilt = 0f;
                }
            }

            _tilt = Mathf.Lerp(_tilt, _desiredTilt, Time.deltaTime * 8f);
            
            _rot = player.rotation.eulerAngles;
            
            _rot.z = _tilt;
            _transform.rotation = Quaternion.Euler(_rot);
       
            vaultOffset = Vector3.Slerp(vaultOffset, Vector3.zero, Time.deltaTime * 7f);
        }
        public void BobOnce(Vector3 bobDirection)
        {
            Vector3 vector = ClampVector(bobDirection * 0.15f, -3f, 3f);
            _desiredBob = vector * _bobMultiplier;
        }
        private void UpdateBob()
        {
            _desiredBob = Vector3.Lerp(_desiredBob, Vector3.zero, Time.deltaTime * _bobSpeed * 0.5f);
            _bobOffset = Vector3.Lerp(_bobOffset, _desiredBob, Time.deltaTime * _bobSpeed);
        }
    
        private Vector3 ClampVector(Vector3 vec, float min, float max)
        {
            return new Vector3(Mathf.Clamp(vec.x, min, max), Mathf.Clamp(vec.y, min, max), Mathf.Clamp(vec.z, min, max));
        }

        public void DeInitialize()
        {
            if (playerState && playerState.model)
            {
                playerState.model.SetActive(true);

                playerState.SetLocal(false);
                
                playerState.WeaponManager.SetWeaponObjects(playerState.weapons);
                
                playerState.WeaponManager.UpdateWeapons(playerState.WeaponManager.WeaponIndexes, playerState.WeaponManager.CurrentWeaponIndex, playerState.WeaponManager.WeaponSkinIndexes);

                playerState = null;
            }

            // Leaving first person spectate: the previously watched player may have
            // been scoped in, so make sure the scope is closed for the free camera.
            if (WeaponManager.Instance)
                WeaponManager.Instance.ResetAim();
        }

        public void SetPlayer(PlayerState ps)
        {
            if (playerState && playerState.model)
            {
                playerState.model.SetActive(true);

                playerState.SetLocal(false);
                
                playerState.WeaponManager.SetWeaponObjects(playerState.weapons);
                
                playerState.WeaponManager.UpdateWeapons(playerState.WeaponManager.WeaponIndexes, playerState.WeaponManager.CurrentWeaponIndex, playerState.WeaponManager.WeaponSkinIndexes);
                
                playerState.SetInfect();
            }
            playerState = ps;

            player = ps.head;

            _rot = player.rotation.eulerAngles;

            DeadCamera.GetInstance().Respawn();
            
            if (ps && ps.model)
            {
                WeaponManager.Instance.WeaponSelectAnimation = false;
                
                ps.SetLocal(true);
                ps.model.SetActive(false);
                
                ps.WeaponManager.SetWeaponObjects(WeaponManager.Instance.weapons);
                
                ps.WeaponManager.UpdateWeapons(ps.WeaponManager.WeaponIndexes, ps.WeaponManager.CurrentWeaponIndex, ps.WeaponManager.WeaponSkinIndexes);
                
                ps.SetInfect();
                GameUIManager.Instance.healthSlider.maxValue = ps.MaxHealth;

                // Switching the spectated player: mirror whether the new target is
                // currently scoped, so the scope follows the watched player instead
                // of staying on from the previous one.
                if (!ps.selfControlled)
                    WeaponManager.Instance.SyncAim(ps.Aiming);
            }
        }
    }
}
