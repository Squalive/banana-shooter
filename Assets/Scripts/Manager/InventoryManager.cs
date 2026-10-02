using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cosmetic;
using Multiplayer;
using Quest;
using Riptide;
using Steamworks;
using UnityEngine;

namespace Manager
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance;

        public static Action OnGetInventory;
        
        public static Action NewItemAdded;

        public CosmeticIndex cosmeticIndex = new CosmeticIndex();
    
        public enum InventoryHandleType
        {
            None,
            GenerateItems,
            SerializeInventory,
            DeserializeInventory,
            Exchange,
            ItemDrop,
            Store,
            GetInventory,
            SerializeNewItem,
            DeserializeNewItem,
        }

        [Serializable]
        public class CosmeticIndex
        {
            public int hatIndex=-1,faceIndex=-1,shoesIndex=-1,hairIndex=-1,clothesIndex=-1,pantIndex=-1;
            public int hatParticle=-1,faceParticle=-1,shoesParticle=-1,hairParticle=-1,clothesParticle=-1,pantParticle = -1;
            public Color hatColor=Color.clear, faceColor=Color.clear, shoesColor=Color.clear, hairColor=Color.clear, clothesColor=Color.clear ,pantColor = Color.clear;
            public float hatShiny = 0, faceShiny=0,shoesShiny=0,hairShiny=0,clothesShiny=0,pantShiny = 0;
            public int musicBoxIndex = 0;
            public int menuSceneIndex = 0;
            public ulong[] ids = new ulong[8];
        
            public ulong[] weaponIds = new ulong[30];
            public ushort[] weaponIndex = new ushort[30];

            public CosmeticIndex(int hatIndex, int faceIndex, int shoesIndex, int hairIndex, int clothesIndex,int pantIndex,
                Color hatColor, Color faceColor, Color shoesColor, Color hairColor, Color clothesColor,Color pantColor,
                float hatShiny,float faceShiny,float shoesShiny,float hairShiny,float clothesShiny,float pantShiny,
                int hatParticle,int faceParticle,int shoesParticle,int hairParticle,int clothesParticle,int pantParticle,
                ushort[] weaponIndex)
            {
                this.hatIndex = hatIndex;
                this.faceIndex = faceIndex;
                this.shoesIndex = shoesIndex;
                this.hairIndex = hairIndex;
                this.pantIndex = pantIndex;
                this.clothesIndex = clothesIndex;
                this.hatColor = hatColor;
                this.faceColor = faceColor;
                this.shoesColor = shoesColor;
                this.hairColor = hairColor;
                this.pantColor = pantColor;
                this.clothesColor = clothesColor;
                this.hatShiny = hatShiny;
                this.faceShiny = faceShiny;
                this.shoesShiny = shoesShiny;
                this.hairShiny = hairShiny;
                this.clothesShiny = clothesShiny;
                this.pantShiny = pantShiny;
                this.weaponIndex = weaponIndex;
                this.hatParticle = hatParticle;
                this.faceParticle = faceParticle;
                this.shoesParticle = shoesParticle;
                this.hairParticle = hairParticle;
                this.clothesParticle = clothesParticle;
                this.pantParticle = pantParticle;
                
            }

            public CosmeticIndex()
            {
                
            }
        }

        // public InventoryHandleType handleType= InventoryHandleType.None;

        public Queue<InventoryHandleType> HandleQueue { get; } = new Queue<InventoryHandleType>();

        public bool CrateOpenAnimationEnable { get; set; } = true;
        public Queue<SteamItemStored> CrateOpenQueue { get; } = new Queue<SteamItemStored>();

        public static uint MaxItems = 100000;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                foreach (var itemsPrefab in itemsPrefabs)
                {
                    prefabs.Add(itemsPrefab.name,itemsPrefab);
                }
            }
        }

        private void Start()
        {
            if (Instance == this)
            {
                foreach (var cosmetic in cosmeticItems)
                {
                    if ( cosmetic != null )
                        CosmeticManager.ItemIdToItem.TryAdd( cosmetic.itemdefid, cosmetic );
                }
                
            }
        }

        void Test()
        {
            if (SteamApps.BIsDlcInstalled(new AppId_t(2238100)))
            {
                HandleQueue.Enqueue(InventoryHandleType.ItemDrop);
                SteamInventory.AddPromoItem(out inventoryHandle, (SteamItemDef_t) 223);
            }
        }

        public void TryToSerializeItem()
        {
            List<SteamItemInstanceID_t> ids = new List<SteamItemInstanceID_t>();

            for (int i = 0; i < cosmeticIndex.ids.Length+cosmeticIndex.weaponIds.Length; i++)
            {
                if (i < cosmeticIndex.ids.Length)
                {
                    if(cosmeticIndex.ids[i]!=0)
                        ids.Add(new SteamItemInstanceID_t(cosmeticIndex.ids[i]));
                }
                else
                {
                    if (cosmeticIndex.weaponIds[i - cosmeticIndex.ids.Length] != 0)
                    {
                        ids.Add(new SteamItemInstanceID_t(cosmeticIndex.weaponIds[i-cosmeticIndex.ids.Length]));
                    }
                }
            }
            EquippedItems.Clear();

            HandleQueue.Enqueue(InventoryHandleType.SerializeInventory);
            if (SteamInventory.GetItemsByID(out inventoryHandle,ids.ToArray(), (uint)ids.Count))
            {
                Debug.Log("Try to serialize inventory...");
            }
        }


        public static float sizeFactor = 1f;
        private void OnEnable()
        {
            result = Callback<SteamInventoryResultReady_t>.Create(OnResultLoaded);
        }

        void SetTags(SteamItemStored steamItemStored,CosmeticItem cosmeticItem, string[] tags)
        {
            steamItemStored.properties.Add("rarity", cosmeticItem.rarity.ToString());
            if (tags != null)
            {
                foreach (var t in tags)
                {
                    if (t.Contains("color"))
                    {
                        string color = t.Remove(0, 6);
                        steamItemStored.properties.Add("color", color);
                                
                    }
                    else if (t.Contains("shiny"))
                    {
                        string shiny = t.Remove(0, 6);
                        steamItemStored.properties.Add("shiny", shiny);
                                
                    }
                    else if (t.Contains("particle"))
                    {
                        string particle = t.Remove(0, 9);
                        steamItemStored.properties.Add("particle", particle);
                    }
                }
            }
        }

        public Queue<SteamItemStored> UpComingItems = new();

        public Action<CosmeticItem,SteamItemStored> OnGetBox;

        public static List<CosmeticItem> EquippedItems = new();
        private void OnResultLoaded(SteamInventoryResultReady_t resultT)
        {
            var handleType = HandleQueue.Count > 0 ?  HandleQueue.Dequeue() : InventoryHandleType.None;
            Debug.Log(handleType);
            // if (handleType == InventoryHandleType.None)
            // {
            //     SteamInventory.DestroyResult(resultT.m_handle);
            //     return;
            // }

            //Check if call secceeded or not
            if (resultT.m_result != EResult.k_EResultOK || SteamInventory.GetResultStatus(resultT.m_handle) != EResult.k_EResultOK)
            {
                Debug.LogError("Failed to load inventory " + resultT.m_result);
                SteamInventory.DestroyResult(inventoryHandle);
                return;
            }

            uint nItems;
            SteamItemDetails_t[] resultItems;
             if (handleType == InventoryHandleType.DeserializeInventory || handleType == InventoryHandleType.DeserializeNewItem)
            {
                foreach (var clientData in PendingUserSteamIds)
                {
                    CSteamID steamId = new CSteamID(clientData.SteamId);
                    if (SteamInventory.CheckResultSteamID(resultT.m_handle, steamId))
                    {
                        Debug.Log($"{steamId} Inventory Result Got loaded");

                        CosmeticIndex authorizedCosmetic = handleType == InventoryHandleType.DeserializeNewItem ? clientData.CosmeticIndex : new CosmeticIndex();
                        
                        //Get the inventory of the player
                        
                        nItems = MaxItems;
                        resultItems = new SteamItemDetails_t[nItems];
                        SteamInventory.GetResultItems(resultT.m_handle, resultItems, ref nItems);

                        CosmeticItem cosmeticItem=null;

                        Color color=Color.clear;

                        float shiny=0;

                        int particle=-1;

                        for (int i = 0; i < nItems; i++)
                        {
                            int itemdefid = resultItems[i].m_iDefinition.m_SteamItemDef;
                            if (!CosmeticManager.ItemIdToItem.TryGetValue(itemdefid,out cosmeticItem))
                            {
                                Debug.Log($"Collection doesnt contain {itemdefid}");
                                continue;
                            }
                            
                            propertyValueBuffer = PropertyValueStringLengthMax;
                            SteamInventory.GetResultItemProperty(resultT.m_handle, (uint) i, "tags", out var tags,
                                ref propertyValueBuffer);

                            int itemIndex = cosmeticItem.index;
                            color = Color.clear;
                            shiny = 0;
                            particle = -1;

                            if (tags != null)
                            {
                                string[] tagss = tags.Split(";");
                                foreach (var t in tagss)
                                {
                                    if (t.Contains("color"))
                                    {
                                        string colorStr = t.Remove(0, 6);

                                        color = GetItemColor(cosmeticItem, colorStr);
                                    }
                                    else if (t.Contains("shiny"))
                                    {
                                        string shinyStr = t.Remove(0, 6);

                                        shiny = GetShiny(shinyStr);
                                    }
                                    else if (t.Contains("particle"))
                                    {
                                        string particleStr = t.Remove(0, 9);

                                        particle = GetParticleItemdefid(particleStr);
                                    }
                                }
                            }

                            //Initialize cosmetics
                            switch (cosmeticItem.inventoryType)
                            {
                                case CosmeticItem.InventoryType.Cosmetics:
                                    switch (cosmeticItem.type)
                                    {
                                        case CosmeticItem.Type.Hair:
                                            authorizedCosmetic.hairIndex = itemIndex;
                                            authorizedCosmetic.hairColor = color;
                                            authorizedCosmetic.hairShiny = shiny;
                                            authorizedCosmetic.hairParticle = particle;
                                            break;
                                        case CosmeticItem.Type.Hat:
                                            authorizedCosmetic.hatIndex = itemIndex;
                                            authorizedCosmetic.hatColor = color;
                                            authorizedCosmetic.hatShiny = shiny;
                                            authorizedCosmetic.hatParticle = particle;
                                            break;
                                        case CosmeticItem.Type.Face:
                                            authorizedCosmetic.faceIndex = itemIndex;
                                            authorizedCosmetic.faceColor = color;
                                            authorizedCosmetic.faceShiny = shiny;
                                            authorizedCosmetic.faceParticle = particle;
                                            break;
                                        case CosmeticItem.Type.Shoes:
                                            authorizedCosmetic.shoesIndex = itemIndex;
                                            authorizedCosmetic.shoesColor = color;
                                            authorizedCosmetic.shoesShiny = shiny;
                                            authorizedCosmetic.shoesParticle = particle;
                                            break;
                                        case CosmeticItem.Type.Clothes:
                                            authorizedCosmetic.clothesIndex = itemIndex;
                                            authorizedCosmetic.clothesColor = color;
                                            authorizedCosmetic.clothesShiny = shiny;
                                            authorizedCosmetic.clothesParticle = particle;
                                            break;
                                        case CosmeticItem.Type.Pant:
                                            authorizedCosmetic.pantIndex = itemIndex;
                                            authorizedCosmetic.pantColor = color;
                                            authorizedCosmetic.pantShiny = shiny;
                                            authorizedCosmetic.pantParticle = particle;
                                            break;
                                        case CosmeticItem.Type.MusicBox:
                                            authorizedCosmetic.musicBoxIndex = itemIndex;
                                            break;
                                        case CosmeticItem.Type.MenuScene:
                                            authorizedCosmetic.menuSceneIndex = itemIndex;
                                            break;
                                    }
                                    break;
                                case CosmeticItem.InventoryType.Weapon:
                                    int offset = (int) cosmeticItem.type - CosmeticMenu.CosmeticOffset;

                                    authorizedCosmetic.weaponIndex[offset] = (ushort) cosmeticItem.index;
                                    
                                    break;
                            }
                        }

                        if (handleType == InventoryHandleType.DeserializeInventory)
                        {
                            clientData.InitializeCosmetics(authorizedCosmetic);
                        
                            NetworkServerManager.Iinstance.SendClientInitialized(clientData.Id); 
                        }
                        else
                        {
                            if (cosmeticItem != null)
                            {
                                Message msg = Message.Create(MessageSendMode.Reliable,(ushort)ServerToClientId.ChangeCosmetic);
                                msg.Add(clientData.Id);
                                msg.Add((ushort) cosmeticItem.type);
                                msg.Add(cosmeticItem.index);
                                msg.Add(color);
                                msg.Add(shiny);
                                msg.Add(particle);
                                NetworkServerManager.Instance.Server.SendToAll(msg);
                                
                            }
                            
                        }

                        PendingUserSteamIds.Remove(clientData);
                        return;
                    }
                }
                Debug.Log($"Doesnt Find The Inventory Result({resultT.m_handle.m_SteamInventoryResult}) Owner");
                SteamInventory.DestroyResult(resultT.m_handle);
                return;
            }

            //Check if result belongs to correct user
            CSteamID expectedId = NetworkManager.Instance.steamId;
            if (!SteamInventory.CheckResultSteamID(resultT.m_handle, expectedId))
            {
                SteamInventory.DestroyResult(resultT.m_handle);

                Debug.LogError("Tried to get an inventory that does not belong to self");
                return;
            }
            
            nItems = MaxItems;
            resultItems = new SteamItemDetails_t[nItems];
            SteamInventory.GetResultItems(inventoryHandle, resultItems, ref nItems);

            if (handleType == InventoryHandleType.GetInventory)
            {
                InventoryItems = new Dictionary<ulong, SteamItemStored>();
                
                Invoke(nameof(Test),1.5f);
            }
            
            for (uint i = 0; i < nItems; i++)
            {
                ulong uid = resultItems[i].m_itemId.m_SteamItemInstanceID;
                int itemdefid = resultItems[i].m_iDefinition.m_SteamItemDef;
                if (!CosmeticManager.ItemIdToItem.ContainsKey(itemdefid))
                {
                    // Debug.LogError("doesnt contain itemdefid");
                    continue;
                }

                propertyValueBuffer = PropertyValueStringLengthMax;
                SteamInventory.GetResultItemProperty(inventoryHandle,  i, "tags", out var tags,
                    ref propertyValueBuffer);

                CosmeticItem cosmeticItem = CosmeticManager.ItemIdToItem[itemdefid];
                
                int amount = resultItems[i].m_unQuantity;
                SteamItemStored steamItemStored;

                switch (handleType)
                {
                    case InventoryHandleType.GetInventory:
                        steamItemStored = new SteamItemStored(resultItems[i],InventoryItems.Count - 1,false);

                        steamItemStored.amountGained = amount;

                        SetTags(steamItemStored, cosmeticItem, tags?.Split(';'));
                        
                        InventoryItems.Add(uid, steamItemStored);
                        
                        break;
                    case InventoryHandleType.Exchange:
                        if (InventoryItems.TryGetValue(uid, out steamItemStored))
                        {
                            if (amount >= 1 && resultItems[i].m_unFlags != 1 << 8)
                            {
                                steamItemStored.amountGained = amount;
                                
                                // if (UIManager.Instance)
                                // {
                                //     string nameText = cosmeticItem.name + " (" + cosmeticItem.GetRarity() + ")\n";
                                //     if (steamItemStored.properties.TryGetValue("color", out var colorString))
                                //     {
                                //         nameText +=
                                //             $"<size=20>Color : <color={steamItemStored.GetColorString()}>{colorString}</color>\n";
                                //     }
                                //
                                //     if (steamItemStored.properties.ContainsKey("shiny") && steamItemStored.properties["shiny"] != "0")
                                //     {
                                //         nameText += $"Shiny : {steamItemStored.properties["shiny"]}\n";
                                //     }
                                //
                                //     UIManager.Instance.newItemNameText.SetText(nameText);
                                //
                                //     UIManager.Instance.newItem.SetActive(true);
                                //     AudioManager.Instance.Play("Reward");
                                //
                                //     UIManager.Instance.NewItemImage.texture = cosmeticItem.icon;
                                // }
                            }
                            else
                            {
                                if (CosmeticMenu.Instance)
                                {
                                    foreach (var instandId in Instance.cosmeticIndex.ids)
                                    {
                                        if (instandId == steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID)
                                        {
                                            CosmeticMenu.Instance.DeEquip(steamItemStored);
                                        }
                                    }
                                }
                                if (InventoryItems.ContainsKey(uid))
                                    InventoryItems.Remove(uid);
                            }
                            
                        }
                        else if(amount >= 1 && resultItems[i].m_unFlags != 1 << 8)
                        {
                            steamItemStored = new SteamItemStored(resultItems[i],InventoryItems.Count - 1,true);

                            if(tags!=null)
                                SetTags(steamItemStored, cosmeticItem, tags.Split(';'));
                            
                            InventoryItems.Add(uid, steamItemStored);
                            
                            UpComingItems.Enqueue(steamItemStored);
                            StartCoroutine(NewTagDisappear(steamItemStored));

                            CheckBulkOpen();
                            CosmeticItem fromItem =  CosmeticManager.ItemIdToItem[resultItems[0].m_iDefinition.m_SteamItemDef];
                            if (fromItem.type == CosmeticItem.Type.Box)
                            {
                                if (!firstCrate)
                                {
                                    firstCrate = true;
                                    AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.BEING_EXPERT);
                                }
                        
                                QuestManager.Instance.GetProgress(QuestType.OpenBox);
                                OnGetBox?.Invoke(cosmeticItem,steamItemStored);
                            }
                            // else if (UIManager.Instance)
                            // {
                            //     string nameText = cosmeticItem.name + " (" + cosmeticItem.GetRarity() + ")\n";
                            //     if (steamItemStored.properties.TryGetValue("color", out var colorString))
                            //     {
                            //         nameText +=
                            //             $"<size=20>Color : <color={steamItemStored.GetColorString()}>{colorString}</color>\n";
                            //     }
                            //
                            //     if (steamItemStored.properties.ContainsKey("shiny") && steamItemStored.properties["shiny"] != "0")
                            //     {
                            //         nameText += $"Shiny : {steamItemStored.properties["shiny"]}\n";
                            //     }
                            //
                            //     UIManager.Instance.newItemNameText.SetText(nameText);
                            //
                            //     UIManager.Instance.newItem.SetActive(true);
                            //     AudioManager.Instance.Play("Reward");
                            //
                            //     UIManager.Instance.NewItemImage.texture = cosmeticItem.icon;
                            // }
                        }
                        
                        NewItemAdded?.Invoke();
                        break;
                    // case InventoryHandleType.ItemDrop:
                    //     steamItemStored = new SteamItemStored(resultItems[i],InventoryItems.Count - 1);
                    //         
                    //     if(tags!=null)
                    //         SetTags(steamItemStored, cosmeticItem, tags.Split(';'));
                    //         
                    //     InventoryItems.Add(uid, steamItemStored);
                    //
                    //     NewItemAdded?.Invoke();
                    //     
                    //     if (UIManager.Instance)
                    //     {
                    //         string nameText = cosmeticItem.name + " (" + cosmeticItem.GetRarity() + ")\n";
                    //         if (steamItemStored.properties.TryGetValue("color", out var colorString))
                    //         {
                    //             nameText +=
                    //                 $"<size=20>Color : <color={steamItemStored.GetColorString()}>{colorString}</color>\n";
                    //         }
                    //
                    //         if (steamItemStored.properties.ContainsKey("shiny") && steamItemStored.properties["shiny"] != "0")
                    //         {
                    //             nameText += $"Shiny : {steamItemStored.properties["shiny"]}\n";
                    //         }
                    //
                    //         UIManager.Instance.newItemNameText.SetText(nameText);
                    //
                    //         UIManager.Instance.newItem.SetActive(true);
                    //         AudioManager.Instance.Play("Reward");
                    //
                    //         UIManager.Instance.NewItemImage.texture = cosmeticItem.icon;
                    //     }
                    //     
                    //     
                    //     
                    //     
                    //     
                    //     
                    //     
                    //     
                    //     
                    //     
                    //     
                    //     break;
                    case InventoryHandleType.SerializeInventory:
                        EquippedItems.Add(cosmeticItem);
                        break;
                    default:
                        if (InventoryItems.TryGetValue(uid, out steamItemStored))
                        {
                            if (amount >= 1 && resultItems[i].m_unFlags != 1 << 8)
                            {
                                steamItemStored.amountGained = amount;
                            } 
                            else
                            {
                                if (InventoryItems.ContainsKey(uid))
                                    InventoryItems.Remove(uid);
                            }
                        }
                        else if(amount >= 1 && resultItems[i].m_unFlags != 1 << 8)
                        {
                            steamItemStored = new SteamItemStored(resultItems[i],InventoryItems.Count - 1,true);

                            if(tags!=null)
                                SetTags(steamItemStored, cosmeticItem, tags.Split(';'));

                            UpComingItems.Enqueue(steamItemStored);
                            InventoryItems.Add(uid, steamItemStored);
                            CheckBulkOpen();
                            
                            StartCoroutine(NewTagDisappear(steamItemStored));
                        }
                        NewItemAdded?.Invoke();

                        if (handleType == InventoryHandleType.ItemDrop)
                        {
                            if (cosmeticItem.type == CosmeticItem.Type.Box )
                            {
                                GameManager.getBox = true;
                                if (NetworkManager.Instance.Client.IsConnected)
                                {
                                    Message message = Message.Create(MessageSendMode.Reliable, (ushort) ClientToServerId.GetOneBox);
                                    message.Add(cosmeticItem.itemdefid);
                                    NetworkManager.Instance.SendByte += message.WrittenLength;
                                    NetworkManager.Instance.Client.Send(message);
                                }
                            }
                        }
                        break;
                }
            }

            switch (handleType)
            {
                case InventoryHandleType.SerializeInventory:
                    SerializeResult(resultT);
                    break;
                case InventoryHandleType.SerializeNewItem:
                    SerializeNewItemResult(resultT);
                    break;
                case InventoryHandleType.GetInventory:
                    Initialized = true;
                    OnGetInventory?.Invoke();
                    break;
            }

            SteamInventory.DestroyResult(resultT.m_handle);
        }

        public void CheckBulkOpen()
        {
            if (CrateOpenQueue.Count <= 0) return;
            if (CrateOpenMenu.Instance)
            {
                CrateOpenMenu.Instance.DisableOpening();
            }

            var item = CrateOpenQueue.Dequeue();
            
            UInt32[] outCount = {1};
            UInt32[] inputCount = {1};
                            
            SteamItemDef_t[] outItemDefTs = {GetCrateDef(item.itemDetails.m_iDefinition.m_SteamItemDef)};
            HandleQueue.Enqueue(CrateOpenAnimationEnable ? InventoryHandleType.Exchange : InventoryHandleType.None);
                            
            SteamInventory.ExchangeItems(out inventoryHandle, outItemDefTs, outCount, 1, new []{item.itemDetails.m_itemId},
                inputCount, 1);
        }

        IEnumerator NewTagDisappear(SteamItemStored steamItemStored)
        {
            int seconds = 60 * 10;
            while (seconds>0)
            {
                yield return new WaitForSeconds(1f);
                seconds--;
            }
            
            if(steamItemStored!=null)
                steamItemStored.SetNewToFalse();
        }

        public static SteamItemDef_t GetCrateDef(int itemdefid)
        {
            SteamItemDef_t defT;
            switch (itemdefid)
            {
                case 136:
                    defT = (SteamItemDef_t) 137;
                    break;
                case 148:
                    defT = (SteamItemDef_t) 2000;
                    break;
                case 155:
                    defT = (SteamItemDef_t) 156;
                    break;
                case 158:
                    defT = (SteamItemDef_t) 178;
                    break;
                case 168:
                    defT = (SteamItemDef_t) 179;
                    break;
                case 193:
                    defT = (SteamItemDef_t) 211;
                    break;
                default:
                    defT = (SteamItemDef_t) 100;
                    break;
            }

            return defT;
        }
        public static Color GetItemColor(CosmeticItem cosmeticItem,string colorStr)
        {
            Color color = CosmeticManager.GetColor(colorStr);

            float alpha = cosmeticItem.alpha;

            if (color == Color.clear)
            {
                return Color.clear;
            }

            color.a = alpha;

            return color;
        }
        
        static float GetShiny(string shinyStr)
        {
            float shiny = 0;
            switch (shinyStr)
            {
                case "0.1":
                    shiny = 1.1f;
                    break;
                case "0.2":
                    shiny = 1.2f;
                    break;
                case "0.4":
                    shiny = 1.4f;
                    break;
                case "0.6":
                    shiny = 1.8f;
                    break;
                case "0.8":
                    shiny =2f;
                    break;
                case "0.9":
                    shiny =2.2f;
                    break;
            }

            return shiny;
        }
        
        static int GetParticleItemdefid(string particleStr)
        {
            foreach (var c in CosmeticManager.ItemIdToItem.Values)
            {
                if (c.type == CosmeticItem.Type.Particle)
                {
                    if (c.tag == particleStr)
                    {
                        return c.itemdefid;
                    }
                }
            }
            return -1;
        }
        public readonly List<ClientData> PendingUserSteamIds = new List<ClientData>();
        public void DeserializeInventory(byte[] pBuffer, ClientData user)
        {
            // for (int i = 0; i < pBuffer.Length; i++)
            // {
            //     if(pBuffer[i] != SerializeInventory[i])
            //         Debug.Log($"{i - 1024} {pBuffer[i]} {SerializeInventory[i]}");
            // }
            
            HandleQueue.Enqueue(InventoryHandleType.DeserializeInventory);
            if (SteamInventory.DeserializeResult(out inventoryHandle, pBuffer, (uint)pBuffer.Length))
            {
                PendingUserSteamIds.Add(user);

                CheckUserInventoryDeserializationFailed(user);
                
                Debug.Log($"Trying to Deserialize {user.Name}'s Inventory, Size: {pBuffer.Length}");
            }
            else
            {
                UserInventoryDeserializationFailed(user);
            }
        }
        
        public void DeserializeNewItem(byte[] pBuffer, ClientData user)
        {
            // for (int i = 0; i < pBuffer.Length; i++)
            // {
            //     if(pBuffer[i] != SerializeInventory[i])
            //         Debug.Log($"{i - 1024} {pBuffer[i]} {SerializeInventory[i]}");
            // }
            HandleQueue.Enqueue(InventoryHandleType.DeserializeNewItem);
            if (SteamInventory.DeserializeResult(out inventoryHandle, pBuffer, (uint)pBuffer.Length))
            {
                PendingUserSteamIds.Add(user);

                Debug.Log($"Trying to Deserialize {user.Name}'s New Item, Size: {pBuffer.Length}");
            }
        }
        async void CheckUserInventoryDeserializationFailed(ClientData user)
        {
            await Task.Delay(5000);
            
            if(PendingUserSteamIds.Contains(user))
                UserInventoryDeserializationFailed(user);
        }
        
        void UserInventoryDeserializationFailed(ClientData user)
        {
            NetworkServerManager.Instance.Server.DisconnectClient(user.Id);
            
            Debug.Log($"Failed To Load {user.Name}'s Inventory, kicking {user.Name} Now...");
        }

        public List<CosmeticItem> cosmeticItems = new List<CosmeticItem>();
        public static Dictionary<ulong, SteamItemStored> InventoryItems = new Dictionary<ulong, SteamItemStored>();
        // 
        private Callback<SteamInventoryResultReady_t> result;
        public static byte[] SerializeInventory {private set; get; }
        
        public static bool Initialized = false;

        public void TryToSerializeNewItem(ulong id)
        {
            HandleQueue.Enqueue(InventoryHandleType.SerializeNewItem);
            var ids = new SteamItemInstanceID_t[]{new(id)};
            
            if (SteamInventory.GetItemsByID(out inventoryHandle,ids, (uint)ids.Length))
            {
                Debug.Log($"Try to serialize new item {id}...");
            }
        }
        
        void SerializeNewItemResult(SteamInventoryResultReady_t resultT)
        {
            uint punOutBufferSize = 0;
            if (SteamInventory.SerializeResult(resultT.m_handle, null, out punOutBufferSize))
            {
                var newItem = new byte[punOutBufferSize];

                SteamInventory.SerializeResult(resultT.m_handle, newItem, out punOutBufferSize);
                
                Debug.Log($"Serialize New Item Success, Size: {punOutBufferSize}");
                
                Message message = Message.Create(MessageSendMode.Reliable,(ushort) ClientToServerId.ChangeCosmetic);

                message.Add(true);

                message.Add(newItem);
                
                NetworkManager.Instance.Client.Send(message);
            }
        }

        void SerializeResult(SteamInventoryResultReady_t resultT)
        {
            uint punOutBufferSize = 0;
            if (SteamInventory.SerializeResult(resultT.m_handle, null, out punOutBufferSize))
            {
                SerializeInventory = new byte[punOutBufferSize];

                SteamInventory.SerializeResult(resultT.m_handle, SerializeInventory, out punOutBufferSize);
                
                Debug.Log($"Serialize Inventory Success, Size: {punOutBufferSize}");
                
                if(!Initialized)
                    GetItem();
            }
        }
        void GetItem()
        {
            HandleQueue.Enqueue(InventoryHandleType.GetInventory);
            SteamInventory.GetAllItems(out inventoryHandle);
        }

        public void GetBox()
        {
            HandleQueue.Enqueue(InventoryHandleType.ItemDrop);
            SteamInventory.TriggerItemDrop(out inventoryHandle,(SteamItemDef_t)11);
            // Debug.Log("get Box  ");
        }

        private bool firstCrate = false;

        private UInt32 propertyValueBuffer;
        private UInt32 PropertyValueStringLengthMax = 100;


        public SteamInventoryResult_t inventoryHandle;

        public void GetLevelUpReward()
        {
            HandleQueue.Enqueue(InventoryHandleType.ItemDrop);
            SteamInventory.AddPromoItem(out inventoryHandle, (SteamItemDef_t) 1112);
        }

        [Serializable]
        public class ParticleItem
        {
            public string name;
            public GameObject prefab;
            public CosmeticItem cosmeticItem;
            public Vector3 cosmeticMenuSize,inGameSize;
        }
        [SerializeField] public ParticleItem[] itemsPrefabs;
        private Dictionary<string, ParticleItem> prefabs = new Dictionary<string, ParticleItem>();

        public GameObject GetItemPrefab(string key)
        {
            return prefabs[key].prefab;
        }

        public ParticleItem GetParticle(string key)
        {
            return prefabs[key];
        }
    }

    public class SteamItemStored : IComparable
    {
        public SteamItemDetails_t itemDetails;
        public Dictionary<string, string> properties = new Dictionary<string, string>();
        public int amountGained;
        public uint Time { get; private set; }
        public bool New { get; private set; }

        public SteamItemStored(SteamItemDetails_t itemDetailsT, int time, bool @new, int amountGained = 1)
        {
            itemDetails = itemDetailsT;
            if (time < 0) time = 0;
            Time = (uint)time;
            New = @new;
            
            this.amountGained = amountGained;
        }

        public void SetNewToFalse()
        {
            New = false;
        }
    
        public int GetRarityScore()
        {
            int score = 0;
            if (properties.TryGetValue("rarity", out var rarity)) score += CosmeticUtility.GetRarityScore(rarity);
            if (properties.TryGetValue("shiny", out var shiny)) score += CosmeticUtility.GetRarityScore(shiny);
            if (properties.TryGetValue("color", out var color)) score += CosmeticUtility.GetRarityScore(color);
            if (properties.TryGetValue("brand", out var brand)) score += CosmeticUtility.GetRarityScore(brand);
            if (properties.TryGetValue("particle", out var particle)) score += CosmeticUtility.GetRarityScore(particle);
            return score;
        }
        public int CompareTo(object obj)
        {
            int otherItemScore = ((SteamItemStored) obj).GetRarityScore();
            int myRarityScore = GetRarityScore();
            if (myRarityScore > otherItemScore) return -1;
            if (myRarityScore == otherItemScore) return 0;
            return 1;
        }

        public Color GetColor()
        {
            return InventoryManager.GetItemColor(CosmeticManager.ItemIdToItem[itemDetails.m_iDefinition.m_SteamItemDef],
                properties.TryGetValue("color",out var color) ? color : String.Empty);
        }

        public float GetShiny()
        {
            float shiny = 0;
            if (properties.ContainsKey("shiny"))
            {
                switch (properties["shiny"])
                {
                    case "0.1":
                        shiny = 1.1f;
                        break;
                    case "0.2":
                        shiny = 1.2f;
                        break;
                    case "0.4":
                        shiny = 1.4f;
                        break;
                    case "0.6":
                        shiny = 1.8f;
                        break;
                    case "0.8":
                        shiny =2f;
                        break;
                    case "0.9":
                        shiny =2.2f;
                        break;
                }
            }

            return shiny;
        }

        public string GetColorString()
        {
            if (!properties.ContainsKey("color"))
                return "";
            string color = properties["color"];
            
            switch (properties["color"])
            {
                case "light blue":
                    color = "#1e90ff";
                    break;
                case "blue":
                    color = "#1e90ff";
                    break;
                case "green":
                    color = "#00ff00";
                    break;
                case "orange":;
                    color = "#ffa500";
                    break;
                case "brown":
                    color = "#a52a2a";
                    break;
                case "golden":
                    color = "#ffd700";
                    break;
                case "mediumslateblue":
                    color = "#7B68EE";
                    break;
                case "pink":
                    color = "#FFC0CB";
                    break;
                case "purple":
                    color = "#8B008B";
                    break;
                case "grey":
                    color = "#708090";
                    break;
                case "white":
                    color = "white";
                    break;
                case "red":
                    color = "red";
                    break;
                case "ruby":
                    color = "#e80000";
                    break;
                case "emerald":
                    color = "#47ff00";
                    break;
                case "sapphire":
                    color = "#0080fe";
                    break;
            }

            return color;
        }

        public int GetParticle()
        {
            if (properties.TryGetValue("particle", out var p))
            {
                foreach (var c in CosmeticManager.ItemIdToItem.Values)
                {
                    if (c.tag == p)
                    {
                        return c.itemdefid;
                    }
                }
            }
            return -1;
        }
    }

    public static class CosmeticUtility
    {
        public static int GetRarityScore(string rarity)
        {
            int score = 0;
            switch (rarity)
            {
                case "Common":
                    score = 1;
                    break;
                case "Uncommon":
                    score = 2;
                    break;
                case "Rare":
                    score = 3;
                    break;
                case "Extraordinary":
                    score = 4;
                    break;
                case "Legendary":
                    score = 5;
                    break;
                case "Original":
                    score = 6;
                    break;
                case "Unique":
                    score = 5;
                    break;
                case "0.1":
                    score = 7;
                    break;
                case "0.2":
                    score = 8;
                    break;
                case "0.4":
                    score = 8;
                    break;
                case "0.6":
                    score = 9;
                    break;
                case "0.8":
                    score = 10;
                    break;
                case "0.9":
                    score = 11;
                    break;
                case "light blue":
                    score = 1;
                    break;
                case "white":
                    score = 1;
                    break;
                case "red":
                    score = 2;
                    break;
                case "yellow":
                    score = 3;
                    break;
                case "grey":
                    score = 3;
                    break;
                case "black":
                    score = 7;
                    break;
                case "blue":
                    score = 2;
                    break;
                case "green":
                    score = 5;
                    break;
                case "orange":
                    score = 4;
                    break;
                case "brown":
                    score = 2;
                   
                    break;
                case "golden":
                    score = 8;
                    break;
                case "mediumslateblue":
                    score = 3;
                    break;
                case "pink":
                    score = 5;
                    break;
                case "purple":
                    score = 4;
                    break;
                case "ruby":
                    score = 9;
                    break;
                case "sapphire":
                    score = 9;
                    break;
                case "emerald":
                    score = 9;
                    break;
                case "stun":
                    score = 4;
                    break;
                case "snow":
                    score = 4;
                    break;
                case "fire":
                    score = 6;
                    break;
            }
            return score;
        }
    }
}