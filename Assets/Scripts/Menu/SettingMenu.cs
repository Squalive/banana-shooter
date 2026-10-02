using System;
using System.Collections.Generic;
using System.Linq;
using Audio;

using Manager;
using Movement;
using Multiplayer;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Weapon;
using ShadowQuality = UnityEngine.ShadowQuality;
using ShadowResolution = UnityEngine.ShadowResolution;

namespace Menu
{
    public class SettingMenu : MonoBehaviour
    {
        public static SettingMenu Instance;

        [Header("FOV")] 
        [SerializeField] private Slider fovSlider;
        [SerializeField] private TextMeshProUGUI fovText;
    
        [Header("Sensitivity")] 
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TextMeshProUGUI sensitivityText;
        
        [Header("Sensitivity")] 
        [SerializeField] private Slider aimSensitivityMultiplierSlider;
        [SerializeField] private TextMeshProUGUI aimSensitivityMultiplierText;
    
        [Header("Camera Shake")] 
        [SerializeField] private Toggle cameraShakeToggle;

        [Header("Shadow Quality")]
        [SerializeField] private Button shadowQualityNext,shadowQualityPreview;
        [SerializeField] private TextMeshProUGUI shadowQualityText;
        private int shadowQualityIndex = 0;
    
        [Header("Shadow Resolution")]
        [SerializeField] private Button shadowResolutionNext,shadowResolutionPreview;
        [SerializeField] private TextMeshProUGUI shadowResolutionText;
        private int shadowResolutionIndex = 0;
    
        [Header("Quality")]
        [SerializeField] private Button qualityNext,qualityPreview;
        [SerializeField] private TextMeshProUGUI qualityText;
        private int qualityIndex = 0;
    
        [Header("Shadow Distance")]
        [SerializeField] private Button shadowDistanceNext,shadowDistancePreview;
        [SerializeField] private TextMeshProUGUI shadowDistanceText;
        private int shadowDistanceIndex = 0;
    
        [Header("Shadow Cascades")]
        [SerializeField] private Button shadowCascadesNext,shadowCascadesPreview;
        [SerializeField] private TextMeshProUGUI shadowCascadesText;
        private int shadowCascadesIndex = 0;
    
        [Header("Anti Aliasing")]
        [SerializeField] private Button antiAliasingNext,antiAliasingPreview;
        [SerializeField] private TextMeshProUGUI antiAliasingText;
        private int antiAliasingIndex = 0;
    
        [Header("Soft Particle")]
        [SerializeField] private Toggle softParticle;
    
        [Header("Language")] public Button languageNext,languagePreview;
        [SerializeField] private TextMeshProUGUI languageText;
        private int languageIndex = 0;
    
        [Header("Resolution")]
        [SerializeField] private TMP_Dropdown resolution;
        private Resolution[] resolutions;
    
        [Header("Full Screen")]
        [SerializeField] private Toggle fullScreen;
    
        [Header("Full Screen Mode")]
        [SerializeField] private Button fullScreenModeNext,fullScreenModePreview;
        [SerializeField] private TextMeshProUGUI fullScreenModeText;
        private int fullScreenModeIndex = 0;
    
        [Header("Vsync")]
        [SerializeField] private Button vsyncNext,vsyncPreview;
        [SerializeField] private TextMeshProUGUI vsyncText;
        private int vsyncIndex = 0;
    
        [Header("Max Fps")] 
        [SerializeField] private Slider maxFpsSlider;
        [SerializeField] private TextMeshProUGUI maxFpsText;
    
        [Header("Master Volume")] 
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeText;

        [Header("Description")] 
        [SerializeField]
        private TMP_InputField description;
    
        [Header("Spawn Particle")] 
        [SerializeField] private Toggle spawnParticleToggle;
    
        [Header("HitMarkerType")]
        [SerializeField] private Button hitMarkerNext,hitMarkerPreview;
        [SerializeField] private TextMeshProUGUI hitMarkerText;
        private int hitMarkerIndex = 0;
    
        [Header("UseArm")] 
        [SerializeField] private Toggle useArmToggle;
    
        [Header("Disable Voice")] 
        [SerializeField] private Toggle disableVoiceToggle;
    
        [Header("UI Volume")] 
        [SerializeField] private Slider uiVolumeSlider;
        [SerializeField] private TextMeshProUGUI uiVolumeText;
    
        [Header("Ambience Volume")] 
        [SerializeField] private Slider ambienceVolumeSlider;
        [SerializeField] private TextMeshProUGUI ambienceVolumeText;
    
        [Header("Sound Effect Volume")] 
        [SerializeField] private Slider soundEffectVolumeSlider;
        [SerializeField] private TextMeshProUGUI soundEffectVolumeText;
    
        [Header("Music Volume")] 
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicVolumeText;
    
        [Header("Show Direction")] 
        [SerializeField] private Toggle showDirToggle;
    
        [Header("CrossHair")] 
        [SerializeField] private Slider widthSlider;
        [SerializeField] private TextMeshProUGUI widthText;
        [SerializeField] private Slider heightSlider;
        [SerializeField] private TextMeshProUGUI heightText;
        [SerializeField] private Slider distanceSlider;
        [SerializeField] private TextMeshProUGUI distanceText;
        public RectTransform left, up, right, bottom,dotTransform;
        public RawImage[] rawImages;
        [SerializeField] private Toggle dotToggle;
    
        [SerializeField] private Slider rSlider,gSlider,bSlider;
        [SerializeField] private TextMeshProUGUI rText,gText,bText;
        public GameObject dot;
    
        [Header("Texture Resolution")]
        [SerializeField] private Button textureNext,texturePreview;
        [SerializeField] private TextMeshProUGUI textureText;
        private int textureIndex = 0;
    
        [Header("Banana Voice")] 
        [SerializeField] private Toggle bananaVoiceToggle;
    
        [Header("Keep Ragdoll")] 
        [SerializeField] private Toggle keepRagdollToggle;
    
        [Header("Bloom")] 
        [SerializeField] private Toggle bloomToggle;
    
        [Header("AutoUpgrade")] 
        [SerializeField] private Toggle autoUpgradeToggle;
    
        [Header("UseNightVission")] 
        [SerializeField] private Toggle useNightVissionToggle;
    
        [Header("Sway Multiplier")] 
        [SerializeField] private Slider swaySlider;
        [SerializeField] private TextMeshProUGUI swayText;
    
        [Header("GrappleHint")] 
        [SerializeField] private Toggle grappleHintToggle;

        [Header("Enable Gore")] 
        [SerializeField] private Toggle goreToggle;
    
        [Header("Enable Tutorial")] 
        [SerializeField] private Toggle tutorialToggle;
    
        [Header("Enable Upgrade Animation")] 
        [SerializeField] private Toggle upgradeAnimationToggle;
        private void Awake()
        {
            Instance = this;

            dotTransform = dot.GetComponent<RectTransform>();
        }

        private void Start()
        {
            foreach (var item in items)
            {
                item.Init();
            }
            fovSlider.onValueChanged.AddListener(SetFOV);
            maxFpsSlider.onValueChanged.AddListener(SetMaxFps);
            swaySlider.onValueChanged.AddListener(SetSwayMultiplier);
        
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            uiVolumeSlider.onValueChanged.AddListener(SetUIVolume);
            ambienceVolumeSlider.onValueChanged.AddListener(SetAmbienceVolume);
            soundEffectVolumeSlider.onValueChanged.AddListener(SetSoundEffectVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        
            widthSlider.onValueChanged.AddListener(SetWidth);
            heightSlider.onValueChanged.AddListener(SetHeight);
            distanceSlider.onValueChanged.AddListener(SetDistance);
        
            dotToggle.onValueChanged.AddListener(SetCrossHairDot);
            widthSlider.maxValue = 100;
            heightSlider.maxValue = 100;
            distanceSlider.maxValue = 300;
        
            rSlider.onValueChanged.AddListener(SetR);
            bSlider.onValueChanged.AddListener(SetB);
            gSlider.onValueChanged.AddListener(SetG);
        
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            aimSensitivityMultiplierSlider.onValueChanged.AddListener(SetAimSensitivityMultiplierSlider);
            
            cameraShakeToggle.onValueChanged.AddListener(SetCameraShake);
            useArmToggle.onValueChanged.AddListener(SetUseArm);
            disableVoiceToggle.onValueChanged.AddListener(delegate(bool arg0) { SetDisableVoice(arg0, true);});
            showDirToggle.onValueChanged.AddListener(delegate(bool arg0) { SetShowDir(arg0, true);});
            autoUpgradeToggle.onValueChanged.AddListener(delegate(bool arg0) { SetAutoUpgrade(arg0, true);});
        
            grappleHintToggle.onValueChanged.AddListener(delegate(bool arg0) { SetGrappleHint(arg0, true);});
        
            goreToggle.onValueChanged.AddListener(delegate(bool arg0) { SetGore(arg0, true);});
            tutorialToggle.onValueChanged.AddListener(delegate(bool arg0) { SetTutorial(arg0, true);});
            upgradeAnimationToggle.onValueChanged.AddListener(delegate(bool arg0) { SetUpgradeAniamtion(arg0, true);});

            keepRagdollToggle.onValueChanged.AddListener(delegate(bool arg0) { SetKeepRagdoll(arg0, true);});
            bloomToggle.onValueChanged.AddListener(delegate(bool arg0) { SetBloom(arg0, true);});
            useNightVissionToggle.onValueChanged.AddListener(delegate(bool arg0) { SetNightVission(arg0, true);});
        
            bananaVoiceToggle.onValueChanged.AddListener(delegate(bool arg0) { SetBananaVoice(arg0, true);});
            description.onValueChanged.AddListener(SetDescription);
        
            hitMarkerNext.onClick.AddListener(delegate { NextHitMarkerType(1); });
            hitMarkerPreview.onClick.AddListener(delegate { NextHitMarkerType(-1); });
        
            shadowQualityNext.onClick.AddListener(delegate { NextShadowQuality(1); });
            shadowQualityPreview.onClick.AddListener(delegate { NextShadowQuality(-1); });
        
        
            shadowResolutionNext.onClick.AddListener(delegate { NextShadowResolution(1); });
            shadowResolutionPreview.onClick.AddListener(delegate { NextShadowResolution(-1); });
        
            qualityNext.onClick.AddListener(delegate { NextQuality(1); });
            qualityPreview.onClick.AddListener(delegate { NextQuality(-1); });
        
            shadowDistanceNext.onClick.AddListener(delegate { NextShadowDistance(1); });
            shadowDistancePreview.onClick.AddListener(delegate { NextShadowDistance(-1); });
        
            shadowCascadesNext.onClick.AddListener(delegate { NextShadowCascade(1); });
            shadowCascadesPreview.onClick.AddListener(delegate { NextShadowCascade(-1); });
        
            antiAliasingNext.onClick.AddListener(delegate { NextAntiAliasing(1); });
            antiAliasingPreview.onClick.AddListener(delegate { NextAntiAliasing(-1); });
        
            softParticle.onValueChanged.AddListener(delegate(bool arg0) { SetSoftParticle(arg0, true);});
            spawnParticleToggle.onValueChanged.AddListener(SetSpawnParticle);
        
            languageNext.onClick.AddListener(delegate { NextLanguage(1); });
            languagePreview.onClick.AddListener(delegate { NextLanguage(-1); });

            bool flag = GameManager.Instance.setting.useSteamLanguage;
            languageNext.interactable = !flag;
            languagePreview.interactable = !flag;

            vsyncNext.onClick.AddListener(delegate { NextVsync(1); });
            vsyncPreview.onClick.AddListener(delegate { NextVsync(-1); });
        
            fullScreen.onValueChanged.AddListener(delegate(bool arg0) { SetFullScreen(arg0, true); });
        
            fullScreenModeNext.onClick.AddListener(delegate { NextFullScreenMode(1); });
            fullScreenModePreview.onClick.AddListener(delegate { NextFullScreenMode(-1); });
        
            texturePreview.onClick.AddListener(delegate { NextTextureResolution(-1); });
            textureNext.onClick.AddListener(delegate { NextTextureResolution(1); });
        
            searchBar.onValueChanged.AddListener(Search);

            Setting setting = GameManager.Instance.setting;
        
            SetSensitivityVisual(setting.sensitivity);
            SetAimSensitivityMultiplierVisual(setting.aimSensitivityMultiplier);
            SetCameraShakeVisual(setting.cameraShake);
            SetShadowQualityVisual(setting.shadowQuality);
            SetShadowResolutionVisual(setting.shadowResolution);
            SetFOVVisual(setting.fov);
            SetAntiAliasingVisual(setting.antiAliasing);
            SetShadowCascadeVisual(setting.shadowCascades);
            SetShadowDistanceVisual(setting.shadowDistance);
            SetSoftParticleVisual(setting.softParticle);
            SetFullScreenVisual(setting.fullScreen);
            SetFullScreenModeVisual(setting.fullScreenMode);
            InitResolution(setting.resolutionIndex);
            SetVsyncVisual(setting.vSync);
            SetMaxFpsVisual(setting.maxFps);
            SetSwayMultiplierVisual(setting.swayMultiplier);
            SetMasterVolumeVisual(setting.volume);
            SetUIVolumeVisual(setting.uiVolume);
            SetAmbienceVolumeVisual(setting.ambienceVolume);
            SetSoundEffectVolumeVisual(setting.soundEffectVolume);
            SetMusicVolumeVisual(setting.musicVolume);
            SetLanguage(setting.language,false);
            SetDescriptionVisual(setting.description);
            SetSpawnParticleVisual(setting.spawnParticle);
            SetHitMarkerTypeVisual((int)setting.hitMarkerType);
            SetUseArmVisual(setting.useArm);
            SetDisableVoiceVisual(setting.disableVoice);
            SetShowDirVisual(setting.showDir);
            SetGrappleHintVisual(setting.grappleHint);
            SetAutoUpgradeVisual(setting.autoUpgrade);
            SetKeepRagdollVisual(setting.keepRagdoll);
            SetBloomVisual(setting.bloom);
            SetNightVissionVisual(setting.useNightVission);
            SetBananaVoiceVisual(setting.disableBananaVoice);
            SetWidthVisual(setting.crossHairWidth);
            SetHeightVisual(setting.crossHairHeight);
            SetDistanceVisual(setting.crossHairDistance);
            SetCrossHairValue();
            SetRVisual(setting.csR);
            SetGVisual(setting.csG);
            SetBVisual(setting.csB);
        
            SetTextureResolutionVisual(setting.textureIndex);
            SetQualityVisual(setting.quality);
        
            SetGoreVisual(setting.enableGore);
            SetTutorialVisual(setting.enableTutorial);
            SetUpgradeAnimationVisual(setting.enableUpgradeAnimation);
        }

        void SetUpgradeAnimationVisual(bool arg0)
        {
            upgradeAnimationToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetUpgradeAniamtion(bool arg0, bool b)
        {
            GameManager.Instance.setting.enableUpgradeAnimation = arg0;
            if(b)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;
            SetUpgradeAnimationVisual(arg0);
        }

        void SetTutorialVisual(bool arg0)
        {
            tutorialToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetTutorial(bool arg0, bool b)
        {
            GameManager.Instance.setting.enableTutorial = arg0;
            if(b)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;
            SetTutorialVisual(arg0);
        }

        void SetGoreVisual(bool arg0)
        {
            goreToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetGore(bool arg0, bool b)
        {
            GameManager.Instance.setting.enableGore = arg0;
            if(b)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;
            SetGoreVisual(arg0);
        }


        private void SetCrossHairDot(bool arg0)
        {
            if (dotToggle.isOn != arg0)
                dotToggle.isOn = arg0;
            GameManager.Instance.setting.enableDot = arg0;
            if (CrossHair.Instance)
            {
                CrossHair.Instance.SetDot(arg0);
            }
            SetCrossHairValue();
        }

        void SetBananaVoiceVisual(bool arg0)
        {
            bananaVoiceToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetBananaVoice(bool arg0,bool playButton)
        {
            GameManager.Instance.setting.disableBananaVoice = arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();

            SetBananaVoiceVisual(arg0);
            GameManager.settingChanged = true;
        }

        void SetBloomVisual(bool arg0)
        {
            bloomToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetBloom(bool arg0,bool playButton)
        {
            GameManager.Instance.setting.bloom = arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();

            GameManager.Instance.GetBloom().active = arg0;
            GameManager.settingChanged = true;
            SetBloomVisual(arg0);
        }

        void SetNightVissionVisual(bool arg0)
        {
            useNightVissionToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetNightVission(bool arg0,bool playButton)
        {
            GameManager.Instance.setting.useNightVission = arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();
            if (WeaponManager.Instance)
            {
                WeaponManager.Instance.nightVission.SetActive(arg0 && WeaponManager.Instance.flashLight.activeSelf);
            }
            GameManager.settingChanged = true;
            SetNightVissionVisual(arg0);
        }

        void SetKeepRagdollVisual(bool arg0)
        {
            keepRagdollToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetKeepRagdoll(bool arg0,bool playButton)
        {
            GameManager.Instance.setting.keepRagdoll = arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();

            if (!arg0)
            {
                foreach (var ragdoll in PlayerRagdoll.ragdolls)
                {
                    Destroy(ragdoll.gameObject);
                }
                PlayerRagdoll.ragdolls.Clear();
            }
            GameManager.settingChanged = true;
            SetKeepRagdollVisual(arg0);
        }

        void SetAutoUpgradeVisual(bool arg0)
        {
            autoUpgradeToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetAutoUpgrade(bool arg0,bool playButton)
        {
            GameManager.Instance.setting.autoUpgrade = arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();
            if (UpgradeInGameMenu.Instance)
            {
                UpgradeInGameMenu.Instance.coin.gameObject.SetActive(!arg0);
            }
            GameManager.settingChanged = true;

            SetAutoUpgradeVisual(arg0);
        }

        void SetGrappleHintVisual(bool arg0)
        {
            grappleHintToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetGrappleHint(bool arg0,bool playButton)
        {
            GameManager.Instance.setting.grappleHint = arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();

            // if (GrappleHint.Instance)
            // {
            //     GrappleHint.Instance.show = arg0;
            //     GrappleHint.Instance.hint.localScale = Vector3.zero;
            // }
            GameManager.settingChanged = true;
            SetGrappleHintVisual(arg0);
        }

        void SetShowDirVisual(bool arg0)
        {
            showDirToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetShowDir(bool arg0,bool playButton)
        {
            GameManager.Instance.setting.showDir = arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();
            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.dir.SetActive(arg0);
            }
            GameManager.settingChanged = true;
            SetShowDirVisual(arg0);
        }

        void SetDisableVoiceVisual(bool arg0)
        {
            disableVoiceToggle.SetIsOnWithoutNotify(arg0);
        }
        void SetDisableVoice(bool arg0,bool playButton)
        {
            GameManager.Instance.setting.disableVoice = arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;
        }

        void SetHitMarkerTypeVisual(int arg0)
        {
            if (hitMarkerIndex != arg0) hitMarkerIndex = arg0;
        
            hitMarkerText.SetText(((HitMarkerType) arg0).ToString());
        }
        void SetHitMarkerType(int arg0,bool playButton)
        {
            GameManager.Instance.setting.hitMarkerType = (HitMarkerType)arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();
        
            GameManager.settingChanged = true;
            SetHitMarkerTypeVisual(arg0);
        }

        void NextHitMarkerType(int index)
        {
            hitMarkerIndex += index;
            if (hitMarkerIndex > 1) hitMarkerIndex = 0;
            if (hitMarkerIndex<0) hitMarkerIndex = 1;
        
            SetHitMarkerType(hitMarkerIndex,true);
        }

        void SetMasterVolumeVisual(float arg0)
        {
            if(Math.Abs(masterVolumeSlider.value - arg0) > 0.1f)
                masterVolumeSlider.SetValueWithoutNotify(arg0);
        
            masterVolumeText.SetText(arg0.ToString("F2"));
        }
        void SetMasterVolume(float arg0)
        {
            GameManager.Instance.setting.volume = arg0;
            AudioManager.Instance.SetMasterVolume(arg0);

            SetMasterVolumeVisual(arg0);
            GameManager.settingChanged = true;
        }

        void SetUIVolumeVisual(float arg0)
        {
            if(Math.Abs(uiVolumeSlider.value - arg0) > 0.1f)
                uiVolumeSlider.SetValueWithoutNotify(arg0);
        
            uiVolumeText.SetText(arg0.ToString("F2"));
        }
        void SetUIVolume(float arg0)
        {
            GameManager.Instance.setting.uiVolume = arg0;
            AudioManager.Instance.SetUIVolume(arg0);

            SetUIVolumeVisual(arg0);
            GameManager.settingChanged = true;
        }

        void SetAmbienceVolumeVisual(float arg0)
        {
            if(Math.Abs(ambienceVolumeSlider.value - arg0) > 0.1f)
                ambienceVolumeSlider.SetValueWithoutNotify(arg0);
        
            ambienceVolumeText.SetText(arg0.ToString("F2"));
        }
        void SetAmbienceVolume(float arg0)
        {
            GameManager.Instance.setting.ambienceVolume = arg0;
            AudioManager.Instance.SetAmbienceVolume(arg0);
            GameManager.settingChanged = true;
            SetAmbienceVolumeVisual(arg0);
        }

        void SetSoundEffectVolumeVisual(float arg0)
        {
            if(Math.Abs(soundEffectVolumeSlider.value - arg0) > 0.1f)
                soundEffectVolumeSlider.SetValueWithoutNotify(arg0);
        
            soundEffectVolumeText.SetText(arg0.ToString("F2"));
        }
        void SetSoundEffectVolume(float arg0)
        {
            GameManager.Instance.setting.soundEffectVolume = arg0;
            AudioManager.Instance.SetSoundEffectVolume(arg0);
            GameManager.settingChanged = true;
            SetSoundEffectVolumeVisual(arg0);
        }

        void SetMusicVolumeVisual(float arg0)
        {
            if(Math.Abs(musicVolumeSlider.value - arg0) > 0.1f)
                musicVolumeSlider.value = arg0;
        
            musicVolumeText.SetText(arg0.ToString("F2"));
        }
        void SetMusicVolume(float arg0)
        {
            MusicManager.Instance.volumeMultiplier = arg0;
            GameManager.Instance.setting.musicVolume = arg0;
        
            GameManager.settingChanged = true;
            SetMusicVolumeVisual(arg0);
        }

        void SetMaxFpsVisual(float fps)
        {
            if(Math.Abs(maxFpsSlider.value - fps) > 0.1f)
                maxFpsSlider.SetValueWithoutNotify(fps);
            maxFpsText.SetText(fps.ToString("F0"));
        }
        void SetMaxFps(float fps)
        {
            GameManager.Instance.setting.maxFps = fps;
            Application.targetFrameRate = (int)fps;
            GameManager.settingChanged = true;
            SetMaxFpsVisual(fps);
        }

        void SetSwayMultiplierVisual(float fps)
        {
            if(Math.Abs(swaySlider.value - fps) > 0.1f)
                swaySlider.SetValueWithoutNotify(fps);
            swayText.SetText(fps.ToString("F1"));
        }
        void SetSwayMultiplier(float fps)
        {
            GameManager.Instance.setting.swayMultiplier = fps;
            GameManager.settingChanged = true;
            SetSwayMultiplierVisual(fps);
        }

        void SetWidthVisual(float arg)
        {
            if(Math.Abs(widthSlider.value - arg) > 0.1f)
                widthSlider.SetValueWithoutNotify(arg);
            widthText.SetText(arg.ToString("F1"));
        }
        void SetWidth(float arg)
        {
            GameManager.Instance.setting.crossHairWidth = arg;

            if (CrossHair.Instance)
            {
                CrossHair.Instance.SetWidth(arg);
            }
            SetCrossHairValue();
            GameManager.settingChanged = true;

            SetWidthVisual(arg);
        }

        void SetHeightVisual(float arg)
        {
            if(Math.Abs(heightSlider.value - arg) > 0.1f)
                heightSlider.SetValueWithoutNotify(arg);
            heightText.SetText(arg.ToString("F1"));
        
        }
        void SetHeight(float arg)
        {
            GameManager.Instance.setting.crossHairHeight = arg;
            if (CrossHair.Instance)
            {
                CrossHair.Instance.SetHeight(arg);
            }

            SetCrossHairValue();

            SetHeightVisual(arg);
            GameManager.settingChanged = true;
        }

        void SetDistanceVisual(float arg)
        {
            if(Math.Abs(distanceSlider.value - arg) > 0.1f)
                distanceSlider.SetValueWithoutNotify(arg);
            distanceText.SetText(arg.ToString("F1"));
        
        }
        void SetDistance(float arg)
        {
            GameManager.Instance.setting.crossHairDistance = arg;
            if (CrossHair.Instance)
            {
                CrossHair.Instance.SetDistance(arg);
            }
            SetCrossHairValue();

            SetDistanceVisual(arg);
            GameManager.settingChanged = true;
        }

        void SetRVisual(float arg)
        {
            if(Math.Abs(rSlider.value - arg) > 0.1f)
                rSlider.SetValueWithoutNotify(arg);
            rText.SetText(arg.ToString("F0"));
        
        }
        void SetR(float arg)
        {
            GameManager.Instance.setting.csR = arg;
            if (CrossHair.Instance)
            {
                CrossHair.Instance.SetColor(new Color(arg/255f,GameManager.Instance.setting.csG/255f,GameManager.Instance.setting.csB/255f));
            }
            SetCrossHairValue();
            GameManager.settingChanged = true;
            SetRVisual(arg);
        }

        void SetBVisual(float arg)
        {
            if(Math.Abs(bSlider.value - arg) > 0.1f)
                bSlider.SetValueWithoutNotify(arg);
            bText.SetText(arg.ToString("F0"));
        
        }
        void SetB(float arg)
        {
            GameManager.Instance.setting.csB = arg;
            if (CrossHair.Instance)
            {
                CrossHair.Instance.SetColor(new Color(GameManager.Instance.setting.csR/255f,GameManager.Instance.setting.csG/255f,arg/255f));
            }
            SetCrossHairValue();
            GameManager.settingChanged = true;
            SetBVisual(arg);
        }

        void SetGVisual(float arg)
        {
            if(Math.Abs(gSlider.value - arg) > 0.1f)
                gSlider.SetValueWithoutNotify(arg);
            gText.SetText(arg.ToString("F0"));
        
        }
        void SetG(float arg)
        {
            GameManager.Instance.setting.csG = arg;
            if (CrossHair.Instance)
            {
                CrossHair.Instance.SetColor(new Color(GameManager.Instance.setting.csR/255f,arg/255f,GameManager.Instance.setting.csB/255f));
            }
            SetCrossHairValue();
            GameManager.settingChanged = true;
            SetGVisual(arg);
        }
        void SetCrossHairValue()
        {
            float costant = 0;
            float distance = GameManager.Instance.setting.crossHairDistance;
            float width = GameManager.Instance.setting.crossHairWidth;
            float height = GameManager.Instance.setting.crossHairHeight;
            left.anchoredPosition = new Vector2(costant - distance / 2, 0);
            up.anchoredPosition = new Vector2(0, costant - distance / 2);
            right.anchoredPosition = new Vector2(costant + distance / 2, 0);
            bottom.anchoredPosition = new Vector2(0, costant + distance / 2);

            left.sizeDelta = new Vector2(width, height);
            right.sizeDelta = new Vector2(width, height);
            up.sizeDelta = new Vector2(height, width);
            bottom.sizeDelta = new Vector2(height, width);

            if(dotTransform!=null)
                dotTransform.sizeDelta = new Vector2(height, height);

            Color color = new Color(GameManager.Instance.setting.csR/255f,  GameManager.Instance.setting.csG/255f, GameManager.Instance.setting.csB/255f);
            foreach (var i in rawImages)
            {
                i.color = color;
            }
            dot.SetActive(GameManager.Instance.setting.enableDot);
        }
        enum Vsync
        {
            Disabled,
            Always,
            Half
        }
        void NextVsync(int index)
        {
            vsyncIndex += index;
            if (vsyncIndex > 2) vsyncIndex = 0;
            if (vsyncIndex<0) vsyncIndex = 2;
        
            SetVsync(vsyncIndex,true);
        }

        void SetVsyncVisual(int i)
        {
            if (vsyncIndex != i) vsyncIndex = i;
            vsyncText.SetText(((Vsync) i).ToString());
        
        
        }
        void SetVsync(int i,bool playButton)
        {

            QualitySettings.vSyncCount = i;
            GameManager.Instance.setting.vSync = i;
            if(playButton)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;

            SetVsyncVisual(i);
        }
        void NextFullScreenMode(int index)
        {
            fullScreenModeIndex += index;
            if (fullScreenModeIndex > 3) fullScreenModeIndex = 0;
            if (fullScreenModeIndex<0) fullScreenModeIndex = 3;
        
            SetFullScreenMode(fullScreenModeIndex,true);
        }
        void NextTextureResolution(int index)
        {
            textureIndex += index;
            if (textureIndex > 3) textureIndex = 0;
            if (textureIndex<0) textureIndex = 3;
        
            SetTextureResolution(textureIndex,true);
        }

        void SetTextureResolutionVisual(int i)
        {
            if (textureIndex != i) textureIndex = i;
        
            textureText.SetText(((Quality) i).ToString());
        }
        void SetTextureResolution(int i,bool playButton)
        {
            QualitySettings.masterTextureLimit = 3-i;
            GameManager.Instance.setting.textureIndex = i;
            if(playButton)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;
        
            SetTextureResolutionVisual(i);
        }

        void SetFullScreenModeVisual(int i)
        {
            if (fullScreenModeIndex != i) fullScreenModeIndex = i;
        
            fullScreenModeText.SetText(((FullScreenMode) i).ToString());
        }
    
        void SetFullScreenMode(int i,bool playButton)
        {
            // A windowed mode and the fullscreen toggle describe the same window state, so keep the
            // two in sync: picking Windowed turns the toggle off, picking any other mode turns it
            // on. Otherwise the stored mode can contradict the toggle, which is how the game ended
            // up launching as a bordered window while the toggle claimed fullscreen.
            GameManager.Instance.setting.fullScreen = (FullScreenMode) i != FullScreenMode.Windowed;
            GameManager.Instance.setting.fullScreenMode = i;
            GameManager.Instance.setting.ApplyDisplay();
            if(playButton)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;

            // Read the values back: ApplyDisplay can correct a mode that cannot be applied.
            SetFullScreenModeVisual(GameManager.Instance.setting.fullScreenMode);
            SetFullScreenVisual(GameManager.Instance.setting.fullScreen);
        }

        void SetFullScreenVisual(bool arg0)
        {
            fullScreen.SetIsOnWithoutNotify(arg0);
        }

        void SetFullScreen(bool f,bool playButton)
        {
            GameManager.Instance.setting.fullScreen = f;
            // ApplyDisplay owns the window state now: assigning Screen.fullScreen here resets the
            // mode to FullScreenWindow, which discarded the mode the player had selected.
            GameManager.Instance.setting.ApplyDisplay();
            if(playButton)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;

            SetFullScreenModeVisual(GameManager.Instance.setting.fullScreenMode);
            SetFullScreenVisual(GameManager.Instance.setting.fullScreen);
        }

        void InitResolution(int resolutionIndex)
        {
            resolutions = Screen.resolutions.Reverse().ToArray();
        
            resolution.ClearOptions();

            if (resolutions.Length == 0)
            {
                // No display modes reported (a headless or remote session): leave the dropdown
                // empty rather than selecting an entry that does not exist.
                return;
            }

            List<string> options = new List<string>();
            // The array is reversed, so index 0 is the largest mode the display can do, which is
            // the right fallback when the desktop resolution is not listed.
            int currentResolutionIndex = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height + " @" + resolutions[i].refreshRate  + "Hz";
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height && 
                    resolutions[i].refreshRate == Screen.currentResolution.refreshRate)
                {
                    currentResolutionIndex = i;
                }
            }
            resolution.AddOptions(options);
        
            resolution.value = resolutionIndex == -1 ? currentResolutionIndex : resolutionIndex;
            resolution.RefreshShownValue();

            resolution.onValueChanged.AddListener(delegate(int arg0) { SetResolution(arg0,true); });
        }

        void SetResolution(int i,bool playButton)
        {
            // The dropdown can outlive a display change, so never index past the mode list.
            if (i < 0 || i >= resolutions.Length) return;

            GameManager.Instance.setting.resolutionIndex = i;
            GameManager.Instance.setting.ApplyDisplay();
            GameManager.settingChanged = true;
            if(playButton)
                AudioManager.Instance.PlayButton();
        }

        void NextLanguage(int index)
        {
            languageIndex += index;
            if (languageIndex > 3) languageIndex = 0;
            if (languageIndex<0) languageIndex = 3;
        
            SetLanguage(languageIndex,true);
        }

        // void SetLanguageVisual(int arg0)
        // {
        //     if (languageIndex != arg0) languageIndex = arg0;
        //     switch (arg0)
        //     {
        //         case  0:
        //             languageText.SetText("简体中文");
        //             break;
        //         case 1:
        //             languageText.SetText("English");
        //             break;
        //         case 2:
        //             languageText.SetText("Polish");
        //             break;
        //         case 3:
        //             languageText.SetText("русский");
        //             break;
        //         case -1:
        //             switch (Application.systemLanguage)
        //             {
        //                 case SystemLanguage.ChineseSimplified:
        //                     languageText.SetText("简体中文");
        //                     break;
        //                 case SystemLanguage.English:
        //                     languageText.SetText("English");
        //                     break;
        //                 case SystemLanguage.Russian:
        //                     languageText.SetText("русский");
        //                     break;
        //                 default:
        //                     languageText.SetText("English");
        //                     break;
        //             }
        //             break;
        //     }
        // }
        void SetLanguage(int arg0,bool playButton)
        {
            if(playButton)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;
            if (GameManager.Instance.setting.useSteamLanguage) return;
            switch (arg0)
            {
                case  0:
                    languageText.SetText("简体中文");
                    LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[arg0]);
                    break;
                case 1:
                    languageText.SetText("English");
                    LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[arg0]);
                    break;
                case 2:
                    languageText.SetText("Polish");
                    LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[arg0]);
                    break;
                case 3:
                    languageText.SetText("русский");
                    LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[arg0]);
                    break;
                case -1:
                    switch (Application.systemLanguage)
                    {
                        case SystemLanguage.ChineseSimplified:
                            languageText.SetText("简体中文");
                            LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[1]);
                            break;
                        case SystemLanguage.English:
                            languageText.SetText("English");
                            LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[0]);
                            break;
                        case SystemLanguage.Russian:
                            languageText.SetText("русский");
                            LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[3]);
                            break;
                        default:
                            languageText.SetText("English");
                            LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[0]);
                            break;
                    }
                    break;
            }

            GameManager.Instance.setting.language = arg0;
        }

        void SetSoftParticleVisual(bool arg0)
        {
            softParticle.SetIsOnWithoutNotify(arg0) ;
        }
        void SetSoftParticle(bool arg0,bool playButton)
        {
            GameManager.Instance.setting.softParticle = arg0;
            QualitySettings.softParticles = arg0;
            if(playButton)
                AudioManager.Instance.PlayButton();
            GameManager.settingChanged = true;

        
            SetSoftParticleVisual(arg0);
        }

        enum AntiAliasing
        {
            None,
            X2,
            X4,
            X8
        }

        void SetAntiAliasingVisual(int arg0)
        {
            if (antiAliasingIndex != arg0) antiAliasingIndex = arg0;
            antiAliasingText.SetText(((AntiAliasing) arg0).ToString()); 
        }
        void SetAntiAliasing(int arg0,bool playButton)
        {
            GameManager.Instance.setting.antiAliasing = arg0;
            QualitySettings.antiAliasing = arg0 * 2;
            GameManager.settingChanged = true;
            if(playButton)
                AudioManager.Instance.PlayButton();

            SetAntiAliasingVisual(arg0);
        }
        void NextAntiAliasing(int index)
        {
            antiAliasingIndex += index;
            if (antiAliasingIndex > 3) antiAliasingIndex = 0;
            if (antiAliasingIndex<0) antiAliasingIndex = 3;
        
            SetAntiAliasing(antiAliasingIndex,true);
        }
        enum ShadowCascade
        {
            One=1,
            Two=2,
            Three,
            Four
        }

        void SetShadowCascadeVisual(int index)
        {
            if (shadowCascadesIndex != index) shadowCascadesIndex = index;
            shadowCascadesText.SetText(((ShadowCascade) index).ToString()); 
        
        }
        void SetShadowCascade(int arg0)
        {
            GameManager.Instance.setting.shadowCascades = arg0;

            SetShadowCascadeVisual(arg0);
            AudioManager.Instance.PlayButton();
        
        
            UniversalRenderPipelineAsset urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            urp.shadowCascadeCount = arg0;
        }
        void NextShadowCascade(int index)
        {
            shadowCascadesIndex += index;
            if (shadowCascadesIndex > 4) shadowCascadesIndex = 1;
            if (shadowCascadesIndex<1) shadowCascadesIndex = 4;
        
            SetShadowCascade(shadowCascadesIndex);
        }
        void NextShadowDistance(int index)
        {
            shadowDistanceIndex += index;
            if (shadowDistanceIndex > 2) shadowDistanceIndex = 0;
            if (shadowDistanceIndex<0) shadowDistanceIndex = 2;
        
            SetShadowDistance(shadowDistanceIndex);
        }

        void SetShadowDistanceVisual(int index)
        {
            if (shadowDistanceIndex != index) shadowDistanceIndex = index;
        
            shadowDistanceText.SetText(((Quality) index).ToString()); 
        }
        void SetShadowDistance(int arg0)
        {
            GameManager.Instance.setting.shadowDistance = arg0;
        
            GameManager.settingChanged = true;

            SetShadowDistanceVisual(arg0);
            AudioManager.Instance.PlayButton();
        
            UniversalRenderPipelineAsset urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            urp.shadowDistance = arg0 * 40;
        }

        void SetFOVVisual(float f)
        {
            if(Math.Abs(fovSlider.value - f) > 0.1f)
                fovSlider.SetValueWithoutNotify(f);
            fovText.SetText(f.ToString("F0"));
        }
        void SetFOV(float fov)
        {
            GameManager.Instance.setting.fov = fov;

            fovText.SetText(fov.ToString("F0"));
            if (WeaponManager.Instance)
            {
                WeaponManager.Instance.defaultFOV = fov;
                WeaponManager.Instance.speedUpFOV = fov+6;
            }
            GameManager.settingChanged = true;
        }

        void SetSensitivityVisual(float f)
        {
            if(Math.Abs(sensitivitySlider.value - f) > 0.1f)
                sensitivitySlider.SetValueWithoutNotify(f);
            sensitivityText.SetText(f.ToString("F0"));
        }
        
        void SetAimSensitivityMultiplierVisual(float f)
        {
            if(Math.Abs(aimSensitivityMultiplierSlider.value - f) > 0.1f)
                aimSensitivityMultiplierSlider.SetValueWithoutNotify(f);
            aimSensitivityMultiplierText.SetText(f.ToString("F1"));
        }
    
        void SetSensitivity(float sensitivity)
        {
            GameManager.Instance.setting.sensitivity = sensitivity;

            if (PlayerMovement.Instance)
            {
                PlayerMovement.Instance.sensitivity = sensitivity;
                SpectateMovement.Instance.sensitivity = sensitivity;
            }
            GameManager.settingChanged = true;
        
            sensitivityText.SetText(sensitivity.ToString("F0"));
        }
        void SetAimSensitivityMultiplierSlider(float sensitivity)
        {
            GameManager.Instance.setting.aimSensitivityMultiplier = sensitivity;

            GameManager.settingChanged = true;
        
            aimSensitivityMultiplierText.SetText(sensitivity.ToString("F1"));
        }
        void SetSpawnParticleVisual(bool flag)
        {
            spawnParticleToggle.SetIsOnWithoutNotify(flag);
        } 
        void SetSpawnParticle(bool flag)
        {
            GameManager.Instance.setting.spawnParticle = flag;

            SetSpawnParticleVisual(flag);
            GameManager.settingChanged = true;
            AudioManager.Instance.PlayButton();
        }

        void SetUseArmVisual(bool flag)
        {
            useArmToggle.SetIsOnWithoutNotify(flag);
        }
        void SetUseArm(bool flag)
        {
            flag = false;
            GameManager.Instance.setting.useArm = flag;
            if (WeaponManager.Instance)
            {
                WeaponManager.Instance.SetArm(flag);
            }
            GameManager.settingChanged = true;
            AudioManager.Instance.PlayButton();

            SetUseArmVisual(flag);
        }

        void SetCameraShakeVisual(bool b)
        {
            cameraShakeToggle.SetIsOnWithoutNotify(b);
        }
        void SetCameraShake(bool shake)
        {
            GameManager.Instance.setting.cameraShake = shake;

            GameManager.settingChanged = true;
            AudioManager.Instance.PlayButton();
        
            SetCameraShakeVisual(shake);
        }
        void NextShadowQuality(int index)
        {
            shadowQualityIndex += index;
            if (shadowQualityIndex > 2) shadowQualityIndex = 0;
            if (shadowQualityIndex<0) shadowQualityIndex = 2;
        
            SetShadowQuality(shadowQualityIndex,true);
        }

        void SetShadowQualityVisual(int index)
        {
            if (shadowQualityIndex != index) shadowQualityIndex = index;
            shadowQualityText.SetText(((ShadowQuality)index).ToString());
        
        }
        void SetShadowQuality(int i,bool flag)
        {
            GameManager.Instance.setting.shadowQuality = i;
        
            QualitySettings.shadows = (ShadowQuality) i;
        
            GameManager.settingChanged = true;
            if(flag)
                AudioManager.Instance.PlayButton();
            SetShadowQualityVisual(i);
        }
        void NextShadowResolution(int index)
        {
            shadowResolutionIndex += index;
            if (shadowResolutionIndex > 3) shadowResolutionIndex = 0;
            if (shadowResolutionIndex<0) shadowResolutionIndex = 3;
        
            SetShadowResolution(shadowResolutionIndex);
        }

        void SetShadowResolutionVisual(int index)
        {
            if (shadowResolutionIndex != index) shadowResolutionIndex = index;
            shadowResolutionText.SetText(((ShadowResolution)index).ToString());
            
        }
        void SetShadowResolution(int i)
        {
            GameManager.Instance.setting.shadowResolution = i;
            QualitySettings.shadowResolution = (ShadowResolution) i;

        
            GameManager.settingChanged = true;
            AudioManager.Instance.PlayButton();
            SetShadowResolutionVisual(i);
        }
        void NextQuality(int index)
        {
            qualityIndex += index;
            if (qualityIndex > 2) qualityIndex = 0;
            if (qualityIndex<0) qualityIndex = 2;
        
            SetQuality(qualityIndex);
        }

        enum Quality
        {
            Low,
            Medium,
            High,
            Ultra
        }

        void SetQualityVisual(int index)
        {
            if (index > 2) index = 0;
            if (!qualityIndex.Equals(index)) qualityIndex = index;
        
            qualityText.SetText(((Quality)index).ToString());
        }
        void SetQuality(int i)
        {
            GameManager.Instance.setting.quality =  i;
        
            GameManager.settingChanged = true;
            AudioManager.Instance.PlayButton();
            SetQualityVisual(i);
        
            QualitySettings.SetQualityLevel(i,true);
            SetShadowQualityVisual((int)QualitySettings.shadows);
            SetSoftParticleVisual(QualitySettings.softParticles);
            SetShadowCascadeVisual(QualitySettings.shadowCascades);
            SetAntiAliasingVisual(QualitySettings.antiAliasing/2);
            SetShadowDistanceVisual((int)QualitySettings.shadowDistance/40);
            SetTextureResolutionVisual(3-QualitySettings.masterTextureLimit);

            GameManager.Instance.setting.shadowQuality = (int)QualitySettings.shadows;
            GameManager.Instance.setting.softParticle = QualitySettings.softParticles;
            GameManager.Instance.setting.shadowCascades = QualitySettings.shadowCascades;
            GameManager.Instance.setting.antiAliasing = QualitySettings.antiAliasing/2;
            GameManager.Instance.setting.shadowDistance = (int)QualitySettings.shadowDistance/40;
            GameManager.Instance.setting.textureIndex = 3-QualitySettings.masterTextureLimit;
        }

        void SetDescriptionVisual(string des)
        {
            description.SetTextWithoutNotify(des);
        }
        void SetDescription(string des)
        {
            GameManager.Instance.setting.description = des;

            if (string.IsNullOrEmpty(des))
            {
                description.SetTextWithoutNotify("Banana");
                GameManager.Instance.setting.description = "Banana";
            }
        
            GameManager.settingChanged = true;
        }
        [SerializeField] private List<GameObject> gamePlays = new List<GameObject>();
        [SerializeField] private List<GameObject> controls = new List<GameObject>();
        [SerializeField] private List<GameObject> graphics = new List<GameObject>();
        [SerializeField] private List<GameObject> videos = new List<GameObject>();
        [SerializeField] private List<GameObject> audio = new List<GameObject>();

        string _currentSetting = "GamePlay";
        
        public void SetContent(string option)
        {
            _currentSetting = option;
            AudioManager.Instance.PlayButton();
            searchBar.SetTextWithoutNotify("");
            SetSetting();
        }

        void SetSetting()
        {
            switch (_currentSetting)
            {
                case "GamePlay":
                    foreach (var gameObject in gamePlays)
                    {
                        gameObject.SetActive(true);
                    }
                    foreach (var gameObject in controls)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in graphics)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in videos)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in audio)
                    {
                        gameObject.SetActive(false);
                    }
                    break;
                case "Control":
                    foreach (var gameObject in gamePlays)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in controls)
                    {
                        gameObject.SetActive(true);
                    }
                    foreach (var gameObject in graphics)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in videos)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in audio)
                    {
                        gameObject.SetActive(false);
                    }
                    break;
                case "Graphics":
                    foreach (var gameObject in gamePlays)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in controls)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in graphics)
                    {
                        gameObject.SetActive(true);
                    }
                    foreach (var gameObject in videos)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in audio)
                    {
                        gameObject.SetActive(false);
                    }
                    break;
                case "Video":
                    foreach (var gameObject in gamePlays)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in controls)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in graphics)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in videos)
                    {
                        gameObject.SetActive(true);
                    }
                    foreach (var gameObject in audio)
                    {
                        gameObject.SetActive(false);
                    }
                    break;
                case "Audio":
                    foreach (var gameObject in gamePlays)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in controls)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in graphics)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in videos)
                    {
                        gameObject.SetActive(false);
                    }
                    foreach (var gameObject in audio)
                    {
                        gameObject.SetActive(true);
                    }
                    break;
            }
        }
        public void ResetFile()
        {
            try
            {
                // Stamp the current version so the fresh file is not mistaken for a legacy save and
                // upgraded again on the next launch.
                Setting defaults = new Setting {settingsVersion = Setting.CurrentSettingsVersion};
                SaveSystem.SaveToJSON(defaults,"setting.json");
                PlayerPrefs.DeleteAll();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                throw;
            }
            GameManager.Instance.LoadSetting();
        }

        [SerializeField]private TMP_InputField searchBar;
        [SerializeField] private SettingItemUI[] items;
        void Search(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                SetSetting();
            }
            else
            {
                foreach (var item in items)
                {
                    item.gameObject.SetActive(item.key.Contains(str.ToLower()));
                }
            }
        }
    }

    [Serializable]
    public enum HitMarkerType
    {
        Out,
        In
    }
    [Serializable]
    public enum HitMarkerSoundType
    {
        OG,
        New
    }

    [Serializable]
    public class Setting
    {
        public float fov=80,sensitivity=50,aimSensitivityMultiplier=0.5f,maxFps=144,volume=1,uiVolume=1f,ambienceVolume=1f,soundEffectVolume=1f,crossHairWidth=11,crossHairHeight=4f,crossHairDistance=23,musicVolume=1f,swayMultiplier=.5f;
        public float csR=255, csG=255, csB=255;
        public bool cameraShake=true,softParticle=true,fullScreen=true,spawnParticle=true,useArm=false,disableVoice=false,showDir=false,enableDot=true,disableBananaVoice=true,keepRagdoll=false,bloom=true,autoUpgrade=true,useNightVission=false,useSteamLanguage=true,grappleHint=true
            ,useController=false,enableGore = true,enableTutorial = true,enableUpgradeAnimation=true;
        public int shadowQuality = 2;
        public HitMarkerType hitMarkerType;
        public HitMarkerSoundType hitMarkerSoundType = HitMarkerSoundType.New;
        public int shadowResolution = 3;
        public int quality=2,shadowCascades=4,antiAliasing=2,shadowDistance=40,resolutionIndex=-1,fullScreenMode=1,vSync=0,language=-1,textureIndex=0;
        public string description="";

        /// <summary>
        /// Version of the saved settings file. Bumped whenever a default changes in a way that
        /// files written by older builds have to be upgraded for.
        /// </summary>
        public const int CurrentSettingsVersion = 1;

        /// <summary>
        /// Version of the file this instance was loaded from, where 0 means "written before the
        /// display defaults were fixed". Deliberately not initialised to CurrentSettingsVersion:
        /// deserialisers leave a field that is absent from the file at its initialiser, so
        /// defaulting to 0 is what makes a legacy file detectable.
        /// </summary>
        public int settingsVersion;

        /// <summary>
        /// Upgrades an instance loaded from an older build, in place. Version 0 shipped
        /// fullScreen = true together with fullScreenMode = 3 (Windowed) as the default, so the
        /// game started as a bordered, non-maximised window. Only a file that is actually in that
        /// state is rewritten, so a deliberately picked mode or resolution is left alone.
        /// Returns true when the instance was upgraded, so the caller can persist it.
        /// </summary>
        public bool Migrate()
        {
            if (settingsVersion >= CurrentSettingsVersion) return false;

            if (fullScreen && fullScreenMode == (int) FullScreenMode.Windowed)
                fullScreenMode = (int) FullScreenMode.FullScreenWindow;

            settingsVersion = CurrentSettingsVersion;
            return true;
        }

        /// <summary>
        /// One window state is described by two fields: the fullscreen toggle and the fullscreen
        /// mode. A fullscreen request must never resolve to Windowed, because older builds
        /// defaulted the mode to Windowed (3) while the toggle was on, which is what produced a
        /// bordered window instead of a maximised fullscreen one.
        /// </summary>
        public static FullScreenMode ResolveFullScreenMode(bool fullScreen, int fullScreenMode)
        {
            if (!fullScreen) return FullScreenMode.Windowed;

            FullScreenMode mode = (FullScreenMode) fullScreenMode;
            return mode == FullScreenMode.Windowed ? FullScreenMode.FullScreenWindow : mode;
        }

        /// <summary>
        /// Applies the stored display settings. Screen.SetResolution is the only call made on
        /// purpose: Screen.fullScreen and Screen.fullScreenMode write the same window state, and
        /// assigning Screen.fullScreen resets the mode, so setting them separately loses the
        /// player's choice. resolutionIndex -1 means "follow the display", i.e. the resolution the
        /// desktop is already running at, which is the maximised borderless view.
        /// </summary>
        public void ApplyDisplay()
        {
            FullScreenMode mode = ResolveFullScreenMode(fullScreen, fullScreenMode);

            // Store the correction, so a mode that could not be applied is not read back from the
            // settings menu. A mode the player picked is left alone while fullscreen is off.
            if (fullScreen) fullScreenMode = (int) mode;

            var resolutions = Screen.resolutions.Reverse().ToArray();
            Resolution resolution = resolutionIndex >= 0 && resolutionIndex < resolutions.Length
                ? resolutions[resolutionIndex]
                : Screen.currentResolution;

            Screen.SetResolution(resolution.width, resolution.height, mode);
        }
    }
}