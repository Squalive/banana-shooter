using System;
using System.Collections;
using Multiplayer;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Menu
{
    public class GameModeTip : MonoBehaviour
    {
        public static GameModeTip Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            if (!NetworkManager.Instance.Client.IsConnected) return;

            
            
        }

        public void SetTipText()
        {
            if (NetworkManager.ClientGameMode != GameMode.SpecialGameMode&&
                NetworkManager.ClientGameMode != GameMode.SpecialNormalGameMode)
            {
                
                StartCoroutine(SetTip($"gm_{NetworkManager.ClientGameMode.ToString().ToLower()}_desc"));
            }
            else
            {
                StartCoroutine(ShootingRange.Instance
                    ? SetTip("gm_shootingrange_desc")
                    : SetTip($"gm_{NetworkManager.ClientServerType.ToString().ToLower()}_desc"));
            }
        }

        private bool disable = false;

        [SerializeField] private RectTransform obj;
        [SerializeField] private CanvasGroup canvasGroup;

        private float _desiredSize = 0, _desiredAlpha = 0;

        [SerializeField] private LocalizeStringEvent text;


        IEnumerator SetTip(string entry)
        {
            yield return new WaitForSeconds(1f);
            
            text.SetEntry(entry);

            _desiredAlpha = 1f;
            _desiredSize = 447;

            yield return new WaitForSeconds(10f);

            _desiredSize = 0;
            _desiredAlpha = 0;


            yield return new WaitForSeconds(10f);
            disable = true;
        }

        private void Update()
        {
            if (disable) return;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, _desiredAlpha, Time.deltaTime * 5f);
            obj.sizeDelta = new Vector2(Mathf.Lerp(obj.sizeDelta.x,_desiredSize,Time.deltaTime*5f),obj.sizeDelta.y);
        }
    }
}
