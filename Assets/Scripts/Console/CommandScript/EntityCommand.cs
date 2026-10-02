using Manager;
using Multiplayer;
using Multiplayer.Interface;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Entity Command",menuName = "Utilities/DeveloperConsole/Commands/Entity Command")]
    public class EntityCommand : ConsoleCommand
    {
        private DeveloperConsoleUI console;
        private DeveloperConsoleUI Console
        {
            get
            {
                if (console == null) return console = DeveloperConsoleUI.Instance;
                return console;
            }
        } 
    
        public override bool Process(string[] args)
        {
            if (!NetworkServerManager.Instance.Server.IsRunning)
            {
                Console.AddMessageToConsole("<color=yellow>Console : You are not the host/color>");
                return false;
            }
            if (args.Length < 1)
            {
                Console.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
                return false;
            }

            if (args[0] == "clear")
            {
                foreach (var entity in GameManager.Entities)
                {
                    Destroy(entity);
                }
                GameManager.Entities.Clear();
                return true;
            }
            return false;
        }
    }
}
