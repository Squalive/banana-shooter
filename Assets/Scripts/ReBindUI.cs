using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ReBindUI : MonoBehaviour
{
    [SerializeField] private InputActionReference inputActionReference;
    [SerializeField] private bool includeMouse;
    [Range(0, 10)][SerializeField] private int selectBinding;
    [SerializeField] private InputBinding.DisplayStringOptions displayStringOptions;

    [Header("Binding Info -- DO NOT EDIt")]
    [SerializeField]
    private InputBinding inputBinding;

    private int bindingIndex;

    private string actionName;

    public Button rebindBtn;

    [Header("UI")][SerializeField] private TextMeshProUGUI text;

    private void OnEnable()
    {
        rebindBtn.onClick.AddListener(DoReBind);


        if (inputActionReference != null)
        {
            GetBindingInfo();
            GameManager.LoadBindingOverride(actionName);
            UpdateUI();
        }
        GameManager.RebindComplete += UpdateUI;
        GameManager.RebindCanceled += UpdateUI;
    }

    public void Refresh()
    {
        if (inputActionReference != null)
        {
            GetBindingInfo();
            GameManager.LoadBindingOverride(actionName);
            UpdateUI();
        }
    }

    private void OnDisable()
    {
        rebindBtn.onClick.RemoveListener(DoReBind);
        GameManager.RebindComplete -= UpdateUI;
        GameManager.RebindCanceled -= UpdateUI;
    }

    private void OnValidate()
    {
        if (inputActionReference == null) return;
        GetBindingInfo();
        UpdateUI();
    }

    void GetBindingInfo()
    {
        if (inputActionReference.action != null)
        {
            actionName = inputActionReference.action.name;
            if (inputActionReference.action.bindings.Count > selectBinding)
            {
                inputBinding = inputActionReference.action.bindings[selectBinding];
                bindingIndex = selectBinding;
            }
        }


    }

    void UpdateUI()
    {
        if (text != null)
        {
            if (Application.isPlaying)
            {
                text.SetText(GameManager.GetBindingName(actionName, bindingIndex));
            }
            else
            {
                text.SetText(inputActionReference.action.GetBindingDisplayString(bindingIndex));
            }
        }

    }

    void DoReBind()
    {
        GameManager.StartRebind(actionName, bindingIndex, text);
    }
}
