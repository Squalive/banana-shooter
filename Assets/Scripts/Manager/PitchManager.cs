
using UnityEngine;
using UnityEngine.Audio;

namespace Manager
{
    public class PitchManager : MonoBehaviour
    {
        public static PitchManager Instance { private set; get; }
        
        [SerializeField] AudioMixer mixer;

        private float _currentPitch = 1f;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            _currentPitch = Mathf.Lerp(_currentPitch, Time.timeScale, Time.unscaledDeltaTime * 15f);

            SetPitch(_currentPitch);
        }

        public void SetPitch(float p)
        {
            _currentPitch = p;
            mixer.SetFloat("Pitch", p);
        }
    }
}
