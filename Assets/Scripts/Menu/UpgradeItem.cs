using Manager;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menu
{
    public class UpgradeItem : MonoBehaviour
    {
        public RawImage[] images;

        public int index = 0;

        public TextMeshProUGUI keyText;

        public RawImage upgradeImage;

        public int cost = 1;

        
        private void Start()
        {
            colorImage = GetComponent<RawImage>();
            normalColor = colorImage.color;
            greyColor = new Color(Color.grey.r,Color.grey.g,Color.grey.b,normalColor.a);
            Setup();
        }

        public void Upgrade(InputAction.CallbackContext ctx)
        {
            if (!GameManager.Instance.CanInput()) return;
            if (InfectedHand.Instance.isInfected) return;
            string upgradeKey = GameManager.Instance.upgrades[index];
            uint i = 0;
            bool success = false;
            switch (upgradeKey)
            {
                case "health":
                    if (UpgradeInGameMenu.Instance)
                    {
                        success=UpgradeInGameMenu.Instance.UpHealth(this);
                    }

                    i = 0;
                    break;
                case "dash":
                    if (UpgradeInGameMenu.Instance)
                    {
                        success=UpgradeInGameMenu.Instance.Dash(this);
                    }
                
                    i = 1;
                    break;
                case "doubleJump":
                    if (UpgradeInGameMenu.Instance)
                    {
                        success=UpgradeInGameMenu.Instance.DoubleJump(this);
                    }
                
                    i = 2;
                    break;
                case "moveSpeed":
                    if (UpgradeInGameMenu.Instance)
                    {
                        success=UpgradeInGameMenu.Instance.UpMovementSpeed(this);
                    }
                
                    i = 3;
                    break;
            }

            UpgradeInGameMenu.Instance.UpgradeBuffer[
                NetworkManager.Instance.InterpolationTick % UpgradeInGameMenu.MaxStoredTick] = success;
            
            Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.Upgrade);

            message.Add(NetworkManager.Instance.ServerTick);
            message.Add(i);
            
            NetworkManager.Instance.SendByte += message.WrittenLength;
            NetworkManager.Instance.Client.Send(message);
        }

        public void UpdateGraphics(int idx)
        {
            currentIndex = idx;
            for (int i = 0; i < images.Length; i++)
            {
                images[i].color = Color.grey;
            }
            for (int i = 0; i < idx; i++)
            {
                images[i].color = Color.white;
            }
        }

        public int maxIndex;
        public int currentIndex=0;

        public void Setup()
        {
            string upgradeKey = GameManager.Instance.upgrades[index];
            upgradeImage.texture = GameManager.Instance.GetUpgradeDetailedTexture2D(upgradeKey);
            maxIndex = GameManager.Instance.GetUpgradeDetailedLength(upgradeKey );
            cost = GameManager.Instance.GetUpgradeDetailedCost(upgradeKey );
            for (int i = 0; i < images.Length; i++)
            {
                images[i].gameObject.SetActive(i<maxIndex);
            }

            switch (index)
            {
                case 0:
                    keyText.SetText(GameManager.GetBindingName("Upgrade0", 0));
                    break;
                case 1:
                    keyText.SetText(GameManager.GetBindingName("Upgrade1", 0));
                    break;
                case 2:
                    keyText.SetText(GameManager.GetBindingName("Upgrade2", 0));
                    break;
            }

        
            SetColor();
        }

        void SetColor()
        {
            if (NetworkManager.Instance.Client.Connection == null) return;
            if (ClientPlayer.list.ContainsKey(NetworkManager.Instance.Client.Id))
            {
                colorImage.color = ClientPlayer.list[NetworkManager.Instance.Client.Id].coins >= cost ? normalColor : greyColor;
            }
            else colorImage.color = greyColor;
        }
        public void SetColor(int coin)
        {
        
            colorImage.color = coin<cost ? greyColor : normalColor;
        }
        private RawImage colorImage;

        private Color normalColor,greyColor;
    }
}
