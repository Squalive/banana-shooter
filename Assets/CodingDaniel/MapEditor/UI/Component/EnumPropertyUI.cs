using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CodingDaniel.MapEditor.UI.Component
{
    public class EnumPropertyUI : BaseProperty
    {
        [SerializeField] private TMP_Dropdown dropdown;

        public override void UpdateValue(object o)
        {
            dropdown.SetValueWithoutNotify((int)o);
        }

        public void SetDropdown(List<string> str)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(str);
        }
        public override void Init(string n, object o,bool c)
        {
            base.Init(n, o,c);
            dropdown.onValueChanged.AddListener(delegate(int arg0) { SetValue(arg0); });

            dropdown.interactable = c;
        }
    }
}
