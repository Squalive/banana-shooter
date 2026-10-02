using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class FaqMenu : MonoBehaviour
    {
        public static FaqMenu Instance { get; private set; }
        
        
        public enum FaqType
        {
            Replay,
        }
        
        
        public enum FaqItemType
        {
            Text,
            Image,
        }
        
        [Serializable]
        public class FaqItem
        {
            public FaqType faqType;
            public List<FaqList> list = new List<FaqList>();
            [Serializable]
            public class FaqList
            {
                public FaqItemType faqItemType;
                public string key;
                public Texture2D texture2D;
                public float multiplier = 1f;
            }
        }

        [SerializeField] private GameObject obj;

        [SerializeField] private LocalizeStringEvent title;

        [SerializeField] private Transform content;

        [SerializeField] private List<FaqItem> items = new List<FaqItem>();

        private Dictionary<FaqType, FaqItem> list = new Dictionary<FaqType, FaqItem>();


        private void Awake()
        {
            Instance = this;

            foreach (var item in items)
            {
                list.Add(item.faqType,item);
            }
            
        }

        [SerializeField] private RawImage imagePrefab;
        [SerializeField] private LocalizeStringEvent textPrefab;

        public void SetFaq(string titleKey, FaqType faqType)
        {
            for (int i = 0; i < content.childCount; i++)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            title.SetEntry(titleKey);

            if (list.TryGetValue(faqType, out var item))
            {
                foreach (var faq in item.list)
                {
                    switch (faq.faqItemType)
                    {
                        case FaqItemType.Text:
                            LocalizeStringEvent text = Instantiate(textPrefab, content);
                            text.SetEntry(faq.key);
                            break;
                        case FaqItemType.Image:
                            RawImage image = Instantiate(imagePrefab, content);
                            image.texture = faq.texture2D;
                            RectTransform rect = image.GetComponent<RectTransform>();
                            Vector2 size = new Vector2(faq.texture2D.width,faq.texture2D.height) * faq.multiplier;
                            rect.sizeDelta = size;
                            
                            break;
                    }
                }
            }

            StartCoroutine(Refresh());
        }

        IEnumerator Refresh()
        {
            int i = 10;
            while (i-->0)
            {
                yield return null;
                
            }
            
            obj.SetActive(true);
        }
        
    }
}
