using Multiplayer;
using Riptide;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Display Tag Command",menuName = "Utilities/DeveloperConsole/Commands/Display Tag Command")]
    public class DisplayTagCommand : ConsoleCommand
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

            bool t = args[0].ToLower() == "true";
            NetworkManager.Instance.displayTag = t;
            PlayerPrefs.SetInt("display_tag",t ? 1:0);
            PlayerPrefs.Save();

            if (NetworkManager.Instance.Client.IsConnected)
            {
                Message message = Message.Create(MessageSendMode.Reliable,(ushort) ClientToServerId.RequestData);
                message.Add((ushort) RequestDataType.DisplayTag);
                message.Add(t);
                NetworkManager.Instance.SendByte += message.WrittenLength;
                NetworkManager.Instance.Client.Send(message);
            }
            
            return true;
        }
    }
}
