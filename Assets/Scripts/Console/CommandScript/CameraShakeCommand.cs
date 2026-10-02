using Movement;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "CameraShake Command",menuName = "Utilities/DeveloperConsole/Commands/CameraShake Command")]
    public class CameraShakeCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            if (args.Length < 1)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be 1</color>");
                return false;
            }

            float amplitude = 0;
            float frequency = 0;
            float duration = 1;

            if (!float.TryParse(args[0], out amplitude))
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : Failed to parse Arguments </color>");
                return false;
            }

            if (args.Length > 1)
            {
                if (float.TryParse(args[1], out frequency))
                {
                    if (args.Length > 2) float.TryParse(args[2], out duration);
                }
            }

            if (SpectateMovement.Instance)
            {
                SpectateMovement.Instance.CameraShake(amplitude,frequency,duration);
            }

            return true;
        }
    }
}
