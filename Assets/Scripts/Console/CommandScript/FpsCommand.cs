
using Console;
using UnityEngine;
[CreateAssetMenu(fileName = "Fps Command",menuName = "Utilities/DeveloperConsole/Commands/Fps Command")]
public class FpsCommand : ConsoleCommand
{
    public override bool Process(string[] args)
    {
        if (args.Length != 1)
        {
            DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
            return false;
        }

        if (bool.TryParse(args[0], out var i))
        {
            Fps.Instance.enable = i;
            Fps.Instance.fps.SetText("");
            return true;
        }

        return false;
    }
}
