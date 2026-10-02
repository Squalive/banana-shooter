
using System.Collections;
using Riptide;
using UnityEngine;
using Weapon;

namespace Multiplayer.Interface
{
    public interface INetworkServer : INetworkServerPacketSent
    {
        void StopServer();
        
        #region Server Callback Function

        void NewPlayerConnect(object sender, ServerConnectedEventArgs e);
        void ClientDisconnect(object sender, ServerDisconnectedEventArgs e);

        #endregion
        

        #region Game Logic

        void StopGame();
        
        void ShootLagCompensation(uint tick, Vector3 lookDir,Vector3 raycastPos,ushort fromClient, ActiveWeapon weapon);

        #endregion
        
        #region Returns of the server properties

        /// <summary>
        /// Get the Available player count
        /// </summary>
        /// <returns></returns>
        int GetAvailablePlayerCount();

        /// <summary>
        /// Check is it a team mode
        /// </summary>
        /// <returns></returns>
        bool IsTeamMode();

        #endregion

        #region Set the server properties outside

        void SetGameState(GameState state,bool send=true);

        void ResetProperties();

        #endregion
    }
}