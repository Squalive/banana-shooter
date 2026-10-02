
using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Menu;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrazyPlayer : MonoBehaviour
{
    public static CrazyPlayer Instance;

    public RectTransform item;
    public TextMeshProUGUI nameText, title;
    public RawImage avatar;
    private void Awake()
    {
        Instance = this;
        desiredPos = outScreenPos;
    }

    private Vector2 desiredPos;
    public Vector2 outScreenPos, inScreenPos;

    private void Update()
    {
        item.anchoredPosition = Vector2.Lerp(item.anchoredPosition, desiredPos, Time.deltaTime * 20f);
        if (data.Count > 0)
        {
            CrazyPlayerData d = data.Dequeue();
            AudioManager.Instance.Play("hit_reverse");
            CancelInvoke(nameof(Clear));
            if (ClientPlayer.list.TryGetValue(d.id, out var player))
            {
                title.SetText(d.str);
                nameText.SetText(player.playerState.Username);
                avatar.texture = player.playerState.AvatarImage;
                desiredPos = inScreenPos;
            }
            Invoke(nameof(Clear),5f);
        }
    }

    private Queue<CrazyPlayerData> data = new Queue<CrazyPlayerData>();

    void SetTitle(string str, ushort id)
    {
        CrazyPlayerData data = new CrazyPlayerData(str, id);
        this.data.Enqueue(data);
    }
    [Serializable]
    public class CrazyPlayerData
    {
        public string str;
        public ushort id;

        public CrazyPlayerData(string _str, ushort _id)
        {
            str = _str;
            id = _id;
        }
    }

    void Clear()
    {
        desiredPos = outScreenPos;
    }

    [MessageHandler((ushort) ServerToClientId.CrazyPlayer, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
    private static void CrazyPlayerSet(Message message)
    {
        string title = message.GetString();
        ushort id = message.GetUShort();
        
        Instance.SetTitle(title,id);
    }
}
