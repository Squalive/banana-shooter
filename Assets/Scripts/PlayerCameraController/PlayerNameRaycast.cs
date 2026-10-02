using System.Collections.Generic;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Client;
using TMPro;
using UnityEngine;

namespace PlayerCameraController
{
    public class PlayerNameRaycast : MonoBehaviour
    {
        public static PlayerNameRaycast Instance { private set; get; }
        
        [SerializeField] public PlayerState localPlayer;
        
        [SerializeField] private LayerMask groundPlayerLayer,playerLayer;
        
        // The text to show the player name
        private TextMeshProUGUI _hitPlayerUI;

        //Store the local transform
        private Transform _transform;

        private void Awake()
        {
            Instance = this;
            _transform = transform;
        }

        public void SetValue(TextMeshProUGUI hitPlayer)
        {
            _hitPlayerUI = hitPlayer;
        }

        private List<PlayerState> CurrentCastedPlayers { get; } = new();
        private List<PlayerState> CastedPlayers { get; } = new();
        private RaycastHit[] _castedHit = new RaycastHit[10];
        
        private void LateUpdate()
        {
            if (!GameUIManager.Instance) return;
            if (Physics.Raycast(_transform.position, _transform.forward, out var hit, 100f, groundPlayerLayer))
            {
                if (hit.transform.root.TryGetComponent(out PlayerState player))
                {
                    if(NetworkManager.Instance.IsTeamMode())
                        _hitPlayerUI.color = player.Team == localPlayer.Team ? Color.cyan : Color.red;
                    else
                    {
                        _hitPlayerUI.color = player.IsInfected ? Color.green : Color.red;
                    }
                    _hitPlayerUI.text = player.Username;
                }
                else if (!string.IsNullOrEmpty(_hitPlayerUI.text))
                {
                    _hitPlayerUI.text = "";
                }
            }
            else if (!string.IsNullOrEmpty(_hitPlayerUI.text))
            {
                _hitPlayerUI.text = "";
            }
            
            //Display the 3d canvas name
            
            int cnt  = Physics.SphereCastNonAlloc(_transform.position, 2f, _transform.forward,_castedHit, 100f, playerLayer);
            if (cnt > 0)
            {
                CurrentCastedPlayers.Clear();
                for (int i = 0; i < cnt; i++)
                {
                    if (_castedHit[i].transform.root.TryGetComponent<PlayerState>(out var player))
                    {
                        if (!CastedPlayers.Contains(player))
                        {
                            CastedPlayers.Add(player);
                            player.DisplayName();
                        }
                        CurrentCastedPlayers.Add(player);
                    }
                }

                if (CastedPlayers.Count > 0)
                {
                    for (int i = 0; i < CastedPlayers.Count; i++)
                    {
                        if (!CurrentCastedPlayers.Contains(CastedPlayers[i]))
                        {
                            CastedPlayers[i].NotDisplayName();
                            CastedPlayers.RemoveAt(i);
                        }
                    }
                }
            
            }
            else if(CastedPlayers.Count>0)
            {
                foreach (var clientPlayer in CastedPlayers)
                {
                    clientPlayer.NotDisplayName();
                }
                CastedPlayers.Clear();
            }
        }
    }
}
