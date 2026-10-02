using System;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Cosmetic
{
    public class DetailTypeItemUI : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;

        [SerializeField] private LocalizeStringEvent text;

        private void OnDestroy()
        {
            toggle.onValueChanged.RemoveAllListeners();
        }

        public void Initialize(string key,int i,ToggleGroup typeGroup)
        {
            text.SetEntry(key);
            toggle.onValueChanged.AddListener(delegate { CosmeticMenu.Instance.SetSubType(i); });
            toggle.group = typeGroup;
        }
    }
}
