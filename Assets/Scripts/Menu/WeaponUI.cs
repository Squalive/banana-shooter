using System;
using System.Collections.Generic;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class WeaponUI : MonoBehaviour
    {
        public List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
        public List<RawImage> images = new List<RawImage>();

        [SerializeField] private GameObject select, deselect;

        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private string actionName=String.Empty;

        private void Awake()
        {
            keyText.SetText(GameManager.GetBindingName(actionName, 0));
        }

        public void SetTexture(Texture2D texture2D)
        {
            foreach (var raw in images)
            {
                raw.texture = texture2D;
            }
        }

        public void SetText(string text)
        {
            foreach (var t in texts)
            {
                t.SetText(text);
            }
        }

        public void Select()
        {
            @select.SetActive(true);
            deselect.SetActive(false);
        
            GameUIManager.Instance.SetWeaponAlpha();
        }
    
        public void DeSelect()
        {
            @select.SetActive(false);
            deselect.SetActive(true);
        }

        public void Display()
        {
            gameObject.SetActive(true);
        }
    
        public void NotDisplay()
        {
            gameObject.SetActive(false);
        }
    }
}
