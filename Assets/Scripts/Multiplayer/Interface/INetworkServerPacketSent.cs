using System.Collections;

namespace Multiplayer.Interface
{
    public interface INetworkServerPacketSent
    {
        /// <summary>
        /// Send the server tickrate to the client for checking
        /// </summary>
        void SendSync();
        /// <summary>
        /// Start round for all connnecting clients
        /// </summary>
        void StartRound();
        /// <summary>
        /// Start round for specific client
        /// </summary>
        /// <param name="toClient"></param>
        void StartRound(ushort toClient);

        bool SetInfectedPlayer(ushort id, ushort fromClient);
        void BananaManMusic();

        /// <summary>
        /// Call this when a client has fully initialized
        /// </summary>
        void SendClientInitialized(ushort client);
    }
}