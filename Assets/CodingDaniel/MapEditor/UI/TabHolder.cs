
using System;
using System.Collections;
using System.Collections.Generic;

using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.UI.AddObject;
using CodingDaniel.MapEditor.UI.Component;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class TabHolder : MonoBehaviour
    {
        public static TabHolder Instance { private set; get; }

        private MaterialPaletteUI _materialPaletteUI;
        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] private CanvasGroup canvasGroup;

        public GameObject editorUI;

        private float _desiredAlpha = 0f;

        private float offset = 0;

        private void Start()
        {
            _materialPaletteUI=MaterialPaletteUI.Instance;
            Invoke(nameof(ResetCanvas),0.1f);
            InvokeRepeating(nameof(RefreshOffset),0,5f);
            
            SteamFriends.SetRichPresence("steam_display", "#MapEditor");
        }

        void RefreshOffset()
        {
            offset = Screen.height * 8f / 9f;
            
        }
        void ResetCanvas()
        {
            canvasGroup.gameObject.SetActive(false);
            canvasGroup.gameObject.SetActive(true);
        }
        private void Update()
        {
            float y = Input.mousePosition.y;


            if (MEBase.Instance.Tools.IsViewing)
            {
                _desiredAlpha = 0f;

            }
            else
            {
                _desiredAlpha = (y > offset || TabList.Instance.isSelected) && !DragWindow.IsDragging ? 1f : 0f;

            }
           
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, _desiredAlpha, Time.deltaTime * 15f);
        }

        public bool UsingUI()
        {
            return Math.Abs(_desiredAlpha - 1f) < 0.1f || _materialPaletteUI.DisplayingUI|| TabList.Instance.isSelected || DragWindow.IsDragging || ComponentMenu.Instance.IsHoverOn() || EditorMenu.Instance.Menu ||
                   PublishMenu.Instance.IsUsingUI() || UVEditorUI.Instance.hoverOnUICheck.Hover;
        }

        
        public void SelectAll()
        {
            IMESelectionComponent selectionComponent = MESelectionComponent.Instance;
            if (selectionComponent != null)
            {
                selectionComponent.CanSelect = true;
                selectionComponent.CanSelectAll = true;
            }
            MEInput.Instance.SelectAll();
        }

        

        [SerializeField] public TMP_InputField mapNameInput,mapDescriptionInput;
        [SerializeField] private GameObject saveWindow;
        [SerializeField] private RawImage img;
        
        
        public void Save()
        {
            StartCoroutine(SaveMap(false));

        }

        
        public void SaveAs()
        {
            StartCoroutine(SaveMap(true));
        }

        [SerializeField] private CanvasGroup mainCanvas;

        [SerializeField] private GameObject settingMenu;

        IEnumerator SaveMap(bool flag)
        {
            mainCanvas.alpha = 0f;

            SpriteGizmoManager.Instance.enabled = false;
            MapBoundVisual.Instance.enabled = false;
            MEBase.Instance.Selection.ActiveGameObject = null;
            SceneGridVisual.Instance.enabled = false;
            
            //TODO: Disable all player spawn point to prevent it got screenshot in preview image
            List<MapSaveObject> disabledObjs = new List<MapSaveObject>();
            for (int i = 0; i < MapSaver.Instance.ObjectsNeedToSave.Count; i++)
            {
                if (MapSaver.Instance.ObjectsNeedToSave[i].type == ObjectType.PlayerSpawnPoint)
                {
                    disabledObjs.Add(MapSaver.Instance.ObjectsNeedToSave[i]);
                    MapSaver.Instance.ObjectsNeedToSave[i].gameObject.SetActive(false);
                }
            }
            
            yield return null;

            int width = 1920;
            int height = 1080;
            Rect rect = new Rect(0, 0,width ,height );
            
            // Create a new RenderTexture with the desired resolution
            RenderTexture rt = new RenderTexture(width, height, 24);

            // Set the active RenderTexture
            RenderTexture.active = rt;

            // Render the scene to the RenderTexture
            Camera currentCamera = MEBase.Instance.Camera;
            currentCamera.targetTexture = rt;
            currentCamera.Render();

            texture2D = new Texture2D(width,height, TextureFormat.RGB24, false);
            
            texture2D.ReadPixels(rect,0,0);
            texture2D.Apply();

            mainCanvas.alpha = 1f;
            
            SpriteGizmoManager.Instance.enabled = true;
            MapBoundVisual.Instance.enabled = settingMenu.activeSelf;
            SceneGridVisual.Instance.enabled = true;

            //TODO: ReEnable it
            foreach (var mapSaveObject in disabledObjs)
            {
                mapSaveObject.gameObject.SetActive(true);
            }
            
            if (flag||!MapSaver.Instance.Save(mapNameInput.text,mapDescriptionInput.text,texture2D,flag))
            {
                OpenSaveWindow();
            }
            // Reset the active RenderTexture
            RenderTexture.active = null;
            currentCamera.targetTexture = null;
            Destroy(rt);
        }

        private Texture2D texture2D;

        void OpenSaveWindow()
        {
            img.texture = texture2D;
            
            saveWindow.SetActive(true);
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
