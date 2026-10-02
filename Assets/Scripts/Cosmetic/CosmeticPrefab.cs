using System;
using System.Linq;
using System.Text;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cosmetic
{
    public class CosmeticPrefab : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public SteamItemStored ItemStored;
        public CosmeticItem selectItem;
        public TextMeshProUGUI amountText;
        public GameObject newItemTag;
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ItemStored == null) return;
            CosmeticMenu.Instance.nameText.SetText(nameText);
            string description = selectItem.description;
            if (description.Contains("\\n"))
            {
                for (int i = 0; i < description.Length; i++)
                {
                    if (description[i] == '\\')
                    {
                        if (i + 2 <= description.Length)
                        {
                            string temp = description.Substring(i,2);
                            if (temp.ToLower() == "\\n")
                            {
                                description = description.Replace(temp, Environment.NewLine);
                            }
                        }
                    }
                }
            }
            CosmeticMenu.Instance.description.SetText(description);
            Invoke(nameof(ShowDetailed),.5f);
        }

        public RectTransform detailed;

        private string nameText;

        void ShowDetailed()
        {
            if (ItemStored == null) return;
            CosmeticMenu.Instance.nameText.SetText(nameText);

            string description = selectItem.description;
            if (description.Contains("\\n"))
            {
                for (int i = 0; i < description.Length; i++)
                {
                    if (description[i] == '\\')
                    {
                        if (i + 2 <= description.Length)
                        {
                            string temp = description.Substring(i,2);
                            if (temp.ToLower() == "\\n")
                            {
                                description = description.Replace(temp, Environment.NewLine);
                            }
                        }
                    }
                }
            }
        
        
            CosmeticMenu.Instance.description.SetText(description);
            CosmeticMenu.Instance.fitter.SetLayoutVertical();
            CosmeticMenu.Instance.detailed.position = detailed.position;
        
            CosmeticMenu.Instance.detailedGroup.alpha = 1;
        }

        void CloseDetailed()
        {
            if (ItemStored == null) return;
            CosmeticMenu.Instance.detailedGroup.alpha = 0;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CloseDetailed();
            CancelInvoke(nameof(ShowDetailed));
        }

        public void SetItem(CosmeticItem cosmeticItem,bool myInventory = false,SteamItemStored newItem = null)
        {
            selectItem = cosmeticItem;
            ItemStored = newItem;

            icon.texture = cosmeticItem.icon;
            StringBuilder nameWithColor = new StringBuilder(cosmeticItem.displayName + "\n");
            Color color = cosmeticItem.GetColor();
            text.color = color;
            bg.color = color;
            

            if (newItem != null)
            {
                newItemTag.SetActive(newItem.New);
                
                selected.SetActive(myInventory&&InventoryManager.Instance.cosmeticIndex.ids.Contains(newItem.itemDetails.m_itemId
                    .m_SteamItemInstanceID) || myInventory&&InventoryManager.Instance.cosmeticIndex.weaponIds.Contains(newItem.itemDetails.m_itemId
                    .m_SteamItemInstanceID));
                
                nameText = selectItem.displayName + " (" + selectItem.GetRarity()+")\n";
                if (ItemStored.properties.ContainsKey("color"))
                {
                    nameText += $"Color : <color={ItemStored.GetColorString()}>{ItemStored.properties["color"]}</color>\n";
                }
                if (ItemStored.properties.ContainsKey("shiny") && ItemStored.properties["shiny"]!="0")
                {
                    nameText += $"Shiny : {ItemStored.properties["shiny"]}\n";
                }
                if (ItemStored.properties.TryGetValue("particle",out var particle))
                {
                
                    nameText+=($"Particle : <color=yellow>{particle}</color>)\n");
                }

                nameText += $"Rarity Score : {ItemStored.GetRarityScore()}\n";
                // nameText += $"Index : {ItemStored.Time}\n";
                
                if(newItem.properties.ContainsKey("color"))
                    nameWithColor.Append($"<color={newItem.GetColorString()}>{newItem.properties["color"]}</color>\n");
                if(newItem.properties.TryGetValue("particle",out var p))
                    nameWithColor.Append($"Particle : {p}\n");
                if (newItem.properties.ContainsKey("shiny"))
                {
                    if(newItem.properties["shiny"]!="0")
                        nameWithColor.Append("<color=yellow>shiny " + newItem.properties["shiny"] + "</color>");
                }
                
                if (newItem.amountGained > 1)
                {
                    amountText.SetText(newItem.amountGained.ToString());
                }

                // if (newItem.amountGained > 1)
                // {
                //     nameWithColor = new StringBuilder(newItem.amountGained + " " + nameWithColor);
                // }
                
                // if (myInventory)
                // {
                //     if (InventoryManager.Instance.cosmeticIndex.ids != null)
                //     {
                //         switch (cosmeticItem.type)
                //         {
                //             case CosmeticItem.Type.Hat:
                //                 if(newItem.itemDetails.m_itemId.m_SteamItemInstanceID==InventoryManager.Instance.cosmeticIndex.ids[0])
                //                     selected.SetActive(true);
                //                 break;
                //             case CosmeticItem.Type.Face:
                //                 if(newItem.itemDetails.m_itemId.m_SteamItemInstanceID==InventoryManager.Instance.cosmeticIndex.ids[1])
                //                     selected.SetActive(true);
                //                 break;
                //             case CosmeticItem.Type.Shoes:
                //                 if(newItem.itemDetails.m_itemId.m_SteamItemInstanceID==InventoryManager.Instance.cosmeticIndex.ids[2])
                //                     selected.SetActive(true);
                //                 break;
                //             case CosmeticItem.Type.Hair:
                //                 if(newItem.itemDetails.m_itemId.m_SteamItemInstanceID==InventoryManager.Instance.cosmeticIndex.ids[3])
                //                     selected.SetActive(true);
                //                 break;
                //             case CosmeticItem.Type.Clothes:
                //                 if(newItem.itemDetails.m_itemId.m_SteamItemInstanceID==InventoryManager.Instance.cosmeticIndex.ids[4])
                //                     selected.SetActive(true);
                //                 break;
                //             case CosmeticItem.Type.Pant:
                //                 if(newItem.itemDetails.m_itemId.m_SteamItemInstanceID==InventoryManager.Instance.cosmeticIndex.ids[5])
                //                     selected.SetActive(true);
                //                 break;
                //             case CosmeticItem.Type.Particle: break;
                //             default:
                //                 if (cosmeticItem.type != CosmeticItem.Type.Rag &&cosmeticItem.type != CosmeticItem.Type.Box && cosmeticItem.type != CosmeticItem.Type.Other)
                //                 {
                //                     int index = (int) cosmeticItem.type - CosmeticMenu.CosmeticOffset;
                //                     if(newItem.itemDetails.m_itemId.m_SteamItemInstanceID==InventoryManager.Instance.cosmeticIndex.weaponIds[index])
                //                         selected.SetActive(true);
                //                 }
                //                 
                //                 break;
                //         }
                //     }
                //     
                // }
            }
            
            text.SetText(nameWithColor);
       
            if(myInventory)
                btn.onClick.AddListener(delegate { CosmeticMenu.Instance.SelectCosmetic(cosmeticItem,newItem); });
            else if(newItem!=null)
                btn.onClick.AddListener(delegate { CosmeticMenu.Instance.SelectUseCosmetic(cosmeticItem,newItem); });

            // if (CosmeticMenu.Instance!=null && gameObject.activeSelf)
            // {
            //     gameObject.SetActive((CosmeticMenu.Instance.selectType == cosmeticItem.type ||
            //                                CosmeticMenu.Instance.selectType == CosmeticItem.Type.None) &&
            //                               CosmeticMenu.Instance.selectInventoryType == cosmeticItem.inventoryType);
            // }
        }

        [SerializeField] public GameObject selected;
        [SerializeField] private TextMeshProUGUI text;

        [SerializeField] private RawImage bg,icon;
        [SerializeField] private Button btn;
    }
}
