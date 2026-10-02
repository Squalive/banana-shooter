
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Components;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Radio : MonoBehaviour
{
    public CanvasGroup group;

    private float desiredAlpha;

    public LocalizeStringEvent playingText;

    private void Awake()
    {
        @group = GetComponentInChildren<CanvasGroup>();
        source = GetComponent<AudioSource>();

        playingText.StringReference.Arguments = new List<object>(){"None"};
        
        PlayMusic();
    }

    private void OnDestroy()
    {
        Release();
    }

    private void Update()
    {
        @group.alpha = Mathf.Lerp(@group.alpha, desiredAlpha, Time.deltaTime * 15f);
    }

    private AudioSource source;

    public void Show()
    {
        CancelInvoke(nameof(Close));
        desiredAlpha = 1f;
        Invoke(nameof(Close),0.3f);
    }

    public void ChangePlayState()
    {
        if(source.isPlaying) source.Pause();
        else source.Play();

        playingText.SetEntry(source.isPlaying ? "playing" : "pausing");
        playingText.RefreshString();
    }

    [SerializeField] private AssetReference[] musicAssets;

    private AssetReference _loadedAsset;
    
    private int _currentIndex = 0;

    public void NextSong(int index)
    {
        _currentIndex += index;
        if (_currentIndex >= musicAssets.Length) _currentIndex = 0;
        else if (_currentIndex < 0) _currentIndex = musicAssets.Length - 1;

        PlayMusic();
    }

    void PlayMusic()
    {
        Release();
        _loadedAsset = musicAssets[_currentIndex];

        var asyncOperation = _loadedAsset.LoadAssetAsync<AudioClip>();
        
        asyncOperation.Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var clip = handle.Result;
                source.clip = clip;
                
                playingText.SetEntry("playing");
                playingText.StringReference.Arguments[0] = clip.name;
                playingText.RefreshString();
                
                source.Play();

            }
        };

        
    }

    void Release()
    {
        if (_loadedAsset != null)
        {
            _loadedAsset.ReleaseAsset();
        }
    }
    
    void Close()
    {
        desiredAlpha = 0f;
    }
}
