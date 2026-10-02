
using Manager;
using Multiplayer;
using Multiplayer.Interface;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "EnableCheat Command",menuName = "Utilities/DeveloperConsole/Commands/EnableCheat Command")]
    public class EnableCheatCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            
            if (NetworkServerManager.Instance.Server.IsRunning)
            {
                if (args.Length < 1)
                {
                    Debug.LogError("There should be only 1 argument");
                    return false;
                }

                if (bool.TryParse(args[0], out var flag))
                {
                    NetworkServerManager.Instance.SetCheatsEnabled(flag);
                    return true;
                }

                Debug.LogError("failed to read the argument");
                return false;
            }
            
            Debug.LogError("sv_cheats is read only");
            
            return false;
        }
    }
}
