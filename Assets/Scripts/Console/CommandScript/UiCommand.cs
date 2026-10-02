
using System;
using Console;
using Menu;
using UnityEngine;

[CreateAssetMenu(fileName = "Ui Command",menuName = "Utilities/DeveloperConsole/Commands/Ui Command")]
public class UiCommand : ConsoleCommand
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
        GameUIManager manager = GameUIManager.Instance;
        if (!manager)
        {
            Console.AddMessageToConsole("<color=yellow>Console : You are not in game/color>");
            return false;
        }
        if (args.Length < 2)
        {
            Console.AddMessageToConsole("<color=yellow>Console : arguments should be 2</color>");
            return false;
        }

        if (args[0] == "game")
        {
            if (int.TryParse(args[1], out var a))
            {
                manager.desiredGameAlpha = a;
                manager.controlByConsole = a != 1;
            }
            return true;
        }
        return false;
    }
}
