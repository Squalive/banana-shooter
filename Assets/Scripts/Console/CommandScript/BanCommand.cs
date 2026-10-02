using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Riptide;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Ban Command",menuName = "Utilities/DeveloperConsole/Commands/Ban Command")]
    public class BanCommand : ConsoleCommand
    {
        public ManageType type = ManageType.Kick;
        public override bool Process(string[] args)
        {
            if (args.Length != 1)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
                return false;
            }

            ushort id = 0;
            ulong steamId = 0;
            foreach (var clientPlayer in ClientPlayer.list.Values)
            {
                if (clientPlayer.playerState.Username == args[0])
                {
                    id = clientPlayer.Id;
                    steamId = clientPlayer.playerState.SteamId;

                    break;
                }
            }

            if (id != 0)
            {
                Message message = Message.Create(MessageSendMode.Reliable, (ushort) ClientToServerId.ManageServer);

                message.Add((ushort) type);
                message.Add(id);
                if(type==ManageType.Ban)
                    message.Add(steamId);

                NetworkManager.Instance.SendByte += message.WrittenLength;
                NetworkManager.Instance.Client.Send(message);
                return true;
            }

            DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : Command not found</color>");
            return false;
        }
    }
}
