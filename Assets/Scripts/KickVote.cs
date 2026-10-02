using System;
using System.Collections.Generic;
using Multiplayer;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class KickVote : MonoBehaviour
{
    public static KickVote Instance;

    public TextMeshProUGUI agreeCountText, disAgreeCountText;

    public static VoteKicking VoteKicking;

    public LocalizeStringEvent kick, vote;

    private void Awake()
    {
        Instance = this;
    }

    private Vector3 desiredPos;
    public Vector3 showPos, defaultPos;
    [SerializeField] private RectTransform kickMessage;
    private void Start()
    {
        desiredPos = defaultPos;
        agreeBtn.onClick.AddListener(Agree);
        disAgreeBtn.onClick.AddListener(DisAgree);
    }

    private void Update()
    {
        kickMessage.anchoredPosition = Vector3.Lerp(kickMessage.anchoredPosition,desiredPos,Time.deltaTime*15f);

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Agree();
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            DisAgree();
        }
    }

    public string kickPlayerName,votePlayerName;
    public void SetPlayerValues(string playerName,VoteKicking vote,string fromPlayer)
    {
        this.kickPlayerName = playerName;
        this.votePlayerName = fromPlayer;

        kick.StringReference.Arguments = new List<object>() {kickPlayerName};
        this.vote.StringReference.Arguments = new List<object> {votePlayerName};

        kick.RefreshString();
        this.vote.RefreshString();
        
        desiredPos = showPos;

        VoteKicking = vote;
        
        agreeCountText.SetText(vote.agreeCount.ToString());
        disAgreeCountText.SetText(vote.disAgreeCount.ToString());

        if (VoteKicking.FromClient == NetworkManager.Instance.Client.Id || VoteKicking.PlayerId == NetworkManager.Instance.Client.Id)
        {
            agreeBtn.interactable = false;
            disAgreeBtn.interactable = false;
        }
    }

    public Button agreeBtn, disAgreeBtn;
    void Agree()
    {
        if (VoteKicking == null|| !agreeBtn.interactable) return;
        Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.AgreeKicking);
        message.Add(VoteKicking.PlayerId);
        NetworkManager.Instance.SendByte += message.WrittenLength;
        NetworkManager.Instance.Client.Send(message);
        agreeBtn.interactable = false;
        disAgreeBtn.interactable = false;
    }

    void DisAgree()
    {
        if (VoteKicking == null || !disAgreeBtn.interactable) return;
        Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.DisAgreeKicking);
        message.Add(VoteKicking.PlayerId);
        NetworkManager.Instance.SendByte += message.WrittenLength;
        NetworkManager.Instance.Client.Send(message);
        agreeBtn.interactable = false;
        disAgreeBtn.interactable = false;
    }

    public void Clear()
    {
        desiredPos = defaultPos;
        VoteKicking = null;
    }
}
[Serializable]
public class VoteKicking
{
    internal ushort PlayerId,FromClient;
    public ushort agreeCount, disAgreeCount;
    public Dictionary<ushort, bool> Players = new Dictionary<ushort, bool>();

    public VoteKicking(ushort playerId,ushort fromClient)
    {
        FromClient = fromClient;
        PlayerId = playerId;
        agreeCount = 1;
        disAgreeCount = 1;
    }
    
}
