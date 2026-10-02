
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class TutorialItem : MonoBehaviour
{
    [SerializeField] private Transform item;
    // [SerializeField] TextMeshProUGUI text0, text1;

    [SerializeField] private LocalizeStringEvent stringEvent,stringEvent1;

    private Vector3 ogPos;
    public void SetValues(string entry,float time)
    {
        //stringEvent.StringReference;
        stringEvent.SetEntry(entry);
        stringEvent1.SetEntry(entry);
        
        stringEvent.RefreshString();
        stringEvent1.RefreshString();

        ogPos = item.localPosition;
        
        Invoke(nameof(Clear),time);
    }
    Vector3 desiredPos = Vector3.zero;

    void Clear()
    {
        desiredPos = ogPos;
        
        Destroy(gameObject,3f);
    }

    private void Update()
    {
        item.localPosition = Vector3.Lerp(item.localPosition,desiredPos, Time.deltaTime*5f);
    }
}
