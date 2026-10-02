
using System;
using System.Collections.Generic;

using Cosmetic;
using Manager;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseMenu : MonoBehaviour
{
    public TextMeshProUGUI nameText, priceText;
    public RawImage image;
    public Transform content;
    private void Start()
    {
        Request();
    }

    void Request()
    {
        requestPrices.Set(SteamInventory.RequestPrices(),OnGetPrices);
    }

    private string currency="";
    private void OnGetPrices(SteamInventoryRequestPricesResult_t param, bool biofailure)
    {
        if (biofailure || param.m_result==EResult.k_EResultFail)
        {
            Debug.LogError("Get prices fail");
            Request();
            return;
        }
        currency = param.m_rgchCurrency;
        uint len = SteamInventory.GetNumItemsWithPrices();

        SteamItemDef_t[] items = new SteamItemDef_t[len];
        ulong[] prices = new ulong[len];
        ulong[] basePrices = new ulong[len];
        if (SteamInventory.GetItemsWithPrices(items, prices, basePrices, len))
        {
            for (int i = 0; i < len; i++)
            {
                if ( CosmeticManager.ItemIdToItem.TryGetValue( items[ i ].m_SteamItemDef, out var it ) )
                {
                    cosmetics.Add( new PurchasableItem( prices[ i ], it ) );
                }
            }
        }

        max = (int)len;
        if (len > 0 && image!=null)
        {
            index = 0;
            image.texture = cosmetics[index].item.icon;
            nameText.SetText(cosmetics[index].item.displayName);
            float price = cosmetics[index].price / 100f;
            priceText.SetText( price.ToString("F2")+ " " + currency);
            
            SetPage();
        }

        if (display != null)
        {
            display.SetActive(true);
            loading.SetActive(false);
        }
        
    }

    private int max = 0;

    public GameObject display, loading;
    
    public void NextItem(int next)
    {
        CancelInvoke(nameof(Next));
        index += next;
        if (index >= max) index = 0;
        else if (index < 0) index = max - 1;
        image.texture = cosmetics[index].item.icon;
        nameText.SetText(cosmetics[index].item.displayName);
        float price = cosmetics[index].price / 100f;
        priceText.SetText( price.ToString("F2")+ " " + currency);
        
        SetPage();
    }

    void SetPage()
    {
        for (int i = 0; i < content.childCount;i++)
        {
            Destroy(content.GetChild(i).gameObject);
        }
        for (int i = 0; i < max; i++)
        {
            Instantiate(i == index ? PrefabManager.Instance.GetPrefab("SelectPages") : PrefabManager.Instance.GetPrefab("DeSelectPages"),content);
        }
        
        Invoke(nameof(Next),5f);
    }

    void Next()
    {
        NextItem(1);
    }
    private int index = 0;

    [Serializable]
    public class PurchasableItem
    {
        public ulong price = 0;
        public CosmeticItem item;

        public PurchasableItem(ulong _price, CosmeticItem _item)
        {
            price = _price;
            item = _item;
        }
    }
    public List<PurchasableItem> cosmetics = new List<PurchasableItem>();

    protected CallResult<SteamInventoryRequestPricesResult_t> requestPrices = new CallResult<SteamInventoryRequestPricesResult_t>();
    protected CallResult<SteamInventoryStartPurchaseResult_t> startPurchase = new CallResult<SteamInventoryStartPurchaseResult_t>();

    public CanvasGroup group;
    private float desiredAlpha;

    
    public void StartPurchase()
    {
        startPurchase.Set(SteamInventory.StartPurchase(
            new SteamItemDef_t[] {(SteamItemDef_t) cosmetics[index].item.itemdefid}, new uint[] {1}, 1),OnStartPurchase);
    }

    private void OnStartPurchase(SteamInventoryStartPurchaseResult_t param, bool biofailure)
    {
        Debug.Log(biofailure);
    }
}
