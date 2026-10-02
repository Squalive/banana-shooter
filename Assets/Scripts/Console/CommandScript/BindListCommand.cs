
using Manager;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "BindList Command",menuName = "Utilities/DeveloperConsole/Commands/BindList Command")]
    public class BindListCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            //unbind <name>
            int i = 1;
            
            foreach (var pair in GameManager.CustomInputActions)
            {
                Debug.Log($"- {i}. Name: {pair.Key}, Key: {pair.Value.Item2}, Command: {pair.Value.Item1}");
                i++;
            }
            
            return true;
        }
    }
}
