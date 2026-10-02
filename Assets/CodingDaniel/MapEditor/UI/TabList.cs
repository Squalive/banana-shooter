using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CodingDaniel.MapEditor.UI
{
    
    public class TabList : MonoBehaviour,IPointerDownHandler
    {
        public static TabList Instance { private set; get; }

        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] public Transform tab;
        [SerializeField] public DropList dropListPrefab;

        public bool isSelected = false;
        public void SpawnList(RectTransform listItem,List<DropListItem> items,bool offsetYEnable=true,bool offsetXEnable=false)
        {
            DropList dropList = Instantiate(dropListPrefab, tab);
            
            dropLists.Add(dropList);

            dropList.SetTabList(listItem, items);
        }

        [SerializeField] private List<TabItem> tabItems = new List<TabItem>();

        public readonly List<DropList> dropLists = new List<DropList>();
        public void OnPointerDown(PointerEventData eventData)
        {
            ClearSelection();
            DestroyDropList();
        }

        public void ClearSelection()
        {
            isSelected = false;
            foreach (var tabItem in tabItems)
            {
                tabItem.ClearSelect();
            }
        }
        public void DestroyDropList()
        {
            for (int i = 0; i < dropLists.Count; i++)
            {
                Destroy(dropLists[i].gameObject);
            }
            dropLists.Clear();
        }
    }
}
