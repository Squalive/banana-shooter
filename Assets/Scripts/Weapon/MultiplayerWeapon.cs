using System;
using System.Collections.Generic;
using Multiplayer.Entity.Client;
using UnityEngine;
using Weapon.WeaponStats;

namespace Weapon
{
    public class MultiplayerWeapon : MonoBehaviour
    {
        public WeaponStat stat;
        
        [Header("Knife")]
        public Firearms.WeaponType Type;

        public Animator animator;
    
        [Header("Gun")]
        public string name;
        public Transform leftHand, rightHand;
        public Transform tip;
        public float spreadAngle;
        public ParticleSystem particleSystem;
        public AudioClip shoot,reload,reset;
        public bool isLocal,dual=false;
    
        public Transform lTip, rTip;
        public ParticleSystem lPart, rPart;

        public bool leftSide = false;

        public bool useGravity=false;
        private void Start()
        {
            startPos = base.transform.localPosition;
        }

        /// <summary>
        /// Owner of this weapon model. Needed to drive the weapon bob for remote /
        /// demo players (the local first person weapons get it from Firearms instead).
        /// </summary>
        public void SetPlayerState(PlayerState playerState)
        {
            _playerState = playerState;
        }

        #region Dynamic
        void Update()
        {
            if (isLocal) return;   
            ReloadGun();
            RecoilGun();
            MovementBob();
            SpeedBob();

            Rotation();
            Vector3 b3=startPos+ desiredBob + speedBob + recoilOffset + new Vector3(0f, 0f - reloadPosOffset, 0f);
        
            transform.localPosition = Vector3.Lerp(transform.localPosition, b3, Time.deltaTime * 15f);
        }

        public float reloadTime;
        private void Rotation()
        {
            // float num = offset.magnitude * 0.03f;
            // if (offset.x < 0f)
            // {
            //     num = 0f - num;
            // }
            // float y = offset.y;
            Vector3 euler = new Vector3(reloadRotation, 0, 0) + recoilRotation;
            try
            {
                if (!(Time.deltaTime <= 0f))
                {
                    transform.localRotation = Quaternion.Lerp(base.transform.localRotation, Quaternion.Euler(euler), Time.deltaTime * 20f);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void ShootAnim()
        {
            float num = 1.5f;
            recoilOffset += -(Vector3.forward + Vector3.up * shootVertical- Vector3.right *shootHorizontal);
            recoilRotation += -new Vector3(shootRotX, UnityEngine.Random.Range(10f, 30f)*shootRotYMultiplier, UnityEngine.Random.Range(-50f, 50f)*shootRotZMultiplier) * num;
        }

        [SerializeField] private float shootVertical = 0.3f, shootHorizontal = 0.35f;
        [SerializeField] private float shootRotX = 90f,shootRotZMultiplier=1f,shootRotYMultiplier=1f;
        private void RecoilGun()
        {
            recoilOffset = Vector3.SmoothDamp(recoilOffset, Vector3.zero, ref recoilOffsetVel, 0.05f);
            recoilRotation = Vector3.SmoothDamp(recoilRotation, Vector3.zero, ref recoilRotVel, 0.07f);
        }

        private void ReloadGun()
        {
            reloadProgress += Time.deltaTime;
            reloadRotation = Mathf.Lerp(0f, desiredReloadRotation, reloadProgress / reloadTimet);
            reloadPosOffset = Mathf.SmoothDamp(reloadPosOffset, 0f, ref rPVel, reloadTimet * 0.2f);
            if (reloadRotation / 360f > (float)spins)
            {
                spins++;
            }
        }

        public void Reload(float time, int spinAmount)
        {
            reloadProgress = 0f;
            reloadRotation = 0f;
            reloadTimet = time;
            spins = 0;
            int num = spinAmount;
            if (num < 1)
            {
                num = Mathf.RoundToInt(time * 3f);
            }
            desiredReloadRotation = -360 * num;
            reloadPosOffset = 0.45f;
        }
    

        private Vector3 startPos;

        private PlayerState _playerState;

        private Vector3 desiredBob;

        private float xBob = 0.12f;

        private float yBob = 0.08f;

        private float zBob = 0.1f;

        private float bobSpeed = 0.45f;

        private void MovementBob()
        {
            if (_playerState == null
                || Mathf.Abs(_playerState.GetVelocity().magnitude) < 4f
                || !_playerState.Grounded
                || _playerState.IsCrouching)
            {
                desiredBob = Vector3.zero;
                return;
            }

            float x = Mathf.PingPong(Time.time * bobSpeed, xBob) - xBob / 2f;
            float y = Mathf.PingPong(Time.time * bobSpeed, yBob) - yBob / 2f;
            float z = Mathf.PingPong(Time.time * bobSpeed, zBob) - zBob / 2f;
            desiredBob = new Vector3(x, y, z);
        }

        private void SpeedBob()
        {
            if (_playerState == null)
            {
                speedBob = Vector3.Lerp(speedBob, Vector3.zero, Time.deltaTime * 10f);
                return;
            }

            Vector2 vector = _playerState.FindVelRelativeToLook() * gunDrag;
            Vector3 vector2 = new Vector3(vector.x, _playerState.GetVelocity().y, vector.y);
            vector2 *= -0.01f;
            vector2 = Vector3.ClampMagnitude(vector2, 0.1f);
            speedBob = Vector3.Lerp(speedBob, vector2, Time.deltaTime * 10f);
        }

        private Vector3 recoilOffset;

        private Vector3 recoilRotation;

        private Vector3 recoilOffsetVel;

        private Vector3 recoilRotVel;

        private float reloadRotation;

        private float desiredReloadRotation;

        private float reloadTimet;

        private float rVel;

        private float reloadPosOffset;

        private float rPVel;

        private float gunDrag = 0.2f;

        private Vector3 speedBob;

        private float reloadProgress;

        private float rotationOffset;

        private Vector3 prevRotation;

        private int spins;

        #endregion

        public List<GameObject> skins = new List<GameObject>();
    }
}
