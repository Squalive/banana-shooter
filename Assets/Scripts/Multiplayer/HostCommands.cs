using System;
using System.Collections.Generic;
using System.Linq;
using Manager;
using Riptide;
using UnityEngine;

namespace Multiplayer
{
    /// <summary>
    /// Chat commands for server hosts, the Steam IDs in the dedicated server's config. Runs on the server,
    /// so players don't need a newer client to use them.
    /// </summary>
    public static class HostCommands
    {
        public static readonly HashSet<ulong> Hosts = new HashSet<ulong>();

        const string Help = "Host commands: /players, /kick <player>, /ban <player>, /bans, /unban <player or steamid>. <player> is a name or the # from /players.";

        public static bool IsHost(ulong steamId) => Hosts.Contains(steamId) || RolesManager.Instance.CheckIsAdmin(steamId);

        /// <summary>Runs a host's chat line that starts with '/'. Returns false for anything that should go to chat.</summary>
        public static bool TryRun(ushort fromClient, string text)
        {
            if (!text.StartsWith("/") || !NetworkServerManager.TryGetAuthorizedSteamId(fromClient, out var steamId) || !IsHost(steamId))
                return false;

            string name = NetworkServerManager.ClientData.TryGetValue(fromClient, out var self) ? self.Name : fromClient.ToString();
            Debug.Log($"[Host] {name} ({steamId}): {text}");

            string[] parts = text.Substring(1).Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string command = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
            string arg = parts.Length > 1 ? parts[1].Trim() : "";

            switch (command)
            {
                case "players":
                    Reply(fromClient, "Players: " + string.Join(", ", NetworkServerManager.ClientData.Values.Select(p => $"#{p.Id} {p.Name}")));
                    break;
                case "kick":
                case "ban":
                    if (!TryFindPlayer(arg, out var target))
                    {
                        Reply(fromClient, $"No player '{arg}'. Use a name or the # from /players.");
                        break;
                    }
                    if (IsHost(target.SteamId))
                    {
                        Reply(fromClient, "You can't kick or ban a host.");
                        break;
                    }

                    string reason = "Kicked by the host";
                    if (command == "ban")
                    {
                        reason = "Banned by the host";
                        NetworkServerManager.Bans.Add(target.SteamId, target.Name, reason);
                    }
                    Debug.Log($"[Host] {name} {(command == "ban" ? "banned" : "kicked")} {target.Name} ({target.SteamId})");
                    NetworkServerManager.Instance.Server.DisconnectClient(target.Id, NetworkServerManager.GetDisconnectMessage(reason));
                    Reply(fromClient, $"{(command == "ban" ? "Banned" : "Kicked")} {target.Name}");
                    break;
                case "bans":
                    var bans = NetworkServerManager.Bans.Active();
                    Reply(fromClient, bans.Count == 0
                        ? "Nobody is banned."
                        : "Bans: " + string.Join(", ", bans.Select(b => $"{b.Name ?? "?"} ({b.SteamId}) {ServerBans.Describe(b)}")));
                    break;
                case "unban":
                    if (!NetworkServerManager.Bans.TryFind(arg, out var ban))
                    {
                        Reply(fromClient, $"No ban for '{arg}'. See /bans.");
                        break;
                    }
                    NetworkServerManager.Bans.Remove(ban.SteamId);
                    Debug.Log($"[Host] {name} unbanned {ban.Name} ({ban.SteamId})");
                    Reply(fromClient, $"Unbanned {ban.Name ?? ban.SteamId.ToString()}");
                    break;
                default:
                    Reply(fromClient, Help);
                    break;
            }

            return true;
        }

        static bool TryFindPlayer(string idOrName, out ClientData player)
        {
            string id = idOrName.TrimStart('#');
            player = NetworkServerManager.ClientData.Values.FirstOrDefault(p => p.Id.ToString() == id)
                     ?? NetworkServerManager.ClientData.Values.FirstOrDefault(p => string.Equals(p.Name, idOrName, StringComparison.OrdinalIgnoreCase));
            return player != null && idOrName != "";
        }

        /// <summary>System message in one player's chat (green, no sender).</summary>
        static void Reply(ushort client, string text)
        {
            var message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.SendMessage);
            message.Add((ushort)0);
            message.Add(0UL);
            message.Add(text);
            message.Add(1);
            NetworkServerManager.Instance.Server.Send(message, client);
        }
    }
}
