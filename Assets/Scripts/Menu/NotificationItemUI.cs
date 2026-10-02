using System;

using UnityEngine;
using UnityEngine.Localization.Components;

namespace Menu
{
    public class NotificationItemUI : MonoBehaviour
    {
        [SerializeField] private LocalizeStringEvent title, info;

        [SerializeField] private Transform child;

        private void Update()
        {
            child.localPosition = Vector3.Lerp(child.localPosition,Vector3.zero, Time.deltaTime*10f);
            
        }

        
        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetValue(string titleKey, string infoKey)
        {
            title.SetEntry(titleKey);
            
            info.SetEntry(infoKey);
            
            Destroy(gameObject,3f);
        }
    }
}
