
using System;
using Manager;
using PlayerCameraController;
using UnityEngine;

public class LookPlayer : MonoBehaviour
{
    private Transform _player;
    private Camera _camera;

    private Transform _transform;

    // private float maxSize = 2f;
    // private float minSize =.5f;
    //
    // private float threshold = 15f;

    [SerializeField]private float scaleFactor = 1f;

    [SerializeField] private GameObject dot, nameObj;
    private void Awake()
    {
        _transform = transform;
        
        SetTarget(ListenerManager.Instance.cameraTransform);
    }

    private void OnEnable()
    {
        ListenerManager.OnCameraChanged += SetTarget;
    }

    private void OnDisable()
    {
        ListenerManager.OnCameraChanged -= SetTarget;
    }

    void SetTarget(Transform t)
    {
        if (t == null)
        {
            _player = null;
            _camera = null;
            return;
        }
        
        _player = t;
        _camera = _player.GetComponentInChildren<Camera>();
        _transform.LookAt(_player);
    }

    private void Update()
    {
        if (_player != null)
        {
            _transform.LookAt(_player);

            float dis = Vector3.Distance(_transform.position, _player.position);

            float fovScaleFactor = Mathf.Tan(Mathf.Deg2Rad * (_camera.fieldOfView * 0.5f)) * 2f;

            float scale = fovScaleFactor * dis;
            
            _transform.localScale = scaleFactor * scale * Vector3.one;

            bool flag = scale > 35;
            
            nameObj.SetActive(!flag);
            dot.SetActive(flag);
        }
    }
}
