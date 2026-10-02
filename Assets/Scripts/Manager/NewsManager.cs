using System;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Web;

namespace Manager
{
    public class NewsManager : MonoBehaviour
    {
        public static NewsManager Instance { private set; get;}

        private AppNewsResult _appNewsResult = null;

        public AppNewsResult AppNewsResult => _appNewsResult;

        public Action<int,Texture2D> NewsImageLoaded;
        public Action NewsLoaded;

        public Dictionary<string, Texture2D> NewsImages = new();

        private async void Awake()
        {
            Instance = this;

            _appNewsResult = await HttpClient.Get<AppNewsResult>(EndPoint.GetNews);

            if (IsNewsLoaded())
            {
                NewsLoaded?.Invoke();
                Debug.Log("News Loaded");

                for (int i = 0; i < _appNewsResult.AppNews.NewsItems.Count; i++)
                {
                    StartCoroutine(LoadTextureToItem(_appNewsResult.AppNews.NewsItems[i].ImageUrl, i));
                }
            }
            else
            {
                Debug.Log("Failed to load news");
            }
        }
        
        IEnumerator LoadTextureToItem(string url,int index)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                
                NewsImageLoaded?.Invoke(index,texture);

                if (NewsImages.ContainsKey(url)) NewsImages.Remove(url);
                
                NewsImages.Add(url,texture);
            }
        }

        public bool IsNewsLoaded()
        {
            return _appNewsResult!=null && _appNewsResult.AppNews.NewsItems.Count>0;
        }

        
    }

    public static class SteamNews
    {
        private static int h1Size = 16;
        public static string SteamRichTextToUnityRichText(this string str)
        {
            return str.Replace("[h1]", $"<size={h1Size}><b>").Replace("[/h1]", "</size></b>");
        }
    }

    [Serializable]
    public class NewsItem
    {
        public string Gid=String.Empty;

        public string Title=String.Empty;

        public string ImageUrl=String.Empty;

        public string Url=String.Empty;

        public string Author=String.Empty;

        public string Contents=String.Empty;

        public string FeedLabel=String.Empty;

        public uint Date=0;

        NewsItem()
        {
            
        }
    }

    [Serializable]
    public class AppNews
    {
        public string Appid;

        public List<NewsItem> NewsItems;

        public int Count = 0;
        
        AppNews()
        {
            Appid=String.Empty;
            NewsItems = new List<NewsItem>();
            Count = 0;
        }
    }

    [Serializable]
    public class AppNewsResult
    {
        public AppNews AppNews;
        
        AppNewsResult()
        {
        }
    }
}
