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

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "BulkStop Command",menuName = "Utilities/DeveloperConsole/Commands/BulkStop Command")]
    public class BulkStopCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            InventoryManager.Instance.CrateOpenQueue.Clear();
            return true;
        }
    }
}
