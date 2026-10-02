using Demo.Entity;
using Movement;
using Multiplayer.Entity.Server;
using Steamworks;
using UnityEngine;

namespace Multiplayer.Entity.Client
{
    public class LocalPlayer : MonoBehaviour
    {
        [SerializeField] private PlayerState playerState;

        private PlayerMovement  _playerMovement;
        private DemoPlayer _demoPlayer;

        private void Awake()
        {
            _playerMovement  = GetComponentInChildren<PlayerMovement>();
            _demoPlayer = GetComponent<DemoPlayer>();
            playerState.SetValues(true, NetworkManager.Instance.steamId.m_SteamID, SteamFriends.GetPersonaName(), Team.Rebel, false);
        }

        private void LateUpdate()
        {
            if (_demoPlayer != null && _playerMovement != null)
            {
                _demoPlayer.PlayerAnimation.SetGround(_playerMovement.IsGrounded());
                _demoPlayer.PlayerAnimation.SetCrouch(_playerMovement.IsCrouching());
                // XRotation is the vertical (pitch) look rotation. desiredX is the
                // horizontal (yaw) rotation and must not be used here.
                _demoPlayer.PlayerAnimation.SetXRotation(_playerMovement.XRotation);
            }
        }
    }
}
