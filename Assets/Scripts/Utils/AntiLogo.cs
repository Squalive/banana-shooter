using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Utils
{
    public class AntiLogo
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Run()
        {
            Task.Run(() =>
            {
                SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
            });
        }
    }
}