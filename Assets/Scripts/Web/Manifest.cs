using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SecureServer;
using UnityEngine;

namespace Web
{
    /// <summary>
    /// Staff roles and official servers. Lives in manifest.json at the repo root and is read from GitHub,
    /// so changing either is a pull request to main instead of a game update.
    /// </summary>
    public class Manifest
    {
        const string Url = "https://raw.githubusercontent.com/Squalive/banana-shooter/main/manifest.json";

        // Launch option to test roles or official servers before they're on main, e.g.
        // +manifest https://raw.githubusercontent.com/<fork>/banana-shooter/<branch>/manifest.json
        const string OverrideArg = "+manifest";

        [JsonProperty("roles")] public Roles Roles = new();
        [JsonProperty("officialServers")] public List<OfficialServer> OfficialServers = new();

        public class OfficialServer
        {
            [JsonProperty("steamId")] public ulong SteamId;
            [JsonProperty("name")] public string Name;
        }

        public static Manifest Current { get; private set; } = new();

        static Task _loading;

        /// <summary>
        /// Downloads once per launch. Falls back to the last copy that downloaded, so an outage keeps
        /// staff roles working; with no copy at all, nobody has a role and no server is official.
        /// </summary>
        public static Task Load() => _loading ??= LoadAsync();

        public bool IsOfficialServer(ulong steamId) => OfficialServers.Any(server => server.SteamId == steamId);

        static async Task LoadAsync()
        {
            string cachePath = Path.Combine(Application.persistentDataPath, "manifest.json");
            string url = OverrideUrl() ?? Url;

            string json = await HttpClient.GetText(url);
            if (Parse(json) is { } downloaded)
            {
                Current = downloaded;
                // A test manifest must not become the fallback for normal launches.
                if (url == Url) TryWriteCache(cachePath, json);
                else Debug.Log($"Using manifest from {url}");
                return;
            }

            if (File.Exists(cachePath) && Parse(File.ReadAllText(cachePath)) is { } cached)
            {
                Current = cached;
                Debug.LogWarning("Manifest download failed, using the cached copy");
            }
        }

        static string OverrideUrl()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, OverrideArg);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        static Manifest Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                return JsonConvert.DeserializeObject<Manifest>(json);
            }
            catch (JsonException e)
            {
                Debug.LogError($"Manifest is invalid: {e.Message}");
                return null;
            }
        }

        static void TryWriteCache(string path, string json)
        {
            try
            {
                File.WriteAllText(path, json);
            }
            catch (IOException e)
            {
                Debug.LogWarning($"Could not cache the manifest: {e.Message}");
            }
        }
    }
}
