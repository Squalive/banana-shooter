using System;
using System.IO;
using Audio;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Demo.UI
{
    public class DemoUIObj : MonoBehaviour , IPointerEnterHandler , IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private RawImage backGround;

        [SerializeField] private Color defaultColor, highlightColor, selectedColor;

        [SerializeField] private TextMeshProUGUI nameText, dateText, lengthText;

        private int _index;

        private Action<int> _onClick;

        private bool _selected = false;

        public FileInfo FileInfo;

        public void Initialize(FileInfo fileInfo,int index, Action<int> onClick)
        {
            FileInfo = fileInfo;
            _index = index;
            _onClick = onClick;
            nameText.SetText(fileInfo.Name);
            dateText.SetText(fileInfo.LastWriteTime.ToString("g"));
            lengthText.SetText(GameManager.GetObjectSize(fileInfo.Length));
        }

        private void Start()
        {
            backGround.color = defaultColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(!_selected)
                backGround.color = highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(!_selected)
                backGround.color = defaultColor;
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            _selected = true;
            AudioManager.Instance.PlayButton();
            _onClick?.Invoke(_index);
            Select();
        }

        public void DeSelect()
        {
            _selected = false;
            backGround.color = defaultColor;
        }

        void Select()
        {
            backGround.color = selectedColor;
        }
    }
}