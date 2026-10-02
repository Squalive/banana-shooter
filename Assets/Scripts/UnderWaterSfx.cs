
using System;
using UnityEngine;
using UnityEngine.Audio;

public class UnderWaterSfx : MonoBehaviour
{
    public static UnderWaterSfx Instance;
    
    private AudioSource source;
    public AudioMixerGroup group;
    [SerializeField] private AudioClip sfx;

    private float desiredVolume=0f;
    private void Awake()
    {
        Instance = this;
        
        source = gameObject.AddComponent<AudioSource>();

        source.loop = true;
        source.clip = sfx;
        source.outputAudioMixerGroup = @group;
        source.volume = desiredVolume;
        
        source.Play();
    }

    
    private void Update()
    {
        source.volume = Mathf.Lerp(source.volume, desiredVolume, Time.deltaTime * 5f);
    }

    public void SetUnderWater(bool underWater)
    {
        desiredVolume = underWater ? 1 : 0;
    }
    
   
}
