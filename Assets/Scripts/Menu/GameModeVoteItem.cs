using Manager;
using Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class GameModeVoteItem : MonoBehaviour
    {
        [SerializeField] public Toggle toggle;
        
        [SerializeField] private LocalizeStringEvent text;

        [SerializeField] private TextMeshProUGUI amountText;

        [SerializeField] private RawImage icon;

        [SerializeField] private CanvasGroup voteAnimation;
        
        public void Initialize(GameMode gameMode,ToggleGroup group)
        {
            text.SetEntry(gameMode.ToString());
            
            amountText.SetText("0");

            icon.texture = PrefabManager.Instance.GetTexture2D($"gm_{gameMode.ToString().ToLower()}");

            toggle.group = group;
        }
        
        public void SetCount(ushort count)
        {
            amountText.SetText(count.ToString());
        }
        
        public void Animate()
        {
            voteAnimation.alpha = 1f;
        }

        private void Update()
        {
            voteAnimation.alpha = Mathf.Lerp(voteAnimation.alpha, 0f, Time.deltaTime * 10f);
        }
    }
}
