using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecureServer
{
    public class Roles
    {
        public List<ulong> Admins = new List<ulong>();
        public List<ulong> Helpers = new List<ulong>();
        public List<ulong> BananaMen = new List<ulong>();
        public List<ulong> DiscordMen = new List<ulong>();
    
        [JsonConstructor]
        public Roles(List<ulong> admins, List<ulong> helpers,List<ulong> bananaMen, List<ulong> discordMen)
        {
            Admins = admins;
            Helpers = helpers;
            BananaMen = bananaMen;
            DiscordMen = discordMen;
        }

        public Roles()
        {
        
        }
    }
}