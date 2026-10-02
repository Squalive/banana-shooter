using System.Collections;
using System.Collections.Generic;

using Manager;
using UnityEngine;

public class ButtonInteratorAnimation : MonoBehaviour
{
    public Outline outline;
    
    private Vector3 _desiredSize,_defaultSize,_desiredPos,_defaultPos;
    private Transform _btn;
    
    private AudioSource _source;
    void Start()
    {
        _btn = outline.transform;
        _defaultSize = _btn.localScale;
        _desiredSize = _defaultSize;
        _defaultPos = _btn.localPosition;
        _desiredPos = _defaultPos;
        _source = GetComponent<AudioSource>();
    }

    private void Update()
    {
        _btn.localScale = Vector3.Lerp(_btn.localScale, _desiredSize, Time.deltaTime * 10f);
        _btn.localPosition = Vector3.Lerp(_btn.localPosition,_desiredPos,Time.deltaTime*10f);
    }
    
    
    public void Click()
    {
        float size = 0.85f;
        _desiredSize = _defaultSize*size;
        float y = 0.05f;
        _desiredPos = _defaultPos - new Vector3(0, y, 0);
        CancelInvoke("Clear");
        Invoke("Clear",0.1f);
        _source.PlayOneShot(PrefabManager.Instance.buttonPress);
    }
    
    void Clear()
    {
        _desiredSize = _defaultSize;
        _desiredPos = _defaultPos;
    }
}
