using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Manager;
using Multiplayer;
using Newtonsoft.Json;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace Menu
{
    public class VoiceLine : MonoBehaviour
    {
        public static VoiceLine Instance;

        public List<VoiceLineData> datas = new List<VoiceLineData>();

        public Dictionary<VoiceKey, VoiceLineData> lines = new Dictionary<VoiceKey, VoiceLineData>();

        [SerializeField] private AudioMixerGroup mixerGroup;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                foreach (var data in datas)
                {
                    lines.Add(data.key,data);
                }

                source = gameObject.AddComponent<AudioSource>();

                source.outputAudioMixerGroup = mixerGroup;
                
                
            }
            else if(Instance!=this)
            {
                Destroy(gameObject);
            }

        
        
        }

        private IEnumerator Start()
        {
            _locales = NetworkManager.Instance.language;

            if (File.Exists(SaveSystem.GetPath("voice_data.json")))
            {
                CoroutineWithData cd = new CoroutineWithData(this,
                    SaveSystem.ReadFileAsyncThread(SaveSystem.GetPath("voice_data.json")));

                yield return cd.coroutine;

                VoiceStoreData s = JsonConvert.DeserializeObject<VoiceStoreData>(cd.result.ToString());

                if (s != null)
                    data = s;
            }
        }

        private Locale[] _locales;

        public CanvasGroup group;
        private float desiredAlpha = 0;

        public LocalizeStringEvent text;

        public void PlayVoice(VoiceKey key)
        {
            // return;
            if (GameManager.Instance.setting.disableBananaVoice) return;
            queueData.Enqueue(lines.ContainsKey(key) ? lines[key] : lines[VoiceKey.error]);
        }

        IEnumerator SetBackGround()
        {
            yield return null;
            var sizeDelta = textTransform.sizeDelta;
            backGround.sizeDelta = new Vector2(sizeDelta.x+30,40);
        }

        private void Update()
        {
            @group.alpha = Mathf.Lerp(@group.alpha, desiredAlpha, Time.deltaTime * 15f);

            if (!source.isPlaying)
            {
                if (queueData.Count > 0)
                {
                    VoiceLineData lineData = queueData.Dequeue();
                    StopAllCoroutines();
                    CancelInvoke(nameof(ClearVoice));
                    desiredAlpha = 1;

                    AudioClip audioClip = lineData.audio;

                    Locale locale = LocalizationSettings.Instance.GetSelectedLocale();

                    if (locale == _locales[3] && lineData.russianAudio!=null)
                    {
                        audioClip = lineData.russianAudio;
                    }
                

                    source.PlayOneShot(audioClip);

                    text.SetEntry(lineData.key.ToString());
                    text.RefreshString();
                    Invoke(nameof(ClearVoice), lineData.audio.length + 0.5f);
                    StartCoroutine(SetBackGround());
                }
            }
        }

        private Queue<VoiceLineData> queueData = new Queue<VoiceLineData>();

        void ClearVoice()
        {
            desiredAlpha = 0;
        }

        private AudioSource source;

        public RectTransform backGround,textTransform;
        
        public VoiceStoreData data = new VoiceStoreData();
        public void StoreData(){
            SaveSystem.SaveToJSON(data,"voice_data.json");
        }
    }

    [Serializable]
    public class VoiceLineData
    {
        public VoiceKey key;
        public AudioClip audio,russianAudio;
    }
    public enum VoiceKey
    {
        hello_dave,
        team_deathMatch,
        brawl,
        team_deathMatch_description,
        brawl_description,
        error,
        ready_A,
        ready_B,
        ready_C_01,
        ready_C_02,
        ready_D_01,
        ready_D_02,
        kill_A,
        kill_B,
        kill_C,
        kill_D_01,
        kill_D_02,
        death_A_01,
        death_A_02,
        death_B_01,
        death_B_02,
        death_C,
        death_D,
        death_E,
        death_F,
        death_G,
        kill_E_01,
        kill_E_02,
        kill_F,
        kill_G,
        funny_A,
        funny_B,
        funny_C,
        funny_D,
        funny_E,
        tdm_win_A,
        tdm_win_B,
        bw_A,
        bw_B,
        speed_A,
        speed_B,
        /// <summary>
        ///  Welcome to endless, stand in the while line area to start the game
        /// </summary>
        gm_endless_start_A,
    
        /// <summary>
        /// Oh you come back to this gamemode ha, so hilarious ive never thought someone wil play this gamemode twice again
        /// </summary>
        gm_endless_start_B,
    
        /// <summary>
        /// Wait did you just come back again? anyway stand in the white line area then ill let my bois to fight with you
        /// </summary>
        gm_endless_start_C, 
    
        /// <summary>
        /// Hah, you seems a true banana man since this is the fourth time you open this gamemode, Go ahead let start the game
        /// </summary>
        gm_endless_start_D, 
    
        /// <summary>
        /// Stand in the white line area*3
        /// </summary>
        gm_endless_start_E,
    
        /// <summary>
        /// Lets try this, just a few robots
        /// </summary>
        gm_endless_spawn_A,
    
        /// <summary>
        /// Come on, there is more robots here, but keep in mind they are kindness\
        /// </summary>
        gm_endless_spawn_B,
    
        /// <summary>
        /// Wow such a genius you have already killed so many my bois
        /// </summary>
        gm_endless_spawn_C_01,
        /// <summary>
        /// But who would guess, i have more ROBOTS(reach 120s)
        /// </summary>
        gm_endless_spawn_C_02,
    
        /// <summary>
        /// Haha i dont believe you can handle this (spawn 5 enemies)
        /// </summary>
        gm_endless_spawn_D,
    
        /// <summary>
        ///  i wont give up (reach 180s)
        /// </summary>
        gm_endless_spawn_E, 
    
        /// <summary>
        /// Ok im speechless
        /// </summary>
        gm_endless_spawn_F,
        
        /// <summary>
        /// Enemy will have a red dot on their head, use your weapon to shoot them
        /// </summary>
        gm_endless_start_F,
        
        /// <summary>
        /// Wait you actually start a fight
        /// </summary>
        gm_endless_start_G_01,
        /// <summary>
        /// Well
        /// </summary>
        gm_endless_start_G_02,
    }

    [Serializable]
    public class VoiceStoreData
    {
        public int endlessPlayAmount = 0;

    }
    
    
}

