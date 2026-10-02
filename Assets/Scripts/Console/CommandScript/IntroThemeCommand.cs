using Manager;
using Multiplayer;
using Riptide;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Intro Theme Command",menuName = "Utilities/DeveloperConsole/Commands/Intro Theme Command")]
    public class IntroThemeCommand : ConsoleCommand
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
            if (args.Length > 1)
            {
                Console.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
                return false;
            }

            if (int.TryParse(args[0], out var index))
            {
                GameManager.Instance.introTheme = index;
                PlayerPrefs.SetInt("intro_theme",index);
                PlayerPrefs.Save();
            }
            
            return true;
        }
    }
}
