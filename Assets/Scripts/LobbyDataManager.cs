
using System;
using System.Collections.Generic;
using UnityEngine;

public class LobbyDataManager : MonoBehaviour
{
    public static LobbyDataManager Instance;

    public static Dictionary<ulong, LobbyData> datas = new Dictionary<ulong, LobbyData>();

    private void Awake()
    {
        if(Instance==null)
            Instance = this;
    }

    [Serializable]
    public class LobbyData 
    {
        public ulong playerId;
        public string playerName;
        public uint kills = 0, deaths = 0;

        public uint exp=0;
        public LobbyData(ulong id, string name, uint kills, uint deaths,uint xp)
        {
            playerId = id;
            playerName = name;
            this.kills = kills;
            this.deaths = deaths;
            exp = xp;
        }

        public void UpdateKill()
        {
            kills++;
        }

        public void UpdateDeaths()
        {
            deaths++;
        }
    }
    
    public class CompareLobbyDataByKill : IComparer<LobbyData>
    {
        public int Compare(LobbyData x, LobbyData y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            if (x.kills > y.kills) return -1;
            if (x.kills < y.kills) return 1;
            return 0;
        }
    }
}
