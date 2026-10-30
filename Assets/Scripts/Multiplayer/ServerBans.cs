using System;
using System.Collections.Generic;
using System.Linq;

namespace Multiplayer
{
    /// <summary>
    /// Steam IDs that may not join this server. A ban without an expiry lasts until the server stops.
    /// </summary>
    public class ServerBans
    {
        public readonly struct Ban
        {
            public readonly ulong SteamId;
            public readonly string Name;
            public readonly DateTime? Expires;
            public readonly string Reason;

            public Ban(ulong steamId, string name, DateTime? expires, string reason)
            {
                SteamId = steamId;
                Name = name;
                Expires = expires;
                Reason = reason;
            }
        }

        readonly Dictionary<ulong, Ban> _bans = new Dictionary<ulong, Ban>();

        public void Add(ulong steamId, string name, string reason, TimeSpan? duration = null, DateTime? now = null)
        {
            DateTime? expires = duration.HasValue ? (now ?? DateTime.UtcNow) + duration.Value : (DateTime?)null;
            _bans[steamId] = new Ban(steamId, name, expires, reason);
        }

        public bool TryGet(ulong steamId, out Ban ban, DateTime? now = null)
        {
            if (!_bans.TryGetValue(steamId, out ban)) return false;
            if (ban.Expires == null || ban.Expires > (now ?? DateTime.UtcNow)) return true;

            _bans.Remove(steamId);
            return false;
        }

        public bool Remove(ulong steamId) => _bans.Remove(steamId);

        /// <summary>Finds an active ban by Steam ID or by the banned player's name, ignoring case.</summary>
        public bool TryFind(string steamIdOrName, out Ban ban, DateTime? now = null)
        {
            foreach (var b in Active(now))
            {
                if (b.SteamId.ToString() == steamIdOrName || string.Equals(b.Name, steamIdOrName, StringComparison.OrdinalIgnoreCase))
                {
                    ban = b;
                    return true;
                }
            }

            ban = default;
            return false;
        }

        public void Clear() => _bans.Clear();

        public List<Ban> Active(DateTime? now = null)
        {
            var time = now ?? DateTime.UtcNow;
            foreach (var expired in _bans.Values.Where(b => b.Expires <= time).ToList())
                _bans.Remove(expired.SteamId);
            return _bans.Values.ToList();
        }

        public static string Describe(Ban ban, DateTime? now = null)
        {
            if (ban.Expires == null) return "until the server restarts";
            var left = ban.Expires.Value - (now ?? DateTime.UtcNow);
            int minutes = Math.Max(1, (int)Math.Ceiling(left.TotalMinutes));
            return minutes == 1 ? "for 1 more minute" : $"for {minutes} more minutes";
        }
    }
}
