
using System;
using CodingDaniel.MapEditor.UI.AddObject;
using UnityEngine;

namespace CodingDaniel.MapEditor.MEEditor.MESave
{
    [Serializable]
    public class MapSaveObject : MonoBehaviour
    {
        public ObjectItem objectItem;
        public ObjectType type=ObjectType.None;
        public string n;
        public void Init(ObjectItem item)
        {
            objectItem = item;
            type = item.type;
        }
        public void Init(ObjectType t,string na)
        {
            type = t;
            n = na;
        }
        private void OnEnable()
        {
            MapSaver.Instance.ObjectsNeedToSave.Add(this);
        }

        private void OnDisable()
        {
            MapSaver.Instance.ObjectsNeedToSave.Remove(this);
        }
    }
}
