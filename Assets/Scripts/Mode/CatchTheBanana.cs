using System;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Multiplayer;
using Multiplayer.Entity.Interface;
using Multiplayer.Entity.Server;
using Multiplayer.Interface;
using Multiplayer.Server;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Mode
{
    public class CatchTheBanana : GameModes
    {
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.CatchTheBanana)
            {
                enabled = false;
                return;
            }
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap != null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.CatchTheBanana.ToString(), sceneName);
            LobbyManager.Instance.SetLobbyGameMode();
        }

        private void OnEnable()
        {
            gameStart += SetBanana;
        }

        private void OnDisable()
        {
            gameStart -= SetBanana;
        }

        void SetBanana()
        {
            int playerAmount = NetworkServerManager.Instance.GetAvailablePlayerCount();

            float ratio = .3f;

            if (playerAmount <= 2)
            {
                ratio = 0.5f;
            }
            else if (playerAmount <= 10)
            {
                ratio = 0.3f;
            }
            else if (playerAmount <= 20)
            {
                ratio = 0.4f;
            }

            int f = (int)(ratio * playerAmount);
            int l = Mathf.Clamp(f, 1, f);
            List<IPlayerServer> players = ServerPlayer.list.Values.Where(p => !p.Eliminated).ToList();
            for (int i = 0; i < l && players.Count > 0; i++)
            {
                int index = Random.Range(0, players.Count);
                players[index].SetHasBanana(true);
                players.RemoveAt(index);
            }
        }
    }
}
