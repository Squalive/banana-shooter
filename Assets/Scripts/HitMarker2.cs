
using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class HitMarker2 : MonoBehaviour
{
    public static HitMarker2 Instance;

    private void Awake()
    {
        Instance = this;
    }

    private Vector3 desiredSize;


    private RawImage image;
    private void Start()
    {
        image = GetComponent<RawImage>();
        
    }

    public void StartHitMarker(Color color)
    {
        image.color = color;
        transform.localRotation = Quaternion.Euler(0,0,Random.Range(-15,15f));
        transform.localScale = Vector3.one*1.3f;
        desiredSize = color == Color.red ?Vector3.one * 0.7f : Vector3.one * 0.5f;
        speed = 40f;
        Invoke("UpSpeed", 0.05f);
        Invoke("DelayRemove", 0.04f);
    }
    private void DelayRemove()
    {
        desiredSize = Vector3.zero;
    }

    private void UpSpeed()
    {
        speed = 25f;
    }
    
    private float speed = 40f;
    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale,desiredSize,Time.deltaTime*speed);
        transform.localRotation = Quaternion.Lerp(transform.localRotation,Quaternion.identity, Time.deltaTime*20f);
    }
}
