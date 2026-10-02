
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Audio;

using CodingDaniel.FileBrowser;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Manager;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI.AddObject
{
    public class AddExternalObjectMenu : MonoBehaviour
    {
        public static AddExternalObjectMenu Instance { private set; get; }
        [SerializeField] private Button addNewCustomDecalBtn,addBtn;

        private int _loadTexture=0;

        private IME _me;
        private IMESelectionComponent _selectionComponent;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform maskRect;
        private void Awake()
        {
            Instance = this;
            _me=MEBase.Instance;
            _selectionComponent=MESelectionComponent.Instance;
            
            addNewCustomDecalBtn.onClick.AddListener(TryToAddDecal);
            addBtn.onClick.AddListener(Create);
            addAudioBtn.onClick.AddListener(TryToAddAudioSource);

            MapSaver.Instance.OnMapLoaded += Init;
        }
        private ObjectItemUI _selectItem;

        public ObjectItemUI SelectItem
        {
            get => _selectItem;
            set => _selectItem = value;
        }

        [SerializeField] private Transform customModelContent;
        void Init()
        {
            foreach (var tuple in MapSaver.Instance.externalDecalMaterials)
            {
                var material = tuple.Item2;
                ObjectItemUI itemUI = Instantiate(prefab, content);
            
                itemUI.Init(ObjectType.Decal,material.name,(Texture2D)material.GetTexture(MapSaver.BaseMap),MapSaver.Instance.GetDecalMaterialIndex(material,true),rootCanvas,maskRect,false);

            }
            
            foreach (var tuple in MapSaver.Instance.externalAudioClips)
            {
                var clip = tuple.Item2;
                ObjectItemUI itemUI = Instantiate(prefab, audioContent);
            
                itemUI.Init(ObjectType.AudioSource,clip.name,audioTexture,MapSaver.Instance.GetAudioIndex(clip),rootCanvas,maskRect,false);
            }

            foreach (var model in MapSaver.Instance.ModelConfigs.Values)
            {
                ObjectItemUI itemUI = Instantiate(prefab, customModelContent);
            
                itemUI.Init(ObjectType.Decoration,model.name,model.previewImage,0,rootCanvas,maskRect,false);
            }
            
            addNewCustomDecalBtn.transform.SetAsLastSibling();
            addAudioBtn.transform.SetAsLastSibling();
            
        }

        private void OnDestroy()
        {
            addNewCustomDecalBtn.onClick.RemoveListener(TryToAddDecal);
            addBtn.onClick.RemoveListener(Create);
            addAudioBtn.onClick.RemoveListener(TryToAddAudioSource);
            
            MapSaver.Instance.OnMapLoaded -= Init;
        }

        

        [SerializeField] private Transform editBar;

        public void OpenEditBar()
        {
            editBar.position = Input.mousePosition;
            editBar.gameObject.SetActive(true);
        }
        public void CloseEditBar()
        {
            if(editBar.gameObject.activeSelf)
                editBar.gameObject.SetActive(false);
        }

        
        public void Edit()
        {
            if (SelectItem == null) return;
            switch (SelectItem.type)
            {
                case ObjectType.Decal:
                    EditDecal();
                    break;
            }
        }

        
        public void Remove()
        {
            if (SelectItem == null) return;

            switch (SelectItem.type)
            {
                case ObjectType.Decal:
                    Material material = MapSaver.Instance.externalDecalMaterials[SelectItem.decalMaterialIndex].Item2;

                    foreach (var objectItem in MapSaver.Instance.ObjectsNeedToSave)
                    {
                        if (objectItem.type == ObjectType.Decal)
                        {
                            DecalProjector decalProjector = objectItem.GetComponent<DecalProjector>();
                            if (decalProjector != null && decalProjector.material.GetInstanceID() == material.GetInstanceID())
                            {
                                Destroy(objectItem.gameObject);
                            }
                        }
                    }
            
                    if (MapSaver.CurrentMap != null)
                    {
                        string path = Path.Combine(MapSaver.CurrentMap.GetDecalTexturePath(), material.name + ".png");

                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
            
                    MapSaver.Instance.externalDecalMaterials.RemoveAt(SelectItem.decalMaterialIndex);
                    break;
                case ObjectType.AudioSource:
                    AudioClip clip = MapSaver.Instance.externalAudioClips[SelectItem.audioClipIndex].Item2;

                    foreach (var objectItem in MapSaver.Instance.ObjectsNeedToSave)
                    {
                        if (objectItem.type == ObjectType.AudioSource)
                        {
                            AudioSource audioSource = objectItem.GetComponent<AudioSource>();

                            if (audioSource.clip == clip)
                            {
                                Destroy(objectItem.gameObject);
                            }
                        }
                    }

                    if (MapSaver.CurrentMap != null)
                    {
                        string path = Path.Combine(MapSaver.CurrentMap.GetAudioPath(), clip.name + ".wav");

                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    
                    MapSaver.Instance.externalAudioClips.RemoveAt(SelectItem.audioClipIndex);
                    break;
                case ObjectType.Decoration:
                    if (MapSaver.Instance.ModelConfigs.TryGetValue(SelectItem.n,out var config))
                    {
                        foreach (var objectItem in MapSaver.Instance.ObjectsNeedToSave)
                        {
                            if (objectItem.type == ObjectType.Decoration)
                            {
                                if (objectItem.n == config.name)
                                {
                                    Destroy(objectItem.gameObject);
                                }
                            }
                        }

                        if (MapSaver.CurrentMap != null)
                        {
                            string path = Path.Combine(MapSaver.CurrentMap.GetModelPath(), config.name);

                            if (Directory.Exists(path))
                            {
                                Directory.Delete(path,true);
                            }
                        }

                        MapSaver.Instance.ModelConfigs.Remove(SelectItem.n);
                    }

                    
                    break;
            }

            
            editBar.gameObject.SetActive(false);
            
            Destroy(SelectItem.gameObject);
        }
        
        
        public void Create()
        {
            CloseEditBar();
            if (SelectItem == null) return;
            switch (SelectItem.type)
            {
                case ObjectType.Decal:
                    ExposeToEditor decal =
                        AddObjectMenu.CreateDecalExposeToEditor(MapSaver.Instance.externalDecalMaterials[decalExternalMat].Item2);
                    
                    decal.transform.SetParent(_me.EditedObject.transform);
                        
                    
                    decal.gameObject.AddComponent<MapSaveObject>().Init(ObjectType.Decal,SelectItem.n);
            

                    _me.Undo.BeginRecord();
                    if (_selectionComponent == null || _selectionComponent.CanSelect)
                    {
                        _me.Selection.ActiveGameObject = decal.gameObject;
                    }

                    _me.Undo.RegisterCreatedObjects(new[] { decal });
                    _me.Undo.EndRecord();
                    break;
                case ObjectType.AudioSource:
                    ExposeToEditor audioSource =
                        CreateAudioSourceExposeToEditor(MapSaver.Instance.externalAudioClips[audioExternalIndex].Item2);
                    
                    audioSource.transform.SetParent(_me.EditedObject.transform);

                    audioSource.gameObject.AddComponent<MapSaveObject>().Init(ObjectType.AudioSource,SelectItem.n);
                    
                    _me.Undo.BeginRecord();
                    
                    if (_selectionComponent == null || _selectionComponent.CanSelect)
                    {
                        _me.Selection.ActiveGameObject = audioSource.gameObject;
                    }

                    _me.Undo.RegisterCreatedObjects(new[] { audioSource });
                    _me.Undo.EndRecord();
                    break;
                case ObjectType.Decoration:
                    var obj = (GameObject)AssetBundleManager.Instance.GetAssetObject(SelectItem.n);
                    Transform t = obj.transform;
                    if (obj == null) break;
                    ExposeToEditor model =
                        AddObjectMenu.CreateDecorationExposeToEditor(obj,_me.EditedObject.transform,t.position,t.rotation,t.localScale, 0, true,true);

                    model.gameObject.AddComponent<MapSaveObject>().Init(ObjectType.Decoration,SelectItem.n);
            

                    _me.Undo.BeginRecord();
                    if (_selectionComponent == null || _selectionComponent.CanSelect)
                    {
                        _me.Selection.ActiveGameObject = model.gameObject;
                    }

                    _me.Undo.RegisterCreatedObjects(new[] { model });
                    _me.Undo.EndRecord();
                    break;
            }
        }
        public void SetSelectItem(ObjectItemUI item)
        {
            CloseEditBar();
            SelectItem = item;

            switch (item.type)
            {
                case ObjectType.Decal:
                    decalExternalMat = SelectItem.decalMaterialIndex;
                    break;
                case ObjectType.AudioSource:
                    audioExternalIndex = SelectItem.audioClipIndex;
                    break;
            }

            AddObjectMenu.Instance.SelectedItem = null;

            addBtn.interactable = true;
        }
        #region Decal

        #region Add

        [SerializeField] private GameObject addDecalPanel;

        [SerializeField] private TMP_InputField decalNameInput;
        private bool _adding = false;
        void TryToAddDecal()
        {
            if (_adding) return;
            _adding = true;
            CloseEditBar();
            string path = FileIOUtil.OpenFileDialog(FileType.Texture);

            if (string.IsNullOrEmpty(path)) return;
            StartCoroutine(LoadImage(path,true));
            _adding = false;
        }
        void TryToCreateDecal()
        {
            addDecalPanel.SetActive(true);
            
            decalNameInput.SetTextWithoutNotify("");
            EventSystem.current.SetSelectedGameObject(decalNameInput.gameObject);
        }
        
        [SerializeField] private Transform content;
        [SerializeField] private ObjectItemUI prefab;

        private List<Texture2D> _texture2Ds = new List<Texture2D>();
        
        public void CreateDecal()
        {
            if (string.IsNullOrEmpty(decalNameInput.text)) return;

            Material newDecalMaterial = new Material(MapSaver.Instance.decalDummyMat)
            {
                name = decalNameInput.text
            };
            Texture2D texture2D = _texture2Ds[_loadTexture];

            newDecalMaterial.SetTexture(MapSaver.BaseMap,texture2D);

            MapSaver.Instance.externalDecalMaterials.Add(new Tuple<string, Material>(_path,newDecalMaterial));

            ObjectItemUI itemUI = Instantiate(prefab, content);
            
            itemUI.Init(ObjectType.Decal,decalNameInput.text,texture2D,MapSaver.Instance.GetDecalMaterialIndex(newDecalMaterial,true),rootCanvas,maskRect,false);
            
            addDecalPanel.SetActive(false);
            
            addNewCustomDecalBtn.transform.SetAsLastSibling();
            itemUI.Click();
        }
        private int decalExternalMat=-1;
        private int audioExternalIndex = -1;
        private string _path = String.Empty;
        
        #endregion

        #region Edit

        void EditDecal()
        {
            CloseEditBar();
            string path = FileIOUtil.OpenFileDialog(FileType.Texture);

            StartCoroutine(LoadImage(path,false));
        }
        
        #endregion

        IEnumerator LoadImage(string path,bool create)
        {
            bool complete = false;
            byte[] data=null;
            var customThread = new Thread(() =>
            {
                data = SaveSystem.ReadByteFromFile(path);
                
                complete = true;
            });
            customThread.Start();

            while (!complete)
            {
                yield return null;
            }

            _path = path;
            customThread.Abort();
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGB24, false);
            texture2D.LoadImage(data);
            
            _texture2Ds.Add(texture2D);

            _loadTexture = _texture2Ds.Count - 1;
            
            if(create)
                TryToCreateDecal();
            else
            {
                MapSaver.Instance.externalDecalMaterials[decalExternalMat] = new Tuple<string, Material>(path,
                    MapSaver.Instance.externalDecalMaterials[decalExternalMat].Item2);
                MapSaver.Instance.externalDecalMaterials[decalExternalMat].Item2.SetTexture(MapSaver.BaseMap,texture2D);
                
                SelectItem.SetTexture(texture2D);
            }
        }
        
        #endregion

        #region AudioSource

        [Header("Audio Source")]
        [SerializeField] private GameObject addAudioPanel;
        [SerializeField] private Button addAudioBtn;
        [SerializeField] private TMP_InputField audioNameInput;

        private List<AudioClip> _clips = new();

        #region Add

        private int _loadAudioIndex = -1;
        
        void TryToAddAudioSource()
        {
            if (_adding) return;
            _adding = true;
            CloseEditBar();
            
            string path = FileIOUtil.OpenFileDialog(FileType.Audio);

            StartCoroutine(LoadAudio(path,true));
            _adding = false;
        }

        IEnumerator LoadAudio(string path,bool create)
        {
            if (string.IsNullOrEmpty(path))
            {
                _loadAudioPath=String.Empty;
                yield break;
            }
            _loadAudioPath = path;
            var audioType = path.EndsWith(".wav") ? AudioType.WAV : AudioType.MPEG;
            using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(path, audioType))
            {
                // download the audio data using DownloadHandlerAudioClip
                DownloadHandlerAudioClip handler = new DownloadHandlerAudioClip(webRequest.url, audioType);
                webRequest.downloadHandler = handler;
                
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                    yield break;

                if (create)
                {
                    addAudioPanel.SetActive(true);

                    _loadAudioIndex = _clips.Count;
                    _clips.Add(handler.audioClip);
                
                    audioNameInput.SetTextWithoutNotify(""); 
                    
                    EventSystem.current.SetSelectedGameObject(audioNameInput.gameObject);
                }
            }
        }

        
        public void AbandonToCreate()
        {
            if (_loadAudioIndex < _clips.Count && _loadAudioIndex >=0)
            {
                _clips.RemoveAt(_loadAudioIndex);
                _loadAudioIndex = -1;
                _loadAudioPath= String.Empty;
            }
        }
        
        string _loadAudioPath = String.Empty;

        [SerializeField] private Texture2D audioTexture;
        [SerializeField] private Transform audioContent;

        
        public void CreateAudio()
        {
            string audioName = audioNameInput.text;
            if (string.IsNullOrEmpty(audioName) ||
                _clips.Count < _loadAudioIndex) return;

            AudioClip clip = _clips[_loadAudioIndex];

            clip.name = audioName;

            int index = MapSaver.Instance.externalAudioClips.Count;
            
            MapSaver.Instance.externalAudioClips.Add(new Tuple<string, AudioClip>(_loadAudioPath,clip));
            
            ObjectItemUI itemUI = Instantiate(prefab, audioContent);
            
            itemUI.Init(ObjectType.AudioSource,audioName,audioTexture,index,rootCanvas,maskRect,false);
            
            addAudioPanel.SetActive(false);
            
            addAudioBtn.transform.SetAsLastSibling();
            
            itemUI.Click();
        }
        #endregion

        #endregion

        public static ExposeToEditor CreateAudioSourceExposeToEditor(AudioClip clip)
        {
            ExposeToEditor exposeToEditor = CreateAudioSource(clip).gameObject.AddComponent<ExposeToEditor>();
            
            exposeToEditor.transform.position = AddObjectMenu.GetPosition();

            return exposeToEditor;
        }
        
        public static AudioSource CreateAudioSource(AudioClip clip)
        {
            GameObject obj = new GameObject();

            AudioSource source = obj.AddComponent<AudioSource>();

            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.playOnAwake = false;

            if (AudioManager.Instance)
            {
                source.outputAudioMixerGroup = AudioManager.Instance.soundEffect;
            }

            return source;
        }
    }
    
    
}
