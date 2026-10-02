using System.Collections.Generic;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Debug Command",menuName = "Utilities/DeveloperConsole/Commands/Debug Command")]
    public class DebugCommand : ConsoleCommand
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

        private readonly HashSet<string> _stringVarFunction = new () {"save", "load", "record"};

        public override bool Process(string[] args)
        {
            // if (args.Length != 2)
            // {
            //     if (args.Length == 1)
            //     {
            //         if (args[0] == "clear")
            //         {
            //             DeveloperConsoleUI.Instance.ClearConsole();
            //             return true;
            //         }
            //         else
            //         {
            //             DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 2</color>");
            //             return false;
            //         }
            //     }
            //     else
            //     {
            //         DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 2</color>");
            //         return false;
            //     }
            //     
            // }

            if (args.Length > 1)
            {
                if (bool.TryParse(args[1], out var flag))
                {
                    switch (args[0])
                    {
                        case "log":
                            Console.enableLog = flag;
                            Debug.unityLogger.logEnabled =
                                console.enableError || console.enableLog || console.enableWarning;
                            return true;
                        case "error":
                            Console.enableError = flag;
                            Debug.unityLogger.logEnabled =
                                console.enableError || console.enableLog || console.enableWarning;
                            return true;
                        case "warning":
                            Console.enableWarning = flag;
                            Debug.unityLogger.logEnabled =
                                console.enableError || console.enableLog || console.enableWarning;
                            return true;
                        case "network":
                            Fps.Instance.enableNetworkingStats = flag;
                            Fps.Instance.fps.SetText("");
                            return true;
                        case "memory":
                            Fps.Instance.enableMemoryStatics = flag;
                            Fps.Instance.fps.SetText("");
                            return true;
                    }
                }
            }

            DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : Command not found</color>");
            return false;
        }
    }
}
