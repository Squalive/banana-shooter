using System;
using System.Collections.Generic;
using Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

public class KeyTip : MonoBehaviour
{
    public static KeyTip Instance;
    
    [SerializeField] private CanvasGroup group;

    [SerializeField] private TextMeshProUGUI keyText;

    [SerializeField] private LocalizeStringEvent des;
    private float desiredAlpha = 0;
    
    [Serializable]
    public class TipItem
    {
        public string key, des;

        public TipItem(string k, string d)
        {
            key = k;
            des = d;
        }
    }

    private Queue<TipItem> items = new Queue<TipItem>();
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Invoke(nameof(ClearTip),1f);
        // if(NetworkManager.Instance.gameMode==GameMode.ReadyUpScene)
        //     SetText("Esc","TabKeyTip");
    }

    public void SetText(string key,string d)
    {
        TipItem item = new TipItem(key,d);
        
        items.Enqueue(item);
    }

    void HalfAlpha()
    {
        desiredAlpha = 0.7f;
        Invoke(nameof(FullAlpha),0.5f);
    }

    void FullAlpha()
    {
        desiredAlpha = 1;
        Invoke(nameof(HalfAlpha),0.5f);
    }

    void ClearAlpha()
    {
        desiredAlpha = 0;
        CancelInvoke(nameof(HalfAlpha));
        CancelInvoke(nameof(FullAlpha));
        CancelInvoke(nameof(ClearTip));
        Invoke(nameof(ClearTip),1f);
    }

    void ClearTip()
    {
        tipping = false;
    }

    private bool tipping = true;
    private void Update()
    {
        if (!tipping)
        {
            if (items.Count > 0)
            {
                tipping = true;
                AudioManager.Instance.Play("tip");
                TipItem item = items.Dequeue();
            
                keyText.SetText(item.key);
        
                des.SetEntry(item.des);
                des.RefreshString();

                desiredAlpha = 1f;
                Invoke(nameof(HalfAlpha),0.5f);
                Invoke(nameof(ClearAlpha),7.5f);
            }
            
        }
        @group.alpha = Mathf.Lerp(@group.alpha, desiredAlpha, Time.deltaTime * 4f);
    }
}
