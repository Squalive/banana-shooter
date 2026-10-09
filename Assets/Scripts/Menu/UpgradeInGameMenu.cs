using System;
using System.Collections;
using Audio;
using Manager;
using Movement;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Menu
{
    public class UpgradeInGameMenu : MonoBehaviour
    {
        public static UpgradeInGameMenu Instance { get; private set; }

        public const uint MaxStoredTick = 128;
        public bool[] UpgradeBuffer { get; } = new bool[MaxStoredTick];
        private void Awake()
        {
            Instance = this;

        }

        private void Start()
        {
            _amountTransform = amountText.GetComponent<RectTransform>();
            originalPos = _amountTransform.anchoredPosition;
            desiredPos = originalPos;
            coin.gameObject.SetActive(!GameManager.Instance.setting.autoUpgrade);
        }

        private float _desiredAlpha = 0f;
        #region UPGRADE METHOD

        public UpgradeItem[] upgradeItems;

        public bool buyUpgradeRecently = false;
        private void OnEnable()
        {

            GameManager.InputManager.Player.Upgrade0.performed += upgradeItems[0].Upgrade;
            GameManager.InputManager.Player.Upgrade1.performed += upgradeItems[1].Upgrade;
            GameManager.InputManager.Player.Upgrade2.performed += upgradeItems[2].Upgrade;
        }
        private void OnDisable()
        {
            GameManager.InputManager.Player.Upgrade0.performed -= upgradeItems[0].Upgrade;
            GameManager.InputManager.Player.Upgrade1.performed -= upgradeItems[1].Upgrade;
            GameManager.InputManager.Player.Upgrade2.performed -= upgradeItems[2].Upgrade;
        }

        [HideInInspector]
        public int moveSpeedIndex = 0;
        public bool UpMovementSpeed(UpgradeItem item)
        {
            if (Player != null)
            {
                if (Mathf.Abs(Player.coins - Player.lastCoin) > 1)
                    return false;
                if (Player.coins - 1 < 0 || moveSpeedIndex >= 5)
                {
                    return false;
                }

                --Player.coins;
                Player.lastCoin = Player.coins;
                SetCoinText(Player.coins);
            }
            if (PlayerMovement.Instance)
            {
                moveSpeedIndex++;
                moveSpeedIndex = Mathf.Clamp(moveSpeedIndex, 0, 5);
                PlayerMovement.Instance.UpMovementSpeed();
            }
            CrazeUpgrade(item);
            item.UpdateGraphics(moveSpeedIndex);
            SetUpgradeItemColor();
            return true;
        }

        public void DownMovementSpeed()
        {
            if (PlayerMovement.Instance)
            {
                moveSpeedIndex = 0;
                moveSpeedIndex = Mathf.Clamp(moveSpeedIndex, 0, 5);
                PlayerMovement.Instance.DownMovementSpeed();
                for (int i = 0; i < upgradeItems.Length; i++)
                {
                    if (GameManager.Instance.upgrades[i] == "moveSpeed")
                        upgradeItems[i].UpdateGraphics(0);
                }
            }
        }
        [HideInInspector]
        public int healthIndex = 0;
        public bool UpHealth(UpgradeItem item)
        {
            if (Player != null)
            {
                if (Mathf.Abs(Player.coins - Player.lastCoin) > 1) return false;
                if (Player.coins - 1 < 0 || healthIndex >= 5)
                {
                    return false;
                }

                --Player.coins;
                Player.lastCoin = Player.coins;
                SetCoinText(Player.coins);
            }

            healthIndex++;
            healthIndex = Mathf.Clamp(healthIndex, 0, 5);

            CrazeUpgrade(item);
            item.UpdateGraphics(healthIndex);
            SetUpgradeItemColor();
            return true;
        }

        public void DownHealth()
        {
            healthIndex = 0;
            healthIndex = Mathf.Clamp(healthIndex, 0, 5);

            for (int i = 0; i < upgradeItems.Length; i++)
            {
                if (GameManager.Instance.upgrades[i] == "health")
                    upgradeItems[i].UpdateGraphics(0);
            }
        }
        void SetUpgradeItemColor()
        {
            foreach (var upgradeItem in upgradeItems)
            {
                upgradeItem.SetColor(Player.coins);
            }
        }
        public void AutoUpgrade()
        {
            if (InfectedHand.Instance.isInfected || !GameManager.Instance.setting.autoUpgrade) return;
            foreach (var upgrade in upgradeItems)
            {
                while (upgrade.currentIndex < upgrade.maxIndex && upgrade.cost <= Player.coins)
                {
                    int coins = Player.coins;
                    upgrade.Upgrade(new InputAction.CallbackContext());
                    if (Player.coins == coins) break;
                }
            }
        }
        public void ClearDash()
        {
            dashIndex--;
            dashIndex = Mathf.Clamp(dashIndex, 0, 1);
            // dashText.SetText($"{dashIndex} / 1");
            dashSlider.gameObject.SetActive(false);
            for (int i = 0; i < upgradeItems.Length; i++)
            {
                if (GameManager.Instance.upgrades[i] == "dash")
                    upgradeItems[i].UpdateGraphics(0);
            }
        }

        public Slider dashSlider;
        [HideInInspector]
        public float dashTimer = 0;

        [HideInInspector]
        public int doubleJumpIndex = 0;

        private ClientPlayer player;

        public ClientPlayer Player
        {
            get
            {
                if (player == null)
                {
                    if (NetworkManager.Instance.Client.Connection == null) return null;
                    ushort id = NetworkManager.Instance.Client.Id;
                    if (ClientPlayer.list.ContainsKey(id))
                    {
                        return player = ClientPlayer.list[id];
                    }
                }

                return player;
            }
        }
        [HideInInspector]
        public int dashIndex = 0;
        public bool DoubleJump(UpgradeItem item)
        {
            if (Player != null)
            {
                if (Mathf.Abs(Player.coins - Player.lastCoin) > 1) return false;
                if (Player.coins - 1 < 0 || doubleJumpIndex >= 5)
                {
                    return false;
                }

                Player.coins -= 1;
                Player.lastCoin = Player.coins;
                SetCoinText(Player.coins);
            }

            doubleJumpIndex++;
            doubleJumpIndex = Mathf.Clamp(doubleJumpIndex, 0, 5);
            PlayerMovement.Instance.maxJumpCount = doubleJumpIndex + 1;
            PlayerMovement.Instance.jumpLeft = PlayerMovement.Instance.maxJumpCount;
            CrazeUpgrade(item);
            item.UpdateGraphics(doubleJumpIndex);
            SetUpgradeItemColor();
            return true;
        }
        public void ClearDoubleJump()
        {
            doubleJumpIndex = 0;
            PlayerMovement.Instance.maxJumpCount = 1;
            doubleJumpIndex = Mathf.Clamp(doubleJumpIndex, 0, 5);
            for (int i = 0; i < upgradeItems.Length; i++)
            {
                if (GameManager.Instance.upgrades[i] == "doubleJump")
                    upgradeItems[i].UpdateGraphics(0);
            }
        }
        public bool Dash(UpgradeItem item)
        {
            if (Player != null)
            {
                if (Mathf.Abs(Player.coins - Player.lastCoin) > 1) return false;
                if (Player.coins - 1 < 0 || dashIndex >= 1)
                {
                    return false;
                }

                Player.coins -= 1;
                Player.lastCoin = Player.coins;
                SetCoinText(Player.coins);
            }

            dashIndex++;
            dashIndex = Mathf.Clamp(dashIndex, 0, 1);
            CrazeUpgrade(item);
            dashSlider.gameObject.SetActive(true);
            item.UpdateGraphics(dashIndex);
            SetUpgradeItemColor();
            return true;
        }
        void CrazeUpgrade(UpgradeItem upgrade)
        {
            buyUpgradeRecently = true;
            if (NetworkManager.ClientGameMode != GameMode.SpecialGameMode && CheckUpgrade(0) && CheckUpgrade(1) && CheckUpgrade(2))
            {
                AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.CRAZY_UPGRADE);
            }
            Invoke(nameof(ClearUpgradeRecently), 1f);

            if (!GameManager.Instance.setting.enableUpgradeAnimation) return;
            string k = GameManager.Instance.upgrades[upgrade.index];

            icon.texture = GameManager.Instance.GetUpgradeDetailedTexture2D(k);
            text.SetEntry("up_" + k);
            amountText.SetText(upgrade.currentIndex.ToString());

            _desiredAlpha = 1f;
            CancelInvoke(nameof(Clear));
            StopAllCoroutines();
            Invoke(nameof(Clear), 5.5f);
            StartCoroutine(SetAmountText(upgrade));
        }

        IEnumerator SetAmountText(UpgradeItem item)
        {
            AudioManager.Instance.Play("build_up");
            yield return new WaitForSeconds(.5f);
            _shaking = true;
            // amountText.materialForRendering.SetFloat("_FaceSoftness",1f);
            yield return new WaitForSeconds(1.5f);
            amountText.SetText(item.currentIndex.ToString());
            AudioManager.Instance.PlayPitched("upgrade", .5f);
            yield return new WaitForSeconds(.5f);
            _shaking = false;
        }
        void Clear()
        {
            _desiredAlpha = 0f;
        }

        void ClearUpgradeRecently()
        {
            buyUpgradeRecently = false;
        }
        public TextMeshProUGUI coin;

        public void SetCoinText(int count)
        {
            coin.SetText(count.ToString());
        }
        bool CheckUpgrade(int upgrade)
        {
            switch (GameManager.Instance.upgrades[upgrade])
            {
                case "health":
                    return healthIndex >= 5;
                case "moveSpeed":
                    return moveSpeedIndex >= 5;
                case "doubleJump":
                    return doubleJumpIndex >= 5;
                case "dash":
                    return dashIndex >= 1;
            }

            return false;
        }

        #endregion

        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private RawImage icon;
        [SerializeField] private LocalizeStringEvent text;
        [SerializeField] private TextMeshProUGUI amountText;

        private RectTransform _amountTransform;
        private bool _shaking = false;
        float _magnitude = 5f;
        Vector2 originalPos, desiredPos;
        private void Update()
        {
            if (dashIndex > 0)
            {
                dashTimer += Time.deltaTime;
                if (dashTimer > 2.5f)
                {
                    dashTimer = 2.5f;
                }

                dashSlider.value = Mathf.Lerp(dashSlider.value, dashTimer / 2.5f, Time.deltaTime * 25f);
            }

            if (_shaking)
            {
                float x = Random.Range(-1f, 1f) * _magnitude;
                float y = Random.Range(-1f, 1f) * _magnitude;
                Vector2 offset = new Vector2(x, y);

                desiredPos = originalPos + offset;
                _amountTransform.anchoredPosition = Vector2.Lerp(_amountTransform.anchoredPosition, desiredPos, Time.deltaTime * 20f);
            }
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, _desiredAlpha, Time.deltaTime * 5f);
        }
    }
}
