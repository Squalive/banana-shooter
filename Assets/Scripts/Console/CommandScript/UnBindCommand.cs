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
    [CreateAssetMenu(fileName = "UnBind Command",menuName = "Utilities/DeveloperConsole/Commands/UnBind Command")]
    public class UnBindCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            if (args.Length != 1)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
                return false;
            }
            
            //unbind <name>

            string bindingName = args[0];
            
            GameManager.Instance.RemoveCustomBinding(bindingName);
            
            return true;
        }
    }
}
