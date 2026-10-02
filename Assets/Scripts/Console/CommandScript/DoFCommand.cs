using System.Linq;
using Movement;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "DoF Command",menuName = "Utilities/DeveloperConsole/Commands/DoF Command")]
    public class DoFCommand : ConsoleCommand
    {
        private readonly string[] _intMethod = { "mode" };
        public override bool Process(string[] args)
        {
            if (args.Length <= 1)
            {
                if (bool.TryParse(args[0], out var flag))
                {
                    SpectateMovement.Instance.SetDof(flag);
                }
            }
            else if (args.Length > 1)
            {
                if (_intMethod.Contains(args[0]))
                {
                    if (int.TryParse(args[1], out var iValue))
                    {
                        switch (args[0])
                        {
                            case "mode":
                                SpectateMovement.Instance.SetDofMode(iValue);
                                return true;
                        }
                    }
                }
                else if (float.TryParse(args[1], out var fValue))
                {
                    switch (args[0])
                    {
                        case "aper":
                            SpectateMovement.Instance.SetDoFAperture(fValue);
                            return true;
                        case "blur":
                            SpectateMovement.Instance.SetDoFBlur(fValue);
                            return true;
                        case "distance":
                            SpectateMovement.Instance.SetDofDistance(fValue);
                            return true;
                        case "nudge":
                            SpectateMovement.Instance.NudgeDofDistance(fValue);
                            return true;
                    }
                }
            }
            
            
            
            return false;
        }
    }
}
