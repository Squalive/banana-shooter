
using System.Collections;
using Audio;

using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.PlayMode;
using CodingDaniel.MapEditor.Utils;
using Manager;
using UnityEngine;

namespace CodingDaniel.MapEditor.UI
{
    public class EditorMenu : MonoBehaviour
    {
        public static EditorMenu Instance { private set; get; }

        private void Awake()
        {
            Instance = this;

            ListenerManager.Instance.SetCamera( MEBase.Instance.Camera.transform);
        }

        public bool Menu
        {
            private set;
            get;
        } = false;
        [SerializeField] private GameObject editorMenu, normalMenu,savingMenu;

        public GameObject SavingMenu
        {
            get => savingMenu;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OpenOrCloseMenu();
            }
        }

        
        public void OpenOrCloseMenu()
        {
            Menu = !Menu;
            
            editorMenu.SetActive(!Menu && !MEBase.Instance.IsPlayMode);
            normalMenu.SetActive(Menu);

            if (Menu)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                if (MEBase.Instance.IsPlayMode)
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }

        
        public void ButtonSound()
        {
            AudioManager.Instance.PlayButton();
        }

        [SerializeField] private GameObject exitPanel;
        
        public void TryToQuit()
        {
            MESelectionComponent.Instance.TryToClearSelection();
            if (MEBase.Instance.HasChanged)
            {
                PlayModeManager.Instance.BackToEditMode();
                exitPanel.SetActive(true);
                ButtonSound();
                return;
            }
            QuitToMenu();
        }

        
        public void SaveToQuit()
        {
            StartCoroutine(StartToSaveToQuit());
        }

        IEnumerator StartToSaveToQuit()
        {
            TabHolder.Instance.Save();
            yield return null;
            yield return null;
            while (MapSaver.Instance.IsSaving)
            {
                yield return null;
            }
            
            QuitToMenu();
        }
        
        
        public void QuitToMenu()
        {
            CursorHelper.Instance.SetCursor(null, Vector2.one * 0.5f, CursorMode.Auto);
            ButtonSound();
            MapSaver.CurrentMap = null;
            MapSaver.Instance.Cleanup();
            LoadingManager.Instance.Menu();
            
            if (GameManager.Instance)
            {
                QualitySettings.SetQualityLevel(GameManager.Instance.setting.quality);
            }
        }
    }
}
