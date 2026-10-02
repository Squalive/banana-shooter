
using Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class UserInterface : MonoBehaviour
    {
        [SerializeField] private RawImage avatorImg;
        [SerializeField] private TextMeshProUGUI nameText;
        private void Start()
        {
            nameText.SetText(NetworkManager.Instance.PersonalName);

            if (NetworkManager.avatarLoaded)
            {
                avatorImg.texture = NetworkManager.Instance.myAvatar;
            }
            else
            {
                NetworkManager.Instance.OnAvatarLoad += OnAvatarLoaded;
            }
        }

        private void OnDestroy()
        {
            NetworkManager.Instance.OnAvatarLoad -= OnAvatarLoaded;
        }

        void OnAvatarLoaded(Texture2D texture2D)
        {
            avatorImg.texture = texture2D;
        }
    }
}
