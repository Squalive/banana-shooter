using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menu
{
    public class NotificationMenu : MonoBehaviour
    {
        private static NotificationMenu _instance;

        public static NotificationMenu Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null)
                {
                    _instance = value;
                }
                else if (_instance != value)
                {
                    Debug.Log($"{nameof(NotificationMenu)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        private void Awake()
        {
            Instance = this;
            SetCanvas(true);
            canvas.interactable = true;
            canvas.blocksRaycasts = false;
        }

        public void SetCanvas(bool flag)
        {
            // canvas.interactable = flag;
            // canvas.blocksRaycasts = flag;
        }

        [SerializeField] private Transform content;
        [SerializeField] private NotificationItemUI prefab;
        [SerializeField] private CanvasGroup canvas;

        public void NewItem(string titleKey, string infoKey)
        {
            int count = content.childCount;
            if (count >= 2)
            {
                for (int i = 0; i < count-1; i++)
                {
                    Destroy(content.GetChild(i).gameObject);
                }
            }
            
            NotificationItemUI item = Instantiate(prefab, content);
            item.SetValue(titleKey, infoKey);
        }
    }
}
