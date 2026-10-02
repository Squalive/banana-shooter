
using System;
using Audio;
using Menu;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI.AddObject
{
    
    public class ObjectItemUI : MonoBehaviour,IPointerDownHandler
    {
        [SerializeField] private ObjectItem item;

        [SerializeField] private LocalizeStringEvent localizeText;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RawImage img;

        [SerializeField] private MaskedObject masked;

        public ObjectType type;

        [Header("Decal")][HideInInspector] public int decalMaterialIndex;
        [Header("Audio")] [HideInInspector]public int audioClipIndex;
        public string n="";

        [SerializeField] public GameObject checkMark;

        private static ObjectItemUI _clickedObject = null;

        private void Start()
        {
            gameObject.AddComponent<ButtonSelect>();
        }

        public void Init(ObjectType t, string key, Texture2D texture2D, int ex,Canvas rootCanvas,RectTransform maskRect, bool local, ObjectItem objectItem=null)
        {
            type = t;
            n = key;
            item = objectItem;
            if (local)
            {
                localizeText.SetEntry(key);
            }
            else
            {
                localizeText.enabled = false;
                text.SetText(key);
            }
            img.texture = texture2D;

            switch (t)
            {
                case ObjectType.Decal:
                    decalMaterialIndex = ex;
                    break;
                case ObjectType.AudioSource:
                    audioClipIndex = ex;
                    break;
            }
            
            masked.Initialize(rootCanvas,maskRect);
        }

        public void SetTexture(Texture2D texture2D)
        {
            img.texture = texture2D;
        }

        void OnClick()
        {
            AudioManager.Instance.PlayButton();
            if (item != null)
            {
                AddObjectMenu.Instance.SelectItem(item);
                
            }
            else
            {
                AddExternalObjectMenu.Instance.SetSelectItem(this);
            }
            
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Click();
        }

        public void Click()
        {
            if (_clickedObject == null)
            {
                _clickedObject = this;
                checkMark.SetActive(true);
            } 
            else if (_clickedObject != this)
            {
                _clickedObject.checkMark.SetActive(false);
                _clickedObject = this;
                checkMark.SetActive(true);
            }
            OnClick();
            if (item == null)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    AddExternalObjectMenu.Instance.OpenEditBar();
                }
            }
        }

    }
}
