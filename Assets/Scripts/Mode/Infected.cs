using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Server;
using Multiplayer.Interface;
using Multiplayer.Server;
using Riptide;
using Steamworks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Mode
{
    public class Infected : GameModes
    {
        public static Infected Instance;

    
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.Infected)
            {
                enabled = false;
                return;
            }
            Instance = this;
        
            if(NetworkServerManager.Instance.Server.IsRunning)
                leftTime = Mathf.Max(100, 15 * NetworkServerManager.Instance.GetAvailablePlayerCount());
        
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap!=null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.Infected.ToString(), sceneName);
            LobbyManager.Instance.SetLobbyGameMode();

            gameStart += StartInfected;

            Tutorial.Instance.SetText("InfectedTip");
        }

        void StartInfected(){
            if(NetworkServerManager.Instance.Server.IsRunning)
                StartCoroutine(FirstInfect());
        }

        private void OnDisable()
        {
            gameStart -= StartInfected;
        }

        public bool infected = false;
        IEnumerator FirstInfect()
        {
            ushort time = 15;
            while (time>0)
            {
                Message message = Message.Create(MessageSendMode.Unreliable,(ushort)ServerToClientId.Message);

                message.Add((ushort)MessageType.Infect);
                message.Add(time);
            
                NetworkServerManager.Instance.Server.SendToAll(message);
                time--;
                yield return new WaitForSeconds(1f);
            }

            var players = ServerPlayer.list.Values.Where(e => !e.Eliminated).ToList();

            List<ushort> ids = new List<ushort>();
            int len = 1;
            int count = players.Count;
            if (count >= 10 && count < 20)
            {
                len = 2;
            }
            else if (count >= 20 && count < 30)
            {
                len = 4;
            }
            else if (count >= 30)
            {
                len = 6;
            }
            
            if (count<= 1)
            {
                infected=true;
                yield break;
            }
            
            while (ids.Count < len)
            {
                ushort id = players[Random.Range(0,count)].Id;
                if(!ids.Contains(id))
                    ids.Add(id);
            }

            foreach (var id in ids)
            {
                if(ServerPlayer.list.ContainsKey(id) && NetworkServerManager.Instance.SetInfectedPlayer(id,id))
                    Debug.Log($"Player {id} gets infect");
                else
                    Debug.Log($"Player {id} infect failed");
            }

            infected = true;
        }
    }
}
