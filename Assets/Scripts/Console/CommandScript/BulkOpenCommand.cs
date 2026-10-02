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
    [CreateAssetMenu(fileName = "BulkOpen Command",menuName = "Utilities/DeveloperConsole/Commands/BulkOpen Command")]
    public class BulkOpenCommand : ConsoleCommand
    {
        private DeveloperConsoleUI _console;
        private DeveloperConsoleUI Console
        {
            get
            {
                if (_console == null) return _console = DeveloperConsoleUI.Instance;
                return _console;
            }
        } 
        public override bool Process(string[] args)
        {
            if (args.Length != 3)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 3</color>");
                return false;
            }
            InventoryManager.Instance.CrateOpenQueue.Clear();

            string itemName = args[0];

            var crates = Console.crates;
            if (int.TryParse(args[1], out var amount)&&bool.TryParse(args[2], out var flag))
            {
                InventoryManager.Instance.CrateOpenAnimationEnable = flag;
                foreach (var crate in crates)
                {
                    if (crate.name == itemName)
                    {
                        foreach (var steamItemStored in InventoryManager.InventoryItems.Values)
                        {
                            if (steamItemStored.itemDetails.m_iDefinition.m_SteamItemDef == crate.itemdefid)
                            {
                                for (int i = 0; i < steamItemStored.amountGained; i++)
                                {
                                    InventoryManager.Instance.CrateOpenQueue.Enqueue(steamItemStored);
                                    --amount;
                                    if (amount <= 0) break;
                                }
                                if (amount <= 0) break;
                            }

                        }

                        InventoryManager.Instance.CheckBulkOpen();

                        return true;
                    }
                }
            
            }


            DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : Command not found</color>");
            return false;
        }
    }
}
