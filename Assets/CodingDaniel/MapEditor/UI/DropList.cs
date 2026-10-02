using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CodingDaniel.MapEditor.UI
{
    public enum DropListItemType
    {
        Button,
        HasChild
    }

    [Serializable]
    public class DropListItem
    {
        public UnityEvent onClicked;
        public DropListItemType type = DropListItemType.Button;
        public string name;
        public List<DropListItem> items = new List<DropListItem>();
    }
    public class DropList : MonoBehaviour
    {
        private const float canvasX = 1920, canvasY = 1080;
        
        [SerializeField] private Transform content;

        [SerializeField] private TabListItem btnPrefab,hasChildPrefab;
        public void SetTabList(RectTransform tabItem, List<DropListItem> items,bool offsetYEnable=true,bool offsetXEnable=false)
        {
            var position = tabItem.position;
            float offsetY = offsetYEnable ? tabItem.sizeDelta.y : 0f;
            float offsetX = offsetXEnable ? tabItem.sizeDelta.x : 0f;

            offsetY *= Screen.height / canvasY;
            offsetX *= Screen.width / canvasX;
            transform.position = new Vector2(position.x + offsetX, position.y -offsetY);
            // Debug.Log(position + " " +transform.position);

            foreach (var item in items)
            {
                if (item.type == DropListItemType.Button)
                {
                    TabListItem tabListItem = Instantiate(btnPrefab, content);

                    tabListItem.Init(item,this);

                    tabListItems.Add(tabListItem);
                }
                else
                {
                    TabListItem tabListItem = Instantiate(hasChildPrefab, content);

                    tabListItem.Init(item,this);

                    tabListItems.Add(tabListItem);
                }
            }
        }

        public readonly List<TabListItem> tabListItems = new List<TabListItem>();
        public readonly List<DropList> dropLists = new List<DropList>();
        public void SpawnList(RectTransform listItem,List<DropListItem> items,bool offsetYEnable=true,bool offsetXEnable=false)
        {
            DropList dropList = Instantiate(TabList.Instance.dropListPrefab, TabList.Instance.tab);
            
            dropLists.Add(dropList);
            TabList.Instance.dropLists.Add(dropList);

            dropList.SetTabList(listItem, items,offsetYEnable,offsetXEnable);
        }
        public void ClearSelection()
        {
            foreach (var tabListItem in tabListItems)
            {
                tabListItem.isSelect = false;
            }
        }

        public void DestroyDropList()
        {
            for (int i = 0; i < dropLists.Count; i++)
            {
                TabList.Instance.dropLists.Remove(dropLists[i]);
                Destroy(dropLists[i].gameObject);
            }
            dropLists.Clear();
            
        }
    }
}
