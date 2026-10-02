
using System.Text;

using Manager;
using Steamworks.NET;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class NewsMenu : MonoBehaviour
    {
        [SerializeField] private GameObject obj;
        [SerializeField] private Button btn;
        
        [SerializeField] private RawImage thumbnail;
        [SerializeField] private TextMeshProUGUI text;

        private int _displayIndex = 0;

        private void Start()
        {
            btn.onClick.AddListener(OpenNewsPage);
            
            if (NewsManager.Instance.IsNewsLoaded())
            {
                DisplayNews();
            }
            else
            {
                obj.SetActive(false);
            }
        }

        private void OnEnable()
        {
            NewsManager.Instance.NewsImageLoaded += OnNewsImageLoaded;
            NewsManager.Instance.NewsLoaded += NewsLoaded;
        }
        
        private void OnDisable()
        {
            NewsManager.Instance.NewsImageLoaded -= OnNewsImageLoaded;
            NewsManager.Instance.NewsLoaded -= NewsLoaded;
        }

        void OpenNewsPage()
        {
            var newsItems = NewsManager.Instance.AppNewsResult.AppNews.NewsItems;
            if (newsItems.Count > _displayIndex)
            {
                NewsItem newsItem = newsItems[_displayIndex];
                if (!string.IsNullOrEmpty(newsItem.Url))
                {
                    SteamManager.OpenSteamBuiltInBrowser(newsItem.Url);
                }
            }
        }
        
        private void NewsLoaded()
        {
            _displayIndex = 0;
            DisplayNews();
        }
        
        private void OnNewsImageLoaded(int arg1, Texture2D arg2)
        {
            if(_displayIndex == arg1)
                thumbnail.texture = arg2;
        }

        void DisplayNews()
        {
            CancelInvoke(nameof(RepeatNextPage));
            
            var newsItems = NewsManager.Instance.AppNewsResult.AppNews.NewsItems;
            if (newsItems.Count > _displayIndex)
            {
                NewsItem newsItem = newsItems[_displayIndex];

                StringBuilder sb = new StringBuilder($"<color=#e8f3f6>{GameManager.JavaTimeStampToDateTime(newsItem.Date)}</color>\n<b><size=18>{newsItem.Title}</size></b>\n");

                int index = newsItem.Contents.IndexOf('\n');

                if(index!=-1)
                    sb.Append(newsItem.Contents.Substring(0,index).SteamRichTextToUnityRichText());
                else
                    sb.Append(newsItem.Contents.SteamRichTextToUnityRichText());
                if (NewsManager.Instance.NewsImages.TryGetValue(newsItem.ImageUrl, out var texture2D))
                {
                    thumbnail.texture = texture2D;
                }
                
                text.SetText(sb.ToString());
            }
            
            Invoke(nameof(RepeatNextPage),5f);
        }

        
        public void NextPage(int index)
        {
            _displayIndex += index;

            if (_displayIndex >= NewsManager.Instance.AppNewsResult.AppNews.NewsItems.Count) _displayIndex = 0;
            else if (_displayIndex < 0) _displayIndex = NewsManager.Instance.AppNewsResult.AppNews.NewsItems.Count;
            
            DisplayNews();
        }

        void RepeatNextPage()
        {
            NextPage(1);
        }
    }
}
