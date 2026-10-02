
using System.Collections.Generic;

using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.UI;
using Manager;
using Movement;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace CodingDaniel.MapEditor.PlayMode
{
    public class PlayModeManager : MonoBehaviour
    {
        public static PlayModeManager Instance { private set; get; }

        private void Awake()
        {
            Instance = this;
        }

        public GameObject player;
        public Transform playerTransform;

        
        public void ChangeToPlayMode(bool flag)
        {
            if (MEBase.Instance.IsPlayMode) return;
            
            MEBase.Instance.Undo.Purge();
            
            if (flag)
            {
                MapSaver.Instance.SetMapData(TabHolder.Instance.mapNameInput.text,TabHolder.Instance.mapDescriptionInput.text,false,false);
            }
            MEBase.Instance.IsPlayMode = true;

            SpriteGizmoManager.Instance.enabled = false;

            MEBase.Instance.EditedObject.SetActive(false);
            MEBase.Instance.PlayModeObject.SetActive(true);
            
            CameraMovement.Instance.gameObject.SetActive(false);
            
            MESelectionComponent.Instance.TryToClearSelection();
            
            MapSaver.Instance.LoadPlayModeMap();
            
            TabHolder.Instance.editorUI.SetActive(false);

            MyVector3 spawnPos = new MyVector3(0,0,0);
            List<MyVector3> list = new List<MyVector3>(MapSaver.CurrentMap.spawnPos);
            if (list.Count > 0)
            {
                spawnPos = list[Random.Range(0, list.Count)];
            }
            player.SetActive(true);
            playerTransform.position = spawnPos.ToVector3();
            
            SpectateMovement.Instance.StopSpect(true);
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        
        public void BackToEditMode()
        {
            if (!MEBase.Instance.IsPlayMode) return;
            MEBase.Instance.Undo.Restore();
            ListenerManager.Instance.SetCamera(MEBase.Instance.Camera.transform);
            MEBase.Instance.IsPlayMode = false;
            SpriteGizmoManager.Instance.enabled = true;
        
            MEBase.Instance.EditedObject.SetActive(true);
            MEBase.Instance.PlayModeObject.SetActive(false);
            
            CameraMovement.Instance.gameObject.SetActive(true);
            
            MESelectionComponent.Instance.TryToClearSelection();
            
            // MapSaver.Instance.LoadEditorMap();
            
            TabHolder.Instance.editorUI.SetActive(true);

            SpectateMovement.Instance.StopSpect(false);
            
            player.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
