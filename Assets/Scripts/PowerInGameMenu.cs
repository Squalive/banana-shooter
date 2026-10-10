
using System;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Riptide;
using Steamworks.NET;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Weapon;

public class PowerInGameMenu : MonoBehaviour
{
    public static PowerInGameMenu Instance;

    public GameObject power;

    public Image fill;

    public RawImage texture;

    public TextMeshProUGUI keyText;

    public CanvasGroup group;

    internal float fillProgress = 0;

    private readonly float increase = 0.005f;
    // private readonly float increase = 0.2f;

    private float desiredAlpha = 0;

    private Image leftFill;

    public bool active = true, inPower = false;

    private bool canUse = false;
    private void Awake()
    {
        Instance = this;
        switch (NetworkManager.ClientServerType)
        {
            case ServerType.Endless:
                break;

            default:
                if (NetworkManager.ClientGameMode == GameMode.Randomizer ||
                    NetworkManager.ClientGameMode == GameMode.GunGame)
                {
                    active = false;
                }
                break;
        }

        texture.color = Color.grey;

        leftFill = @group.GetComponent<Image>();

        GameManager.PowerDetail detail = GameManager.Instance.GetPowerDetail(GameManager.Instance.power);

        if (detail != null)
        {
            texture.texture = detail.texture2D;
        }

        power.SetActive(active);
    }

    private void OnEnable()
    {
        keyText.SetText(GameManager.GetBindingName("Power", 0));
        GameManager.InputManager.Player.Power.performed += Use;
    }

    private void OnDisable()
    {

        GameManager.InputManager.Player.Power.performed -= Use;
    }

    private void Use(InputAction.CallbackContext obj)
    {
        if (!active || !canUse || inPower) return;
        inPower = true;
        Message message;
        switch (GameManager.Instance.power)
        {
            case GameManager.PowerType.Boomer:
                message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.SpecialWeapon);
                message.Add((short)10);
                NetworkManager.Instance.SendByte += message.WrittenLength;
                NetworkManager.Instance.Client.Send(message);
                break;
            case GameManager.PowerType.Rocket_Launcher:
                message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.SpecialWeapon);
                message.Add((short)16);
                NetworkManager.Instance.SendByte += message.WrittenLength;
                NetworkManager.Instance.Client.Send(message);
                break;
            case GameManager.PowerType.DualSMG:
                message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.SpecialWeapon);
                message.Add((short)17);
                NetworkManager.Instance.SendByte += message.WrittenLength;
                NetworkManager.Instance.Client.Send(message);
                break;
        }
        StopBreathe();
    }

    [MessageHandler((ushort)ServerToClientId.SpecialWeaponDisable, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
    private static void SpecialWeaponDisable(Message message)
    {
        ushort id = message.GetUShort();

        if (ClientPlayer.list.TryGetValue(id, out var player))
        {
            if (player.playerState.WeaponManager.SpecialWeapon != null)
            {
                if (player.playerState.WeaponManager.CurrentWeapon != null && player.playerState.WeaponManager.SpecialWeapon == player.playerState.WeaponManager.CurrentWeapon)
                {
                    player.playerState.WeaponManager.SwitchWeapon(0);
                }
                player.playerState.WeaponManager.SpecialWeapon = null;
            }

            if (player.IsLocal)
            {
                Instance.ClearSpecialWeaponForLocal();
            }
        }
    }

    public void ClearSpecialWeaponForLocal()
    {
        if (WeaponManager.Instance.SpecialWeapon != null)
        {
            if (WeaponManager.Instance.SpecialWeapon == WeaponManager.Instance.CurrentWeapon)
            {
                WeaponManager.Instance.CurrentWeaponIndex = 0;
            }
            WeaponManager.Instance.DisableSpecialWeapon();
        }
        if (inPower)
            EnableTimer();
    }

    private ClientPlayer localPlayer;
    void EnableTimer()
    {
        canUse = false;
        inPower = false;
        CancelInvoke(nameof(StartBreath));
        CancelInvoke(nameof(Breathe));
        leftTime = 0f;
        desiredAlpha = 0f;
        texture.color = Color.grey;
        fillProgress = 0;
        fill.fillAmount = fillProgress;
        group.alpha = 0f;
        leftFill.fillAmount = 1f;
    }

    private void Update()
    {
        if (!SteamManager.Initialized) return;

        if (NetworkManager.Instance.Client.Connection != null)
        {
            if (localPlayer == null)
            {
                ushort id = NetworkManager.Instance.Client.Id;
                if (ClientPlayer.list.ContainsKey(id))
                {
                    localPlayer = ClientPlayer.list[id];
                }
                else
                {
                    return;
                }
            }
        }

        if (localPlayer != null && localPlayer.Dead) return;
        if (active)
        {
            if (!canUse)
            {
                if (leftTime > 0)
                {
                    leftTime -= Time.deltaTime * leftTimeToDecrease;

                    if (leftTime <= 0)
                    {
                        //stop
                        leftTime = 0.1f;

                    }

                    leftFill.fillAmount = Mathf.Lerp(leftFill.fillAmount, leftTime, Time.deltaTime * 15f);
                }
                else
                {
                    fillProgress += increase * Time.deltaTime;

                    fill.fillAmount = Mathf.Lerp(fill.fillAmount, fillProgress, Time.deltaTime * 15f);


                    if (fillProgress >= 1)
                    {
                        canUse = true;
                        StartBreath();

                        texture.color = Color.white;
                    }
                }

            }

            @group.alpha = Mathf.Lerp(@group.alpha, desiredAlpha, Time.deltaTime * 10f);
        }
    }

    void StartBreath()
    {
        desiredAlpha = 1f;

        Invoke(nameof(Breathe), .5f);
    }

    void Breathe()
    {
        desiredAlpha = 0f;

        Invoke(nameof(StartBreath), .5f);
    }

    void StopBreathe()
    {
        canUse = false;
        CancelInvoke(nameof(StartBreath));
        CancelInvoke(nameof(Breathe));
        desiredAlpha = 1f;
        texture.color = Color.white;
        fillProgress = 1;
        fill.fillAmount = fillProgress;

        leftTime = 1f;
    }

    private float leftTime = 0;
    private float leftTimeToDecrease = 0.05f;
}
