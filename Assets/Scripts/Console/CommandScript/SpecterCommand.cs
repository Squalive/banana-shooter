using Manager;
using Multiplayer;
using Multiplayer.Interface;
using Riptide;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Specter Command",menuName = "Utilities/DeveloperConsole/Commands/Specter Command")]
    public class SpecterCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            if (!NetworkServerManager.Instance.Server.IsRunning)
            {
                if (!RolesManager.Instance.CheckIsAdmin(NetworkManager.Instance.steamId.m_SteamID))
                {
                    DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : Server is not running or you are not a admin/color>");
                    return false;
                }
            }
        
            if (args.Length != 1)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
                return false;
            }

            if (bool.TryParse(args[0], out var i))
            {
                bool flag = i;
            
                Message message = Message.Create(MessageSendMode.Reliable,(ushort) ClientToServerId.SpecterMode);

                message.Add(flag);
                NetworkManager.Instance.SendByte += message.WrittenLength;
            
                NetworkManager.Instance.Client.Send(message);
                return true;
            }

            return false;
        }
    }
}
