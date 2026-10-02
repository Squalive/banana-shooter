
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.Utils;
using Manager;
using Multiplayer;
using Multiplayer.Interface;
using Steamworks;
using Steamworks.NET;
using UnityEngine;
using UnityEngine.Networking;

namespace SteamWorkshop
{
    public enum WorkshopItemType
    {
        None,
        Map
    }

    [Serializable]
    public class WorkshopItem
    {
        public PublishedFileId_t fileId;
        public EItemState state;
        public SteamUGCDetails_t Details;
        public DateTime PostDate;
        public string path;
        public ulong size;

        public string previewUrl;

        public WorkshopItem(PublishedFileId_t fileId, EItemState state)
        {
            this.fileId = fileId;
            this.state = state; 
        }
    }
    [DefaultExecutionOrder(1)]
    public class SteamWorkshopManager : MonoBehaviour
    {
        public static SteamWorkshopManager Instance { private set; get; }
        
        public static bool Initialized { private set; get; } = false;

        public static Action OnQueryLoaded;

        public static bool QueryLoaded = false;

        public static PublishedFileId_t[] SubscribedItems;

        public static Dictionary<PublishedFileId_t, WorkshopItem> WorkshopItems =
            new Dictionary<PublishedFileId_t, WorkshopItem>();

        public Action<float, EItemUpdateStatus> OnGetProgress;
        public Action<bool,EResult> OnUploadFinish;

        public static Action<WorkshopItem> OnWorkshopItemInstalled;
        public static Action<PublishedFileId_t> OnUnSubscribeItem;
        public static Action<PublishedFileId_t> OnSubscribeItem;

        public static WorkshopItemType Type { private set; get; } = WorkshopItemType.None;

        private PublishedFileId_t _publishedFileId;

        private string _updateNote;
        public static AppId_t _appId;
        
        private CallResult<SteamUGCQueryCompleted_t> _queryResult;
        private UGCQueryHandle_t _queryHandle;

        protected Callback<ItemInstalled_t> ItemInstalled;
        protected Callback<RemoteStoragePublishedFileSubscribed_t> FileSubscribed;
        protected Callback<RemoteStoragePublishedFileUnsubscribed_t> FileUnSubscribed;
        protected Callback<DownloadItemResult_t> DownloadItemResult;
        private void Awake()
        {
            Instance = this;
            
            if (!SteamManager.Initialized) return;
            _appId = SteamUtils.GetAppID();
            // Debug.Log(_appId);
            
            _queryResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnUGCQueryCompleted);
            
            ItemInstalled = Callback<ItemInstalled_t>.Create(OnItemInstalled);
            FileSubscribed = Callback<RemoteStoragePublishedFileSubscribed_t>.Create(OnFileSubscribed);
            FileUnSubscribed = Callback<RemoteStoragePublishedFileUnsubscribed_t>.Create(OnFileUnSubscribed);
            DownloadItemResult = Callback<DownloadItemResult_t>.Create(OnDownloadItemResult);
        }

        
        void Start()
        {
            if (!SteamManager.Initialized)
            {
                Initialized = true;
                return;
            }
            uint maxLen = SteamUGC.GetNumSubscribedItems();
            
            Preload.IncreaseTotalStep((int)maxLen);

            SubscribedItems = new PublishedFileId_t[maxLen];
            WorkshopItems.Clear();

            uint length= SteamUGC.GetSubscribedItems(SubscribedItems, maxLen);

            for (int i = 0; i < length; i++)
            {
                PublishedFileId_t fileId = SubscribedItems[i];
                EItemState state = (EItemState)SteamUGC.GetItemState(fileId);

                WorkshopItem workshopItem = new WorkshopItem(fileId, state);
                
                if (SteamUGC.GetItemInstallInfo(fileId, out var size, out var path, 256, out var timeStamp))
                {
                    DateTime date = DateTimeOffset.FromUnixTimeSeconds(timeStamp).LocalDateTime;

                    workshopItem.PostDate = date;
                    workshopItem.path = path;
                    workshopItem.size = size;
                    // if (NetworkServerManager.EnabledWorkshopMaps.Count < 50)
                    // {
                    //     NetworkServerManager.EnabledWorkshopMaps.Add(fileId);
                    // }
                    
                    MapSaver.Instance.LoadWorkshopMap(path,fileId);
                    
                    Preload.Instance.NextStep();
                }
                
                WorkshopItems.Add(fileId,workshopItem);
            }

            _queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(SubscribedItems, maxLen);

            var call = SteamUGC.SendQueryUGCRequest(_queryHandle);
            _queryResult.Set(call);

            Initialized = true;
        }
        private void OnFileUnSubscribed(RemoteStoragePublishedFileUnsubscribed_t param)
        {
            if (_appId != param.m_nAppID)
            {
                Debug.LogError("App id isnt correct");
                return;
            }

            NetworkServerManager.EnabledWorkshopMaps.Remove(param.m_nPublishedFileId);
            NetworkServerManager.OnEnabledMapsChanged?.Invoke();
            
            if (SubscribedItems != null)
            {
                var subscribedItems=SubscribedItems.ToList();
                subscribedItems.Remove(param.m_nPublishedFileId);

                SubscribedItems = subscribedItems.ToArray();

                WorkshopItems.Remove(param.m_nPublishedFileId);
                
                OnUnSubscribeItem?.Invoke(param.m_nPublishedFileId);
                
                Debug.Log($"UnSubscribe item: {param.m_nPublishedFileId}");
            }
        }

        private void OnFileSubscribed(RemoteStoragePublishedFileSubscribed_t param)
        {
            if (_appId != param.m_nAppID)
            {
                Debug.LogError("App id isnt correct");
                return;
            }
            
            if (SubscribedItems != null)
            {
                var subscribedItems= SubscribedItems.ToList();
                subscribedItems.Add(param.m_nPublishedFileId);

                SubscribedItems = subscribedItems.ToArray();
                
                PublishedFileId_t fileId = param.m_nPublishedFileId;
                EItemState state = (EItemState)SteamUGC.GetItemState(fileId);

                WorkshopItem workshopItem = new WorkshopItem(fileId, state);

                WorkshopItems.TryAdd(fileId,workshopItem);
                OnSubscribeItem?.Invoke(param.m_nPublishedFileId);
                Debug.Log($"Subscribe item: {param.m_nPublishedFileId}");

                if ((EItemState.k_EItemStateInstalled | EItemState.k_EItemStateSubscribed) == state)
                {
                    _queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(new[] {param.m_nPublishedFileId}, 1);

                    var call = SteamUGC.SendQueryUGCRequest(_queryHandle);
                    _queryResult.Set(call);
                }
            }
        }

        private void OnItemInstalled(ItemInstalled_t param)
        {
            if (_appId != param.m_unAppID)
            {
                Debug.LogError("App id isnt correct");
                return;
            }

            EItemState state = (EItemState)SteamUGC.GetItemState(param.m_nPublishedFileId);

            if (!WorkshopItems.TryGetValue(param.m_nPublishedFileId, out var item))
            {
                item = new WorkshopItem(param.m_nPublishedFileId, state);
                WorkshopItems.Add(param.m_nPublishedFileId, item);
            }
            if (SteamUGC.GetItemInstallInfo(param.m_nPublishedFileId, out var size, out var path, 1000, out var timeStamp))
            {
                DateTime date = DateTimeOffset.FromUnixTimeSeconds(timeStamp).LocalDateTime;

                item.state = state;
                item.PostDate = date;
                item.path = path;
                item.size = size;
                    
                MapSaver.Instance.LoadWorkshopMap(path,param.m_nPublishedFileId);
            }
                
            _queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(new[] {param.m_nPublishedFileId}, 1);

            var call = SteamUGC.SendQueryUGCRequest(_queryHandle);
            _queryResult.Set(call);
            
            Debug.Log(param.m_nPublishedFileId + " New item installed");
        }
        public void CreateOrUpdateWorkshopItem(PublishedFileId_t id,WorkshopItemType type,string updateNote)
        {
            Type = type;
            _updateNote = updateNote;
            _publishedFileId = id;
            if (id == (PublishedFileId_t)0)
            {
                Debug.Log("Creating Item...");

                var createItemCall =
                    SteamUGC.CreateItem(_appId, EWorkshopFileType.k_EWorkshopFileTypeCommunity);

                var createItemResult = CallResult<CreateItemResult_t>.Create(OnCreateItemResult);
                createItemResult.Set(createItemCall);
            }
            else
            {
                TryStartItemUpdate();
            }
        }

        private void OnUGCQueryCompleted(SteamUGCQueryCompleted_t result, bool ioFailure)
        {
            if (ioFailure || result.m_unNumResultsReturned == 0)
            {
                // Failed to retrieve workshop item info
                Debug.Log("Failed to retrieve workshop item info");
                return;
            }

            WorkshopItem item=null;
            for (uint i = 0; i < result.m_unNumResultsReturned; i++)
            {
                SteamUGCDetails_t itemDetails;
                if (!SteamUGC.GetQueryUGCResult(_queryHandle, i, out itemDetails))
                {
                    // Failed to get item details
                    Debug.Log("Failed to get item details");
                    continue;
                }
                if (WorkshopItems.TryGetValue(itemDetails.m_nPublishedFileId, out item))
                {
                    item.Details = itemDetails;

                    if (SteamUGC.GetQueryUGCPreviewURL(result.m_handle, i, out var url, 1024))
                    {
                        item.previewUrl = url;
                        // StartCoroutine(LoadTextureToItem(url, item));
                    }
                }
                
            }

            if (!QueryLoaded)
            {
                QueryLoaded = true;
                OnQueryLoaded?.Invoke();
            }
            else
            {
                if (item != null)
                {
                    // if (NetworkServerManager.EnabledWorkshopMaps.Count < 50)
                    // {
                    //     NetworkServerManager.EnabledWorkshopMaps.Add(item.fileId);
                    //     NetworkServerManager.OnEnabledMapsChanged?.Invoke();
                    // }
                    OnWorkshopItemInstalled?.Invoke(item);
                }
            }

            if (SteamUGC.ReleaseQueryUGCRequest(result.m_handle))
            {
                Debug.Log("Release query handle successfully");
            }
            else
            {
                Debug.LogError("Failed to release query handle");
            }
        }
        private void OnCreateItemResult(CreateItemResult_t param, bool biofailure)
        {
            if (!biofailure && param.m_eResult == EResult.k_EResultOK)
            {
                _publishedFileId = new PublishedFileId_t((ulong)param.m_nPublishedFileId);
                Debug.Log("Create workshop item with ID " + _publishedFileId);
                if (MapSaver.CurrentMap != null)
                {
                    MapSaver.CurrentMap.isPublished = true;
                    MapSaver.CurrentMap.fileId = _publishedFileId;
                }
                        
                MapSaver.Instance.SaveBsmFileOnly();
                TryStartItemUpdate();
                
                
            }
            else
            {
                Debug.LogError("Failed to create workshop item");
            }
        }

        void TryStartItemUpdate()
        {
            var updateHandle = SteamUGC.StartItemUpdate(_appId, _publishedFileId);
            if (updateHandle == UGCUpdateHandle_t.Invalid)
            {
                Debug.LogError("Failed to start item update");
                return;
            }
            
            Debug.Log($"Start Updating Item Status... ({_publishedFileId.m_PublishedFileId})");


            switch (Type)
            {
                case WorkshopItemType.Map:
                    Debug.Log("Cleaning up the steam temp folder");
            
                    string destinationPath = MapSaver.SteamTemp;
            
                    foreach (var filePath in Directory.GetFiles(destinationPath))
                    {
                        File.Delete(filePath);
                    }

                    foreach (var subdirectoryPath in Directory.GetDirectories(destinationPath))
                    {
                        Directory.Delete(subdirectoryPath, true);
                    }
            
            
                    Debug.Log("Copying files to steam temp folder");
                    MapData data = MapSaver.CurrentMap;

                    string basePath = MapSaver.path;
            
                    //Get the source files
                    List<string> files = new List<string>
                    {
                        basePath + data.GetNameString() + ".bsm",
                        basePath + data.GetNameString() + ".jpg"
                    };

                    List<string> directories = new List<string>()
                    {
                        data.GetTexturePath(),
                        data.GetDecalTexturePath(),
                        data.GetAudioPath(),
                        data.GetModelPath()
                    };

                    foreach (var directory in directories)
                    {
                        if (Directory.Exists(directory))
                        {
                            var sourceDir = new DirectoryInfo(directory);
                            sourceDir.DeepCopy(destinationPath);
                        }
                    }

                    foreach (var filePath in files)
                    {
                        var newFilePath = filePath.Replace(basePath, destinationPath);
                        File.Copy(filePath, newFilePath, true);
                    }

                    if (!SteamUGC.SetItemTitle(updateHandle, data.name))
                    {
                        Debug.LogError("Failed to set item title");
                    }
                    
                    if (!SteamUGC.SetItemDescription(updateHandle, data.description))
                    {
                        Debug.LogError("Failed to set item description");
                    }
                    if (!SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic))
                    {
                        Debug.LogError("Failed to set item to public");
                    }

                    string previewPath = MapSaver.path + data.GetNameString() + ".jpg";
                    if (!SteamUGC.SetItemPreview(updateHandle,previewPath))
                    {
                        Debug.LogError("Failed to set item preview image");
                    }
                    
                    if (!SteamUGC.SetItemTags(updateHandle,ParseTags("Map")))
                    {
                        Debug.LogError("Failed to set item tags");
                    }
                    if (!SteamUGC.SetItemContent(updateHandle,MapSaver.SteamTemp))
                    {
                        Debug.LogError("Failed to set item content");
                    }

                    var submitItemUpdateCall = SteamUGC.SubmitItemUpdate(updateHandle,_updateNote);
                    var submitItemUpdateResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemUpdateComplete);
                    submitItemUpdateResult.Set(submitItemUpdateCall);
                    break;
                    
            }
            
            
            StartCoroutine(GetUpdateProgress(8,updateHandle));
        }

        private void OnSubmitItemUpdateComplete(SubmitItemUpdateResult_t param, bool biofailure)
        {
            Debug.Log(biofailure + " " + param.m_eResult);
            if (!biofailure && param.m_eResult == EResult.k_EResultOK)
            {
                Debug.Log($"Submit item ({param.m_nPublishedFileId.m_PublishedFileId}) update successfully");
                
                SteamFriends.ActivateGameOverlayToWebPage("steam://url/CommunityFilePage/" + param.m_nPublishedFileId.m_PublishedFileId);
                
                switch (Type)
                {
                    case WorkshopItemType.Map:
                        // MEBase.Instance.HasChanged = true;

                        break;
                }
                OnUploadFinish?.Invoke(true,param.m_eResult);
            }
            else
            {
                OnUploadFinish?.Invoke(false,param.m_eResult);
                Debug.LogError("Failed to submit item update");
            }
            
            StopAllCoroutines();
        }

        IEnumerator GetUpdateProgress(int times,UGCUpdateHandle_t handle)
        {
            for (int i = 0; i < times; i++)
            {
                yield return new WaitForSeconds(.5f);
                GetUpdateProgress(handle);
            }
        }
        void GetUpdateProgress(UGCUpdateHandle_t handle)
        {
            EItemUpdateStatus updateStatus = SteamUGC.GetItemUpdateProgress(handle, out var processed, out var total);
            
            if(total!=0)
                OnGetProgress?.Invoke((float)processed / total,updateStatus);
        }
        
        List<string> ParseTags(string tagsString)
        {
            var tags = new List<string>();
            foreach (var t in tagsString.Split(','))
            {
                tags.Add(t.Trim());
            }
            return tags;
        }

        public void ReadMore()
        {
            SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/sharedfiles/workshoplegalagreement");
        }

        public void DownloadItemTemp(PublishedFileId_t fileIdT)
        {
            if (SteamUGC.DownloadItem(fileIdT, true))
            {
                OnItemStartDownloading?.Invoke(fileIdT);
                Debug.Log("Start to download: " + fileIdT);
            }
        }

        public static Action<PublishedFileId_t> OnItemDownloaded;
        public static Action<PublishedFileId_t> OnItemStartDownloading;

        private void OnDownloadItemResult(DownloadItemResult_t param)
        {
            if (param.m_unAppID != _appId)
            {
                Debug.LogError("Wrong app id");
                return;
            }

            if (param.m_eResult == EResult.k_EResultOK)
            {
                Debug.Log($"{param.m_nPublishedFileId} is downloaded");

                OnItemDownloaded?.Invoke(param.m_nPublishedFileId);
            }
        }

    }
}
