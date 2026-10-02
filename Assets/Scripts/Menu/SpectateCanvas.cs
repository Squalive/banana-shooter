using System;
using Movement;
using Multiplayer.Entity.Client;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class SpectateCanvas : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        [SerializeField] private TextMeshProUGUI nameText;

        [SerializeField] private TextMeshProUGUI kdText;

        [SerializeField] private RawImage profileImage;

        private void OnEnable()
        {
            SpectateMovement.Instance.OnFirstPersonSpectatePlayer += SetValues;
            SpectateMovement.Instance.OnStopFirstPersonSpectate += DeInitialize;
        }

        private void OnDisable()
        {
            SpectateMovement.Instance.OnFirstPersonSpectatePlayer -= SetValues;
            SpectateMovement.Instance.OnStopFirstPersonSpectate -= DeInitialize;
        }

        void SetValues(PlayerState playerState)
        {
            panel.SetActive(true);
            
            nameText.SetText(playerState.Username);
            
            profileImage.texture = playerState.AvatarImage;

            if (playerState.TryGetComponent(out ClientPlayer clientPlayer))
            {
                SetKd(clientPlayer.Kills,clientPlayer.Deaths);
            }
            else
            {
                kdText.SetText(String.Empty);
            }
        }

        void DeInitialize()
        {
            panel.SetActive(false);
        }

        public void SetKd(int kill,int death)
        {
            kdText.SetText($"{kill}K / {death}D");
        }
    }
}
