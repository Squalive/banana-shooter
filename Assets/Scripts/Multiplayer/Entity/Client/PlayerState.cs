
using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Cosmetic;
using DitzelGames.FastIK;
using Extensions;
using EZCameraShake;
using Manager;
using Menu;
using Movement;
using Multiplayer.Entity.Server;
using PlayerCameraController;
using Pool;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;
using Weapon;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Client
{
    public class PlayerState : MonoBehaviour
    {
        public bool selfControlled = false;

        public static PlayerState LocalPlayer = null;
        
        public static List<PlayerState> PlayerStates { get; } = new();
        public static bool DisplayPlayerName { get; set; } = true;
        public bool IsLocal { get; set; }
        public ulong SteamId { get; private set; }
        public string Username { get; private set; }
        public Team Team { get;private set; }
        public bool IsInfected { get; set; }
        public bool IsCrouching { get; set; } = false;
        public bool Grounded { get;set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; } = 100;
        public bool Aiming { get; set; } = false;
        
        // Can this player be spectated by other ? true : false
        public bool CanSpectate { get; private set; }

        public Texture2D AvatarImage { get; private set; } = null;

        private float _deltaX=0, _deltaY=0, _desiredDeltaX =0,_desiredDeltaY =0;

        private Vector3 _velocity;
        
        public InventoryManager.CosmeticIndex CosmeticIndex { get; set; }
        
        public Grappling grappling;
        
        public CanvasGroup group;
        private float _desiredNameAlpha = 0;

        public Transform weaponHolder;

        public Transform head;
        public Rigidbody rb;

        public GameObject model;
        public List<MultiplayerWeapon> weapons = new List<MultiplayerWeapon>();

        [SerializeField] private Transform playerTransform;
        [SerializeField] List<Transform> bones = new();
        
        public PlayerWeaponManager WeaponManager;
        
        public List<GameObject> hatCosmetics = new List<GameObject>();
        public List<GameObject> faceCosmetics = new List<GameObject>();
        public List<GameObject> shoeLCosmetics = new List<GameObject>();
        public List<GameObject> shoeRCosmetics = new List<GameObject>();
        public List<GameObject> hairCosmetics = new List<GameObject>();
        public List<GameObject> clothesCosmetics = new List<GameObject>();
        public List<GameObject> pantCosmetics = new List<GameObject>();
        public SkinnedMeshRenderer daveHair,clothes,pant;
        
        [SerializeField] private MeshRenderer[] eye;
        public SkinnedMeshRenderer[] models;

        [SerializeField] private GameObject clawKnife;
        [SerializeField] Transform clawKnifeHandPos;
        [SerializeField] public Outline outline;
        private ClientPlayer.OutlineType _outlineType=ClientPlayer.OutlineType.None;
        
        [SerializeField] private LookedToObject leftHandTarget, rightHandTarget;
        [SerializeField] private List<FastIKFabric> iks = new();
        [SerializeField] private Animator clawKnifeAnim;
        [SerializeField] private TextMeshProUGUI nameText;

        [SerializeField] private GameObject light;
        
        public void SetValues(bool isLocal,ulong steamId, string username, Team team,bool canSpectate)
        {
            IsLocal = isLocal;
            if (selfControlled) 
            {
                PlayerNameRaycast.Instance.localPlayer = this;

                LocalPlayer = this;
                
                SpectateMovement.Instance.SetToLocal(this);
            }
            SteamId = steamId;
            Username = username;
            Team = team;
            Health = MaxHealth;
            CanSpectate = canSpectate;

            InitializeAvatarImage();

            if ( light )
                light.SetActive( NetworkManager.ClientGameMode == GameMode.PVE );
        }

        public void InitializeWeaponManager(AudioSource audioSource)
        {
            var w = IsLocal ? Weapon.WeaponManager.Instance.weapons : weapons;
            WeaponManager = new PlayerWeaponManager(IsLocal, w, leftHandTarget, rightHandTarget, iks, this, clawKnifeAnim, audioSource, weaponHolder, head);
        }

        public void SetLocal(bool flag)
        {
            IsLocal = flag;
            WeaponManager.SetLocal(flag);
        }
        
        private void Start()
        {
            PlayerStates.Add(this);

            NetworkManager.OnAvatarImageLoaded += OnImageLoaded;
        }

        private void OnDestroy()
        {
            NetworkManager.OnAvatarImageLoaded -= OnImageLoaded;
            PlayerStates.Remove(this);
            if (IsLocal)
            {
                PlayerNameRaycast.Instance.localPlayer = null;
            }

            if (selfControlled)
            {
                LocalPlayer = null;
            }
        }

        private void Update()
        {
            if (!selfControlled && !IsLocal)
            {
                group.alpha = Mathf.Lerp(@group.alpha, NetworkManager.Instance.IsTeamMode(this) || !DisplayPlayerName ? 0: _desiredNameAlpha, Time.unscaledDeltaTime * 15f);

                if (Health > 0)
                {
                    WeaponManager.Update();
                }
            }

            if (GameUIManager.Instance && IsLocal)
            {
                GameUIManager.Instance.healthSlider.value = Mathf.Lerp(GameUIManager.Instance.healthSlider.value, Health,
                    Time.unscaledDeltaTime * 15f);
                
                Color color = GameUIManager.Instance.gradient.Evaluate(GameUIManager.Instance.healthSlider.normalizedValue);

                GameUIManager.Instance.healthImage.color = color;
                GameUIManager.Instance.image.color = color;
                GameUIManager.Instance.healthText.color = color;
            
                GameUIManager.Instance.healthText.SetText(Health.ToString());
            }
        }

        private void LateUpdate()
        {
            if (IsLocal && !selfControlled)
            {
                _deltaX = Mathf.Lerp(_deltaX, _desiredDeltaX, Time.deltaTime * 25f);
                _deltaY = Mathf.Lerp(_deltaY, _desiredDeltaY, Time.deltaTime * 25f);

                _desiredDeltaX = Mathf.Lerp(_desiredDeltaX, 0, Time.deltaTime * 20f);
                _desiredDeltaY = Mathf.Lerp(_desiredDeltaY, 0, Time.deltaTime * 20f);
            }
        }

        public void DisplayName()
        {
            _desiredNameAlpha = 1f;
        }

        public void NotDisplayName()
        {
            _desiredNameAlpha = 0f;
        }

        public void TakeHealth(int health,int maxHealth)
        {
            Health = health;
            MaxHealth = maxHealth;
            if (IsLocal)
            {
                if (GameUIManager.Instance)
                {
                    GameUIManager.Instance.Health();
                    GameUIManager.Instance.Hurt(Health,MaxHealth);
                    GameUIManager.Instance.healthSlider.maxValue = MaxHealth;
                }
            }
        }

        public void TakeDamage(int damage, bool headShot, Vector3 pos, Vector3 normal, bool effect, bool attackerIsLocal)
        {
            Health -= damage;
            Health = Mathf.Clamp(Health, 0, MaxHealth);
            
            if (IsLocal)
            {
                rb.AddForce(-normal*20f,ForceMode.Impulse);
            }
            
            TakeDamageEffect(damage,headShot,pos,normal,effect,attackerIsLocal);
        }
        
        public void SetHealth(int health, int damage, bool headShot, Vector3 pos, Vector3 normal, bool effect, bool attackerIsLocal)
        {
            Health = health;
            Health = Mathf.Clamp(Health, 0, MaxHealth);
            
            if (IsLocal)
            {
                CameraShaker.Instance.ShakeOnce(3f, 3f, 0.1f, 0.5f);
                rb.AddForce(-normal*20f,ForceMode.Impulse);
            }
            
            TakeDamageEffect(damage,headShot,pos,normal,effect,attackerIsLocal);
        }

        void TakeDamageEffect(int damage, bool headShot, Vector3 pos, Vector3 normal, bool effect, bool attackerIsLocal)
        {
            AudioManager.Instance.SoundEffect3D("Blood",playerTransform.position);

            if (attackerIsLocal)
            {
                if (headShot)
                {
                    HitMarker.Instance.StartHitMarker(Color.yellow);
                    AudioManager.Instance.Play("headshotrapid");
                }
                else
                {
                    HitMarker.Instance.StartHitMarker(Color.white);
                }
            }
            else
            {
                if (headShot)
                {
                    AudioManager.Instance.SoundEffect3D("headshotrapid", playerTransform.position, 1f, 10f);
                }
            }
            if (GameManager.Instance.setting.enableGore && effect && GameManager.Instance.setting.spawnParticle)
            {
                ObjectPooler.Instance.SpawnFromPool("Blood", pos, Quaternion.LookRotation(normal));
            }

            HitMarker3D h = ObjectPooler.Instance.SpawnFromPool("HitMarker",pos, Quaternion.LookRotation(ListenerManager.Instance.cameraTransform.position-pos)).GetComponent<HitMarker3D>();
            h.text.SetText(damage.ToString());
            
            if (IsLocal)
            {
                CameraShaker.Instance.ShakeOnce(3f, 3f, 0.1f, 0.5f);
                GameUIManager.Instance.Hurt(Health,MaxHealth);
                DamageUI.Instance.Damage();

                if (Health < 30 && !GameManager.Instance.knewDead)
                {
                    GameManager.Instance.Dead();
                }
            }
        }

        public void Dead(bool headShot,bool wallbang, int weaponIndex, bool attackerIsLocal)
        {
            // HitMarker.Instance.StartHitMarker(Color.red);
            Health = 0;
            
            WeaponManager.IsUsingSpecialWeapon();

            WeaponManager.ClearCurrentWeapons();

            if (IsLocal)
            {
                Weapon.WeaponManager.Instance.StopAim(new InputAction.CallbackContext());
                Weapon.WeaponManager.Instance.DisableAllWeapons();
                if(GameManager.Instance.setting.cameraShake)CameraShaker.Instance.ShakeOnce(5f, 5f, 0.1f, 0.7f);
            }

            if (attackerIsLocal)
            {
                HitMarker.Instance.StartHitMarker(Color.red);
                if(GameManager.Instance.setting.cameraShake)
                    CameraShaker.Instance.ShakeOnce(3f, 3f, 0.1f, 0.5f);
                GameUIManager.Instance.KillSecured(Username);
                
                AudioManager.Instance.Play("KillSecured");
            }
            
            if (GameManager.Instance.setting.enableGore&&GameManager.Instance.setting.spawnParticle)
            {
                ObjectPooler.Instance.SpawnFromPool("BloodSplashBig", playerTransform.position, Quaternion.identity);
            }
        }

        public void SetAiming(bool aiming)
        {
            Aiming = aiming;

            if (IsLocal)
            {
                if (!selfControlled)
                {
                    if (aiming)
                    {
                        Weapon.WeaponManager.Instance.StartAim(new InputAction.CallbackContext());
                    }
                    else
                    {
                        Weapon.WeaponManager.Instance.StopAim(new InputAction.CallbackContext());
                    }
                }
            }
        }

        public void Respawn()
        {
            if (IsLocal)
            {
                GameUIManager.Instance.gameScene.SetActive(!GameUIManager.Instance.pause);
                GameUIManager.Instance.healthSlider.maxValue = MaxHealth;
                GameUIManager.Instance.Hurt(Health,MaxHealth);
                GameUIManager.Instance.Clear();
                if(GameManager.Instance.setting.cameraShake)CameraShaker.Instance.ShakeOnce(5f, 5f, 0.1f, 0.7f);
            }
        }

        public void SetInfect()
        {
            if (IsInfected)
            {
                WeaponManager.DisableAllWeapons();

                MaxHealth = 200;
                // if (HasPerk(Perk.Fat))
                //     MaxHealth = (int) (MaxHealth * PerkManager.FatMultiplier);
                Health = MaxHealth;

                if (IsLocal)
                {
                    Weapon.WeaponManager.Instance.StopAim(new InputAction.CallbackContext());

                    Weapon.WeaponManager.Instance.DisableAllWeapons();

                    Tutorial.Instance.SetText("InfectedTip2");
                
                    if (GameUIManager.Instance)
                    {
                        foreach (var weaponUi in GameUIManager.Instance.weaponUis)
                        {
                            weaponUi.NotDisplay();
                        }
                        GameUIManager.Instance.SetInfected(IsInfected,selfControlled);
                        GameUIManager.Instance.healthSlider.maxValue = MaxHealth;

                        GameUIManager.Instance.throwObjImage.texture = PrefabManager.Instance.throwObjTexture[1];
                    }
                    InfectedHand.Instance.SetInfect(IsInfected);
                    return;
                }
                
                foreach (var e in eye)
                {
                    e.material.color = Color.red;
                }

                foreach (var meshRenderer in models)
                {
                    meshRenderer.material.color = new Color(119/255f,154/255f,91/255f);
                }
            
                CancelInvoke(nameof(ClearOutline));
                _outlineType = ClientPlayer.OutlineType.Infected;
            
                OutlineDisplay(Color.green, Outline.Mode.OutlineVisible);
            
                clawKnife.SetActive(true);
                rightHandTarget.parent = clawKnifeHandPos;
                leftHandTarget.parent = null;
                iks[0].enabled = false;
                iks[1].enabled = true;

                if (GameManager.Instance.setting.spawnParticle)
                {
                    Instantiate(PrefabManager.Instance.GetPrefab("InfectedParticle"), playerTransform.position,
                        Quaternion.identity);
                }
            
                nameText.color = Color.green;
            }
            else
            {
                if (!IsLocal)
                {
                    if (NetworkManager.ClientGameMode != GameMode.TeamDeathMatch)
                    {
                        foreach (var e in eye)
                        {
                            e.material.color = Color.black;
                        }

                        if(_outlineType != ClientPlayer.OutlineType.Invincible)
                            outline.enabled = false;
                        foreach (var meshRenderer in models)
                        {
                            meshRenderer.material.color = Color.white;
                        }

                        clawKnife.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (GameUIManager.Instance)
                    {
                        GameUIManager.Instance.SetInfected(IsInfected,selfControlled);
                    }
                    InfectedHand.Instance.SetInfect(IsInfected);
                }
            }
        }

        public GameObject SpawnRagdoll(bool forceLimbs, bool forceDefault = false)
        {
            string n = !forceDefault && (GameManager.Instance.setting.enableGore && (Random.Range(0, 10) < 3 || forceLimbs))
                ? "Ragdoll Severd Limbs"
                : "Ragdoll";
            PlayerRagdoll ragdoll = Instantiate(PrefabManager.Instance.GetPrefab(n), playerTransform.position+Vector3.down*0.5f,
                playerTransform.rotation).GetComponent<PlayerRagdoll>();
            ragdoll.SetBones(bones);
            ragdoll.SetCosmetic(CosmeticIndex);
            ragdoll.SetInfected(IsInfected);
            Destroy(ragdoll.gameObject,GameManager.Instance.setting.keepRagdoll ? 180f : 5f);

            return ragdoll.gameObject;
        }

        public void ExplodeRagdoll()
        {
            Vector3 explodePos = playerTransform.position+Vector3.down*0.5f;
            PlayerRagdoll ragdoll = Instantiate(PrefabManager.Instance.GetPrefab("Ragdoll Severd Limbs"),explodePos ,
                playerTransform.rotation).GetComponent<PlayerRagdoll>();
            ragdoll.SetBones(bones);
            ragdoll.SetCosmetic(CosmeticIndex);
            ragdoll.SetInfected(IsInfected);
            Destroy(ragdoll.gameObject,GameManager.Instance.setting.keepRagdoll ? 180f : 10f);

            foreach (var rb in ragdoll.rbs)
            {
                rb.AddExplosionForce(0.5f, explodePos, 4f, 0, ForceMode.Impulse);
            }

            ObjectPooler.Instance.SpawnFromPool("PlayerExplode", explodePos,
                Quaternion.Euler(-90, 0, 0));
            
            AudioManager.Instance.SoundEffect3D("Explosion", explodePos);
        }

        public void SetOutlineType(ClientPlayer.OutlineType outlineType)
        {
            _outlineType = outlineType;
        }

        public ClientPlayer.OutlineType GetOutlineType()
        {
            return _outlineType;
        }
        
        public void OutlineDisplay(Color color, float duration)
        {
            if (IsLocal) return;
            _outlineType = ClientPlayer.OutlineType.Death;
            outline.enabled = true;

            outline.OutlineColor = color;
        
            Invoke(nameof(ClearOutline),duration);
        }
        public void OutlineDisplay(Color color,Outline.Mode mode = Outline.Mode.OutlineAll)
        {
            if (IsLocal) return;
            outline.enabled = true;

            outline.OutlineMode = mode;
            outline.OutlineColor = color;
        }
        public void ClearOutline()
        {
            CancelInvoke(nameof(ClearOutline));
            if (!IsInfected && outline)
            {
                _outlineType = ClientPlayer.OutlineType.None;
                outline.enabled = false;
                outline.OutlineColor = Color.white;
            }
        }

        public void ClearOutlineDuration(float duration)
        {
            Invoke(nameof(ClearOutline), duration);
        }
        

        void InitializeAvatarImage()
        {
            int imageId = SteamFriends.GetMediumFriendAvatar((CSteamID) SteamId);
            if (imageId == -1) return;

            AvatarImage = SteamTextureUtils.GetSteamImageAsTexture(imageId);
        }

        void OnImageLoaded(CSteamID steamID, Texture2D avatarImage)
        {
            if (steamID.m_SteamID == SteamId)
            {
                AvatarImage = avatarImage;
                NetworkManager.OnAvatarImageLoaded -= OnImageLoaded;
            }
        }

        #region Cosmetic

        public void SetPlayerCosmetics(InventoryManager.CosmeticIndex cosmetic)
        {
            if (!IsLocal)
            {
                SetCosmetics(cosmetic.hatIndex,cosmetic.hatColor,cosmetic.hatShiny,cosmetic.hatParticle,hatCosmetics,ref _hatParticle);
                
                SetCosmetics(cosmetic.faceIndex,cosmetic.faceColor,cosmetic.faceShiny,cosmetic.faceParticle,faceCosmetics,ref _faceParticle);
                
                SetCosmetics(cosmetic.shoesIndex, cosmetic.shoesColor, cosmetic.shoesShiny, cosmetic.shoesParticle, shoeLCosmetics, ref _shoeLParticle);
                SetCosmetics(cosmetic.shoesIndex, cosmetic.shoesColor, cosmetic.shoesShiny, cosmetic.shoesParticle, shoeRCosmetics, ref _shoeRParticle);
                
                SetCosmetics(cosmetic.hairIndex,cosmetic.hairColor,cosmetic.hairShiny,cosmetic.hairParticle,hairCosmetics,ref _hairParticle,daveHair.gameObject);
                
                SetCosmetics(cosmetic.pantIndex,cosmetic.pantColor,cosmetic.pantShiny,cosmetic.pantParticle,pantCosmetics,ref _pantParticle,pant.gameObject);
                
                SetCosmetics(cosmetic.clothesIndex,cosmetic.clothesColor,cosmetic.clothesShiny,cosmetic.clothesParticle,clothesCosmetics,ref _clotheParticle,clothes.gameObject);
                
            }
            this.CosmeticIndex = cosmetic;
        }
        
        Transform _hatParticle;
        Transform _faceParticle;
        Transform _shoeLParticle;
        Transform _shoeRParticle;
        Transform _hairParticle;
        Transform _clotheParticle;
        Transform _pantParticle;
        
        void SetCosmetics(int index, Color color, float shiny, int particle,List<GameObject> cosmetics, ref Transform particleTran,GameObject alreadyHave=null)
        {
            if (IsLocal) return;
            foreach (var cosmetic in cosmetics)
            {
                cosmetic.gameObject.SetActive(false);
            }
            if(particleTran) Destroy(particleTran.gameObject);
            if (index < cosmetics.Count && index!=-1)
            {
                if (cosmetics[index] != null)
                {
                    if (alreadyHave)
                    {
                        alreadyHave.SetActive(false);
                    }
                    cosmetics[index].SetActive(true);
                    foreach (var ren in cosmetics[index].GetComponentsInChildren<Renderer>())
                    {
                        if (color != Color.clear)
                        {
                            ren.material.color = color;

                            if (CosmeticManager.IsGoldenColor(color))
                            {
                                ren.material.SetFloat(MapSaver.Smoothness, 1f);
                                ren.material.SetFloat(MapSaver.Metallic, 0.6f);
                            }
                        }
                        if (shiny != 0)
                        { 
                            ren.material.EnableKeyword("_EMISSION");
                            ren.material.SetColor(CosmeticMenu.EmissionColor, color * shiny);
                        }
                        else
                        {
                            ren.material.DisableKeyword("_EMISSION");
                        }
                    }
                
                    if (particle != -1 && CosmeticManager.ItemIdToItem.TryGetValue(particle, out var item))
                    {
                        InventoryManager.ParticleItem particleItem = InventoryManager.Instance.GetParticle(item.tag);
                        particleTran = Instantiate(particleItem.prefab).transform;
                        particleTran.position  = cosmetics[index].transform.position+new Vector3(0,0.0095f,0);

                        particleTran.localScale = particleItem.inGameSize;

                        particleTran.gameObject.layer= CosmeticMenu._clientPlayerLayer;

                        for (int i = 0; i < particleTran.childCount; i++)
                        {
                            particleTran.GetChild(i).gameObject.layer =CosmeticMenu._clientPlayerLayer ;
                        }
                        
                        CosmeticVFX cosmeticVFX = particleTran.GetComponent<CosmeticVFX>();

                        if (cosmeticVFX != null)
                        {
                            var filter = cosmetics[index].GetComponentInChildren<MeshFilter>();
                            if (filter == null)
                            {
                                particleTran.parent = cosmetics[index].transform;
                                var skin = cosmetics[index].GetComponentInChildren<SkinnedMeshRenderer>();
                                cosmeticVFX.SetSkinnedMeshRenderer(skin);
                                cosmeticVFX.SetTransform(skin.rootBone,true);
                            }
                            else
                            {
                                particleTran.parent = cosmetics[index].transform.parent;
                                cosmeticVFX.SetMesh(filter.sharedMesh);
                                cosmeticVFX.SetTransform(filter.transform);
                            }
                        }
                        else
                        {
                            particleTran.parent = cosmetics[index].transform.parent;
                        }
                    }
                    
                }
            }
            else
            {
                if (alreadyHave)
                    alreadyHave.SetActive(true);
            }
        }

        public void ChangeCosmetic(CosmeticItem.Type type,int index,Color color,float shiny,int particle)
        {
            switch (type)
            {
                case CosmeticItem.Type.Hat:
                    CosmeticIndex.hatIndex = index;
                    CosmeticIndex.hatColor = color;
                    CosmeticIndex.hatShiny = shiny;
                    CosmeticIndex.hatParticle = particle;
                    SetCosmetics(index,color,shiny,particle,hatCosmetics,ref _hatParticle);
                    break;
                case CosmeticItem.Type.Face:
                    CosmeticIndex.faceIndex = index;
                    CosmeticIndex.faceColor = color;
                    CosmeticIndex.faceShiny = shiny;
                    CosmeticIndex.faceParticle = particle;
                    SetCosmetics(index,color,shiny,particle,faceCosmetics,ref _faceParticle);
                    break;
                case CosmeticItem.Type.Shoes:
                    CosmeticIndex.shoesIndex = index;
                    CosmeticIndex.shoesColor = color;
                    CosmeticIndex.shoesShiny = shiny;
                    CosmeticIndex.shoesParticle = particle;
                    SetCosmetics(index,color,shiny,particle,shoeLCosmetics,ref _shoeLParticle);
                    SetCosmetics(index,color,shiny,particle,shoeRCosmetics,ref _shoeRParticle);
                    break;
                case CosmeticItem.Type.Hair:
                    CosmeticIndex.hairIndex = index;
                    CosmeticIndex.hairColor = color;
                    CosmeticIndex.hairShiny = shiny;
                    CosmeticIndex.hairParticle = particle;
                
                    if(!IsLocal)SetCosmetics(index,color,shiny,particle,hairCosmetics,ref _hairParticle,daveHair.gameObject);
                    break;
                case CosmeticItem.Type.Clothes:
                    CosmeticIndex.clothesIndex = index;
                    CosmeticIndex.clothesColor = color;
                    CosmeticIndex.clothesShiny = shiny;
                    CosmeticIndex.clothesParticle = particle;
                
                    if(!IsLocal)SetCosmetics(index,color,shiny,particle,clothesCosmetics,ref _clotheParticle,clothes.gameObject);
                    break;
                case CosmeticItem.Type.Pant:
                    CosmeticIndex.pantIndex = index;
                    CosmeticIndex.pantColor = color;
                    CosmeticIndex.pantShiny = shiny;
                    CosmeticIndex.pantParticle = particle;
                
                    if(!IsLocal)SetCosmetics(index,color,shiny,particle,pantCosmetics,ref _pantParticle,pant.gameObject);
                    break;
                default:
                    CosmeticIndex.weaponIndex[(int) type - CosmeticMenu.CosmeticOffset] = (ushort)index;
                    break;
            }
        }

        #endregion

        public void SetCanSpectate(bool canSpectate)
        {
            CanSpectate = canSpectate;
        }

        public Vector2 FindVelRelativeToLook()
        {
            float lookAngle = playerTransform.eulerAngles.y;
            var velocity = GetVelocity();
            return VelocityExtensions.FindVelRelativeToLook(lookAngle, velocity);
        }

        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
        }

        public Vector3 GetVelocity()
        {
            return _velocity;
        }

        public void SetDelta(float deltaX, float deltaY)
        {
            if (!IsLocal) return;
            _desiredDeltaX += deltaX;
            _desiredDeltaY += deltaY;
        }

        public float GetDeltaX()
        {
            return _deltaX;
        }
        
        public float GetDeltaY()
        {
            return _deltaY;
        }
    }
}
