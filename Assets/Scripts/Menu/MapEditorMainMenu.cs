
using System;
using System.Collections.Generic;
using System.IO;

using CodingDaniel.MapEditor.MEEditor.MESave;
using Manager;
using Multiplayer;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menu
{
    public class MapEditorMainMenu : MonoBehaviour,IPointerClickHandler
    {
        public static MapEditorMainMenu Instance { private set; get; }

        private void Awake()
        {
            Instance = this;

            Vector2 delta = previewImg.rectTransform.sizeDelta;
            previewTexture = new Texture2D((int)delta.x, (int)delta.y,TextureFormat.RGB24,false);
        }

        [SerializeField] private Transform content;
        [SerializeField] private MapItemUI prefab;

        [SerializeField] private GameObject detail;
        [SerializeField] private TextMeshProUGUI mapNameText,mapDescriptionText;
        [SerializeField] private LocalizeStringEvent createdDateText;

        [SerializeField] private RawImage previewImg;
        [SerializeField] private GameObject loading;

        [SerializeField] private GameObject enterBtn;

        private Texture2D previewTexture;
        private void Start()
        {
            foreach (var data in MapSaver.Instance.EditingMaps)
            {
                MapItemUI ui = Instantiate(prefab, content);
                
                ui.Init(data);
            }
        }

        [SerializeField] private GameObject[] scores = new GameObject[5];
        private FileInfo _selectedMap;
        private GameObject _selectObj;

        public void SelectMap(FileInfo data,MapMetadata md,GameObject obj,bool join, uint per)
        {
            if (join)
            {
                EnterMap();
                return;
            }

            if (data==null)
            {
                _selectedMap = null;
                _selectObj = null;
                EnterMap();
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                scores[i].SetActive(i <= (int)(per-1));
            }

            _selectObj = obj;
            _selectedMap = data;
            detail.SetActive(true);
            enterBtn.SetActive(true);
            
            mapNameText.SetText(md.Name);
            mapDescriptionText.SetText(string.IsNullOrEmpty(md.Description) ? "(empty)" : md.Description);

            createdDateText.StringReference.Arguments = new List<object>() {data.CreationTime};
            createdDateText.RefreshString();
            
            LoadPreviewImg(data.Name.Substring(0,data.Name.Length-4));
        }

        async void LoadPreviewImg(string n)
        {
            previewImg.gameObject.SetActive(false);
            loading.SetActive(true);

            string path = MapSaver.path + n + ".jpg";

            if (File.Exists(path))
            {
                byte[] data = await SaveSystem.ReadByteFromFileAsync(path);

                previewTexture.LoadImage(data);
                previewImg.texture = previewTexture;
                previewImg.gameObject.SetActive(true);
                loading.SetActive(false);
            }
        }

        
        public void EnterMap()
        {
            MapSaver.CurrentMapFile = _selectedMap;
            
            MusicManager.Instance.ChangeMusic(MusicManager.MusicType.None);
            SceneManager.LoadSceneAsync("MapEditor", LoadSceneMode.Single);
            editBar.gameObject.SetActive(false);
            detail.SetActive(false);
            enterBtn.SetActive(false);
        }

        public Transform editBar;
        
        public void RemoveMap()
        {
            if (_selectedMap == null || _selectObj == null) return;
            string basePath = _selectedMap.FullName.Substring(0,_selectedMap.FullName.Length-4);

            string bsmPath = basePath + ".bsm";

            if (File.Exists(bsmPath))
            {
                try
                {
                    File.Delete(bsmPath);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
                
                Destroy(_selectObj);

                if (MapSaver.Instance.EditingMaps.Contains(_selectedMap))
                {
                    MapSaver.Instance.EditingMaps.Remove(_selectedMap);
                }
            }

            string previewPath = basePath + ".jpg";

            if (File.Exists(previewPath))
            {
                try
                {
                    File.Delete(previewPath);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
            }
            
            string mmdPath = basePath + ".metadata";

            if (File.Exists(mmdPath))
            {
                try
                {
                    File.Delete(mmdPath);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
            }

            string texturePath = basePath + "_texture";
            
            if (Directory.Exists(texturePath))
            {
                Directory.Delete(texturePath,true);
            }
            
            string decalPath = basePath + "_decal_texture";
            
            if (Directory.Exists(decalPath))
            {
                Directory.Delete(decalPath,true);
            }
            
            string audioPath = basePath + "_audio";
            
            if (Directory.Exists(audioPath))
            {
                Directory.Delete(audioPath,true);
            }
            
            editBar.gameObject.SetActive(false);
            detail.SetActive(false);
            enterBtn.SetActive(false);

            _selectedMap = null;
            _selectObj = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            detail.SetActive(false);
            _selectedMap = null;
            enterBtn.SetActive(false);
            editBar.gameObject.SetActive(false);
        }
    }
}
