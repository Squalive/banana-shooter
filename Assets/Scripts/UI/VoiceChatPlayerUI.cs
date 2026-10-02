
using Multiplayer;
using Multiplayer.Interface;
using TMPro;
using UnityEngine;

namespace UI
{
    public class VoiceChatPlayerUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        
        [SerializeField] private TextMeshProUGUI nameText;
        
        public AudioSource VoiceSource { get; private set; }

        private float _desiredAlpha = 0f;

        public SteamVoiceChatPeer Peer;

        public void Initialize(ClientData clientData)
        {
            VoiceSource = gameObject.AddComponent<AudioSource>();

            VoiceSource.outputAudioMixerGroup = MusicManager.Instance.master;
            
            nameText.SetText(clientData.Name);

            Peer = new SteamVoiceChatPeer(VoiceSource);
            
            gameObject.SetActive(false);
        }

        private void Update()
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, _desiredAlpha, Time.deltaTime * 15f);

            if (Mathf.Abs(canvasGroup.alpha-0.001f) <= 0.1f)
            {
                gameObject.SetActive(false);
            }
        }

        public void Speak()
        {
            _desiredAlpha = 1f;

            CancelInvoke(nameof(StopSpeak));
            Invoke(nameof(StopSpeak), 1f);
            
            gameObject.SetActive(true);
        }

        void StopSpeak()
        {
            _desiredAlpha = 0f;
        }
    }
}
