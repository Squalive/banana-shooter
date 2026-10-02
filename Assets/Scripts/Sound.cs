// Audio.Sound
using System;
using UnityEngine;


namespace Audio
{
    [Serializable]
    public class Sound
    {
        public string name;

        public AudioClip clip;

        [Range(0f, 2f)] public float volume=1f;

        [Range(0f, 2f)] public float pitch=1f;
        

        public bool loop;

        public bool bypass;
        
        public bool bypassReverb;
        

        [HideInInspector] public AudioSource source;
    }
}