using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Multiplayer;
using Multiplayer.Interface;
using Newtonsoft.Json;
using Riptide;
using Steamworks;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Utils;
using Web;

namespace Manager
{
    public class AnticheatManager : MonoBehaviour
    {
        public static AnticheatManager Instance { get; private set; }
        
        public List<ulong> BannedPlayer = new List<ulong>();
        
        enum WatchdogIntent : uint
        {
            watchdog_intent_disabled = 0u,
            watchdog_intent_production = 1u,

            // This should only be used in development to ensure that Watchdog Anti-Cheat is setup properly.
            // If any error occurs during this mode, A popup will occur notifying you of the issue.
            watchdog_intent_testing = 2u,
        }

        public Dictionary<ushort, float> Heartbeats = new();
        
        private static readonly uint CE1 = FnvConstants.CreateHash("watchdog_cheatengine_1");
        private static readonly uint CE2 = FnvConstants.CreateHash("watchdog_cheatengine_2");
        private static readonly uint DLL1 = FnvConstants.CreateHash("watchdog_unsigneddll_1");
        private static readonly uint HANDLE1 = FnvConstants.CreateHash("watchdog_unsignedhandle_1");
        private static readonly uint HWID = FnvConstants.CreateHash("watchdog_hwid");
        private static readonly uint HEARTBEAT = FnvConstants.CreateHash("watchdog_heartbeat");

#if UNITY_EDITOR
        const WatchdogIntent intent = WatchdogIntent.watchdog_intent_disabled;
#else
        const WatchdogIntent intent = WatchdogIntent.watchdog_intent_production;
#endif
        
#if UNITY_EDITOR
        const string CLIENT_DLL = "watchdog.client.stub.dll";
#else
        const string CLIENT_DLL = "watchdog.client.dll";
#endif
        
        [DllImport(CLIENT_DLL)]
        static extern bool watchdog_client_initialize(WatchdogIntent intent);

        [DllImport(CLIENT_DLL)]
        static extern void watchdog_client_deinitialize();
        
        [DllImport(CLIENT_DLL)]
        static extern void watchdog_client_set_url( [MarshalAs(UnmanagedType.LPUTF8Str)] string lpString );

        [DllImport(CLIENT_DLL)]
        static extern void watchdog_client_tick();
        
        [DllImport(CLIENT_DLL)]
        static extern void watchdog_client_set_auth( [MarshalAs(UnmanagedType.LPUTF8Str)] string lpString , int stringLength );
        
        [DllImport(CLIENT_DLL)]
        static extern bool watchdog_client_peek_message([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] outBuffer, int length);

        protected Callback<GetTicketForWebApiResponse_t> OnWebApiTicketResponse;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            return;
            try
            {
                if ( !watchdog_client_initialize( intent ) )
                {
                    Debug.LogError("Anticheat initialize failed");
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    return;
                }
                else
                {
                    Debug.Log("Anticheat initialize success");
                }

                watchdog_client_set_url(EndPoint.AntiCheatDetection);

                SteamUser.GetAuthTicketForWebApi("anticheat");
                
                OnWebApiTicketResponse = Callback<GetTicketForWebApiResponse_t>.Create(OnWebApiTicketRespond);
            }
            catch ( DllNotFoundException )
            {
                Debug.LogError("Anticheat initialize failed");
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return;
            }
            
        }

        private void OnWebApiTicketRespond(GetTicketForWebApiResponse_t param)
        {
            if (param.m_eResult != EResult.k_EResultOK)
            {
                Debug.Log("Failed to get the web api ticket");
                return;
            }
            if (param.m_rgubTicket != null)
            {
                string ticket = BitConverter.ToString(param.m_rgubTicket).Replace("-", String.Empty) + " ";
                
                Debug.Log($"Set auth, ticket size = {ticket.Length}");
                
                watchdog_client_set_auth(ticket, ticket.Length - 1);
            }
        }

        private void Update()
        {
            return;
            watchdog_client_tick();
            
            if (NetworkManager.Instance.Client.IsConnected)
            {
                byte[] data = new byte[1024];
                if ( watchdog_client_peek_message( data, data.Length ) )
                {
                    // Debug.Log("Anticheat Message Found");
                    Message message = Message.Create(MessageSendMode.Reliable, (ushort) ClientToServerId.AntiCheatData) ;
            
                    message.Add(data);
            
                    NetworkManager.Instance.Client.Send(message); 
                }
            }
            

#if !UNITY_EDITOR
            if ( NetworkServerManager.Instance.Server.IsRunning )
            {
                foreach ( var client in NetworkServerManager.Instance.Server.Clients )
                {
                    if (Heartbeats.TryGetValue(client.Id, out var t))
                    {
                        if ( Time.time - t > 30 )
                        {
                            Debug.Log($"{client.Id} stop heartbeating, kicking now...");
                            //NetworkServerManager.Instance.Server.DisconnectClient(client.Id);
                        }
                    }
                }
            }
#endif
            
            
            
        }

        [MessageHandler((ushort)ClientToServerId.AntiCheatData,
            NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void CheckAntiCheatData(ushort fromClient,Message message)
        {
            byte[] bytes = message.GetBytes();
            
            string result = System.Text.Encoding.UTF8.GetString(bytes);

            AnticheatReport obj = JsonConvert.DeserializeObject<AnticheatReport>(result);

            foreach (var val in obj.reports)
            {
                switch (val.report_code)
                {
                    case var value when value == CE1:
                        NetworkServerManager.Instance.Server.DisconnectClient(fromClient);
                        
                        Debug.Log($"{fromClient} is using ce 1, kicking it now...");
                        return;
                    case var value when value == CE2:
                        NetworkServerManager.Instance.Server.DisconnectClient(fromClient);
                        
                        Debug.Log($"{fromClient} is using ce 2, kicking it now...");
                        return;
                    case var value when value == DLL1:
                        // NetworkServerManager.Instance.Server.DisconnectClient(fromClient);
                        
                        Debug.Log($"{fromClient} is using dll 1, stop kicking it now...");
                        return;
                    case var value when value == HANDLE1:
                        // NetworkServerManager.Instance.Server.DisconnectClient(fromClient);
                        
                        Debug.Log($"{fromClient} is using handle 1, kicking it now...");
                        return;
                    case var value when value == HEARTBEAT:
                        // Debug.Log($"Set {fromClient} heartbeat");
                        Instance.Heartbeats[fromClient] = Time.time;
                        return;
                }
            }
        }

        private void OnApplicationQuit()
        {
            //watchdog_client_deinitialize();
        }
    }
    
    [Serializable]
    public class AnticheatReport
    {
        public AnticheatData[] reports = Array.Empty<AnticheatData>();
        [Serializable]
        public class AnticheatData
        {
            public int report_code;
        }
    }
}
