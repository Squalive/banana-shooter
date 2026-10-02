using System;
using Console;
using Movement;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Demo.UI
{
    public class DemoCanvas : MonoBehaviour
    {
        public static DemoCanvas Instance { private set; get; }

        public static bool UIEnabled = true;

        [SerializeField] public GameObject canvas, recordingUi, recordingImage;
        
        [SerializeField] private Slider slider;

        [SerializeField] private TextMeshProUGUI lengthText;

        [SerializeField] private Button playOrPauseBtn,skipPreviousBtn,skipNextBtn,skipBtn, changePerspectiveBtn;

        [SerializeField] private RawImage playOrPauseImage;

        [SerializeField] private Texture2D playTexture2D, pauseTexture2D;

        [SerializeField] public TextMeshProUGUI timerText, tickText, timeScaleText, demoNameText, dateText;
        
        private float _timer = .5f;
        private bool _flag = false;
        private void Awake()
        {
            Instance = this;
            
            slider.onValueChanged.AddListener(SetTick);
            
            playOrPauseBtn.onClick.AddListener(SetPause);
            skipPreviousBtn.onClick.AddListener(SkipPrevious);
            skipNextBtn.onClick.AddListener(SkipNext);
            skipBtn.onClick.AddListener(Skip);
            changePerspectiveBtn.onClick.AddListener(ChangePerspective);
            
            RefreshPauseImage();
        }

        private void OnEnable()
        {
            DemoManager.OnRecordingChanged += RefreshRecordingUI;
        }

        private void OnDisable()
        {
            DemoManager.OnRecordingChanged -= RefreshRecordingUI;
        }

        private void Update()
        {
            if (DemoManager.Recording)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    _timer = .5f;
                    _flag = !_flag;
                    recordingImage.SetActive(_flag);
                }
            }
            
            if (DemoManager.Replaying)
            {
                if (UIEnabled)
                {
                    if (Keyboard.current.leftAltKey.wasPressedThisFrame)
                    {
                        bool flag = Cursor.visible;

                        if (flag)
                        {
                            Cursor.visible = false;
                            Cursor.lockState = CursorLockMode.Locked;
                        }
                        else
                        {
                            Cursor.visible = true;
                            Cursor.lockState = CursorLockMode.None;
                        }
                    }
                }
                if (Keyboard.current.tabKey.wasPressedThisFrame && !DeveloperConsoleUI.Instance.uiCanvas.activeSelf)
                {
                    UIEnabled = !UIEnabled;
                    canvas.SetActive(UIEnabled);
                }
            }
        }

        public void Initialize(DemoData demoData)
        {
            canvas.SetActive(true);
            slider.SetValueWithoutNotify(0);
            slider.maxValue = demoData.endTick;
            slider.minValue = 0;
            
            timerText.SetText("00:00");
            tickText.SetText($"(0/{demoData.endTick})");
            timeScaleText.SetText(Time.timeScale.ToString("F1"));
            demoNameText.SetText(demoData.Name);
            dateText.SetText(demoData.DateTime.ToString("G"));
        }

        public void DeInitialize()
        {
            canvas.SetActive(false);
        }

        public void SetSliderValue(int tick)
        {
            slider.SetValueWithoutNotify(tick);

            int len = (int)(tick * 0.02f);

            var min = len / 60;
            var seconds = (len % 60);

            string m = min < 10 ? $"0{min}" : min.ToString("F0");
            string s = seconds < 10 ? $"0{seconds}" : seconds.ToString("F0");
            
            lengthText.SetText($"{m}:{s}");
            
            timerText.SetText($"{m}:{s}");
            
            tickText.SetText($"({tick}/{slider.maxValue.ToString("F0")})");
        }

        void SetTick(float fTick)
        {
            if (!DemoManager.Replaying) return;

            int tick = (int)fTick;

            DemoManager.ReplayPaused = true;
            
            DemoManager.Instance.SetReplayTick(tick);
            
            RefreshPauseImage();
        }

        void SetPause()
        {
            if (!DemoManager.Replaying) return;

            DemoManager.ReplayPaused = !DemoManager.ReplayPaused;

            RefreshPauseImage();
        }

        void RefreshPauseImage()
        {
            playOrPauseImage.texture = DemoManager.ReplayPaused ? playTexture2D : pauseTexture2D;
        }

        void SkipPrevious()
        {
            DemoManager.Instance.SetReplayTick(0);
        }

        void SkipNext()
        {
            DemoManager.Instance.SetReplayTick((int)(slider.maxValue - 1));
        }

        void Skip()
        {
            DemoManager.Instance.SkipSeconds();
        }

        private void ChangePerspective()
        {
            SpectateMovement.Instance.LoopPerspective();
        }

        void RefreshRecordingUI(bool flag)
        {
            recordingUi.SetActive(flag);

            if (flag)
            {
                _timer = 0.5f;
                _flag = false;
                recordingImage.SetActive(false);
            }
        }
    }
}
