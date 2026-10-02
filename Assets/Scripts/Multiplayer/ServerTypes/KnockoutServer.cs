using Multiplayer.Interface;
using UnityEngine;

namespace Multiplayer.ServerTypes
{
    public class KnockoutServer : IServerType
    {
        public int Round { get; private set; } = 0;
        
        public float OverrideGameTime(float defaultTime)
        {
            NewRound();
            // y = 25x + -20t
            float timeFactor = 25f;
            float timeOffset = -20 * 10f;
            
            int playerCount = NetworkServerManager.GetAvailableClientCount();

            playerCount = Mathf.Clamp(playerCount, 12, 20);

            float time = playerCount * timeFactor + timeOffset;

            if (NetworkServerManager.ServerGameMode == GameMode.Infected)
            {
                time = defaultTime;
            }
            
            return time;
        }

        public bool GetEliminated()
        {
            return NetworkServerManager.IsPlaying;
        }

        public ushort OverrideMinimalPlayerCount(ushort defaultMinimalPlayerCount)
        {
            return 6;
        }

        void NewRound()
        {
            ++Round;
        }

        public void ResetRound()
        {
            Round = 0;
        }
    }
}