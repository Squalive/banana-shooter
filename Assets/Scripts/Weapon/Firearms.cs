using System;
using System.Collections.Generic;
using Audio;
using EZCameraShake;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Client.Enemy;
using PlayerCameraController;
using Pool;
using Riptide;
using Safe;
using UnityEngine;
using UnityEngine.InputSystem;
using Weapon.WeaponStats;
using Random = UnityEngine.Random;

namespace Weapon
{
    public abstract class Firearms : MonoBehaviour, IWeapon
    {
        public enum WeaponType
        {
            Gun,
            Knife,
            LaserGun,
            Boomer,
        }

        [SerializeField] protected WeaponStat stat;

        [Header("Arm")] public bool useArm = false;
        public Transform arm;
        private Transform weapon;
        public int weaponIndex = 0;
        public Transform leftHandIK, rightHandIK;

        [Header("WeaponType")]
        public WeaponType weaponType = WeaponType.Gun;
        [Header("Knife")]
        public float waitTimeToAttack = 0.2f;

        [Header("Gun")]
        public Transform muzzlePoint;
        public ParticleSystem muzzleParticle;

        [SerializeField] public AudioClip shoot, reload, reset;

        public bool useGravity = false;

        public int maxAmmo = 30;

        public SafeInt currentAmmo;
        public float fireRate = 10;
        public Animator animator;

        public bool cantReload = false;

        private float lastFireTime;
        public bool allowHolding;

        [HideInInspector]
        public float reloadMultiplier = 1f;

        private void Awake()
        {
            weapon = arm;
            currentAmmo = new SafeInt(maxAmmo);
        }

        protected virtual void Start()
        {
            _multiplayerWeapon = GetComponent<MultiplayerWeapon>();
        }

        private void OnEnable()
        {
            currentGunDragMultiplier = GameManager.Instance.setting.swayMultiplier;

            ResetReloadMultiplier();
        }

        void ResetReloadMultiplier()
        {
            reloadMultiplier = PerkManager.Instance.HasPerk(Perk.QuickHand) ? 0.6f : 1f;
        }

        public bool isAiming;
        protected virtual void Update()
        {
            // if (shooting)
            // {
            //     hotValuef += Time.deltaTime*hotValueFactor;
            //     hotValuef = Mathf.Clamp(hotValuef, 0, recoil.Count-1);
            //     hotValue = (int)hotValuef;
            //     if (hotValue >= recoil.Count-1) hotValue = recoil.Count-1;
            // }
            // else
            // {
            //     hotValuef -= Time.deltaTime*hotValueFactor;
            //     hotValuef = Mathf.Clamp(hotValuef, 0, recoil.Count-1);
            //     hotValue = (int)hotValuef;
            //     if (hotValue <= 0) hotValue = 0;
            // }
            MovementBob();
            RecoilGun();
            SpeedBob();
            ReloadGun();

            Vector3 defaultPos = isAiming ? aimPos : startPos;

            bool cursorVisible = Cursor.visible;

            float mx = cursorVisible ? 0 : Input.GetAxis("Mouse X");
            float my = cursorVisible ? 0 : Input.GetAxis("Mouse Y");

            if (!WeaponManager.Instance.CurrentPlayer.selfControlled)
            {
                mx = WeaponManager.Instance.CurrentPlayer.GetDeltaX();
                my = WeaponManager.Instance.CurrentPlayer.GetDeltaY();
            }

            float b = -mx * gunDrag * currentGunDragMultiplier * drag;
            float b2 = -my * gunDrag * currentGunDragMultiplier * drag;

            desX = Mathf.Lerp(desX, b, Time.deltaTime * 10f);
            desY = Mathf.Lerp(desY, b2, Time.deltaTime * 10f);
            Rotation(new Vector2(desX, desY));
            Vector3 b3 = defaultPos + desiredBob + recoilOffset + speedBob + new Vector3(0f, 0f - reloadPosOffset, 0f) + new Vector3(desX, desY, 0f);

            weapon.localPosition = Vector3.Lerp(weapon.localPosition, b3, Time.deltaTime * 15f);
        }

        private float drag = 0.3f;
        public int bulletCount;
        public bool createBullet = true;
        List<ClientPlayer> hitPlayer = new List<ClientPlayer>();
        private RaycastHit[] hits = new RaycastHit[20];
        void KnifeAttack()
        {
            int cnt = Physics.RaycastNonAlloc(PlayerCam.transform.position,
                PlayerCam.transform.forward, hits, 3f, PrefabManager.Instance.whatIsHittable, QueryTriggerInteraction.Ignore);
            hitPlayer.Clear();
            clientEnemies.Clear();
            for (int i = 0; i < cnt; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ClientPlayer"))
                {
                    ClientPlayer clientPlayer = hit.transform.root.GetComponent<ClientPlayer>();
                    if (hitPlayer.Contains(clientPlayer)) continue;
                    hitPlayer.Add(clientPlayer);
                }

                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Client") && hit.transform.root.CompareTag("Enemy"))
                {
                    ClientEnemy enemy = hit.transform.root.GetComponent<ClientEnemy>();
                    if (clientEnemies.Contains(enemy)) continue;
                    clientEnemies.Add(enemy);
                }
                Hit(hit, false);
            }

            if (cnt > 0)
            {
                // Message message = Message.Create(MessageSendMode.Unreliable,(ushort)ClientToServerId.Shoot);
                //
                // message.Add(bulletCount);
                // message.Add(hits[0].point);
                // NetworkManager.Instance.SendByte += message.WrittenLength;
                //
                // NetworkManager.Instance.Client.Send(message);
                return;
            }
            cnt = Physics.SphereCastNonAlloc(PlayerCam.transform.position, 1,
                PlayerCam.transform.forward, hits, 3f, PrefabManager.Instance.whatIsHittable, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < cnt; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ClientPlayer"))
                {
                    ClientPlayer clientPlayer = hit.transform.root.GetComponent<ClientPlayer>();
                    if (hitPlayer.Contains(clientPlayer)) continue;
                    hitPlayer.Add(clientPlayer);
                }

                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Client") && hit.transform.root.CompareTag("Enemy"))
                {
                    ClientEnemy enemy = hit.transform.root.GetComponent<ClientEnemy>();
                    if (clientEnemies.Contains(enemy)) continue;
                    clientEnemies.Add(enemy);
                }
                Hit(hit, false);
            }
        }

        void Hit(RaycastHit hit, bool wall)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                if (GameManager.Instance.setting.spawnParticle)
                {
                    ObjectPooler.Instance.SpawnFromPool("BulletHit", hit.point, Quaternion.LookRotation(hit.normal));
                }
                int rand = Random.Range(0, PrefabManager.Instance.broadSwordHit.Length);
                AudioManager.Instance.PlayGunReload(PrefabManager.Instance.broadSwordHit[rand]);
            }
            else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ShootingTarget"))
            {
                var root = hit.transform.root;
                ShootingTarget target = root.GetComponent<ShootingTarget>();
                ShootingTarget2 target2 = root.GetComponent<ShootingTarget2>();
                if (target != null)
                    target.Hit();
                else if (target2 != null)
                    target2.SetTargetPos();
                if (GameManager.Instance.setting.spawnParticle)
                {
                    ObjectPooler.Instance.SpawnFromPool("BulletHit", hit.point, Quaternion.LookRotation(hit.normal));
                }
                HitMarker.Instance.StartHitMarker(Color.white);
                HitMarker3D h = ObjectPooler.Instance.SpawnFromPool("HitMarker", hit.point, Quaternion.LookRotation(MoveCamera.Instance.transform.position - hit.point)).GetComponent<HitMarker3D>();
                h.text.SetText(damage.ToString());
            }
            else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Client") && hit.transform.root.CompareTag("Enemy"))
            {
                ClientEnemy clientEnemy = hit.transform.root.GetComponent<ClientEnemy>();

                NetworkManager.Instance.HitEnemy(clientEnemy.Id, hit.normal);

            }
            else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ClientPlayer"))
            {
                bool isHead = hit.collider.gameObject.CompareTag("Head");
                ClientPlayer player = hit.transform.root.GetComponent<ClientPlayer>();
                if (GameManager.Instance.setting.enableGore && GameManager.Instance.setting.spawnParticle)
                {
                    ObjectPooler.Instance.SpawnFromPool("Blood", hit.point, Quaternion.LookRotation(hit.normal));
                }
                if (NetworkManager.Instance.IsTeamMode(player.playerState) || (NetworkManager.ClientGameMode == GameMode.Infected && player.playerState.IsInfected == InfectedHand.Instance.isInfected)) return;
                if (isHead)
                {
                    AudioManager.Instance.Play("headshotrapid");
                }

                HitMarker.Instance.StartHitMarker(isHead ? Color.yellow : Color.white);
            }
        }

        private MultiplayerWeapon _multiplayerWeapon;
        [SerializeField] private float recoilX = -.1f, recoilY = .1f, recoilZ = .05f;
        private float force = 2f;
        public bool semiAuto = false;
        private Transform camera => MoveCamera.Instance.camTransform;
        RaycastHit[] _hit = new RaycastHit[20];
        public bool enableSmokeTrail = false;
        public void DoAttack()
        {
            bool success = false;
            if (currentAmmo.GetValue() <= 0 && !isReload)
            {
                DoReload(0);
                return;
            }
            if (!IsAllowShooting() || isReload || WeaponManager.Instance.CurrentPlayer.Health <= 0 || NetworkManager.Instance.CheckMultiplayerGameModeStarted())
            {
                return;
            }

            Vector3 hitPoint;
            Vector3 raycastPos = camera.position;
            lastFireTime = Time.time;

            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.Shoot);

            uint predictTick = NetworkManager.Instance.InterpolationTick;
            message.Add(predictTick);
            message.Add(camera.forward);
            message.Add(raycastPos);

            NetworkManager.Instance.SendByte += message.WrittenLength;

            NetworkManager.Instance.Client.Send(message);

            float dis;
            RaycastHit hit;
            switch (weaponType)
            {
                case WeaponType.Knife:
                    Shoot();

                    KnifeShoot(false);
                    break;
                case WeaponType.Gun:

                    if (isAiming && (!semiAuto || currentAmmo.GetValue() <= 0)) WeaponManager.Instance.StopAim(new InputAction.CallbackContext());
                    Shoot();
                    Recoil.Instance.RecoilFir(recoilX, recoilY, recoilZ);

                    success = GunShoot(false);

                    lastFireTime = Time.time;

                    CancelInvoke(nameof(CheckAmmo));
                    Invoke(nameof(CheckAmmo), 0.3f);

                    break;
                case WeaponType.LaserGun:
                    Shoot();
                    StopSound();
                    hitPoint = Vector3.zero;
                    // + new Vector3(Random.Range(-recoil[hotValue].x, recoil[hotValue].x),
                    //     Random.Range(recoil[hotValue].y - 0.3f, recoil[hotValue].y + 0.3f), 0f)
                    if (Physics.Raycast(raycastPos, camera.forward, out hit, 5, PrefabManager.Instance.whatIsHittable, QueryTriggerInteraction.Ignore)) hitPoint = hit.point;
                    ShootAnim();
                    if (GameManager.Instance.setting.spawnParticle) muzzleParticle.Play();
                    if (shoot != null)
                        AudioManager.Instance.PlayGunShoot(shoot);
                    if (hitPoint == Vector3.zero) break;
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Client") && hit.transform.root.CompareTag("Enemy"))
                    {
                        ClientEnemy clientEnemy = hit.transform.root.GetComponent<ClientEnemy>();

                        NetworkManager.Instance.HitEnemy(clientEnemy.Id, hit.normal);

                    }
                    else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ShootingTarget"))
                    {
                        ShootingTarget target = hit.transform.root.GetComponent<ShootingTarget>();
                        target.Hit();
                        HitMarker.Instance.StartHitMarker(Color.white);
                        HitMarker3D h = ObjectPooler.Instance.SpawnFromPool("HitMarker", hit.point, Quaternion.LookRotation(MoveCamera.Instance.transform.position - hit.point)).GetComponent<HitMarker3D>();
                        h.text.SetText(damage.ToString());
                    }
                    else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ClientPlayer"))
                    {
                        bool isHead = hit.collider.gameObject.CompareTag("Head");
                        // int actualDamage = isHead ? damage * 2 : damage;
                        ClientPlayer player = hit.transform.root.GetComponent<ClientPlayer>();
                        if (NetworkManager.Instance.IsTeamMode(player.playerState)) return;
                        if (isHead)
                        {
                            AudioManager.Instance.Play("headshotrapid");
                        }
                        HitMarker.Instance.StartHitMarker(isHead ? Color.yellow : Color.white);

                    }
                    Invoke("StopSound", 0.1f);
                    break;
                case WeaponType.Boomer:
                    currentAmmo--;
                    currentAmmo.SetValue(Mathf.Clamp(currentAmmo.GetValue(), 0, maxAmmo));
                    Shoot();
                    Recoil.Instance.RecoilFir(recoilX, recoilY, recoilZ);
                    if (GameManager.Instance.setting.spawnParticle)
                    {
                        if (trail == null)
                        {
                            trail =
                                Instantiate(PrefabManager.Instance.GetPrefab("SmokeParticle"), muzzlePoint.position, Quaternion.identity)
                                    .GetComponent<SmokeTrail>();
                            trail.parent = muzzlePoint;
                            trail.weapon = this;
                            Destroy(trail.gameObject, 4f);
                        }
                    }


                    hitPoint = Vector3.zero;
                    // + new Vector3(Random.Range(-recoil[hotValue].x, recoil[hotValue].x),
                    //     Random.Range(recoil[hotValue].y - 0.3f, recoil[hotValue].y + 0.3f), 0f)
                    if (Physics.Raycast(raycastPos, PlayerCam.transform.forward, out hit, 1000,
                            PrefabManager.Instance.whatIsHittable, QueryTriggerInteraction.Ignore)) hitPoint = hit.point;
                    ShootAnim();
                    dis = hitPoint == Vector3.zero ? 1000f : hit.distance;
                    if (createBullet && dis > 8)
                        CreateExplosiveBullet(hitPoint != Vector3.zero ? hitPoint : PlayerCam.transform.position + PlayerCam.transform.forward * 100f);
                    else
                    {
                        enemies.Clear();
                        clients.Clear();
                        shootingTarget.Clear();
                        shootingTarget2.Clear();
                        clientEnemies.Clear();

                        if (GameManager.Instance.setting.spawnParticle) Instantiate(PrefabManager.Instance.GetPrefab("ExplosionParticle"), hit.point, Quaternion.LookRotation(hit.normal));

                        AudioManager.Instance.SoundEffect3D("Explosion", hit.point, 1f, 20f);
                        int cnt = Physics.OverlapSphereNonAlloc(hit.point, 18f, colliders);

                        for (int i = 0; i < cnt; i++)
                        {
                            Collider col = colliders[i];
                            Rigidbody rb = col.GetComponent<Rigidbody>();
                            Transform root = col.transform.root;
                            ClientPlayer player = root.GetComponent<ClientPlayer>();
                            ClientEnemy clientEnemy = root.GetComponent<ClientEnemy>();
                            if (col.gameObject.layer == LayerMask.NameToLayer("Client") && root.CompareTag("Enemy") && clientEnemy != null && !clientEnemies.Contains(clientEnemy))
                            {
                                clientEnemies.Add(clientEnemy);

                                NetworkManager.Instance.HitEnemy(clientEnemy.Id, hit.normal);

                            }
                            else if (col.gameObject.layer == LayerMask.NameToLayer("ClientPlayer") && !clients.Contains(player))
                            {
                                clients.Add(player);
                                bool isHead = col.gameObject.CompareTag("Head");
                                if (player == null || NetworkManager.Instance.IsTeamMode(player.playerState)) continue;
                                // if(GameManager.Instance.setting.spawnParticle)Instantiate(PrefabManager.Instance.GetPrefab("Blood"), hit.point, Quaternion.LookRotation(hit.normal));
                                HitMarker.Instance.StartHitMarker(isHead ? Color.yellow : Color.white);

                                // NetworkManager.Instance.TakeDamage(player.Id,isHead,false,(ushort)weaponIndex);
                            }
                            if (rb)
                            {
                                rb.AddExplosionForce(80f, hit.point, 25f, .5f, ForceMode.Impulse);
                            }
                        }
                    }
                    if (shoot != null)
                        AudioManager.Instance.PlayGunShoot(shoot);
                    if (GameManager.Instance.setting.cameraShake)
                        CameraShaker.Instance.ShakeOnce(2, 2, 0.1f, 0.4f);
                    lastFireTime = Time.time;

                    // message = Message.Create(MessageSendMode.Unreliable,(ushort)ClientToServerId.Shoot);
                    //
                    // message.Add(bulletCount);
                    // message.Add(hitPoint);
                    // NetworkManager.Instance.SendByte += message.WrittenLength;
                    //
                    // NetworkManager.Instance.Client.Send(message);
                    break;
            }

            WeaponManager.Instance.ShootingBuffer[predictTick % WeaponManager.MaxStoredSize] = success;
        }

        void CheckAmmo()
        {
            if (currentAmmo.GetValue() <= 0) DoReload(0);
        }

        public void KnifeShoot(bool visualOnly)
        {
            if (CompareTag("Broadsword"))
            {
                if (Random.Range(0, 2) == 0) animator.SetTrigger(Attack);
                else animator.SetTrigger("Attack1");
            }
            else
            {
                animator.SetTrigger(Attack);
            }
            if (shoot != null)
                AudioManager.Instance.PlayGunShoot(shoot);
            if (!visualOnly)
                Invoke(nameof(KnifeAttack), waitTimeToAttack);
        }

        public bool GunShoot(bool visualOnly)
        {
            currentAmmo--;
            currentAmmo.SetValue(Mathf.Clamp(currentAmmo.GetValue(), 0, maxAmmo));
            bool success = false;
            RaycastHit hit;
            Vector3 raycastPos = camera.position;
            if (GameManager.Instance.setting.spawnParticle)
            {
                if (trail == null)
                {
                    trail =
                        Instantiate(PrefabManager.Instance.GetPrefab("SmokeParticle"), muzzlePoint.position,
                            Quaternion.identity).GetComponent<SmokeTrail>();
                    trail.parent = muzzlePoint;
                    trail.weapon = this;
                    Destroy(trail.gameObject, 4f);
                }
            }
            spreadAngle = Velocity.magnitude > 15f ? runSpread : normalSpread;

            ShootAnim();
            for (int i = 0; i < bulletCount; i++)
            {
                var forward = camera.forward;
                Vector3 offset = spreadAngle / PlayerCam.fieldOfView * Random.insideUnitCircle;
                Bullet bullet = ObjectPooler.Instance.SpawnFromPool("Bullet", muzzlePoint.position,
                    Quaternion.LookRotation(forward + offset)).GetComponent<Bullet>();

                bullet.Initialization(forward + offset, 1500f, 0, LayerMask.NameToLayer("Bullet"), true, useGravity, enableSmokeTrail);

                if (!visualOnly)
                {
                    int count = Physics.RaycastNonAlloc(raycastPos, forward + offset, _hit, 1000f,
                        PrefabManager.Instance.whatIsHittable, QueryTriggerInteraction.Ignore);
                    if (count > 0)
                    {
                        int idx = 0;
                        float d = float.MaxValue;
                        for (int j = 0; j < count; j++)
                        {
                            if (_hit[j].distance < d)
                            {
                                d = _hit[j].distance;
                                idx = j;
                            }
                        }

                        hit = _hit[idx];
                        if (hit.point == Vector3.zero)
                            hit = _hit[0];

                        success = GunHit(hit);
                    }
                }
            }
            if (GameManager.Instance.setting.spawnParticle) muzzleParticle.Play();
            if (shoot != null)
                AudioManager.Instance.PlayGunShoot(shoot);
            if (GameManager.Instance.setting.cameraShake)
                CameraShaker.Instance.ShakeOnce(2, 2, 0.1f, 0.4f);

            return success;
        }

        List<Enemy> enemies = new List<Enemy>();
        List<ClientEnemy> clientEnemies = new List<ClientEnemy>();
        List<ClientPlayer> clients = new List<ClientPlayer>();
        List<ShootingTarget> shootingTarget = new List<ShootingTarget>();
        List<ShootingTarget2> shootingTarget2 = new List<ShootingTarget2>();
        private Collider[] colliders = new Collider[80];
        bool GunHit(RaycastHit hit)
        {
            if (hit.point != Vector3.zero)
            {
                var root = hit.transform.root;
                Rigidbody rb = hit.collider.gameObject.GetComponent<Rigidbody>();

                if (hit.transform.root.CompareTag("Enemy"))
                {
                    ClientEnemy clientEnemy = root.GetComponent<ClientEnemy>();
                    NetworkManager.Instance.HitEnemy(clientEnemy.Id, hit.normal);

                    return true;
                }
                else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ClientPlayer"))
                {
                    bool isHead = hit.collider.gameObject.CompareTag("Head");
                    if (isHead)
                    {
                        if (WeaponManager.Instance.CurrentWeapon != null &&
                            WeaponManager.Instance.CurrentWeapon.gameObject.CompareTag("Sniper"))
                        {
                            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.HEAD_SHOT);
                        }
                    }
                    // int actualDamage = isHead ? damage * 2 : damage;
                    ClientPlayer player = root.GetComponent<ClientPlayer>();
                    if (GameManager.Instance.setting.enableGore && GameManager.Instance.setting.spawnParticle)
                    {
                        ObjectPooler.Instance.SpawnFromPool("Blood", hit.point, Quaternion.LookRotation(hit.normal));
                    }
                    if (NetworkManager.Instance.IsTeamMode(player.playerState)) return false;
                    HitMarker.Instance.StartHitMarker(isHead ? Color.yellow : Color.white);

                    if (isHead) AudioManager.Instance.Play("headshotrapid");
                    // NetworkManager.Instance.TakeDamage(player.Id,isHead,wall,(ushort)weaponIndex);

                    return true;
                }
                if (rb)
                {
                    rb.AddForce(-hit.normal * force, ForceMode.Impulse);
                    return false;
                }
            }

            return false;

        }

        void StopSound()
        {
            AudioManager.Instance.StopGunShoot();
        }

        public Vector3 aimPos;
        public void DoAim()
        {
            isAiming = true;
            Aim();
        }

        public void DoNoAim()
        {
            isAiming = false;
            DeAim();
        }

        public bool allowToAim = false;

        public SmokeTrail trail;

        public int spinAmount;
        public bool isReload = false;
        public bool animateUi = true;
        [Tooltip("Will be multiplied by the reload time for spining")]
        public float spinReloadPercantage = 1;

        /// <summary>
        /// Tick sent with the last reload request, reused when the request has to
        /// be repeated so the server can still correlate it with this reload.
        /// </summary>
        private uint _reloadRequestTick;

        public void DoReload(int defaultAmmo, bool selfControl = true)
        {
            if (cantReload) return;
            if (currentAmmo.GetValue() >= maxAmmo || isReload || WeaponManager.Instance.CurrentPlayer.Health <= 0 || NetworkManager.Instance.CheckMultiplayerGameModeStarted()) return;
            spinAmount = stat.spinAmount;
            isReload = true;
            Reload();
            currentAmmo.SetValue(defaultAmmo);
            Reload(reloadTime * reloadMultiplier * spinReloadPercantage, spinAmount);
            GameUIManager.Instance.RecordPreviusState();
            if (animateUi)
                GameUIManager.Instance.Reload(reloadTime * reloadMultiplier);
            Invoke(nameof(SetReload), reloadTime * reloadMultiplier);

            DeAim();

            WeaponManager.Instance.arm.enabled = false;

            if (selfControl)
            {
                _reloadRequestTick = NetworkManager.Instance.InterpolationTick;

                SendReloadRequest(_reloadRequestTick);
            }
        }

        void SendReloadRequest(uint tick)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.WeaponReload);

            WeaponManager.Instance.LastReloadTick.Enqueue(new Tuple<uint, int>(
                tick, WeaponManager.Instance.CurrentWeaponIndex));

            message.Add(tick);

            NetworkManager.Instance.Client.Send(message);
        }

        public virtual void Select(bool doAnimation)
        {
            gameObject.SetActive(true);
            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.crossHair.SetActive(true);
            }
            if (doAnimation)
            {
                if (reset != null)
                    AudioManager.Instance.PlayGunReset(reset);
                if (weapon)
                    weapon.localPosition = startPos - new Vector3(0, 2, 5);
                Reload(0.2f, 1);
            }
        }

        public void DeSelect()
        {
            CancelInvoke(nameof(CheckAmmo));
            if (isReload)
            {
                CancelInvoke(nameof(SetReload));
                GameUIManager.Instance.StopReload();
            }

            isReload = false;
            desiredReloadRotation = 0;
            gameObject.SetActive(false);

        }

        public bool playResetAfterReload = false;

        void SetReload()
        {
            GameUIManager.Instance.StopReload();
            currentAmmo.SetValue(maxAmmo);
            if (playResetAfterReload && reset != null)
                AudioManager.Instance.PlayGunReset(reset);
            isReload = false;

            WeaponManager.Instance.arm.enabled = useArm && GameManager.Instance.setting.useArm;
        }

        protected abstract void Shoot();
        protected abstract void Reload();
        protected abstract void Aim();

        void DeAim()
        {
            WeaponManager.Instance.cams[1].cullingMask = WeaponManager.Instance.weaponCamDefaultLayer;
            GameUIManager.Instance.gameScene.SetActive(!GameUIManager.Instance.gameEnd && WeaponManager.Instance.CurrentPlayer.Health > 0 && !GameUIManager.Instance.pause);
            GameUIManager.Instance.scope.SetActive(false);
        }

        bool IsAllowShooting()
        {
            return Time.time - 1 / stat.fireRate > lastFireTime;
        }

        public int damage;
        public float spreadAngle, normalSpread, runSpread;

        private void CreateExplosiveBullet(Vector3 point)
        {
            Vector3 dir = PlayerCam.transform.forward;
            if (point != Vector3.zero) dir = (point - muzzlePoint.position).normalized;
            spreadAngle = Velocity.magnitude > 15f ? runSpread : normalSpread;
            for (int i = 0; i < bulletCount; i++)
            {
                Vector3 offset = (spreadAngle / PlayerCam.fieldOfView) * Random.insideUnitCircle;
                dir += offset;
                Bullet bullet = ObjectPooler.Instance.SpawnFromPool("ExplosiveBullet", muzzlePoint.position,
                    Quaternion.LookRotation(dir)).GetComponent<Bullet>();

                bullet.Initialization(dir, 200f, damage, LayerMask.NameToLayer("Bullet"), true, useGravity);
                dir -= offset;
            }
        }
        #region Dynamic

        private void MovementBob()
        {
            if (Mathf.Abs(Velocity.magnitude) < 4f || !WeaponManager.Instance.CurrentPlayer.Grounded || WeaponManager.Instance.CurrentPlayer.IsCrouching) //
            {
                desiredBob = Vector3.zero;
                return;
            }

            float x;
            float y;
            float z;
            x = Mathf.PingPong(Time.time * bobSpeed, xBob) - xBob / 2f;
            y = Mathf.PingPong(Time.time * bobSpeed, yBob) - yBob / 2f;
            z = Mathf.PingPong(Time.time * bobSpeed, zBob) - zBob / 2f;
            desiredBob = new Vector3(x, y, z);
        }

        public float normalReloadTime, reloadTime;
        private void Rotation(Vector2 offset)
        {
            float num = offset.magnitude * 0.03f;
            if (offset.x < 0f)
            {
                num = 0f - num;
            }
            float y = offset.y;
            Vector3 euler = new Vector3(y: (0f - offset.x) * 40f, x: y * 80f + reloadRotation, z: num * 50f) + recoilRotation;
            try
            {
                if (!(Time.deltaTime <= 0f))
                {
                    transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(euler), Time.deltaTime * 20f);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
        private void SpeedBob()
        {
            // Vector2 vector = ReplayManager.Instance.Replaying? _replayPlayer.FindVelRelativeToLook()*drag : PlayerMovement.Instance.FindVelRelativeToLook()*drag;
            Vector2 vector = WeaponManager.Instance.CurrentPlayer.FindVelRelativeToLook() * drag;
            Vector3 vector2 = new Vector3(vector.x, Velocity.y, vector.y);
            vector2 *= -0.01f;
            vector2 = Vector3.ClampMagnitude(vector2, 0.1f);
            speedBob = Vector3.Lerp(speedBob, vector2, Time.deltaTime * 10f);
        }

        [Header("ShootBack")]
        public float shootBack = 1f;
        public void ShootAnim()
        {
            float num = 1.5f;
            recoilOffset += -(Vector3.forward * shootBack + Vector3.up * shootVertical - Vector3.right * shootHorizontal);
            recoilRotation += -new Vector3(shootRotX, Random.Range(10f, 30f) * shootRotYMultiplier, Random.Range(-50f, 50f) * shootRotZMultiplier) * num;
        }

        [SerializeField] private float shootVertical = 0.3f, shootHorizontal = 0.35f;
        [SerializeField] private float shootRotX = 90f, shootRotZMultiplier = 1f, shootRotYMultiplier = 1f;
        private void RecoilGun()
        {
            recoilOffset = Vector3.SmoothDamp(recoilOffset, Vector3.zero, ref recoilOffsetVel, 0.05f);
            recoilRotation = Vector3.SmoothDamp(recoilRotation, Vector3.zero, ref recoilRotVel, 0.07f);
        }

        public void ResetDynamic()
        {
            recoilOffset = Vector3.zero;
            recoilRotation = Vector3.zero;
            desiredBob = Vector3.zero;
            speedBob = Vector3.zero;
            // weapon.localPosition = startPos;
            // transform.localRotation = Quaternion.identity;
            // if (isReload)
            // {
            //     reloadProgress = 0;
            //     reloadRotation = 0;
            //     reloadPosOffset = 0;
            //     CancelInvoke(nameof(SetReload));
            //     GameUIManager.Instance.StopReload();
            // }
        }

        private float vel;

        private void ReloadGun()
        {
            reloadProgress += Time.deltaTime;
            float t;
            if (reloadTimet != 0.0)
            {
                t = reloadProgress / reloadTimet;
            }
            else
            {
                t = 0.0f;
            }
            reloadRotation = Mathf.Lerp(0f, desiredReloadRotation, t);
            reloadPosOffset = Mathf.SmoothDamp(reloadPosOffset, 0f, ref rPVel, reloadTimet * 0.2f);
            if (reloadRotation / 360f > spins)
            {
                spins++;
            }
        }

        void Reload(float time, int spinAmount)
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

        protected Vector3 Velocity => WeaponManager.Instance.CurrentPlayer.GetVelocity();

        protected Camera PlayerCam => MoveCamera.Instance.cam;

        private Vector3 startPos = new Vector3(0, -0.5f, 0);

        private Vector3 desiredBob;

        private float xBob = 0.12f;

        private float yBob = 0.08f;

        private float zBob = 0.1f;

        private float bobSpeed = 0.45f;

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

        public float currentGunDragMultiplier = .5f;

        private float desX;

        private float desY;

        private Vector3 speedBob;

        private float reloadProgress;

        private float rotationOffset;

        private Vector3 prevRotation;

        private int spins;
        private static readonly int Attack = Animator.StringToHash("Attack");

        #endregion

    }
}
