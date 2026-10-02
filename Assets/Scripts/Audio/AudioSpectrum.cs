using System;
using UnityEngine;

namespace Audio
{
    public class AudioSpectrum : MonoBehaviour
    {
        public static float SpectrumValue { private set; get; }

        public static bool Running = false;

        private float[] _audioSpectrum;

        private void Start()
        {
            _audioSpectrum = new float[128];
        }

        private void Update()
        {
            if (!Running)
                return;
            
            AudioListener.GetSpectrumData(_audioSpectrum, 0, FFTWindow.Hamming);

            if (_audioSpectrum is { Length: > 0 })
            {
                SpectrumValue = _audioSpectrum[0] * 100;
            }
        }
    }
}
