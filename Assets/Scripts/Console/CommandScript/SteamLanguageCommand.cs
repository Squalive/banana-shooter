using Manager;
using Menu;
using Multiplayer;
using Steamworks;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "SteamLanguage Command",menuName = "Utilities/DeveloperConsole/Commands/SteamLanguage Command")]
    public class SteamLanguageCommand : ConsoleCommand
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
                bool flag = i;
                GameManager.Instance.setting.useSteamLanguage = flag;

                SettingMenu menu = SettingMenu.Instance;
                if (menu)
                {
                    menu.languageNext.interactable = !flag;
                    menu.languagePreview.interactable = !flag;
                
                    if (flag)
                    {
                        string language = SteamApps.GetCurrentGameLanguage();

                        switch (language)
                        {
                            case "english":
                                LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[1]);
                                break;
                            case "schinese":
                                LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[0]);
                                break;
                            case "russian":
                                LocalizationSettings.Instance.SetSelectedLocale(NetworkManager.Instance.language[3]);
                                break;
                        }
                    }
                
                }
                return true;
            }

            return false;
        }
    }
}
