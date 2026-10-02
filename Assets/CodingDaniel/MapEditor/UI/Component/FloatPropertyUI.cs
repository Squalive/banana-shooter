using TMPro;
using UnityEngine;

namespace CodingDaniel.MapEditor.UI.Component
{
    public class FloatPropertyUI : BaseProperty
    {
        [SerializeField] private TMP_InputField inputField;

        public override void UpdateValue(object o)
        {
            inputField.SetTextWithoutNotify(o.ToString());
        }

        public override void Init(string n, object o,bool c)
        {
            base.Init(n, o,c);
            inputField.onEndEdit.AddListener(SetValue);

            inputField.interactable = c;
        }
    }
}
