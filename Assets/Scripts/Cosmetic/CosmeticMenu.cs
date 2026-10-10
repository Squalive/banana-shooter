using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Audio;

using CodingDaniel.MapEditor.MEEditor.MESave;
using Manager;
using Menu;
using Multiplayer;
using Riptide;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Cosmetic
{
    public class CosmeticMenu : MonoBehaviour, IPointerClickHandler
    {
        public static CosmeticMenu Instance { private set; get; }

        [SerializeField] public GameObject inventoryMenu;

        private void Awake()
        {
            Instance = this;

            _normalInvDesiredPos = Vector2.zero;

            _useInvDesiredPos = _useInvOutsidePos;
            _clientPlayerLayer = LayerMask.NameToLayer("Cosmetic");
        }
        [SerializeField] private Item3DViewer movePlayer;
        private PlayerRender display;

        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private VolumeProfile profile;

        [SerializeField] private GameObject newItemHint;
        [SerializeField] private TextMeshProUGUI newItemAmountText;

        private DepthOfField _depthOfField;
        public readonly Dictionary<ulong, CosmeticPrefab> InventoryItemsObj = new Dictionary<ulong, CosmeticPrefab>();

        private const uint MaxAmount = 32;

        private CosmeticPrefab[] _normalItems = new CosmeticPrefab[MaxAmount];
        private CosmeticPrefab[] _useItems = new CosmeticPrefab[MaxAmount];

        [SerializeField] private DetailTypeItemUI detailTypePrefab;

        [SerializeField] private GameObject playerRenderImage;

        [SerializeField] private Button cancelBtn;

        public enum InventoryBaseType
        {
            Everything,
            Boxes,
            Cosmetics,
            Skins,
            Equipped,
            Particles,
            Display,
            Other
        }


        enum InventorySortType
        {
            Newest,
            Rarity,
            Alphabetical,
        }


        enum BoxType
        {
            AllBoxes = 0,
            Special,
        }


        enum CosmeticType
        {
            AllCosmetics = 0,
            Hair,
            Hat,
            Face,
            Shoes,
            Clothes,
            Pant,
        }


        public enum SkinType
        {
            AllSkins = 0,
            AssaultRifle,
            ShotGun,
            SubMachineGun,
            LightMachineGun,
            Pistol,
            Knife,
            Sniper,
        }


        enum ParticleType
        {
            AllParticles = 0,
        }


        enum DisplayType
        {
            AllDisplays = 0,
        }

        public InventoryBaseType baseType = InventoryBaseType.Everything;

        private List<Dictionary<ulong, SteamItemStored>> NormalPages { get; } = new List<Dictionary<ulong, SteamItemStored>>();
        private List<Dictionary<ulong, SteamItemStored>> UsePages { get; } = new List<Dictionary<ulong, SteamItemStored>>();

        int CurrentNormalPage { get; set; } = 0;
        int CurrentUsePage { get; set; } = 0;

        [SerializeField] private TextMeshProUGUI normalPageText, usePageText;
        [SerializeField] private ToggleGroup typeGroup;

        private InventorySortType Sorting { get; set; } = InventorySortType.Newest;

        public bool menu = false;

        private int _subType = 0;
        private void Start()
        {
            display = menu ? PlayerDisplayMenu.Instance : PlayerDisplay.Instance;

            hatCosmetics = display.hatCosmetics;
            hairCosmetics = display.hairCosmetics;
            clothesCosmetics = display.clothesCosmetics;
            faceCosmetics = display.faceCosmetics;
            pantCosmetics = display.pantCosmetics;
            daveHair = display.daveHair;
            clothe = display.clothe;
            pant = display.pant;
            shoeLCosmetics = display.shoeLCosmetics;
            shoeRCosmetics = display.shoeRCosmetics;
            movePlayer.obj = display.player;

            cam = display.cam;

            itemCam = CosmeticRender.Instance.itemCam;
            meshRenderer = CosmeticRender.Instance.meshRenderer;
            meshFilter = CosmeticRender.Instance.meshFilter;

            CosmeticRender.Instance.light.enabled = RenderSettings.sun == null;
            dropDown.Dropdown.onValueChanged.AddListener(SetType);

            searchInput.onValueChanged.AddListener(SetSearchItem);

            if (profile != null)
            {
                _depthOfField = (DepthOfField)profile.components[4];
            }

            GameObject obj = PrefabManager.Instance.GetPrefab("Cosmetic");

            for (int i = 0; i < MaxAmount; i++)
            {
                _normalItems[i] = Instantiate(obj, inventoryContent).GetComponent<CosmeticPrefab>();
                _useItems[i] = Instantiate(obj, useInvContent).GetComponent<CosmeticPrefab>();

                _normalItems[i].gameObject.SetActive(false);
                _useItems[i].gameObject.SetActive(false);
            }

            if (!InventoryManager.Initialized)
                InventoryManager.OnGetInventory += InitializeItems;
            else
                InitializeItems();
        }

        private void OnEnable()
        {
            InventoryManager.NewItemAdded += RefreshPage;
        }

        private void OnDisable()
        {
            dropDown.Dropdown.onValueChanged.RemoveAllListeners();
            InventoryManager.NewItemAdded -= RefreshPage;
            InventoryManager.OnGetInventory -= InitializeItems;
        }

        [SerializeField] private Transform detailContent;

        void InitializeItems()
        {
            RefreshPage();
            RefreshDetailType();

            foreach (var cosmeticItem in InventoryManager.EquippedItems)
            {
                // Debug.Log(cosmeticItem.name);
                EquipCosmeticOnly(cosmeticItem);
            }
        }

        public void SetSubType(int i)
        {
            _subType = i;

            AudioManager.Instance.PlayButton();

            RefreshPage();
            NextNormalPage(0);
        }

        void RefreshDetailType()
        {
            _subType = 0;
            for (int i = 0; i < detailContent.childCount; i++)
            {
                Destroy(detailContent.GetChild(i).gameObject);
            }

            int len = 1;

            switch (baseType)
            {
                case InventoryBaseType.Boxes:
                    len = 2;
                    break;
                case InventoryBaseType.Cosmetics:
                    len = 7;
                    break;
                case InventoryBaseType.Skins:
                    len = 8;
                    break;
                case InventoryBaseType.Particles:
                    len = 1;
                    break;
                case InventoryBaseType.Display:
                    len = 1;
                    break;
                case InventoryBaseType.Other:
                    len = 1;
                    break;
            }

            for (int i = 0; i < len; i++)
            {
                string key = "Everything";
                switch (baseType)
                {
                    case InventoryBaseType.Boxes:
                        key = ((BoxType)i).ToString();
                        break;
                    case InventoryBaseType.Cosmetics:
                        key = ((CosmeticType)i).ToString();
                        break;
                    case InventoryBaseType.Skins:
                        key = ((SkinType)i).ToString();
                        break;
                    case InventoryBaseType.Particles:
                        key = ((ParticleType)i).ToString();
                        break;
                    case InventoryBaseType.Display:
                        key = ((DisplayType)i).ToString();
                        break;
                }
                DetailTypeItemUI itemUi = Instantiate(detailTypePrefab, detailContent);

                itemUi.Initialize(key, i, typeGroup);
            }

        }

        void RefreshPage()
        {
            Queue<SteamItemStored> upComingItems = InventoryManager.Instance.UpComingItems;

            bool isNewItems = upComingItems.Count > 0;
            newItemHint.SetActive(isNewItems);
            if (isNewItems)
            {
                newItemAmountText.SetText(upComingItems.Count.ToString());
            }

            InventoryItemsObj.Clear();
            NormalPages.Clear();
            UsePages.Clear();
            _useInventoryObj.Clear();
            var items = InventoryManager.InventoryItems.Values.ToArray();

            switch (Sorting)
            {
                case InventorySortType.Newest:
                    items = items.OrderByDescending(o => o.Time).ToArray();
                    break;
                case InventorySortType.Rarity:
                    items = items.OrderByDescending(o => o.GetRarityScore()).ToArray();
                    break;
                case InventorySortType.Alphabetical:
                    items = items.OrderBy(o => (int)CosmeticManager.ItemIdToItem[o.itemDetails.m_iDefinition.m_SteamItemDef].displayName[0]).ToArray();
                    break;
            }

            for (int i = 0; i < MaxAmount; i++)
            {
                _normalItems[i].gameObject.SetActive(false);
            }

            foreach (var steamItemStored in items)
            {
                AddItemInstance(steamItemStored, CosmeticManager.ItemIdToItem[steamItemStored.itemDetails.m_iDefinition.m_SteamItemDef], steamItemStored.amountGained);
            }
        }

        void AddItemInstance(SteamItemStored steamItemStored, CosmeticItem cosmeticItem, int amount)
        {
            bool newPage = false;
            Dictionary<ulong, SteamItemStored> list;
            if (NormalPages.Count > 0)
            {
                list = NormalPages[^1];
            }
            else
            {
                list = new Dictionary<ulong, SteamItemStored>();
                newPage = true;
            }

            if (list.Count >= MaxAmount)
            {
                list = new Dictionary<ulong, SteamItemStored>();
                newPage = true;
            }

            bool flag = cosmeticItem.displayName.ToLower().Trim().Contains(_searchName.ToLower().Trim());

            if (flag)
            {
                int type = (int)cosmeticItem.type;
                switch (baseType)
                {
                    case InventoryBaseType.Boxes:
                        flag = cosmeticItem.type == CosmeticItem.Type.Box;
                        break;
                    case InventoryBaseType.Cosmetics:
                        flag = cosmeticItem.type != CosmeticItem.Type.Box && type <= 7 && (_subType + 1 == type || _subType == 0);
                        break;
                    case InventoryBaseType.Skins:
                        flag = type >= 10 && cosmeticItem.type != CosmeticItem.Type.Other && cosmeticItem.type != CosmeticItem.Type.Badge && cosmeticItem.type != CosmeticItem.Type.MusicBox
                               && cosmeticItem.type != CosmeticItem.Type.MenuScene &&
                               (_subType == 0 || _subType == (int)NetworkManager.Instance.weaponInfo[type - 10].weaponType);
                        break;
                    case InventoryBaseType.Particles:
                        flag = cosmeticItem.type == CosmeticItem.Type.Particle;
                        break;
                    case InventoryBaseType.Display:
                        flag = cosmeticItem.type == CosmeticItem.Type.Badge;
                        break;
                    case InventoryBaseType.Equipped:
                        flag = InventoryManager.Instance.cosmeticIndex.ids.Contains(steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID) ||
                               InventoryManager.Instance.cosmeticIndex.weaponIds.Contains(steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID);
                        break;
                }
            }


            if (flag)
                list.Add(steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID, steamItemStored);

            if (newPage)
            {
                NormalPages.Add(list);
            }

            if (flag && NormalPages.Count - 1 == CurrentNormalPage)
            {
                CosmeticPrefab prefab = _normalItems[NormalPages[CurrentNormalPage].Count - 1];
                prefab.gameObject.SetActive(true);

                prefab.SetItem(cosmeticItem, true, steamItemStored);

                InventoryItemsObj.Add(steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID, prefab);

                prefab.amountText.SetText(amount > 1 ? amount.ToString() : String.Empty);
            }

            if (cosmeticItem.canApplyParticle)
            {
                list = UsePages.Count > 0 ? UsePages[^1] : new Dictionary<ulong, SteamItemStored>();

                if (list.Count >= MaxAmount)
                {
                    list = new Dictionary<ulong, SteamItemStored>();
                }

                list.Add(steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID, steamItemStored);

                if (list.Count == 1)
                {
                    UsePages.Add(list);
                }

                if (UsePages.Count - 1 == CurrentUsePage)
                {
                    CosmeticPrefab prefab = _useItems[UsePages[CurrentUsePage].Count - 1];
                    prefab.gameObject.SetActive(true);

                    prefab.SetItem(cosmeticItem, false, steamItemStored);

                    _useInventoryObj.Add(steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID, prefab);

                    if (amount > 1)
                    {
                        prefab.amountText.SetText(amount.ToString());
                    }
                }
            }
        }



        public void NextNormalPage(int next)
        {
            CurrentNormalPage += next;

            if (CurrentNormalPage < 0) CurrentNormalPage = NormalPages.Count - 1;
            else if (CurrentNormalPage >= NormalPages.Count) CurrentNormalPage = 0;

            normalPageText.SetText((CurrentNormalPage + 1).ToString());

            var page = NormalPages.Count > CurrentNormalPage ? NormalPages[CurrentNormalPage].ToArray() : Array.Empty<KeyValuePair<ulong, SteamItemStored>>();

            InventoryItemsObj.Clear();

            for (int i = 0; i < MaxAmount; i++)
            {
                if (i < page.Length)
                {
                    var item = page[i].Value;
                    _normalItems[i].gameObject.SetActive(true);

                    _normalItems[i].SetItem(CosmeticManager.ItemIdToItem[item.itemDetails.m_iDefinition.m_SteamItemDef], true, item);

                    InventoryItemsObj.Add(item.itemDetails.m_itemId.m_SteamItemInstanceID, _normalItems[i]);

                    _normalItems[i].amountText.SetText(item.amountGained > 1 ? item.amountGained.ToString() : String.Empty);
                }
                else
                {
                    _normalItems[i].gameObject.SetActive(false);
                }
            }
        }


        public void NextUsePage(int next)
        {
            AudioManager.Instance.PlayButton();
            CurrentUsePage += next;

            if (CurrentUsePage < 0) CurrentUsePage = UsePages.Count - 1;
            else if (CurrentUsePage >= UsePages.Count) CurrentUsePage = 0;

            usePageText.SetText((CurrentUsePage + 1).ToString());

            var page = UsePages.Count > CurrentUsePage ? UsePages[CurrentUsePage].ToArray() : Array.Empty<KeyValuePair<ulong, SteamItemStored>>();

            _useInventoryObj.Clear();

            for (int i = 0; i < MaxAmount; i++)
            {
                if (i < page.Length)
                {
                    var item = page[i].Value;
                    _useItems[i].gameObject.SetActive(true);

                    _useItems[i].SetItem(CosmeticManager.ItemIdToItem[item.itemDetails.m_iDefinition.m_SteamItemDef], false, item);

                    _useInventoryObj.Add(item.itemDetails.m_itemId.m_SteamItemInstanceID, _normalItems[i]);

                    _useItems[i].amountText.SetText(item.amountGained > 1 ? item.amountGained.ToString() : String.Empty);
                }
                else
                {
                    _useItems[i].gameObject.SetActive(false);
                }
            }
        }

        private string _searchName = "";
        private void SetSearchItem(string arg0)
        {
            _searchName = arg0.ToLower();
            RefreshPage();
            NextNormalPage(0);
        }



        private void OnDestroy()
        {
            if (_spawnItem != null)
                Destroy(_spawnItem);
        }

        public LocalizeDropdown dropDown;

        public Transform inventoryContent;

        public Camera itemCam;

        public void GetItem()
        {
            equipBtn.SetActive(false);
            deequipBtn.SetActive(false);
            recycleBtn.SetActive(false);

            craftBtn.SetActive(false);
            meshRenderer.enabled = false;
            openBtn.SetActive(false);

            EnableCam();

            var newItemsQueue = InventoryManager.Instance.UpComingItems;

            int count = newItemsQueue.Count;

            for (int i = 0; i < count; i++)
            {
                var item = newItemsQueue.Dequeue();
            }

            newItemHint.SetActive(false);
        }


        public void SetSorting(int i)
        {
            Sorting = (InventorySortType)i;
            CurrentNormalPage = 0;
            RefreshPage();
            NextNormalPage(0);
            RefreshDetailType();
        }

        private Camera cam;
        public void EnableCam()
        {
            cam.enabled = display.useCam;

        }

        public void DisableCam()
        {
            cam.enabled = false;


        }

        public Item3DViewer cosmeticItem3dViewer;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        private CosmeticItem selectItem;
        private SteamItemStored selectSteamItemStored;

        public RectTransform selectItemObj;

        public const int CosmeticOffset = 10;

        private GameObject _spawnItem;
        public void SelectCosmetic(CosmeticItem item, SteamItemStored steamItemStored)
        {
            if (UIManager.Instance) UIManager.Instance.newItem.SetActive(false);
            inspectWindow.SetActive(false);
            selectItemObj.gameObject.SetActive(true);
            selectItemObj.position = Input.mousePosition;
            EventSystem.current.SetSelectedGameObject(null);
            AudioManager.Instance.PlayButton();
            selectSteamItemStored = steamItemStored;
            selectItem = item;

            if (_spawnItem != null)
                Destroy(_spawnItem);
            SetMesh(item, steamItemStored);

            if (item.type != CosmeticItem.Type.Box && item.type != CosmeticItem.Type.Rag)
            {
                recycleBtn.SetActive(true);
            }
            else
            {
                recycleBtn.SetActive(false);
                equipBtn.SetActive(false);
            }

            deequipBtn.SetActive(false);
            equipBtn.SetActive(false);
            craftBtn.SetActive(false);
            useBtn.SetActive(false);
            inspectBtn.SetActive(true);
            combineBtn.SetActive(false);
            if (item.type == CosmeticItem.Type.Rag)
            {
                craftBtn.SetActive(true);
                craftBtn.GetComponent<Button>().interactable = selectSteamItemStored.amountGained >= 10;
            }
            else
            {
                craftBtn.SetActive(false);
            }

            ulong itemStored = 0;
            switch (item.type)
            {
                case CosmeticItem.Type.Hat:
                    itemStored = InventoryManager.Instance.cosmeticIndex.ids[0];
                    break;
                case CosmeticItem.Type.Face:
                    itemStored = InventoryManager.Instance.cosmeticIndex.ids[1];

                    break;
                case CosmeticItem.Type.Shoes:
                    itemStored = InventoryManager.Instance.cosmeticIndex.ids[2];

                    break;
                case CosmeticItem.Type.Hair:
                    itemStored = InventoryManager.Instance.cosmeticIndex.ids[3];
                    break;
                case CosmeticItem.Type.Clothes:
                    itemStored = InventoryManager.Instance.cosmeticIndex.ids[4];
                    break;
                case CosmeticItem.Type.Pant:
                    itemStored = InventoryManager.Instance.cosmeticIndex.ids[5];
                    break;
                case CosmeticItem.Type.MusicBox:
                    itemStored = InventoryManager.Instance.cosmeticIndex.ids[6];
                    break;
                case CosmeticItem.Type.MenuScene:
                    itemStored = InventoryManager.Instance.cosmeticIndex.ids[7];
                    inspectBtn.SetActive(false);
                    break;
                case CosmeticItem.Type.Box:
                    openBtn.SetActive(true);
                    break;
                case CosmeticItem.Type.Particle:
                    openBtn.SetActive(false);
                    useBtn.SetActive(true);

                    combineItem = steamItemStored;
                    break;
                default:
                    if (item.type != CosmeticItem.Type.Rag && item.type != CosmeticItem.Type.Other)
                    {
                        itemStored = InventoryManager.Instance.cosmeticIndex.weaponIds[(int)item.type - CosmeticOffset];
                    }
                    break;
            }

            if (selectItem.type != CosmeticItem.Type.Box && selectItem.type != CosmeticItem.Type.Rag && selectItem.type != CosmeticItem.Type.Particle)
            {
                if (itemStored == 0 || itemStored != selectSteamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID)
                {
                    equipBtn.SetActive(true);
                    openBtn.SetActive(false);
                }
                else if (itemStored == selectSteamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID)
                {
                    deequipBtn.SetActive(true);
                    openBtn.SetActive(false);
                }
            }
        }

        #region Function

        public void DisableItemCam()
        {
            itemCam.enabled = false;

            if (_depthOfField != null)
            {
                _depthOfField.gaussianStart.value = 3.1f;
            }
            AudioManager.Instance.PlayButton();
        }

        public void Purchase()
        {
            AudioManager.Instance.PlayButton();
            SteamFriends.ActivateGameOverlayToWebPage("https://store.steampowered.com/itemstore/1949740/");
        }

        public void Inspect()
        {
            if (UIManager.Instance) UIManager.Instance.SetButton(cancelBtn);
            okayBtn.SetActive(true);
            realCombineBtn.SetActive(false);
            cancelCombineBtn.SetActive(false);
            InspectItem(false);

        }



        public void Equip()
        {
            selectItemObj.gameObject.SetActive(false);
            AudioManager.Instance.PlayButton();
            equipBtn.SetActive(false);
            openBtn.SetActive(false);
            deequipBtn.SetActive(true);

            ulong uid = Equip(selectItem, selectSteamItemStored);

            if (InventoryItemsObj.ContainsKey(uid))
                InventoryItemsObj[uid].selected.SetActive(false);
            GameManager.inventoryChanged = true;
            if (InventoryItemsObj.ContainsKey(selectSteamItemStored.itemDetails.m_itemId
                .m_SteamItemInstanceID))
            {
                InventoryItemsObj[selectSteamItemStored.itemDetails.m_itemId
                    .m_SteamItemInstanceID].selected.SetActive(true);
            }

            if (NetworkManager.Instance.Client.IsConnected)
            {
                InventoryManager.Instance.TryToSerializeNewItem(selectSteamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID);
            }
            else
            {
                InventoryManager.Instance.TryToSerializeItem();
            }
        }


        public void DeEquip()
        {
            selectItemObj.gameObject.SetActive(false);
            AudioManager.Instance.PlayButton();

            DeEquip(selectSteamItemStored);
        }

        public void DeEquip(SteamItemStored steamItemStored)
        {
            if (InventoryItemsObj.TryGetValue(steamItemStored.itemDetails.m_itemId
                    .m_SteamItemInstanceID, out var prefab))
            {
                prefab.selected.SetActive(false);
            }
            InventoryManager.CosmeticIndex index = InventoryManager.Instance.cosmeticIndex;

            switch (CosmeticManager.ItemIdToItem[steamItemStored.itemDetails.m_iDefinition.m_SteamItemDef].type)
            {
                case CosmeticItem.Type.Hat:
                    InventoryManager.Instance.cosmeticIndex.hatIndex = -1;
                    InventoryManager.Instance.cosmeticIndex.hatColor = Color.clear;
                    InventoryManager.Instance.cosmeticIndex.ids[0] = 0;
                    InventoryManager.Instance.cosmeticIndex.hatParticle = -1;
                    equipBtn.SetActive(true);
                    openBtn.SetActive(false);
                    deequipBtn.SetActive(false);

                    SetCosmetics(index.hatIndex, index.hatColor, index.hatShiny, index.hatParticle, hatCosmetics, ref _hatParticle);

                    break;
                case CosmeticItem.Type.Face:
                    InventoryManager.Instance.cosmeticIndex.faceIndex = -1;
                    InventoryManager.Instance.cosmeticIndex.faceColor = Color.clear;
                    InventoryManager.Instance.cosmeticIndex.ids[1] = 0;
                    InventoryManager.Instance.cosmeticIndex.faceParticle = -1;
                    equipBtn.SetActive(true);
                    openBtn.SetActive(false);
                    deequipBtn.SetActive(false);

                    SetCosmetics(index.faceIndex, index.faceColor, index.faceShiny, index.faceParticle, faceCosmetics, ref _faceParticle);
                    break;
                case CosmeticItem.Type.Shoes:
                    InventoryManager.Instance.cosmeticIndex.shoesIndex = -1;
                    InventoryManager.Instance.cosmeticIndex.shoesColor = Color.clear;
                    InventoryManager.Instance.cosmeticIndex.ids[2] = 0;
                    InventoryManager.Instance.cosmeticIndex.shoesParticle = -1;
                    equipBtn.SetActive(true);
                    openBtn.SetActive(false);
                    deequipBtn.SetActive(false);
                    SetCosmetics(index.shoesIndex, index.shoesColor, index.shoesShiny, index.shoesParticle, shoeLCosmetics, ref _shoeLParticle);
                    SetCosmetics(index.shoesIndex, index.shoesColor, index.shoesShiny, index.shoesParticle, shoeRCosmetics, ref _shoeRParticle);
                    break;
                case CosmeticItem.Type.Hair:
                    InventoryManager.Instance.cosmeticIndex.hairIndex = -1;
                    InventoryManager.Instance.cosmeticIndex.hairColor = Color.clear;
                    InventoryManager.Instance.cosmeticIndex.ids[3] = 0;
                    InventoryManager.Instance.cosmeticIndex.hairParticle = -1;
                    equipBtn.SetActive(true);
                    openBtn.SetActive(false);
                    deequipBtn.SetActive(false);
                    SetCosmetics(index.hairIndex, index.hairColor, index.hairShiny, index.hairParticle,
                        hairCosmetics, ref _hairParticle, daveHair.gameObject);
                    break;
                case CosmeticItem.Type.Clothes:
                    InventoryManager.Instance.cosmeticIndex.clothesIndex = -1;
                    InventoryManager.Instance.cosmeticIndex.clothesColor = Color.clear;
                    InventoryManager.Instance.cosmeticIndex.ids[4] = 0;
                    InventoryManager.Instance.cosmeticIndex.clothesParticle = -1;
                    equipBtn.SetActive(true);
                    openBtn.SetActive(false);
                    deequipBtn.SetActive(false);
                    SetCosmetics(index.clothesIndex, index.clothesColor, index.clothesShiny, index.clothesParticle, clothesCosmetics, ref _clotheParticle, clothe.gameObject);
                    break;
                case CosmeticItem.Type.Pant:
                    InventoryManager.Instance.cosmeticIndex.pantIndex = -1;
                    InventoryManager.Instance.cosmeticIndex.pantColor = Color.clear;
                    InventoryManager.Instance.cosmeticIndex.ids[5] = 0;
                    InventoryManager.Instance.cosmeticIndex.pantParticle = -1;
                    equipBtn.SetActive(true);
                    openBtn.SetActive(false);
                    deequipBtn.SetActive(false);
                    SetCosmetics(index.pantIndex, index.pantColor, 0, index.pantParticle, pantCosmetics, ref _pantParticle, pant.gameObject);
                    break;
                case CosmeticItem.Type.MusicBox:
                    InventoryManager.Instance.cosmeticIndex.musicBoxIndex = 0;
                    InventoryManager.Instance.cosmeticIndex.ids[6] = 0;

                    MusicManager.Instance.ChangeMusic(MusicManager.Instance.music);
                    break;
                case CosmeticItem.Type.MenuScene:
                    InventoryManager.Instance.cosmeticIndex.menuSceneIndex = 0;
                    InventoryManager.Instance.cosmeticIndex.ids[7] = 0;

                    if (MenuScene.Instance)
                    {
                        MenuScene.Instance.index = 0;
                        MenuScene.Instance.Refresh();
                    }
                    break;
                default:
                    if (selectItem.type != CosmeticItem.Type.Rag && selectItem.type != CosmeticItem.Type.Other)
                    {
                        int i = (int)selectItem.type - CosmeticOffset;
                        InventoryManager.Instance.cosmeticIndex.weaponIndex[i] = 0;
                        InventoryManager.Instance.cosmeticIndex.weaponIds[i] = 0;
                    }

                    break;
            }

            GameManager.inventoryChanged = true;

            if (NetworkManager.Instance.Client.IsConnected)
            {
                Message message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.ChangeCosmetic);

                message.Add(false);

                message.Add((ushort)selectItem.type);

                NetworkManager.Instance.Client.Send(message);

            }
            else
            {
                InventoryManager.Instance.TryToSerializeItem();
            }
        }

        ulong Equip(CosmeticItem item, SteamItemStored steamItemStored)
        {
            ulong uid = 0;
            InventoryManager.CosmeticIndex index = InventoryManager.Instance.cosmeticIndex;
            switch (item.type)
            {
                case CosmeticItem.Type.Hat:
                    uid = InventoryManager.Instance.cosmeticIndex.ids[0];
                    InventoryManager.Instance.cosmeticIndex.hatIndex = item.index;
                    InventoryManager.Instance.cosmeticIndex.hatColor = steamItemStored.GetColor();
                    InventoryManager.Instance.cosmeticIndex.hatShiny = steamItemStored.GetShiny();
                    InventoryManager.Instance.cosmeticIndex.hatParticle = steamItemStored.GetParticle();
                    InventoryManager.Instance.cosmeticIndex.ids[0] = steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID;
                    SetCosmetics(index.hatIndex, index.hatColor, index.hatShiny, index.hatParticle, hatCosmetics, ref _hatParticle);
                    break;
                case CosmeticItem.Type.Face:
                    uid = InventoryManager.Instance.cosmeticIndex.ids[1];
                    InventoryManager.Instance.cosmeticIndex.faceIndex = item.index;
                    InventoryManager.Instance.cosmeticIndex.faceColor = steamItemStored.GetColor();
                    InventoryManager.Instance.cosmeticIndex.faceShiny = steamItemStored.GetShiny();
                    InventoryManager.Instance.cosmeticIndex.faceParticle = steamItemStored.GetParticle();
                    InventoryManager.Instance.cosmeticIndex.ids[1] = steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID;
                    SetCosmetics(index.faceIndex, index.faceColor, index.faceShiny, index.faceParticle, faceCosmetics, ref _faceParticle);

                    break;
                case CosmeticItem.Type.Shoes:
                    uid = InventoryManager.Instance.cosmeticIndex.ids[2];
                    InventoryManager.Instance.cosmeticIndex.shoesIndex = item.index;
                    InventoryManager.Instance.cosmeticIndex.shoesColor = steamItemStored.GetColor();
                    InventoryManager.Instance.cosmeticIndex.shoesShiny = steamItemStored.GetShiny();
                    InventoryManager.Instance.cosmeticIndex.shoesParticle = steamItemStored.GetParticle();
                    InventoryManager.Instance.cosmeticIndex.ids[2] = steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID;
                    SetCosmetics(index.shoesIndex, index.shoesColor, index.shoesShiny, index.shoesParticle, shoeLCosmetics, ref _shoeRParticle);
                    SetCosmetics(index.shoesIndex, index.shoesColor, index.shoesShiny, index.shoesParticle, shoeRCosmetics, ref _shoeLParticle);

                    break;
                case CosmeticItem.Type.Hair:
                    uid = InventoryManager.Instance.cosmeticIndex.ids[3];
                    InventoryManager.Instance.cosmeticIndex.hairIndex = item.index;
                    InventoryManager.Instance.cosmeticIndex.hairColor = steamItemStored.GetColor();
                    InventoryManager.Instance.cosmeticIndex.hairShiny = steamItemStored.GetShiny();
                    InventoryManager.Instance.cosmeticIndex.hairParticle = steamItemStored.GetParticle();
                    InventoryManager.Instance.cosmeticIndex.ids[3] = steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID;
                    SetCosmetics(index.hairIndex, index.hairColor, index.hairShiny, index.hairParticle,
                        hairCosmetics, ref _hairParticle, daveHair.gameObject);
                    // SetHair(InventoryManager.Instance.cosmeticIndex.hairIndex,
                    //     InventoryManager.Instance.cosmeticIndex.hairColor,
                    //     InventoryManager.Instance.cosmeticIndex.hairShiny);
                    break;
                case CosmeticItem.Type.Clothes:
                    uid = InventoryManager.Instance.cosmeticIndex.ids[4];
                    InventoryManager.Instance.cosmeticIndex.clothesIndex = item.index;
                    InventoryManager.Instance.cosmeticIndex.clothesColor = steamItemStored.GetColor();
                    InventoryManager.Instance.cosmeticIndex.clothesShiny = steamItemStored.GetShiny();
                    InventoryManager.Instance.cosmeticIndex.clothesParticle = steamItemStored.GetParticle();
                    InventoryManager.Instance.cosmeticIndex.ids[4] = steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID;
                    SetCosmetics(index.clothesIndex, index.clothesColor, index.clothesShiny, index.clothesParticle, clothesCosmetics, ref _clotheParticle, clothe.gameObject);
                    break;
                case CosmeticItem.Type.Pant:
                    uid = InventoryManager.Instance.cosmeticIndex.ids[5];
                    InventoryManager.Instance.cosmeticIndex.pantIndex = item.index;
                    InventoryManager.Instance.cosmeticIndex.pantColor = steamItemStored.GetColor();
                    InventoryManager.Instance.cosmeticIndex.pantShiny = steamItemStored.GetShiny();
                    InventoryManager.Instance.cosmeticIndex.pantParticle = steamItemStored.GetParticle();
                    InventoryManager.Instance.cosmeticIndex.ids[5] = steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID;
                    SetCosmetics(index.pantIndex, index.pantColor, index.pantShiny, index.pantParticle, pantCosmetics, ref _pantParticle, pant.gameObject);
                    break;
                case CosmeticItem.Type.MusicBox:
                    uid = InventoryManager.Instance.cosmeticIndex.ids[6];

                    InventoryManager.Instance.cosmeticIndex.musicBoxIndex = item.index;

                    InventoryManager.Instance.cosmeticIndex.ids[6] = steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID;

                    MusicManager.Instance.ChangeMusic(MusicManager.Instance.music);
                    break;
                case CosmeticItem.Type.MenuScene:
                    uid = InventoryManager.Instance.cosmeticIndex.ids[7];

                    InventoryManager.Instance.cosmeticIndex.menuSceneIndex = item.index;

                    InventoryManager.Instance.cosmeticIndex.ids[7] = steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID;

                    if (MenuScene.Instance)
                    {
                        MenuScene.Instance.index = item.index;
                        MenuScene.Instance.Refresh();
                    }
                    break;
                default:
                    if (item.type != CosmeticItem.Type.Rag && item.type != CosmeticItem.Type.Other)
                    {
                        int i = (int)item.type - CosmeticOffset;
                        uid = InventoryManager.Instance.cosmeticIndex.weaponIds[i];
                        InventoryManager.Instance.cosmeticIndex.weaponIndex[i] = (ushort)item.index;

                        InventoryManager.Instance.cosmeticIndex.weaponIds[i] =
                            steamItemStored.itemDetails.m_itemId.m_SteamItemInstanceID;
                    }

                    break;
            }

            return uid;
        }

        void EquipCosmeticOnly(CosmeticItem item)
        {
            InventoryManager.CosmeticIndex index = InventoryManager.Instance.cosmeticIndex;
            switch (item.type)
            {
                case CosmeticItem.Type.Hat:
                    SetCosmetics(index.hatIndex, index.hatColor, index.hatShiny, index.hatParticle, hatCosmetics, ref _hatParticle);
                    break;
                case CosmeticItem.Type.Face:
                    SetCosmetics(index.faceIndex, index.faceColor, index.faceShiny, index.faceParticle, faceCosmetics, ref _faceParticle);

                    break;
                case CosmeticItem.Type.Shoes:
                    InventoryManager.Instance.cosmeticIndex.shoesIndex = item.index;
                    SetCosmetics(index.shoesIndex, index.shoesColor, index.shoesShiny, index.shoesParticle, shoeLCosmetics, ref _shoeRParticle);
                    SetCosmetics(index.shoesIndex, index.shoesColor, index.shoesShiny, index.shoesParticle, shoeRCosmetics, ref _shoeLParticle);

                    break;
                case CosmeticItem.Type.Hair:
                    InventoryManager.Instance.cosmeticIndex.hairIndex = item.index;
                    SetCosmetics(index.hairIndex, index.hairColor, index.hairShiny, index.hairParticle,
                        hairCosmetics, ref _hairParticle, daveHair.gameObject);
                    break;
                case CosmeticItem.Type.Clothes:
                    InventoryManager.Instance.cosmeticIndex.clothesIndex = item.index;
                    SetCosmetics(index.clothesIndex, index.clothesColor, index.clothesShiny, index.clothesParticle, clothesCosmetics, ref _clotheParticle, clothe.gameObject);
                    break;
                case CosmeticItem.Type.Pant:
                    InventoryManager.Instance.cosmeticIndex.pantIndex = item.index;
                    SetCosmetics(index.pantIndex, index.pantColor, index.pantShiny, index.pantParticle, pantCosmetics, ref _pantParticle, pant.gameObject);
                    break;
                default:
                    if (item.type != CosmeticItem.Type.Rag && item.type != CosmeticItem.Type.Other)
                    {
                        int i = (int)item.type - CosmeticOffset;
                        InventoryManager.Instance.cosmeticIndex.weaponIndex[i] = (ushort)item.index;
                    }

                    break;
            }
        }

        public void OpenBox()
        {
            selectItemObj.gameObject.SetActive(false);
            AudioManager.Instance.PlayButton();
            openBtn.SetActive(false);
            CrateOpenMenu.Instance.OpenPage(selectItem, selectSteamItemStored);
        }

        public void Recycle()
        {
            selectItemObj.gameObject.SetActive(false);
            AudioManager.Instance.PlayButton();
            recycleBtn.SetActive(false);
            equipBtn.SetActive(false);
            deequipBtn.SetActive(false);
            InventoryManager.Instance.HandleQueue.Enqueue(InventoryManager.InventoryHandleType.Exchange);
            UInt32[] outCount = { 1 };
            UInt32[] inputCount = { 1 };
            SteamItemDef_t[] outItemDefTs = { (SteamItemDef_t)1000 };
            SteamItemInstanceID_t[] instanceIDTs = { selectSteamItemStored.itemDetails.m_itemId };
            SteamInventory.ExchangeItems(out InventoryManager.Instance.inventoryHandle, outItemDefTs, outCount, 1, instanceIDTs,
                inputCount, 1);



        }


        public void CraftBox()
        {
            selectItemObj.gameObject.SetActive(false);
            craftBtn.SetActive(false);
            InventoryManager.Instance.HandleQueue.Enqueue(InventoryManager.InventoryHandleType.Exchange);
            UInt32[] outCount = { 1 };
            UInt32[] inputCount = { 10 };
            SteamItemDef_t[] outItemDefTs = { (SteamItemDef_t)200 };
            SteamItemInstanceID_t[] instanceIDTs = { selectSteamItemStored.itemDetails.m_itemId };
            SteamInventory.ExchangeItems(out InventoryManager.Instance.inventoryHandle, outItemDefTs, outCount, 1, instanceIDTs,
                inputCount, 10);
        }

        #endregion

        private SteamItemStored combineItem;

        public TextMeshProUGUI itemNameText;
        public GameObject inspectWindow;
        public RectTransform detailed;
        public CanvasGroup detailedGroup;
        public TextMeshProUGUI nameText, description;

        public ContentSizeFitter fitter;

        public GameObject equipBtn, deequipBtn, openBtn, recycleBtn, craftBtn, useBtn, combineBtn, inspectBtn;

        public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public void OnPointerClick(PointerEventData eventData)
        {
            selectItemObj.gameObject.SetActive(false);
        }

        #region PlayerDisplay

        public List<GameObject> hatCosmetics = new List<GameObject>();
        public List<GameObject> faceCosmetics = new List<GameObject>();
        public List<GameObject> shoeLCosmetics = new List<GameObject>();
        public List<GameObject> shoeRCosmetics = new List<GameObject>();
        public SkinnedMeshRenderer daveHair, clothe, pant;
        public List<GameObject> hairCosmetics = new List<GameObject>();
        public List<GameObject> pantCosmetics = new List<GameObject>();
        public List<GameObject> clothesCosmetics = new List<GameObject>();

        [HideInInspector]
        public Transform _hatParticle;
        [HideInInspector]
        public Transform _faceParticle;
        [HideInInspector]
        public Transform _shoeLParticle;
        [HideInInspector]
        public Transform _shoeRParticle;
        [HideInInspector]
        public Transform _hairParticle;
        [HideInInspector]
        public Transform _clotheParticle;
        [HideInInspector]
        public Transform _pantParticle;

        public static LayerMask _clientPlayerLayer;
        public void SetCosmetics(int index, Color color, float shiny, int particle, List<GameObject> cosmetics,
            ref Transform particleTran, GameObject alreadyHave = null)
        {
            if (cosmetics == null) return;
            foreach (var cosmetic in cosmetics)
            {
                cosmetic.gameObject.SetActive(false);
            }
            if (particleTran) Destroy(particleTran.gameObject);
            if (index < cosmetics.Count && index != -1)
            {
                GameObject cos = cosmetics[index];
                if (cos != null)
                {
                    if (alreadyHave)
                    {
                        alreadyHave.SetActive(false);
                    }
                    cos.SetActive(true);

                    foreach (var ren in cos.GetComponentsInChildren<Renderer>())
                    {
                        if (color != Color.clear)
                        {
                            ren.material.color = color;

                            if (CosmeticManager.IsGoldenColor(color))
                            {
                                ren.material.SetFloat(MapSaver.Smoothness, .8f);
                                ren.material.SetFloat(MapSaver.Metallic, 0.6f);
                            }
                        }
                        if (shiny != 0)
                        {
                            ren.material.EnableKeyword("_EMISSION");
                            ren.material.SetColor(EmissionColor, color * shiny);
                        }
                        else
                        {
                            ren.material.DisableKeyword("_EMISSION");
                        }
                    }
                    if (particle != -1 && CosmeticManager.ItemIdToItem.TryGetValue(particle, out var item))
                    {
                        InventoryManager.ParticleItem particleItem = InventoryManager.Instance.GetParticle(item.tag);
                        particleTran = Instantiate(particleItem.prefab).transform;
                        particleTran.position = cos.transform.position + new Vector3(0, 0.0095f, 0);

                        particleTran.localScale = particleItem.cosmeticMenuSize;

                        particleTran.gameObject.layer = _clientPlayerLayer;

                        for (int i = 0; i < particleTran.childCount; i++)
                        {
                            particleTran.GetChild(i).gameObject.layer = _clientPlayerLayer;
                        }


                        CosmeticVFX cosmeticVFX = particleTran.GetComponent<CosmeticVFX>();

                        if (cosmeticVFX != null)
                        {
                            var filter = cos.GetComponentInChildren<MeshFilter>();
                            if (filter == null)
                            {
                                particleTran.parent = cos.transform;
                                var skin = cos.GetComponentInChildren<SkinnedMeshRenderer>();
                                cosmeticVFX.SetSkinnedMeshRenderer(skin);
                                cosmeticVFX.SetTransform(skin.rootBone, true);
                            }
                            else
                            {
                                particleTran.parent = cos.transform.parent;
                                cosmeticVFX.SetMesh(filter.sharedMesh);
                                cosmeticVFX.SetTransform(filter.transform);
                            }
                        }
                        else
                        {
                            particleTran.parent = cos.transform.parent;
                        }
                    }
                }
                else
                {
                    if (alreadyHave) alreadyHave.SetActive(true);
                }
            }
            else
            {
                if (alreadyHave) alreadyHave.SetActive(true);
            }
        }
        #endregion

        void SetType(int type)
        {
            baseType = (InventoryBaseType)type;

            CurrentNormalPage = 0;
            RefreshPage();
            NextNormalPage(0);
            RefreshDetailType();
        }

        [SerializeField] private RectTransform normalInventory, useInventory;

        private Vector2 _normalInvDesiredPos, _useInvDesiredPos;
        private readonly Vector2 _normalInvOutsidePos = new Vector2(Mathf.Clamp(-Screen.height * 3, -4000, -2000), 0);
        private readonly Vector2 _useInvOutsidePos = new Vector2(0, Mathf.Clamp(-Screen.height * 3, -4000, -2000));

        private void Update()
        {
            normalInventory.anchoredPosition = Vector2.Lerp(normalInventory.anchoredPosition, _normalInvDesiredPos,
                Time.deltaTime * 15f);
            useInventory.anchoredPosition = Vector2.Lerp(useInventory.anchoredPosition, _useInvDesiredPos,
                Time.deltaTime * 15f);

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (!menu && GameUIManager.Instance.pause)
                {
                    playerRenderImage.SetActive(!playerRenderImage.activeSelf);
                }
            }

        }


        public void OpenUseInv()
        {
            _normalInvDesiredPos = _normalInvOutsidePos;
            _useInvDesiredPos = Vector2.zero;
            selectItemObj.gameObject.SetActive(false);

            if (relatedText.text != selectItem.displayName)
            {
                relatedIcon.texture = selectItem.icon;
                relatedText.SetText(selectItem.displayName);
                relatedText.color = selectItem.GetColor();

            }

            CurrentUsePage = 0;
        }

        private Dictionary<ulong, CosmeticPrefab> _useInventoryObj = new Dictionary<ulong, CosmeticPrefab>();


        public void BackToNormalInv()
        {
            _normalInvDesiredPos = Vector2.zero;
            _useInvDesiredPos = _useInvOutsidePos;
        }

        [SerializeField] private Transform useInvContent;

        [SerializeField] private RawImage relatedIcon;
        [SerializeField] private TextMeshProUGUI relatedText;

        public void SelectUseCosmetic(CosmeticItem item, SteamItemStored steamItemStored)
        {
            if (UIManager.Instance) UIManager.Instance.newItem.SetActive(false);
            inspectWindow.SetActive(false);
            selectItemObj.gameObject.SetActive(true);
            selectItemObj.position = Input.mousePosition;
            EventSystem.current.SetSelectedGameObject(null);
            AudioManager.Instance.PlayButton();
            selectSteamItemStored = steamItemStored;
            selectItem = item;


            SetMesh(item, steamItemStored);
            SetMesh(CosmeticManager.ItemIdToItem[combineItem.itemDetails.m_iDefinition.m_SteamItemDef], combineItem, true);

            deequipBtn.SetActive(false);
            equipBtn.SetActive(false);
            craftBtn.SetActive(false);
            useBtn.SetActive(false);
            inspectBtn.SetActive(false);
            recycleBtn.SetActive(false);

            combineBtn.SetActive(true);
        }

        void SetMesh(CosmeticItem item, SteamItemStored steamItemStored, bool previewCombine = false)
        {
            if (item.type == CosmeticItem.Type.Particle)
            {
                if (_spawnItem != null) Destroy(_spawnItem);
                meshRenderer.enabled = previewCombine;

                _spawnItem = Instantiate(InventoryManager.Instance.GetItemPrefab(item.tag),
                    meshFilter.transform.position, Quaternion.Euler(item.defaultRotation));


                LayerMask layerMask = LayerMask.NameToLayer("Cosmetic");
                _spawnItem.layer = layerMask;
                for (int i = 0; i < _spawnItem.transform.childCount; i++)
                {
                    _spawnItem.transform.GetChild(i).gameObject.layer = layerMask;
                }
                if (!previewCombine)
                    cosmeticItem3dViewer.SetObj(_spawnItem.transform);
                _spawnItem.transform.localScale = item.GetSize() * InventoryManager.sizeFactor;
                _spawnItem.transform.parent = meshFilter.transform;

                CosmeticVFX cosmeticVFX = _spawnItem.GetComponent<CosmeticVFX>();

                if (cosmeticVFX != null)
                {
                    cosmeticVFX.SetMesh(previewCombine ? meshFilter.mesh : null);
                    cosmeticVFX.SetTransform(meshFilter.transform);
                }

                return;
            }
            else
            {
                meshFilter.mesh = item.mesh;
                Transform transform1;
                (transform1 = meshRenderer.transform).rotation = Quaternion.Euler(item.defaultRotation);
                cosmeticItem3dViewer.SetObj(transform1);

                meshRenderer.materials = item.materials;
                meshRenderer.enabled = true;

                if (item.materials[0].HasColor("_Color"))
                {
                    Color defaultColor = item.materials[0].color;
                    Color returnColor = steamItemStored.GetColor();

                    meshRenderer.material.color = returnColor == Color.clear ? defaultColor : returnColor;

                    if (CosmeticManager.IsGoldenColor(returnColor))
                    {
                        meshRenderer.material.SetFloat(MapSaver.Smoothness, .8f);
                        meshRenderer.material.SetFloat(MapSaver.Metallic, 0.6f);
                    }
                }

                meshFilter.transform.localScale = item.GetSize() * InventoryManager.sizeFactor;
            }

            if (steamItemStored.properties.TryGetValue("particle", out var p))
            {
                foreach (var it in CosmeticManager.ItemIdToItem.Values)
                {
                    if (it.tag == p)
                    {
                        if (_spawnItem != null) Destroy(_spawnItem);
                        _spawnItem = Instantiate(InventoryManager.Instance.GetItemPrefab(it.tag),
                            meshFilter.transform.position, Quaternion.Euler(it.defaultRotation));

                        LayerMask layerMask = LayerMask.NameToLayer("Cosmetic");
                        _spawnItem.layer = layerMask;
                        for (int i = 0; i < _spawnItem.transform.childCount; i++)
                        {
                            _spawnItem.transform.GetChild(i).gameObject.layer = layerMask;
                        }
                        _spawnItem.transform.localScale = it.GetSize() * InventoryManager.sizeFactor;

                        _spawnItem.transform.parent = meshFilter.transform;

                        CosmeticVFX cosmeticVFX = _spawnItem.GetComponent<CosmeticVFX>();

                        if (cosmeticVFX != null)
                        {
                            cosmeticVFX.SetMesh(meshFilter.mesh);
                            cosmeticVFX.SetTransform(meshFilter.transform);
                        }
                    }
                }
            }

            if (steamItemStored.properties.ContainsKey("shiny"))
            {
                if (steamItemStored.properties["shiny"] != "0")
                {
                    meshRenderer.material.EnableKeyword("_EMISSION");
                }

                float intensity = 0;
                switch (steamItemStored.properties["shiny"])
                {
                    case "0.1":
                        intensity = 1.1f;
                        // meshRenderer.material.SetColor(EmissionColor,meshRenderer.material.color*1.1f);
                        break;
                    case "0.2":
                        intensity = 1.2f;
                        // meshRenderer.material.SetColor(EmissionColor,meshRenderer.material.color*1.2f);
                        break;
                    case "0.4":
                        intensity = 1.4f;
                        // meshRenderer.material.SetColor(EmissionColor,meshRenderer.material.color*1.4f);
                        break;
                    case "0.6":
                        intensity = 1.8f;
                        // meshRenderer.material.SetColor(EmissionColor,meshRenderer.material.color*1.8f);
                        break;
                    case "0.8":
                        intensity = 2f;
                        // meshRenderer.material.SetColor(EmissionColor,meshRenderer.material.color*2f);
                        break;
                    case "0.9":
                        intensity = 2.2f;
                        break;
                }

                var color = meshRenderer.material.color;
                meshRenderer.material.SetVector(EmissionColor, color * intensity);
            }
        }

        [SerializeField] private GameObject okayBtn, realCombineBtn, cancelCombineBtn;

        public void InspectCombine()
        {
            okayBtn.SetActive(false);
            realCombineBtn.SetActive(true);
            cancelCombineBtn.SetActive(true);
            InspectItem(true);
        }
        void InspectItem(bool preview)
        {
            selectItemObj.gameObject.SetActive(false);
            AudioManager.Instance.PlayButton();
            inspectWindow.SetActive(true);
            StringBuilder nameText = new StringBuilder(selectItem.displayName + " (" + selectItem.GetRarity() + ")\n");
            if (selectSteamItemStored.properties.ContainsKey("color"))
            {
                nameText.Append($"<size=28>Color : <color={selectSteamItemStored.GetColorString()}>{selectSteamItemStored.properties["color"]}</color></size>\n");
            }
            if (selectSteamItemStored.properties.ContainsKey("shiny") && selectSteamItemStored.properties["shiny"] != "0")
            {
                nameText.Append($"<size=28>Shiny : {selectSteamItemStored.properties["shiny"]}</size>\n");
            }
            if (selectSteamItemStored.properties.TryGetValue("particle", out var p))
            {
                CosmeticItem item = InventoryManager.Instance.GetParticle(p).cosmeticItem;

                nameText.Append($"<size=28>Particle : <color=yellow>{p}</color> (<color={item.GetColorString()}>{item.GetRarity()}</color>)</size>\n");
            }
            if (preview && combineItem != null)
            {
                CosmeticItem cosmeticItem =
                    CosmeticManager.ItemIdToItem[combineItem.itemDetails.m_iDefinition.m_SteamItemDef];
                if (cosmeticItem.tag != "")
                {
                    nameText.Append($"Particle : <color={cosmeticItem.GetColorString()}>{cosmeticItem.tag}</color>\n");
                }

            }
            itemNameText.SetText(nameText);

            itemCam.enabled = true;

            if (_depthOfField != null)
            {
                _depthOfField.gaussianStart.value = 500f;
            }

        }

        public void Combine()
        {
            DisableItemCam();
            inspectWindow.SetActive(false);
            AudioManager.Instance.PlayButton();
            InventoryManager.Instance.HandleQueue.Enqueue(InventoryManager.InventoryHandleType.Exchange);
            UInt32[] outCount = { 1 };
            UInt32[] inputCount = { 1, 1 };
            SteamItemDef_t[] outItemDefTs = { selectSteamItemStored.itemDetails.m_iDefinition };
            SteamItemInstanceID_t[] instanceIDTs = { combineItem.itemDetails.m_itemId, selectSteamItemStored.itemDetails.m_itemId };
            SteamInventory.ExchangeItems(out InventoryManager.Instance.inventoryHandle, outItemDefTs, outCount, 1, instanceIDTs,
                inputCount, 2);

            BackToNormalInv();
        }
    }
}
