
using UnityEngine;

namespace Audio
{
    public class AudioSyncer : MonoBehaviour
    {
        public float bias;
        public float timeStep;
        public float timeToBeat;
        public float restSmoothTime;
        
        private float _previousAudioValue;
        private float _audioValue;
        private float _timer;

        protected bool IsBeat;

        private void Update()
        {
            OnUpdate();
        }

        protected virtual void OnUpdate()
        {
            _previousAudioValue = _audioValue;
            _audioValue = AudioSpectrum.SpectrumValue;

            if (_previousAudioValue > bias && _audioValue <= bias)
            {
                if(_timer > timeStep)
                    OnBeat();
            }

            if (_previousAudioValue <= bias && _audioValue > bias)
            {
                if(_timer > timeStep)
                    OnBeat();
            }

            _timer += Time.deltaTime;
        }

        public virtual void OnBeat()
        {
            _timer = 0;
            IsBeat = true;
        }
    }
}
