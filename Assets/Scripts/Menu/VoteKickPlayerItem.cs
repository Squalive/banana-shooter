using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class VoteKickPlayerItem : MonoBehaviour
    {
        public string playerName;
        public ushort connectionId;
        public ulong playerSteamId;
        [SerializeField] public TextMeshProUGUI playerNameText;
        [SerializeField] private RawImage avatar;
        public void SetPlayerValues(string _playerName,ushort _connectionId,ulong steamId, Texture2D texture2D)
        {
            playerName = _playerName;
            connectionId = _connectionId;
            playerSteamId = steamId;
            playerNameText.SetText(playerName);

            avatar.texture = texture2D;
        }
    }
}
