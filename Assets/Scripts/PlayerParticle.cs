
using EZCameraShake;
using Menu;
using Multiplayer.Entity.Client;
using UnityEngine;

public class PlayerParticle : MonoBehaviour
{
    public static PlayerParticle Instance { get; private set; }
    
    private bool _initialized = false;
    
    private PlayerState _currentPlayer;

    private float currentVol;

    private float volVel;
    
    [SerializeField] private Transform playerCam;
    public float speedLineMultiplier = 3f;

    [SerializeField] AudioSource wind;

    public void InitializePlayer(PlayerState currentPlayer)
    {
        _currentPlayer = currentPlayer;
        
        _initialized = true;
    }

    public void DeInitialize()
    {
        _initialized = false;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        psEmission = ps.emission;

        InvokeRepeating(nameof(CameraShake), 0, 0.2f);
    }
    private void CameraShake()
    {
        if (!_initialized) return;
        float num = _currentPlayer.GetVelocity().magnitude / 9f;
        CameraShaker.Instance.ShakeOnce(num, 0.1f * num, 0.25f, 0.2f);
        // Invoke(nameof(CameraShake), 0.2f);
    }
    
    private void FixedUpdate()
    {
        SpeedLines();
    }

    private void Update()
    {
        PlayWindSound();
    }

    public ParticleSystem ps;

    private ParticleSystem.EmissionModule psEmission;
    private void SpeedLines()
    {
        if (!_initialized) return;
        Quaternion desiredRot = _currentPlayer.GetVelocity().normalized == Vector3.zero
            ? Quaternion.Euler(0, -90, 0)
            : Quaternion.LookRotation(-_currentPlayer.GetVelocity().normalized);
        ps.transform.rotation = desiredRot;
        float num = Vector3.Angle(_currentPlayer.GetVelocity(), playerCam.forward) * 0.15f;
        if (num < 1f)
        {
            num = 1f;
        }
        float rateOverTimeMultiplier = _currentPlayer.GetVelocity().magnitude*speedLineMultiplier / num;
        if (!_currentPlayer.Grounded)
        {
            rateOverTimeMultiplier /= 2;
        }
        psEmission.rateOverTimeMultiplier = rateOverTimeMultiplier;
    }

    private void PlayWindSound()
    {
        if (!_initialized) return;
        float num = _currentPlayer.GetVelocity().magnitude;
        if (_currentPlayer.Grounded)
        {
            if (num < 20f)
            {
                num = 0f;
            }
            num = (num - 20f) / 30f;
        }
        else
        {
            num = (num - 10f) / 30f;
        }
        if (num > 1f)
        {
            num = 1f;
        }
        num *= 1f;
        currentVol = Mathf.SmoothDamp(currentVol, num, ref volVel, 0.2f);
        if ( GameUIManager.Instance&& GameUIManager.Instance.pause)
        {
            currentVol = 0f;
        }
        wind.volume = currentVol*0.5f;
    }
}
