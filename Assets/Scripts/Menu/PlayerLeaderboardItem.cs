using System;
using System.Collections.Generic;
using Level;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class PlayerLeaderboardItem : MonoBehaviour
    {
        [SerializeField]public TextMeshProUGUI text, killCountTextMesh;

        [SerializeField] private LocalizeStringEvent killCountText;

        [SerializeField] private RawImage levelImage;
        [SerializeField] private Image bg;
        [SerializeField] private TextMeshProUGUI levelText;

        public LobbyDataManager.LobbyData data;

        public bool eliminated = false;

        [SerializeField] private ItemUIColor defaultItemColor, deActiveItemColor;

        public void Initialize(LobbyDataManager.LobbyData d,uint index, bool eliminate)
        {
            data = d;
            eliminated = eliminate;
            killCountText.StringReference.Arguments = new List<object>() {d.kills};
            killCountText.RefreshString();
            
            text.SetText($"{index}. {data.playerName}");
            
            int level = 0;
            float total = 0;
            for (int i = 1; i < 99; i++)
            {
                total += Mathf.Floor(i + 300 * Mathf.Pow(2, i / 7f));
                if (total > data.exp)
                {
                    level = i - 1;
                    break;
                }
            }

            Color color = LevelManager.Instance.GetColor(level);

            levelImage.color = color;
            levelText.color = color;
            
            levelText.SetText(level.ToString());
            
            SetColor();
        }

        void SetColor()
        {
            levelImage.gameObject.SetActive(!eliminated);
            if (eliminated)
            {
                bg.color = deActiveItemColor.bgColor;
                text.color = deActiveItemColor.textColor;
                killCountTextMesh.color = deActiveItemColor.killCountColor;
            }
            else
            {
                bg.color = defaultItemColor.bgColor;
                text.color = defaultItemColor.textColor;
                killCountTextMesh.color = defaultItemColor.killCountColor;
            }
        }
        
        private void OnValidate()
        {
            if (Application.isEditor) SetColor();
        }

        [Serializable]
        public class ItemUIColor
        {
            [SerializeField] public Color bgColor, textColor, killCountColor;
        }
    }
}
