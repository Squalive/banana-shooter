using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cosmetic;
using Manager;
using Multiplayer;
using Multiplayer.Client;
using Riptide;
using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Bind Command",menuName = "Utilities/DeveloperConsole/Commands/Bind Command")]
    public class BindCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            if (args.Length < 3)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should at least be 2</color>");
                return false;
            }
            
            string command = String.Empty;

            for (int i = 2; i < args.Length; i++)
            {
                string a = args[i];
                if(i + 1 < args.Length) a+= " ";
                command += a;
            }
            
            var customAction = new InputAction(args[0], InputActionType.Button, $"<keyboard>/{args[1]}");

            customAction.performed += _ =>
            {
                if(!FunctionUtils.IsBlocked())
                    DeveloperConsoleUI.Instance.DeveloperConsole.ProcessCommand(command);
            };
                
            customAction.Enable();
                
            GameManager.Instance.AddCustomBinding(args[0],command,args[1],customAction,true);

            return true;
        }
    }
}
