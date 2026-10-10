
using System;
using System.Collections.Generic;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Server;
using Multiplayer.Interface;
using Riptide;
using Steamworks;
using Steamworks.NET;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace UI
{
    public class VoiceChatUIManager : MonoBehaviour
    {
        public static VoiceChatUIManager Instance { get; private set; }

        public bool IsSpeaking { get; private set; }

        private CSteamID _mySteamId;

        private Dictionary<ushort, VoiceChatPlayerUI> _list = new Dictionary<ushort, VoiceChatPlayerUI>();

        [SerializeField] private VoiceChatPlayerUI prefab;

        [SerializeField] private Transform content;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _mySteamId = SteamUser.GetSteamID();
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        void EnableInput()
        {
            GameManager.InputManager.Player.Microphone.started += StartVoiceChat;
            GameManager.InputManager.Player.Microphone.canceled += StopVoiceChat;
        }

        void DisableInput()
        {
            GameManager.InputManager.Player.Microphone.started -= StartVoiceChat;
            GameManager.InputManager.Player.Microphone.canceled -= StopVoiceChat;
        }

        public void NewPlayer(ClientData data)
        {
            if (_list.TryGetValue(data.Id, out var playerUi))
            {
                Destroy(playerUi);
                _list.Remove(data.Id);
            }

            VoiceChatPlayerUI playerUI = Instantiate(prefab, content);

            playerUI.Initialize(data);

            _list.Add(data.Id, playerUI);
        }

        public void RemovePlayer(ushort id)
        {
            if (_list.TryGetValue(id, out var playerUi))
            {
                Destroy(playerUi);
                _list.Remove(id);
            }
        }

        public void Clear()
        {
            foreach (var playerUi in _list.Values)
            {
                Destroy(playerUi);
            }

            _list.Clear();
        }

        private void Update()
        {
            if (!SteamManager.Initialized || GameManager.Instance.setting.disableVoice) return;
            if (!NetworkManager.Instance.Client.IsConnected) return;
            if (IsSpeaking)
            {
                uint compressed;
                EVoiceResult result = SteamUser.GetAvailableVoice(out compressed);

                if (result == EVoiceResult.k_EVoiceResultOK)
                {
                    if (compressed > 1024)
                    {
                        if (GameUIManager.Instance)
                            GameUIManager.Instance.SetMicSpeak();

                        byte[] destBuffer = new byte[1024];
                        uint byteWritten;
                        result = SteamUser.GetVoice(true, destBuffer, 1024, out byteWritten);

                        if (result == EVoiceResult.k_EVoiceResultOK && byteWritten > 0)
                        {
                            Message message = Message.Create(MessageSendMode.Unreliable,
                                (ushort)ClientToServerId.VoiceChat);

                            message.Add(destBuffer);
                            message.Add(byteWritten);
                            NetworkManager.Instance.SendByte += message.WrittenLength;

                            NetworkManager.Instance.Client.Send(message);

                            if (Instance._list.TryGetValue(NetworkManager.Instance.Client.Id, out var playerUI))
                            {
                                playerUI.Speak();
                            }
                        }
                    }
                }
            }
        }

        [MessageHandler((ushort)ServerToClientId.VoiceChat, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void VoiceChat(Message message)
        {
            if (GameManager.Instance.setting.disableVoice) return;
            ushort id = message.GetUShort();

            uint sampleRate = SteamUser.GetVoiceOptimalSampleRate();

            byte[] destBuffer = new byte[sampleRate * 2];
            uint bytesWritten;
            EVoiceResult ret = SteamUser.DecompressVoice(message.GetBytes(), message.GetUInt(), destBuffer,
                (uint)destBuffer.Length, out bytesWritten, sampleRate);

            if (ret == EVoiceResult.k_EVoiceResultOK && bytesWritten > 0)
            {
                VoiceChatPacket packet = new VoiceChatPacket(bytesWritten, destBuffer);

                if (ClientPlayer.list.TryGetValue(id, out var player))
                {
                    player.Peer.OnNewSample(packet);
                }
                else if (Instance._list.TryGetValue(id, out var playerUI))
                {
                    playerUI.Speak();
                    playerUI.Peer.OnNewSample(packet);
                }
            }
        }

        void StartVoiceChat(InputAction.CallbackContext ctx)
        {
            if (!NetworkManager.Instance.Client.IsConnected) return;
            // if (gameUi.pause || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || NetworkManager.Instance.CantPlay()) return;
            IsSpeaking = true;
            if (GameUIManager.Instance)
                GameUIManager.Instance.micIcon.SetActive(true);
            SteamUser.StartVoiceRecording();
            SteamFriends.SetInGameVoiceSpeaking(_mySteamId, true);
        }

        void StopVoiceChat(InputAction.CallbackContext ctx)
        {
            IsSpeaking = false;
            if (GameUIManager.Instance)
                GameUIManager.Instance.micIcon.SetActive(false);
            SteamUser.StopVoiceRecording();
            SteamFriends.SetInGameVoiceSpeaking(_mySteamId, false);
        }
    }
}
