using System;
using System.Collections.Generic;
using System.Linq;

namespace Console
{
    public class DeveloperConsole
    {
        private readonly IEnumerable<IConsoleCommand> commands;
        public DeveloperConsole(IEnumerable<IConsoleCommand> commands)
        {
            this.commands = commands;
        }

        List<string> list = new List<string>();
        public void ProcessCommand(string inputValue)
        {
            if (string.IsNullOrEmpty(inputValue)) return;
        
            string[] inputSplit = inputValue.Split(' ');

            string commandInput = inputSplit[0];
            string[] args = inputSplit.Skip(1).ToArray();
        
            list.Clear();
            foreach (var arg in args)
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    list.Add(arg);
                }
            }
            if(args.Length < 1 || string.IsNullOrEmpty(args[^1]))
                list.Add("");

            args = list.ToArray();

        
            ProcessCommand(commandInput,args);
        }

        public void ProcessCommand(string commandInput, string[] args)
        {
            bool processed = false;
            foreach (var command in commands)
            {
                if (!commandInput.Equals(command.CommandWord, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (command.Process(args))
                {
                    DeveloperConsoleUI.Instance.AddMessageToConsole("<b>></b> "+commandInput + " " + string.Join(' ', args));
                    return;
                }
                else
                {
                    processed = true;
                }
            }
            if(!processed)
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : Command not found</color>");
        }
    }
}
