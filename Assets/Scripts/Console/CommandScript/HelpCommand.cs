using System.Collections.Generic;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Help Command",menuName = "Utilities/DeveloperConsole/Commands/Help Command")]
    public class HelpCommand : ConsoleCommand
    {
        public List<Argument> helps = new List<Argument>();
        public override bool Process(string[] args)
        {
            if (args.Length != 1)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
                return false;
            }

            int index = 0;
            int.TryParse(args[0], out index);

            if (index > helps.Count - 1)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole($"<color=yellow>Console : pages only has {helps.Count}</color>");
                return false;
            }

            for (int i = 0; i < helps[index].args.Count; i++)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole(helps[index].args[i]);
            }

            return true;
        }
    }
}
