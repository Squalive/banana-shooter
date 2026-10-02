
using System;
using System.Collections;
using Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSelect : MonoBehaviour,IPointerEnterHandler
{
    private bool _interactable=true;

    private IEnumerator Start()
    {
        yield return null;
        if (TryGetComponent(out Button btn))
        {
            _interactable = btn.interactable;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(_interactable)
            AudioManager.Instance.Play("select_button");
    }
}
