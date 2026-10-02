
using Manager;
using UnityEngine;
namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Noclip Command",menuName = "Utilities/DeveloperConsole/Commands/Noclip Command")]
    public class NoclipCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            return FunctionUtils.NoClip();
        }
        
    }
}
