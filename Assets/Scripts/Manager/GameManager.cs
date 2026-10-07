using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Audio;

using Console;
using Demo;
using EZCameraShake;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Interface;
using Newtonsoft.Json;
using Save;
using Steamworks;
using Steamworks.NET;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using JsonException = Newtonsoft.Json.JsonException;

namespace Manager
{
    [DefaultExecutionOrder(-5)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public static bool Initialized { private set; get; } = false;

        public static bool SettingsLoaded = false;
        public static Action<Setting> OnSettingLoaded;

        [NonSerialized]
        public Setting setting;

        public string[] upgrades = new string[3];

        public int introTheme = 4;
        public ThrowObjectMenu.ThrowObjectType tacticalProp = ThrowObjectMenu.ThrowObjectType.Grenade;
        public static int lastKill = 0, lastDie = 0;
        public static bool getBox = false, win = false, hitRecently = false, survive = false, finalRound = false;

        public static bool hostGameDontShowMeThisAgain = false;

        //Command line prefix
        private const string HasArgPrefix = "+", HasNoArgPrefix = "-";

        protected Callback<PersonaStateChange_t> PersonaStateCallback;


        public enum PowerType
        {
            Boomer,
            Rocket_Launcher,
            DualSMG
        }

        public PowerType power = PowerType.Boomer;

        [Serializable]
        public class UpgradeDetailed
        {
            public string name;
            public Texture2D texture2D;
            public int maxIndex = 1, cost = 1;
        }

        public List<UpgradeDetailed> upgradeDetaileds = new List<UpgradeDetailed>();

        public static InputManager InputManager;
        public static Dictionary<string, Tuple<string, string, InputAction>> CustomInputActions = new();

        public static event Action RebindComplete;
        public static event Action RebindCanceled;
        public static event Action<InputAction, int> RebindStarted;

        public static int RagdollLimited => GameManager.Instance.setting.keepRagdoll ? 50 : 10;

        public LayerMask whatIsGround, lagCompensationHitboxLayer;

        [SerializeField] public VolumeProfile volume;

        private ColorAdjustments _adjustments;
        private Bloom _bloom;
        private DepthOfField _depthOfField;

        public static List<GameObject> Entities = new List<GameObject>();

        public Action<ClientPlayer, ClientPlayer> ClientLocalPlayerDead;
        public Action<IPlayerServer, IPlayerServer> ServerPlayerDead;
        public Action<Camera> PlayerSpawn;
        public Action<ClientPlayer> LocalPlayerSetWeapon;
        public Action PlayerDestroy;

        [SerializeField] public List<string> maps = new();
        public Dictionary<string, bool> mapPlayed = new();

        public Action<CSteamID, EPersonaChange> OnPersonaStateChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InputManager ??= new InputManager();
                InputManager.Enable();
            }

            setting = new Setting();
            _adjustments = (ColorAdjustments)volume.components[3];
            _bloom = (Bloom)volume.components[0];
            _depthOfField = (DepthOfField)volume.components[6];
            _depthOfField.active = false;

            hostGameDontShowMeThisAgain = PlayerPrefs.GetInt("hostGameDontShowMeThisAgain", 0) == 1;
        }


        private IEnumerator Start()
        {
            PersonaStateCallback = Callback<PersonaStateChange_t>.Create(OnPersonaStateChangedCall);

            CheckCommandLineArgs();
            earRinging = gameObject.AddComponent<AudioSource>();

            earRinging.outputAudioMixerGroup = MusicManager.Instance.master;

            earRinging.loop = true;
            earRinging.volume = 0f;
            earRinging.clip = earRingingClip;

            earRinging.Play();

            introTheme = PlayerPrefs.GetInt("intro_theme", 4);

            LoadCustomBindings();

            CoroutineWithData cd;
            if (File.Exists(SaveSystem.GetPath("Perks.json")))
            {
                //Read the perks
                cd = new CoroutineWithData(this, SaveSystem.ReadFileAsyncThread(SaveSystem.GetPath("Perks.json")));

                yield return cd.coroutine;

                try
                {
                    List<Perk> perks = JsonHelper.FromJson<Perk>(cd.result.ToString()).ToList();

                    if (!perks.Contains(Perk.None) && perks.Count == 3)
                    {
                        PerkManager.Instance.perks = perks;

                        for (int i = 0; i < PerkManager.Instance.perks.Count; i++)
                        {
                            if ((int)PerkManager.Instance.perks[i] > 7)
                            {
                                PerkManager.Instance.perks[i] = Perk.Bot;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Code to execute if deserialization fails
                    Debug.LogError("Error while deserializing perks JSON: " + e.Message);
                }


            }

            Preload.Instance.NextStep();

            //Read the throwable's type
            ThrowObjectMenu.ThrowObjectType type = (ThrowObjectMenu.ThrowObjectType)PlayerPrefs.GetInt("throwObjects", 0);
            tacticalProp = (int)type > 4 ? ThrowObjectMenu.ThrowObjectType.Grenade : type;

            if (File.Exists(SaveSystem.GetPath("map_travel")))
            {
                //Read the map travel amount
                cd = new CoroutineWithData(this, SaveSystem.LoadBinaryDataAsync("map_travel"));

                yield return cd.coroutine;

                if (cd.result is List<bool> data)
                {
                    for (int i = 0; i < maps.Count; i++)
                    {
                        bool flag = false;
                        if (i < data.Count)
                            flag = data[i];
                        mapPlayed.Add(maps[i], flag);
                    }
                }
                else
                {
                    foreach (var t in maps)
                    {
                        mapPlayed.Add(t, false);
                    }
                }
            }
            else
            {
                foreach (var t in maps)
                {
                    mapPlayed.Add(t, false);
                }
            }

            Preload.Instance.NextStep();


            //Read the settings
            yield return LoadSetting();

            Preload.Instance.NextStep();

            if (File.Exists(SaveSystem.GetPath("upgrades")))
            {
                //Read the upgrades
                cd = new CoroutineWithData(this, SaveSystem.LoadBinaryDataAsync("upgrades"));

                yield return cd.coroutine;

                if (cd.result is string[] up && up.Length == upgrades.Length)
                    upgrades = up;
            }

            Preload.Instance.NextStep();

            if (File.Exists(SaveSystem.GetPath("weapons")))
            {
                //Read the weapons
                cd = new CoroutineWithData(this, SaveSystem.LoadBinaryDataAsync("weapons"));
                yield return cd.coroutine;

                if (cd.result is short[] wea && wea.Length >= 3)
                {
                    var weaponInfo = NetworkManager.Instance.weaponInfo;
                    for (int i = 0; i < 3; i++)
                    {
                        if (wea[i] == 6) wea[i] = 1;
                        if (wea[i] < 0 || wea[i] >= weaponInfo.Count || weaponInfo[wea[i]].specialWeapon) continue;

                        NetworkManager.Instance.Weapons[i] = wea[i];
                    }
                }
            }

            Preload.Instance.NextStep();


            if (File.Exists(SaveSystem.GetPath("power")))
            {
                //Read the Power
                cd = new CoroutineWithData(this, SaveSystem.LoadBinaryDataAsync("power"));
                yield return cd.coroutine;

                if (cd.result is PowerType p)
                    power = p;
            }

            Preload.Instance.NextStep();


            if (File.Exists(SaveSystem.GetPath("group.json")))
            {
                cd = new CoroutineWithData(this, SaveSystem.ReadFileAsyncThread(SaveSystem.GetPath("group.json")));

                yield return cd.coroutine;

                try
                {
                    NetworkManager.Instance.currentGroup = JsonConvert.DeserializeObject<CSteamID>(cd.result.ToString());

                }
                catch (JsonException e)
                {
                    // Code to execute if deserialization fails
                    Debug.LogError("Error while deserializing group JSON: " + e.Message);
                    NetworkManager.Instance.currentGroup = CSteamID.Nil;
                }
            }

            Preload.Instance.NextStep();

            yield return LoadInventory();

            Preload.Instance.NextStep();

            Initialized = true;
        }

        private void OnDestroy()
        {
            foreach (var inputAction in CustomInputActions.Values)
            {
                inputAction.Item3.Disable();
                inputAction.Item3.Dispose();
            }
            CustomInputActions.Clear();
        }

        public void AddCustomBinding(string n, string command, string key, InputAction action, bool save = false)
        {
            if (string.IsNullOrEmpty(n))
            {
                action.Disable();
                action.Dispose();
                Debug.LogError($"Name is null");
                return;
            }
            if (CustomInputActions.ContainsKey(n))
            {
                action.Disable();
                action.Dispose();
                Debug.LogError($"{n} Already Exist In Binding List");
                return;
            }
            CustomInputActions.Add(n, new Tuple<string, string, InputAction>(command, key, action));

            if (save)
                SaveCustomBindings();
        }

        public void RemoveCustomBinding(string bindingName)
        {
            if (CustomInputActions.TryGetValue(bindingName, out var tuple))
            {
                tuple.Item3.Disable();
                tuple.Item3.Dispose();
                CustomInputActions.Remove(bindingName);

                SaveCustomBindings();
            }
            else
            {
                Debug.LogError($"{bindingName} Not Found In List");
            }
        }

        void SaveCustomBindings()
        {
            string bindingPath = Path.Combine(Application.dataPath, "..", "bindings.ini");

            StringBuilder sb = new StringBuilder();

            foreach (var pair in CustomInputActions)
            {
                sb.Append($"{pair.Key} {pair.Value.Item2} {pair.Value.Item1}\n");
            }

            var stream = File.Open(bindingPath, FileMode.Create, FileAccess.Write);

            using (StreamWriter streamWriter = new StreamWriter(stream))
            {
                streamWriter.Write(sb.ToString());
            }

            stream.Dispose();
        }

        void LoadCustomBindings()
        {
            string bindingPath = Path.Combine(Application.dataPath, "..", "bindings.ini");

            if (File.Exists(bindingPath))
            {
                using (StreamReader streamReader = new StreamReader(bindingPath))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        string[] args = line.Split(" ");

                        if (args.Length >= 3)
                        {
                            string n = args[0];

                            string key = args[1];

                            string command = String.Empty;

                            for (int i = 2; i < args.Length; i++)
                            {
                                string a = args[i];
                                if (i + 1 < args.Length) a += " ";
                                command += a;
                            }

                            var customAction = new InputAction(args[0], InputActionType.Button, $"<keyboard>/{args[1]}");

                            customAction.performed += _ =>
                            {
                                if (!FunctionUtils.IsBlocked())
                                    DeveloperConsoleUI.Instance.DeveloperConsole.ProcessCommand(command);
                            };

                            customAction.Enable();

                            AddCustomBinding(n, command, key, customAction);
                        }
                        else
                        {
                            Debug.LogError($"Failed to load custom bindings: {line}");
                        }
                    }

                    streamReader.Dispose();
                }
            }

        }

        private void OnPersonaStateChangedCall(PersonaStateChange_t param)
        {
            if (FriendManager.Friends.TryGetValue((CSteamID)param.m_ulSteamID, out var friend))
            {
                switch (param.m_nChangeFlags)
                {
                    case EPersonaChange.k_EPersonaChangeStatus:
                        friend.RefreshState();
                        break;
                    case EPersonaChange.k_EPersonaChangeRichPresence:
                        friend.RefreshRichPresence();
                        break;
                }
            }
            OnPersonaStateChanged?.Invoke((CSteamID)param.m_ulSteamID, param.m_nChangeFlags);
        }

        public Bloom GetBloom()
        {
            return _bloom;
        }

        public DepthOfField GetDepthOfField()
        {
            return _depthOfField;
        }

        private float desiredEarVolume = 0f;
        public AudioClip earRingingClip;

        public void HitSomeOne()
        {
            hitRecently = true;
            CancelInvoke(nameof(ClearHit));
            Invoke(nameof(ClearHit), 1f);
        }

        void ClearHit()
        {
            hitRecently = false;
        }
        public Texture2D GetUpgradeDetailedTexture2D(string upgradeName)
        {
            foreach (var upgrade in upgradeDetaileds)
            {
                if (upgrade.name == upgradeName)
                {
                    return upgrade.texture2D;
                }
            }

            return null;
        }
        public int GetUpgradeDetailedLength(string upgradeName)
        {
            foreach (var upgrade in upgradeDetaileds)
            {
                if (upgrade.name == upgradeName)
                {
                    return upgrade.maxIndex;
                }
            }

            return 0;
        }
        public int GetUpgradeDetailedCost(string upgradeName)
        {
            foreach (var upgrade in upgradeDetaileds)
            {
                if (upgrade.name == upgradeName)
                {
                    return upgrade.cost;
                }
            }

            return 0;
        }

        public UpgradeDetailed GetUpgradeDetailed(string upgradeKey)
        {
            foreach (var upgrade in upgradeDetaileds)
            {
                if (upgrade.name == upgradeKey)
                {
                    return upgrade;
                }
            }

            return null;
        }
        void SaveInventory()
        {
            SaveSystem.SaveToJSON(InventoryManager.Instance.cosmeticIndex, "inventory.json");
        }
        IEnumerator LoadInventory()
        {
            if (File.Exists(SaveSystem.GetPath("inventory.json")))
            {
                CoroutineWithData cd = new CoroutineWithData(this, SaveSystem.ReadFileAsyncThread(SaveSystem.GetPath("inventory.json")));

                yield return cd.coroutine;
                InventoryManager.CosmeticIndex c;
                try
                {
                    c = JsonConvert.DeserializeObject<InventoryManager.CosmeticIndex>(cd.result.ToString());

                }
                catch (JsonException e)
                {
                    // Code to execute if deserialization fails
                    c = new InventoryManager.CosmeticIndex();
                    Debug.LogError("Error while deserializing inventory JSON: " + e.Message);
                    NetworkManager.Instance.currentGroup = CSteamID.Nil;
                }

                if (c != null)
                {
                    if (c.ids.Length < 8)
                    {
                        c.ids = new ulong[8];

                        c.hatColor = Color.clear;
                        c.faceColor = Color.clear;
                        c.clothesColor = Color.clear;
                        c.hairColor = Color.clear;
                        c.shoesColor = Color.clear;
                        c.pantColor = Color.clear;

                        c.hatIndex = -1;
                        c.faceIndex = -1;
                        c.clothesIndex = -1;
                        c.hairIndex = -1;
                        c.shoesIndex = -1;
                        c.pantIndex = -1;

                        c.hatShiny = 0;
                        c.faceShiny = 0;
                        c.clothesShiny = 0;
                        c.hairShiny = 0;
                        c.shoesShiny = 0;
                        c.pantShiny = 0;

                        c.hatParticle = 0;
                        c.faceParticle = 0;
                        c.clothesParticle = 0;
                        c.hairParticle = 0;
                        c.shoesParticle = 0;
                        c.pantParticle = 0;

                        c.musicBoxIndex = 0;
                        c.menuSceneIndex = 0;
                        SaveInventory();
                    }
                    if (c.weaponIds.Length != 30) c.weaponIds = new ulong[30];
                    if (c.weaponIndex.Length != 30) c.weaponIndex = new ushort[30];
                    InventoryManager.Instance.cosmeticIndex = c;

                }
            }

            InventoryManager.Instance.TryToSerializeItem();
        }
        void SaveSetting()
        {
            // Record the version so a migrated setting is not upgraded again on the next launch,
            // which would override a windowed mode the player has chosen since.
            setting.settingsVersion = Setting.CurrentSettingsVersion;
            SaveSystem.SaveToJSON(setting, "setting.json");
        }

        private SettingMenu _settingMenu;
        private static bool loaded = false;
        public IEnumerator LoadSetting()
        {
            if (File.Exists(SaveSystem.GetPath("setting.json")))
            {
                CoroutineWithData cd = new CoroutineWithData(this,
                    SaveSystem.ReadFileAsyncThread(SaveSystem.GetPath("setting.json")));

                yield return cd.coroutine;

                try
                {
                    Setting s = JsonConvert.DeserializeObject<Setting>(cd.result.ToString());

                    if (s != null)
                    {
                        setting = s;
                        setting.useArm = false;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while deserializing inventory JSON: " + e.Message);
                }
            }

            // Upgrades a setting.json written by a build that still defaulted to a bordered,
            // non-maximised window, and persists the upgrade so it only happens once.
            if (setting.Migrate())
                settingChanged = true;

            OnSettingLoaded?.Invoke(setting);
            SettingsLoaded = true;

            if (setting.useSteamLanguage)
            {
                if (SteamManager.Initialized)
                {
                    string language = SteamApps.GetCurrentGameLanguage();

                    switch (language)
                    {
                        case "english":
                            LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[1]);
                            break;
                        case "schinese":
                            LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[0]);
                            break;
                        case "russian":
                            LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[3]);
                            break;
                    }
                }
            }

            _bloom.active = setting.bloom;

            MusicManager.Instance.volumeMultiplier = setting.musicVolume;

            QualitySettings.SetQualityLevel(setting.quality, true);
            UniversalRenderPipelineAsset urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            QualitySettings.antiAliasing = setting.antiAliasing * 2;
            urp.shadowCascadeCount = setting.shadowCascades;
            urp.shadowDistance = setting.shadowDistance * 40;
            QualitySettings.softParticles = setting.softParticle;
            // Screen.fullScreen and Screen.fullScreenMode both write the same window state, and
            // assigning Screen.fullScreen resets the mode, so the two fields are resolved into a
            // single mode and applied through one SetResolution call in ApplyDisplay. That also
            // repairs the old fullScreenMode default of 3 (Windowed), which launched the game as
            // a bordered, non-maximised window even though the fullscreen toggle was on, and it
            // applies the display's own resolution when resolutionIndex is -1. That branch used
            // to only look up an index without ever calling SetResolution, so the native
            // resolution was never applied on a first run.
            setting.ApplyDisplay();

            QualitySettings.vSyncCount = setting.vSync;
            Application.targetFrameRate = (int)setting.maxFps;
            QualitySettings.masterTextureLimit = 3 - setting.textureIndex;

            AudioManager.Instance.SetMasterVolume(setting.volume);
            AudioManager.Instance.SetSoundEffectVolume(setting.soundEffectVolume);
            AudioManager.Instance.SetAmbienceVolume(setting.ambienceVolume);
            AudioManager.Instance.SetUIVolume(setting.uiVolume);
        }

        public bool knewDead = false;
        public void Dead()
        {
            knewDead = true;
            Invoke(nameof(ClearDead), 3f);
        }

        void ClearDead()
        {
            knewDead = false;
        }
        public static void StartRebind(string actionName, int bindingIndex, TextMeshProUGUI text)
        {
            EventSystem.current.SetSelectedGameObject(null);
            InputAction action = InputManager.asset.FindAction(actionName);
            if (action == null || action.bindings.Count <= bindingIndex) return;

            if (action.bindings[bindingIndex].isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isComposite)
                    Rebind(action, bindingIndex, text, true);
            }
            else Rebind(action, bindingIndex, text, false);
        }

        private static void Rebind(InputAction action, int bindingIndex, TextMeshProUGUI text, bool allCompositeParts)
        {
            if (action == null || bindingIndex < 0) return;
            string t = UIManager.IsItChinese()
                ? $"按下一个 {action.expectedControlType}"
                : $"Press a Key";
            text.SetText(t);

            action.Disable();

            var rebind = action.PerformInteractiveRebinding(bindingIndex);

            rebind.OnComplete(operation =>
            {
                action.Enable();
                operation.Dispose();

                if (allCompositeParts)
                {
                    var nextBindingIndex = bindingIndex + 1;
                    if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isComposite)
                    {
                        Rebind(action, nextBindingIndex, text, true);
                    }
                }

                SaveBindingOverride(action);
                RebindComplete?.Invoke();
            });

            rebind.OnCancel(operation =>
            {
                action.Enable();
                operation.Dispose();
                RebindCanceled?.Invoke();
            });

            rebind.WithCancelingThrough("<keyboard>/escape");

            RebindStarted?.Invoke(action, bindingIndex);
            rebind.Start();
        }

        public static string GetBindingName(string actionName, int bindingIndex)
        {
            InputManager ??= new InputManager();

            InputAction action = InputManager.asset.FindAction(actionName);
            return action.GetBindingDisplayString(bindingIndex);
        }

        private static void SaveBindingOverride(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                PlayerPrefs.SetString(action.actionMap + action.name + i, action.bindings[i].overridePath);
            }
        }

        public static void LoadBindingOverride(string actionName)
        {
            InputManager ??= new InputManager();

            InputAction action = InputManager.asset.FindAction(actionName);

            for (int i = 0; i < action.bindings.Count; i++)
            {
                string path = PlayerPrefs.GetString(action.actionMap + action.name + i);
                if (!string.IsNullOrEmpty(path))
                    action.ApplyBindingOverride(i, path);
            }
        }

        public bool CanInput()
        {
            return !Chat.Instance.IsChat() && !DemoManager.Replaying;
        }

        public void SetExposure(float a)
        {

            desiredEarVolume = 1f;

            StopAllCoroutines();
            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.gameGroup.alpha = 0;
                GameUIManager.Instance.desiredGameAlpha = 0;

                StartCoroutine(GoBlind(a));
            }
        }

        public LayerMask serverPlayer, flashBangHitLayer;
        IEnumerator GoBlind(float a)
        {
            yield return new WaitForEndOfFrame();

            int width = Screen.width;
            int height = Screen.height;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            GameUIManager.Instance.afterImage.texture = tex;


            GameUIManager.Instance.afterImage.color = new Color(1f, 1f, 1f, 0f);

            GameUIManager.Instance.desiredFlashAlpha = 0f;
            _adjustments.postExposure.value = a;
            desiredExposure = a;

            yield return new WaitForSeconds(0.65f);

            desiredExposure = 0;
            desiredEarVolume = 0f;
            while (_adjustments.postExposure.value > 4f)
            {
                yield return null;
            }

            GameUIManager.Instance.afterImage.color = new Color(1f, 1f, 1f, 0.2f);
            if (!GameUIManager.Instance.controlByConsole)
                GameUIManager.Instance.desiredGameAlpha = 1f;
        }

        internal float desiredExposure = 0f;

        private static readonly float baseDis = 10f;
        public void CameraShake3D(float mag, float rough, float fadeIn, float fadeOut, Vector3 pos)
        {
            if (CameraShaker.Instance)
            {
                float p = (baseDis / Vector3.Distance(CameraShaker.Instance.transform.position, pos));
                CameraShaker.Instance.ShakeOnce(mag * p, rough * p, fadeIn, fadeOut);
            }
        }

        private void Update()
        {
            if (_adjustments != null)
            {
                _adjustments.postExposure.value =
                    Mathf.Lerp(_adjustments.postExposure.value, desiredExposure, Time.deltaTime);
            }
            else
            {

                _adjustments = (ColorAdjustments)volume.components[4];
            }

            if (earRinging)
            {
                earRinging.volume = Mathf.Lerp(earRinging.volume, desiredEarVolume, Time.deltaTime);
            }
        }

        public static bool groupChanged = false, perkChanged = false, voiceLineChanged = false, throwableChanged = false, inventoryChanged = false, settingChanged = false;
        private void OnApplicationQuit()
        {
            _adjustments.postExposure.value = 0;
            if (settingChanged)
                SaveSetting();
            if (inventoryChanged)
                SaveInventory();
            if (perkChanged)
                SaveSystem.SaveToJSON(PerkManager.Instance.perks, "Perks.json");
            if (groupChanged)
                SaveSystem.SaveToJSON(NetworkManager.Instance.currentGroup, "group.json");
            if (voiceLineChanged)
                VoiceLine.Instance.StoreData();
            if (throwableChanged)
                PlayerPrefs.SetInt("throwObjects", (int)tacticalProp);
            if (throwableChanged)
                PlayerPrefs.Save();
        }

        private AudioSource earRinging;

        [Serializable]
        public class PowerDetail
        {
            public PowerType type;
            public string key;
            public Texture2D texture2D;
        }

        public List<PowerDetail> powerDetails = new List<PowerDetail>();

        public PowerDetail GetPowerDetail(PowerType n)
        {
            foreach (var detail in powerDetails)
            {
                if (detail.type == n)
                {
                    return detail;
                }
            }

            return null;
        }

        public static string GetObjectSize(long length, bool fullName = false)
        {
            StringBuilder afterFix = new StringBuilder();

            int times = 0;
            while (length / 1024 > 0)
            {
                length /= 1024;
                times++;
            }

            switch (times)
            {
                case 0:
                    afterFix.Append(fullName ? "Byte" : "B");
                    break;
                case 1:
                    afterFix.Append(fullName ? "Kilobyte" : "KB");
                    break;
                case 2:
                    afterFix.Append(fullName ? "Megabyte" : "MB");
                    break;
                case 3:
                    afterFix.Append(fullName ? "Gigabyte" : "GB");
                    break;
                case 4:
                    afterFix.Append(fullName ? "Terabyte" : "TB");
                    break;
            }

            return length + " " + afterFix;
        }

        public bool CheckAdminOrHelper() // True - break
        {
            CSteamID steamID = SteamUser.GetSteamID();
            return RolesManager.Instance.CheckIsAdmin(steamID.m_SteamID) || RolesManager.Instance.CheckIsHelper(steamID.m_SteamID);
        }

        public static DateTime JavaTimeStampToDateTime(uint javaTimeStamp)
        {
            // Java timestamp is milliseconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(javaTimeStamp);
            return dateTime;
        }

        #region Command Line

        void CheckCommandLineArgs()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(arguments[i]))
                    continue;

                string key = arguments[i].Trim().ToLower();

                StringBuilder sb = new StringBuilder();

                if (key.StartsWith(HasArgPrefix))
                {
                    key = key.Substring(1, key.Length - 1);

                    //Get the arg
                    for (int j = i + 1; j < arguments.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(arguments[j]))
                            continue;

                        string value = arguments[j].Trim();

                        if (value.StartsWith(HasArgPrefix) || value.StartsWith(HasNoArgPrefix))
                            break;

                        sb.Append(value);
                        sb.Append(" ");
                    }

                    ParseArgument(key, sb.Length == 0 ? String.Empty : sb.ToString().Trim());
                }
            }
        }

        void ParseArgument(string key, string value)
        {
            if (String.CompareOrdinal(key, "fps") == 0)
            {
                bool flag = value == "1";

                Fps.Instance.enable = flag;
            }
            else if (String.CompareOrdinal(key, "network") == 0)
            {
                bool flag = value == "1";

                Fps.Instance.enableNetworkingStats = flag;
            }
            else if (String.CompareOrdinal(key, "memory") == 0)
            {
                bool flag = value == "1";

                Fps.Instance.enableMemoryStatics = flag;
            }
        }
        #endregion
    }

}
