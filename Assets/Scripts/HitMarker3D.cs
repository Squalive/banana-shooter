
using PlayerCameraController;
using Pool;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class HitMarker3D : MonoBehaviour,IPooledObject
{
    public TextMeshProUGUI text;
    
    private Vector3 desiredDir;

    private Vector3 desiredScale;

    private Vector3 scaleVel;

    private float posSpeed = 0.12f;

    private float scaleSpeed;

    private float rotSpeed;

    private float posVel2;

    private float a;
    private CanvasGroup cg;

    private void Update()
    {
        transform.position += desiredDir * posSpeed;
        posSpeed = Mathf.Lerp(posSpeed, 0.002f, Time.deltaTime * 13f);
        transform.localScale = Vector3.SmoothDamp(base.transform.localScale, desiredScale, ref scaleVel, 0.05f);
        cg.alpha = Mathf.Lerp(cg.alpha, 0f, Time.deltaTime);
        if (!MoveCamera.Instance) return;
        transform.LookAt(base.transform.position + (transform.position - MoveCamera.Instance.transform.position));
    }
    
    private void DestroySelf()
    {
        gameObject.SetActive(false);
    }

    public void OnObjectSpawn()
    {
        if (!MoveCamera.Instance) return;
        cg.alpha = 1;
        desiredDir = Vector3.up * Random.Range(0.2f, 0.4f) + Vector3.right * Random.Range(-1.5f, 1.5f);
        var transform1 = transform;
        desiredScale = transform1.localScale;
        transform1.localScale = Vector3.zero;

        transform.LookAt(base.transform.position + (transform1.position -MoveCamera.Instance.transform.position));
        Invoke(nameof(DestroySelf),1.6f);
    }

    public void OnObjectInit()
    {
        cg = GetComponent<CanvasGroup>();
        
    }
}
