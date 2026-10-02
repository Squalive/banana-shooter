
using System.Collections;
using Audio;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Manager;
using Quest;
using SteamWorkshop;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Menu
{
    public class Tutorial : MonoBehaviour
    {

        public static Tutorial Instance;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform textTransform;
        [SerializeField] private LocalizeStringEvent text;

        private Vector3 desiredSize;
        private float desiredAlpha = 0;

        public bool old = false;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        
        }

        private IEnumerator Start()
        {
            while (!QuestManager.Initialized)
            {
                yield return null;
            }
            
            while (!GameManager.Initialized)
            {
                yield return null;
            }
            
            while (!MapSaver.Initialized)
            {
                yield return null;
            }
            
            while (!SteamWorkshopManager.Initialized)
            {
                yield return null;
            }

            while (!Preload.Initialized)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            
            Set();
        }

        void Set()
        {
            SetText("StartGameTip",4);
        }

        [SerializeField]private GameObject textPrefab;

        [SerializeField] private Transform content;

        public void SetText(string entry,float time = 10)
        {
            if (!GameManager.Instance.setting.enableTutorial)
            {
                enabled = false;
                return;
            }
            AudioManager.Instance.Play("tip");
            if (old)
            {
                TutorialItem item = Instantiate(textPrefab, content).GetComponent<TutorialItem>();

                item.SetValues(entry,time);
            }
            else
            {
                SetTutorial(entry,time);
            
            }
        }

        void SetTutorial(string entry,float time)
        {
            desiredSize = Vector3.one;
            desiredAlpha = 1f;
                
            text.SetEntry(entry);
            
            CancelInvoke(nameof(ClearTutorial));
            Invoke(nameof(ClearTutorial),time);
        }

        void ClearTutorial()
        {
            desiredSize = Vector3.zero;
            desiredAlpha = 0;
        }

        private void Update()
        {
            //textTransform.localScale = Vector3.Slerp(textTransform.localScale,desiredSize,Time.deltaTime*15f);
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, desiredAlpha, Time.deltaTime*5f);
        }
    }
}
