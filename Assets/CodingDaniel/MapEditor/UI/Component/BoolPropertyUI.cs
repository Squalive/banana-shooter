using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI.Component
{
    public class BoolPropertyUI : BaseProperty
    {
        [SerializeField] private Toggle toggle;

        public override void UpdateValue(object o)
        {
            toggle.SetIsOnWithoutNotify((bool)o);
        }

        public override void Init(string n, object o,bool c)
        {
            base.Init(n, o,c);
            toggle.onValueChanged.AddListener(delegate(bool arg0) { SetValue(arg0); });
            toggle.interactable = c;
        }
    }
}
