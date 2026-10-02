using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace CodingDaniel.MapEditor.UI.Component
{
    public interface IBaseProperty
    {
        void SetValue(object o);
        void UpdateValue(object o);
    }
    public class BaseProperty : MonoBehaviour,IBaseProperty
    {
        public Action<object> onValueSet;
        //Use Localization 
        [SerializeField] private LocalizeStringEvent text;

        private bool canEdit = true;
        public virtual void SetValue(object o)
        {
            onValueSet?.Invoke(o);
        }

        public virtual void UpdateValue(object o)
        {
            
        }

        private void OnDestroy()
        {
            onValueSet = null;
        }

        public virtual void Init(string n, object o,bool canEdit)
        {
            text.SetEntry(n);
            UpdateValue(o);
            this.canEdit = canEdit;
            
            
        }
    }
}
