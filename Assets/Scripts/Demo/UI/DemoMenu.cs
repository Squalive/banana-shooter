
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Audio;

using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Demo.UI
{
    public class DemoMenu : MonoBehaviour
    {
        private DemoUIObj[] _objs = Array.Empty<DemoUIObj>();
        
        [SerializeField] private Transform demoContentTrans;

        [SerializeField] private DemoUIObj prefab;

        [SerializeField] private GameObject loadingPanel;

        [SerializeField] private Button folderButton;

        private int _selectIndex = -1;

        private int _sortingType = 1;

        private bool _descent=true;

        private void Start()
        {
            folderButton.onClick.AddListener(DemoMenuUtils.OpenDemoFolder);
            Display();
        }

        private void OnEnable()
        {
            DemoManager.OnDemoLoad += loadingPanel.SetActive;
        }

        private void OnDisable()
        {
            DemoManager.OnDemoLoad -= loadingPanel.SetActive;
        }

        void Display()
        {
            FileInfo[] fileInfos = DemoManager.DemoFiles;
            _objs = new DemoUIObj[fileInfos.Length];

            for (int i = 0; i < fileInfos.Length; i++)
            {
                FileInfo info = fileInfos[i];

                DemoUIObj obj = Instantiate(prefab, demoContentTrans);
                
                obj.Initialize(info, i , Select);

                _objs[i] = obj;
            }
            
            SortObjs();
        }

        void DestroyEverything()
        {
            _selectIndex = -1;
            for (int i = 0; i < _objs.Length; i++)
            {
                Destroy(_objs[i].gameObject);
            }
        }

        void Select(int i)
        {
            if (_selectIndex == i) return;
            if (_selectIndex != -1 && _selectIndex < _objs.Length)
            {
                _objs[_selectIndex].DeSelect();
            }
            _selectIndex = i;
        }

        void SortObjs()
        {
            DemoUIObj[] sortedObjs = _objs;
            switch (_sortingType)
            {
                case 0:
                    sortedObjs = _descent ? _objs.OrderByDescending(n => n.FileInfo.Name).ToArray() : _objs.OrderBy(n => n.FileInfo.Name).ToArray();
                    break;
                case 1:
                    sortedObjs = _descent ? _objs.OrderByDescending(n => n.FileInfo.LastWriteTime).ToArray() : _objs.OrderBy(n => n.FileInfo.LastWriteTime).ToArray();
                    break;
                case 2:
                    sortedObjs = _descent ? _objs.OrderByDescending(n => n.FileInfo.Length).ToArray() : _objs.OrderBy(n => n.FileInfo.Length).ToArray();
                    break;
            }
            
            for (int i = 0; i < sortedObjs.Length; i++)
            {
                sortedObjs[i].transform.SetAsLastSibling();
            }
        }

        
        public void SetSortingType(int type)
        {
            if (_sortingType == type)
            {
                _descent = !_descent;
            }
            else
            {
                _descent = false;
            }
            _sortingType = type;

            SortObjs();
        }

        
        public void Play()
        {
            if (_selectIndex != -1)
            {
                DemoManager.Instance.PlayDemo(_selectIndex);
            }
        }

        public void Refresh()
        {
            DestroyEverything();
            DemoManager.Instance.RefreshSaveFile();
            Display();
        }

        
        public void Delete()
        {
            if (_selectIndex != -1)
            {
                DemoManager.Instance.RemoveDemo(_selectIndex);
                DestroyEverything();
                Display();
            }
        }
    }

    public static class DemoMenuUtils
    {
        public static void OpenDemoFolder()
        {
            string path = DemoManager.SavePath;
            
            Application.OpenURL("file://"+path);
        }
    }
}
