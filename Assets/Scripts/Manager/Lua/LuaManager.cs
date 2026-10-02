using Multiplayer;
using Riptide;
using UnityEngine;

namespace Manager.Lua
{
    public class LuaManager : MonoBehaviour
    {
        [MessageHandler((ushort)ServerToClientId.CustomMessage, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void CustomMessageReceive(Message message)
        {
            string key = message.GetString();
            Debug.Log("Received a custom message (" + key + ") " + message.WrittenLength);
        }
    }
}