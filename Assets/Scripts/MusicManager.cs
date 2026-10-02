
using System;
using Audio;
using Manager;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

public class MusicManager : MonoBehaviour
{
    public enum MusicType
    {
        None,
        MainMenu,
        WinningMusic
    }
    
    [Serializable]
    public class MusicBox
    {
        public AssetReference[] mainMenuMusicAsset;
        public AssetReference winningMusicAsset;
        public uint ticksToPlayWin;
    }
    
    public static MusicManager Instance;
    
    public AudioMixerGroup master;

    public float volumeMultiplier = 1f;

    private AudioSource _source;

    private AudioClip _loadedMusic;

    private AssetReference _loadedAsset;

    public MusicType music = MusicType.None;

    private float _desiredVolume = 0;

    private bool _playing = false;

    [SerializeField] private MusicBox[] musicBoxes;

    public uint TicksToPlayWin => musicBoxes[InventoryManager.Instance.cosmeticIndex.musicBoxIndex].ticksToPlayWin;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            _source = gameObject.AddComponent<AudioSource>();

            _source.outputAudioMixerGroup = master;
            _source.clip = null;
            _source.loop = true;
            _source.volume = _desiredVolume;

            _source.playOnAwake = true;
        }
    }
    
    private void Update()
    {
        _source.volume = _playing ? Mathf.Lerp(_source.volume, _desiredVolume*volumeMultiplier, Time.deltaTime) : Mathf.Lerp(_source.volume, 0, Time.deltaTime);
    }
    
    public void ChangeMusic(MusicType type)
    {
        music = type;

        Stop();

        var musicBox = musicBoxes[InventoryManager.Instance.cosmeticIndex.musicBoxIndex];

        AsyncOperationHandle<AudioClip> operationHandle;
        switch (music)
        {
            case MusicType.None:
                break;
            case MusicType.MainMenu:

                _loadedAsset = musicBox.mainMenuMusicAsset[Random.Range(0, musicBox.mainMenuMusicAsset.Length)];

                operationHandle = _loadedAsset.LoadAssetAsync<AudioClip>();

                operationHandle.Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadedMusic = handle.Result;
                        _source.clip = _loadedMusic;
                        _desiredVolume = 1f;
                        _source.PlayDelayed(1f);
                        Invoke(nameof(Play), 1f);
                    }
                };
                AudioSpectrum.Running = true;
                
                break;
            case MusicType.WinningMusic:
                _loadedAsset = musicBox.winningMusicAsset;
                
                operationHandle = _loadedAsset.LoadAssetAsync<AudioClip>();

                operationHandle.Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadedMusic = handle.Result;
                        _source.clip = _loadedMusic;
                        _desiredVolume = 1f;
                        _source.Play();
                        Play();
                    }
                };
                break;
        }
    }

    private void Stop()
    {
        _playing = false;
        if (_loadedAsset != null)
        {
            _loadedAsset.ReleaseAsset();
            _loadedAsset = null;
        }
        _loadedMusic = null;
        _desiredVolume = 0;
        AudioSpectrum.Running = false;
    }

    private void Play()
    {
        _playing = true;
    }
}
