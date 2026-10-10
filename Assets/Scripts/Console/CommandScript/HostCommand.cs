using Multiplayer;
using Multiplayer.Migration;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Host Command", menuName = "Utilities/DeveloperConsole/Commands/Host Command")]
    public class HostCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            if (!NetworkServerManager.Instance.Server.IsRunning || HostMigration.Instance == null)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : only the host can pass the host</color>");
                return false;
            }

            if (args.Length != 1)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
                return false;
            }

            foreach (var data in NetworkServerManager.ClientData.Values)
            {
                if (data.Name != args[0] && data.Id.ToString() != args[0]) continue;

                if (HostMigration.Instance.HandOffTo(data.Id))
                    return true;
                break;
            }

            DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : player not found</color>");
            return false;
        }
    }
}
