using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class SourceItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RawImage img;

        public object source;

        public void Init(string name, Texture texture,object ob)
        {
            text.SetText(name);
            img.texture = texture;
            source = ob;
            
        }

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(Apply);
        }

        void Apply()
        {
            SourceUI.Instance.Apply(this);
        }
        
    }
}
