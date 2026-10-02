
using SecureServer;
using UnityEngine;
using Web;

namespace Manager
{
    public class RolesManager : MonoBehaviour
    {
        public static RolesManager Instance { private set; get; }

        public static bool Initialized = false;

        private Roles Roles;

        private void Awake()
        {
            Instance = this;
        }

        public async void TryToInitialize()
        {
            Roles = await HttpClient.Get<Roles>(EndPoint.GetRoles);

            if (Roles == null)
            {
                Roles = new Roles();

                Roles.Admins.Add(76561198983573782);
                Roles.Admins.Add(76561199122234463);
                Roles.Admins.Add(76561199219111958);
                Roles.Admins.Add(76561199249655111);
                Roles.Admins.Add(76561199105161625);
                Roles.Admins.Add(76561199005172531);
                Roles.Admins.Add(76561199006434997);
                Roles.Admins.Add(76561198988537123);

                Roles.Helpers.Add(76561199005172531);
                Roles.Helpers.Add(76561199118696695);
                Roles.Helpers.Add(76561199111933866);
                Roles.Helpers.Add(76561199093875295);
                Roles.Helpers.Add(76561199221010497);
        
                Roles.BananaMen.Add(76561198082478126);
                Roles.BananaMen.Add(76561198111628514);
                Roles.BananaMen.Add(76561199012777691);
                Roles.BananaMen.Add(76561199083002878);
                Roles.BananaMen.Add(76561199072165951);
                Roles.BananaMen.Add(76561198358284964);
                Roles.BananaMen.Add(76561198414875492);
                Roles.BananaMen.Add(76561198985014584);
                Roles.BananaMen.Add(76561199221216028);

                Roles.DiscordMen.Add(76561198127619402);
                Roles.DiscordMen.Add(76561199065617028);
                
                Debug.LogError("Roles Request Failed, Use local hardcoded roles");
            }

            Initialized = true;
        }

        public bool CheckIsAdmin(ulong id)
        {
            return Roles.Admins.Contains(id);
        }
        
        public bool CheckIsHelper(ulong id)
        {
            return Roles.Helpers.Contains(id);
        }
        
        public bool CheckIsBananaMan(ulong id)
        {
            return Roles.BananaMen.Contains(id);
        }
        
        public bool CheckIsDiscordMan(ulong id)
        {
            return Roles.DiscordMen.Contains(id);
        }
    }
}
