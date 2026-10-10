
using SecureServer;
using UnityEngine;
using Web;

namespace Manager
{
    public class RolesManager : MonoBehaviour
    {
        public static RolesManager Instance { private set; get; }

        public static bool Initialized = false;

        private Roles Roles = new Roles();

        private void Awake()
        {
            Instance = this;
            Initialized = false;
        }

        public async void TryToInitialize()
        {
            try
            {
                await Manifest.Load();
                Roles = Manifest.Current.Roles ?? new Roles();
            }
            finally
            {
                Initialized = true;
            }
        }

        public bool CheckIsAdmin(ulong id)
        {
            return Roles?.Admins?.Contains(id) == true;
        }

        public bool CheckIsHelper(ulong id)
        {
            return Roles?.Helpers?.Contains(id) == true;
        }

        public bool CheckIsBananaMan(ulong id)
        {
            return Roles?.BananaMen?.Contains(id) == true;
        }

        public bool CheckIsDiscordMan(ulong id)
        {
            return Roles?.DiscordMen?.Contains(id) == true;
        }
    }
}
