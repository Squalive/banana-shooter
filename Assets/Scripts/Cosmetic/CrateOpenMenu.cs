using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Audio;

using Manager;
using Menu;
using Multiplayer;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Cosmetic
{
    public class CrateOpenMenu : MonoBehaviour
    {
        public static CrateOpenMenu Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            StartCoroutine(Glowing());
        }

        private void Start()
        {
            boxCamera = CrateItem.Instance.cam;
            boxCamera.enabled = false;
            meshFilter = CrateItem.Instance.filter;
            _itemTransform = meshFilter.transform;
            _render = CrateItem.Instance.render;
            _render.enabled = false;
            viewer.SetObj(_render.transform);
        }

        [Header("Crate")]
        [SerializeField] private GameObject boxPage;
        [SerializeField] private Camera boxCamera;

        [SerializeField] private LocalizeStringEvent unlock;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField]private Item3DViewer viewer;
        [SerializeField] private Transform content;
        [SerializeField] private Animator anim;
        [SerializeField] private Button backButton,openBtn;
        private MeshRenderer _render;
        private Transform _itemTransform;

        private static readonly float sizeFactor=10f;
        private CosmeticItem _currentItem;
        private SteamItemStored _currentSteamItemStored;
        private static readonly int Open = Animator.StringToHash("Open");
        private static readonly int Roll = Animator.StringToHash("Roll");
        private bool _isRolling;
        private float _currentDelay,_currentTime;
        
        [Header("Roll")]
        [SerializeField] private float defaultDelay=0.05f,delayMultiplier = 1.4f;
        [SerializeField] private CanvasGroup gradient;
        private float desiredAlpha = 0.14f;
        [SerializeField] private float defaultAlpha = 0.14f;

        [SerializeField] private RawImage icon,rarityImg;
        private int _lastIndex = -1;
        
        public bool IsOpening { get; private set; }
        public void OpenPage(CosmeticItem item,SteamItemStored steamItemStored)
        {
            if (item.type != CosmeticItem.Type.Box) return;
            
            CosmeticMenu.Instance.inventoryMenu.SetActive(false);
            
            _currentItem = item;
            _currentSteamItemStored = steamItemStored;
            //Reset Page
            anim.Play("CrateUI_idle",-1,0);
            
            //Enable UI and camera
            boxCamera.enabled = true;
            _render.enabled = true;
            boxPage.SetActive(true);
            openBtn.interactable = true;
            
            //Set Item
            //UI
            unlock.StringReference.Arguments = new List<object>() {$"<b>{item.displayName} ({item.GetRarity()})</b>"};
            unlock.RefreshString();
            
            //Object
            meshFilter.mesh = item.mesh;
            _render.materials = item.materials;
            _itemTransform.localScale = item.GetSize()*sizeFactor;
            _itemTransform.rotation = Quaternion.Euler(item.defaultRotation);
            viewer.SetObj(_itemTransform);

            //TODO: Display what would you get from the box
            InitItem(item);
            
            if(UIManager.Instance)
                UIManager.Instance.SetButton(backButton);
            IsOpening = true;
        }

        public void DisableOpening()
        {
            IsOpening = false;
        }
        
        public void ClosePage()
        {
            if (!IsOpening) return;
            IsOpening = false;
            boxCamera.enabled = false;
            boxPage.SetActive(false);
            CosmeticMenu.Instance.inventoryMenu.SetActive(true);
            _render.enabled = false;
            if (_isRolling)
            {
                //If it is rolling
                if (_rolling != null)
                {
                    StopCoroutine(_rolling);
                    DisplayRewardItem(_receiveItem,_receiveSteamItemStored);
                }

                else if (_rewarding != null)
                {
                    StopCoroutine(_rewarding);
                    DisplayRewardItem(_receiveItem,_receiveSteamItemStored);
                }
            }
            
            _isRolling = false;
            // CancelInvoke(nameof(TestRoll));
        }
        #region Init Item

        void InitItem(CosmeticItem cosmeticItem)
        {
            for (int i = 0; i < content.childCount; i++)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            foreach (var relatedItem in cosmeticItem.relatedItem)
            {
                CosmeticPrefab item = Instantiate(PrefabManager.Instance.GetPrefab("Cosmetic"),
                    content).GetComponent<CosmeticPrefab>();
                
                item.SetItem(relatedItem);
            }
            
        }

        #endregion

        private CosmeticItem _receiveItem;
        private SteamItemStored _receiveSteamItemStored;
        void TestRoll(CosmeticItem receiveItem, SteamItemStored receiveSteamItemStored)
        {
            _receiveItem = receiveItem;
            _receiveSteamItemStored = receiveSteamItemStored;
            if(IsOpening)
                _rolling = StartCoroutine(WaitForRoll());
            else
                DisplayRewardItem(receiveItem,receiveSteamItemStored);
        }

        private Coroutine _rolling,_rewarding;
        IEnumerator WaitForRoll()
        {
            int time = 8;
            while (time>0)
            {
                yield return new WaitForSeconds(0.1f);
                time--;

                if (!IsOpening)
                {
                    DisplayRewardItem(_receiveItem,_receiveSteamItemStored);
                    yield break;
                }
            }
           
            anim.SetTrigger(Roll);
            _currentDelay = defaultDelay;
            _currentTime = 0;
            icon.texture = _currentItem.relatedItem[Random.Range(0, _currentItem.relatedItem.Count)].icon;
            _isRolling = true;
            boxCamera.enabled = false;
            _render.enabled = false;
        }

        private void Update()
        {
            gradient.alpha = Mathf.Lerp(gradient.alpha, desiredAlpha, Time.deltaTime * 1f);
            if(!_isRolling)return;
            
            _currentTime += Time.deltaTime;
            if (_currentTime >= _currentDelay)
            {
                if (_currentDelay >= 0.7f)
                {
                    AudioManager.Instance.Play("crate_open1");
                    _isRolling = false;
                    icon.texture = _receiveItem.icon;
                    rarityImg.color = _receiveItem.GetColor();

                    _rewarding= StartCoroutine(DisplayReward(_receiveItem,_receiveSteamItemStored));
                }
                else
                {
                    AudioManager.Instance.Play("ticking");
                    _currentTime -= _currentDelay;
                    int index;
                    index = Random.Range(0, 10) > 1 ? Random.Range(0, _currentItem.relatedItem.Count/2) : Random.Range(_currentItem.relatedItem.Count/2, _currentItem.relatedItem.Count);
                    
                    while (_lastIndex==index)
                    {
                        if (Random.Range(0, 10) >= 3)
                        {
                            index = Random.Range(0, _currentItem.relatedItem.Count/2);
                        }
                        else
                        {
                            index = Random.Range(_currentItem.relatedItem.Count/2, _currentItem.relatedItem.Count);
                        }
                    }

                    _lastIndex = index;
                    icon.texture = _currentItem.relatedItem[_lastIndex].icon;
                    rarityImg.color = _currentItem.relatedItem[_lastIndex].GetColor();
                }
                
            }

            _currentDelay += Time.deltaTime * 0.1f * delayMultiplier;
        }
        
        
        public void OpenCrate()
        {
            InventoryManager.Instance.CrateOpenQueue.Clear();
            AudioManager.Instance.Play("crate_open0");
            anim.SetTrigger(Open);
            openBtn.interactable = false;
            EventSystem.current.SetSelectedGameObject(null);

            desiredAlpha = defaultAlpha;
            _lastIndex = -1;
            
            InventoryManager.Instance.HandleQueue.Enqueue(InventoryManager.InventoryHandleType.Exchange);
            UInt32[] outCount = {1};
            UInt32[] inputCount = {1};
            SteamItemDef_t defT = InventoryManager.GetCrateDef(_currentSteamItemStored.itemDetails.m_iDefinition.m_SteamItemDef);
            SteamItemDef_t[] outItemDefTs = {defT};
            SteamItemInstanceID_t[] instanceIDTs = {_currentSteamItemStored.itemDetails.m_itemId};
            SteamInventory.ExchangeItems(out InventoryManager.Instance.inventoryHandle, outItemDefTs, outCount, 1, instanceIDTs,
                inputCount, 1);
        }


        IEnumerator Glowing()
        {
            while (true)
            {
                desiredAlpha = 0.14f;
                yield return new WaitForSeconds(1f);
                desiredAlpha = 0.5f;
                yield return new WaitForSeconds(1f);
            }
        }

        IEnumerator DisplayReward(CosmeticItem receiveItem, SteamItemStored receiveSteamItemStored)
        {
            yield return new WaitForSeconds(1f);
            ClosePage();
            
            DisplayRewardItem(receiveItem,receiveSteamItemStored);
        }

        void DisplayRewardItem(CosmeticItem receiveItem, SteamItemStored receiveSteamItemStored)
        {
            CosmeticMenu.Instance.meshFilter.mesh = receiveItem.mesh;
            CosmeticMenu.Instance.meshRenderer.materials = receiveItem.materials;
            Transform transform1;
            (transform1 = CosmeticMenu.Instance.meshRenderer.transform).rotation = Quaternion.Euler(receiveItem.defaultRotation);
            transform1.localScale = receiveItem.GetSize()*InventoryManager.sizeFactor;
            CosmeticMenu.Instance.meshRenderer.enabled = true;
            
            CosmeticMenu.Instance.cosmeticItem3dViewer.SetObj(transform1);
            
            string nameText = receiveItem.displayName + " (" + receiveItem.GetRarity() + ")\n";
            string nameWithColor = receiveItem.displayName;
            // itemStatus += $"Type : {cosmeticItem.type}\n";
            if (receiveSteamItemStored.properties.ContainsKey("color"))
            {

                Color c = receiveSteamItemStored.GetColor();
                if(c!=Color.clear)
                    CosmeticMenu.Instance.meshRenderer.material.color = c;
                nameWithColor += $" (<color={receiveSteamItemStored.GetColorString()}>{receiveSteamItemStored.properties["color"]}</color>)";
                nameText +=
                    $"<size=35>Color : <color={receiveSteamItemStored.GetColorString()}>{receiveSteamItemStored.properties["color"]}</color>\n";
            }
            
            AudioManager.Instance.Play("Reward");
            if (receiveSteamItemStored.properties.ContainsKey("shiny"))
            {
                if (receiveSteamItemStored.properties["shiny"] != "0")
                {
                    CosmeticMenu.Instance.meshRenderer.material.EnableKeyword("_EMISSION");
                    nameWithColor += $" shiny {receiveSteamItemStored.properties["shiny"]}";
                    nameText += $"Shiny : {receiveSteamItemStored.properties["shiny"]}\n";
                }
            
                switch (receiveSteamItemStored.properties["shiny"])
                {
                    case "0.1":
                        CosmeticMenu.Instance.meshRenderer.material.SetColor(CosmeticMenu.EmissionColor,
                            CosmeticMenu.Instance.meshRenderer.material.color * 1.1f);
                        break;
                    case "0.2":
                        CosmeticMenu.Instance.meshRenderer.material.SetColor(CosmeticMenu.EmissionColor,
                            CosmeticMenu.Instance.meshRenderer.material.color * 1.2f);
                        break;
                    case "0.4":
                        CosmeticMenu.Instance.meshRenderer.material.SetColor(CosmeticMenu.EmissionColor,
                            CosmeticMenu.Instance.meshRenderer.material.color * 1.4f);
                        break;
                    case "0.6":
                        CosmeticMenu.Instance.meshRenderer.material.SetColor(CosmeticMenu.EmissionColor,
                            CosmeticMenu.Instance.meshRenderer.material.color * 1.8f);
                        break;
                    case "0.8":
                        CosmeticMenu.Instance.meshRenderer.material.SetColor(CosmeticMenu.EmissionColor,
                            CosmeticMenu.Instance.meshRenderer.material.color * 2f);
                        break;
                    case "0.9":
                        CosmeticMenu.Instance.meshRenderer.material.SetColor(CosmeticMenu.EmissionColor,
                            CosmeticMenu.Instance.meshRenderer.material.color * 2.2f);
                        break;
                }
            
            
            }

            CosmeticMenu.Instance.itemNameText.SetText(nameText);
            string content =
                $"<color={receiveSteamItemStored.GetColorString()}>{NetworkManager.Instance.PersonalName} Got one out of the box {nameWithColor}</color>";
            
            if(NetworkManager.Instance.Client.IsConnected)
                NetworkManager.Instance.SendMsg(content,2);
            
            CosmeticMenu.Instance.itemCam.enabled = true;
            CosmeticMenu.Instance.inspectWindow.SetActive(true);
        }

        private void OnEnable()
        {
            InventoryManager.Instance.OnGetBox += TestRoll;
        }

        private void OnDisable()
        {
            InventoryManager.Instance.OnGetBox -= TestRoll;
        }
    }
}
