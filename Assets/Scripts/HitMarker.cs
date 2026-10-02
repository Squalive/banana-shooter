
using Audio;
using EZCameraShake;
using Manager;
using Menu;
using Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class HitMarker : MonoBehaviour
{
    public static HitMarker Instance;

    private Vector3 maxSize;

    private Vector3 desiredScale;

    private float speed = 2f;
    [SerializeField] private RawImage image;
    
    private void Awake()
    {
        Instance = this;
        maxSize = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * speed);
    }

    public void StartHitMarker(Color color)
    {
        if(GameManager.Instance.setting.cameraShake)
            CameraShaker.Instance.ShakeOnce(2.5f, 2.5f, 0.2f, 0.6f);

        switch (GameManager.Instance.setting.hitMarkerSoundType)
        {
            case HitMarkerSoundType.New:
                AudioManager.Instance.Play("new_hit_sound");
                break;
            case HitMarkerSoundType.OG:
                AudioManager.Instance.Play("HitMarker");
                AudioManager.Instance.Play("Hit");
                break;
        }
        // AudioManager.Instance.Play("Hit2");
        // AudioManager.Instance.Play("Hit3");
        // AudioManager.Instance.Play("Hit4");
        if (GameManager.Instance.setting.hitMarkerType == HitMarkerType.In)
        {
            HitMarker2.Instance.StartHitMarker(color);
            return;
        }
        image.color = color;
        speed = 25f;
        // Invoke(nameof(UpSpeed), 0.05f);
        desiredScale = Vector3.zero;
        transform.localScale = maxSize;
        Invoke(nameof(DelayRemove), 0.04f);
    }
    public void StartHitMarkerRobot(Color color)
    {
        if(GameManager.Instance.setting.cameraShake)
            CameraShaker.Instance.ShakeOnce(2.5f, 2.5f, 0.2f, 0.6f);
        switch (GameManager.Instance.setting.hitMarkerSoundType)
        {
            case HitMarkerSoundType.New:
                AudioManager.Instance.Play("new_hit_sound");
                break;
            case HitMarkerSoundType.OG:
                AudioManager.Instance.Play("bulletimpact_robot");
                break;
        }
        if (GameManager.Instance.setting.hitMarkerType == HitMarkerType.In)
        {
            HitMarker2.Instance.StartHitMarker(color);
            return;
        }
        image.color = color;
        speed = 40f;
        Invoke(nameof(UpSpeed), 0.05f);
        desiredScale = maxSize;
        Invoke(nameof(DelayRemove), 0.04f);
        
    }

    private void DelayRemove()
    {
        desiredScale = Vector3.zero;
    }

    private void UpSpeed()
    {
        speed = 25f;
    }
}
