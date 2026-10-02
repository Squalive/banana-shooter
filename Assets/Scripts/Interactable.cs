
using System;
using Audio;

using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public UnityEvent interactEvent,highLightEvent,deSelectEvent;

    public Outline outline;

    private void Start()
    {
        if (outline)
            outline.enabled = false;

        _transform = transform;

        defaultSize = _transform.localScale;
        interactSize = defaultSize * 0.7f;
        desiredSize = defaultSize;

        highLightEvent.AddListener(HighLight);
        deSelectEvent.AddListener(DeSelect);
    }
    private void DeSelect()
    {
        outline.enabled = false;
    }
    private void HighLight()
    {
        outline.enabled = true;
    }

    private Vector3 desiredSize,defaultSize,interactSize;
    public bool isAnimated = false;

    private Transform _transform;

    private void Update()
    {
        if (!isAnimated) return;
        
        _transform.localScale = Vector3.Lerp(_transform.localScale,desiredSize,Time.deltaTime*15f);
    }

    
    public void InteractScale()
    {
        AudioManager.Instance.Play("tip");
        desiredSize = interactSize;
        
        CancelInvoke(nameof(BackToDefault));
        Invoke(nameof(BackToDefault),0.1f);
    }

    void BackToDefault()
    {
        desiredSize = defaultSize;
    }
}
