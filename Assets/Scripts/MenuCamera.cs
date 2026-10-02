
using EZCameraShake;
using Manager;
using UnityEngine;

public class MenuCamera : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(Shake),0.5f);

        ListenerManager.Instance.SetCamera(transform);
    }

    void Shake()
    {
        CameraShaker.GetInstance("MenuCamera").StartShake(2.5f, 0.1f, 0.5f);
    }
}
