using Multiplayer;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Connect Command",menuName = "Utilities/DeveloperConsole/Commands/Connect Command")]
    public class ConnectCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            if (args.Length != 1)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
                return false;
            }

            if (NetworkManager.Instance.Client.IsConnected)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : Client has already connected</color>");
                return false;
            }
            
            ServerManager.Instance.Connect(args[0]);
            
            return false;
        }
    }
}
