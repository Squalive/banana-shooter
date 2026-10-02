using System.Collections;
using System.Collections.Generic;
using Movement;
using Multiplayer.Entity.Client;
using UnityEngine;

public class SlideAudio : MonoBehaviour
{
    public static SlideAudio Instance { private set; get; }
    private static bool _initialized = false;
    
    private PlayerState _currentPlayer;

    private AudioSource _sfx;
    
    private void Awake()
    {
        Instance = this;
        _sfx = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (!_initialized) return;
        float b = 0f;
        if (_currentPlayer.Grounded && _currentPlayer.IsCrouching)
        {
            b = _currentPlayer.GetVelocity().magnitude;
            b = Mathf.Clamp(b * 0.0125f, 0f, 0.6f);
        }
        _sfx.volume = Mathf.Lerp(_sfx.volume, b, Time.deltaTime * 15f);
    }

    public void InitializePlayer(PlayerState currentPlayer)
    {
        _currentPlayer = currentPlayer;
        _initialized = true;
    }

    public void DeInitialize()
    {
        _sfx.volume = 0;
        _currentPlayer = null;
        _initialized = false;
    }
}
