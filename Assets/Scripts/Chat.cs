
using System;
using System.Collections.Generic;
using System.Text;
using Console;
using Cosmetic;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Interface;
using Multiplayer.Server;
using Riptide;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
    public static Chat Instance;
    [SerializeField] CanvasGroup group;

    private float desiredAlpha = 0, bgAlpha = 0;


    public ulong steamId;
    [SerializeField] private TMP_InputField input, content;

    [SerializeField] private GameObject messageObj, prefab;
    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    public void SetSteamId(ulong steamId)
    {
        this.steamId = steamId;
    }

    private void Start()
    {
        if (Instance != this) return;
        GameManager.InputManager.Player.Chat.performed += StartChat;
    }

    private void OnDestroy()
    {
        GameManager.InputManager.Player.Chat.performed -= StartChat;
    }

    public void DisableBlockraycast()
    {
        @group.blocksRaycasts = false;

        if (!GameVoteMenu.Instance && (!GameUIManager.Instance || !GameUIManager.Instance.scoreBoard.activeSelf))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    [MessageHandler((ushort)ClientToServerId.SendMessage, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
    private static void ReceiveMessage(ushort fromClient, Message message)
    {
        if (NetworkServerManager.ClientData.TryGetValue(fromClient, out var data))
        {
            string text = message.GetString();
            int type = message.GetInt();

            if (type < 0 || type > 2 || string.IsNullOrEmpty(text) || text.Length > 300) return;
            if (type == 0 && (text.Length > 200 || text.Contains("<color"))) return;

            if (type == 0)
            {
                text = Instance.SwearCheck(text);
            }

            Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.SendMessage);
            msg.Add(fromClient);
            msg.Add(data.SteamId);
            msg.Add(text);
            msg.Add(type);
            NetworkServerManager.Instance.Server.SendToAll(msg);
        }
    }
    [MessageHandler((ushort)ServerToClientId.SendMessage, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
    private static void SendMessage(Message message)
    {
        ushort fromClient = message.GetUShort();
        ulong steamId = message.GetULong();
        string text = message.GetString();
        int type = message.GetInt();

        switch (type)
        {
            case 0:
                Instance.AddChat(text, steamId, fromClient);
                break;
            case 1:
                Instance.AddMessage(text, Color.green);
                break;
            case 2:
                Instance.AddChat(text, steamId, 0);
                break;
        }
    }

    [SerializeField] private List<string> swear = new List<string>();
    void StartChat(InputAction.CallbackContext obj)
    {
        if (!NetworkManager.Instance.Client.IsConnected || (GameUIManager.Instance && GameUIManager.Instance.pause) || DeveloperConsoleUI.Instance.uiCanvas.activeSelf) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        CancelInvoke(nameof(NotDisplay));
        @group.blocksRaycasts = true;
        bgAlpha = 0.7f;
        string text = SwearCheck(input.text);

        if (input.isFocused)
        {
            if (string.IsNullOrEmpty(text))
            {
                EventSystem.current.SetSelectedGameObject(null);
                DisableBlockraycast();
                NotDisplay();
                group.alpha = 0f;
                return;
            }

            if (@group.blocksRaycasts && !text.Contains("<color"))
            {
                if (text.Length > 200)
                {
                    AddMessage(UIManager.IsItChinese() ? "消息太长" : "Message too long", Color.yellow);
                }
                else
                {
                    NetworkManager.Instance.SendMsg(text, 0);
                }
                input.SetTextWithoutNotify(String.Empty);
                EventSystem.current.SetSelectedGameObject(null);
                DisableBlockraycast();
                NotDisplay();
                group.alpha = 0f;
                return;
            }
        }


        input.SetTextWithoutNotify("");
        Display();
        input.Select();
    }

    public string SwearCheck(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        foreach (var s in swear)
        {
            if (str.ToLower().Contains(s))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if (str.ToLower()[i] == s[0])
                    {
                        if (i + s.Length <= str.Length)
                        {
                            string temp = str.Substring(i, s.Length);
                            if (temp.ToLower() == s)
                            {
                                str = str.Replace(temp, "banana");
                            }
                        }

                    }
                }
            }
        }

        return str;
    }


    private void Update()
    {
        float targetAlpha = NetworkManager.GameState == GameState.Voting ? desiredAlpha : 0;
        if (GameUIManager.Instance)
        {
            targetAlpha = GameUIManager.Instance.gameScene.activeSelf ? desiredAlpha : desiredAlpha == 0 ? 0 : 0.15f;
        }

        @group.alpha = Mathf.Lerp(@group.alpha, targetAlpha, Time.deltaTime * 15f);
    }

    void Display()
    {
        @group.blocksRaycasts = true;
        desiredAlpha = 1f;
        messageObj.SetActive(false);
    }

    void NotDisplay()
    {
        input.DeactivateInputField(true);
        @group.blocksRaycasts = false;
        desiredAlpha = 0;
        messageObj.SetActive(true);
    }

    public bool IsChat()
    {
        return @group.blocksRaycasts;
    }

    [SerializeField] private Transform c;
    void AddChat(string content, ulong steamId, ushort fromClient)
    {
        DestroyUselessThing();

        if (!input.isFocused && !CosmeticMenu.Instance.inspectWindow.activeSelf)
            DisableBlockraycast();

        GameObject obj = Instantiate(prefab, c);

        TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();

        Destroy(obj, 10f);

        if (NetworkManager.ClientData.TryGetValue(fromClient, out var data))
        {
            string t =
                $"{GetPlayerNameNetwork(data.Name, steamId, data.DisplayTag, data.OwnedDlc)}: {content}";
            this.content.text += "\n" + t;

            text.SetText(t);
        }
        else if (content.Contains("<color") && fromClient == 0)
        {
            this.content.text += "\n" + content;

            text.SetText(content);
        }
    }

    public void AddMessage(string content, Color color)
    {
        DestroyUselessThing();
        // Display();
        if (!input.isFocused)
            DisableBlockraycast();

        this.content.text += "\n" + content;

        GameObject obj = Instantiate(prefab, c);

        TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>();

        Destroy(obj, 10f);

        text.color = color;
        text.SetText(content);
    }

    void DestroyUselessThing()
    {
        if (content.text.Length > 1200) content.text = content.text.Substring(1000);
    }
    public void DestroyEveryThing()
    {
        EventSystem.current.SetSelectedGameObject(null);
        for (int i = 0; i < c.childCount; i++)
        {
            Destroy(c.GetChild(i).gameObject);
        }
    }
    public string GetPlayerName(ulong steamId)
    {
        string playerName = SteamFriends.GetFriendPersonaName((CSteamID)steamId);
        string name = SwearCheck(playerName);
        name = GetNameTag(name, steamId);

        return name;
    }
    public string GetPlayerNameNetwork(string playerName, ulong steamId, bool displayTag, bool ownedDlc = false)
    {
        string name = SwearCheck(playerName);

        name = displayTag ? GetNameTag(name, steamId, ownedDlc) : name;

        return name;
    }
    string GetNameTag(string playerName, ulong steamId, bool ownedDlc = false)
    {
        string name = playerName;

        if (steamId == 76561198983573782)//Dev
        {
            name = $"<color=#ff3c2f>[Dev] {playerName}</color>";
        }
        else if (RolesManager.Instance.CheckIsAdmin(steamId))
        {
            name = $"<color=#006ba8>[Admin] {playerName}</color>";
        }
        else if (RolesManager.Instance.CheckIsHelper(steamId))
        {
            name = $"<color=#8A2BE2>[HELPER] {playerName}</color>";
        }
        else if (RolesManager.Instance.CheckIsDiscordMan(steamId))
        {
            name = $"<color=#00BFFF>[Discord] {playerName}</color>";
        }
        else if (RolesManager.Instance.CheckIsBananaMan(steamId))
        {
            name = $"<color=#FFFF00>[Banana] {playerName}</color>";
        }
        else if (steamId == 76561199081360658)
        {
            name = $"<color=#FFD700>[SHIT] {playerName}</color>";
        }
        else if (steamId == LobbyManager.Instance.owner.m_SteamID)
        {
            name = $"<color=#DAA520>[Host] {playerName}</color>";
        }
        else if (steamId == this.steamId)
        {
            name = $"<color=#FFBA00>{playerName}</color>";
        }
        else if (SteamFriends.HasFriend((CSteamID)steamId, EFriendFlags.k_EFriendFlagImmediate))
        {
            name = $"<color=green>{playerName}</color>";
        }

        if (ownedDlc)
        {
            name = $"<color=#ffe800>[DLC]</color> {name}";
        }

        return name;
    }
}
