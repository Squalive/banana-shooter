using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Manager;
using Multiplayer;
using Multiplayer.Entity.Server;
using Save;
using SecureServer;
using UnityEngine;
using UnityEngine.Networking;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

partial class Program
{
    static int failures, passes;
    static async Task<int> Main(string[] args)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        await Test("shorter binary write removes old bytes", async () => {
            var p = Temp(); File.WriteAllBytes(p, new byte[] { 1, 2, 3, 4 });
            await SaveSystem.WriteToFileAsync(p, new byte[] { 9 });
            Equal(new byte[] { 9 }, File.ReadAllBytes(p));
        });
        await Test("thread writer truncates shorter file", async () => {
            var p = Temp(); File.WriteAllBytes(p, new byte[] { 1, 2, 3, 4 });
            await SaveSystem.WriteToFileAsyncThread(p, new byte[] { 9 });
            Equal(new byte[] { 9 }, File.ReadAllBytes(p));
        });
        await Test("GET protocol errors return no data", async () => {
            Http(UnityWebRequest.Result.ProtocolError, "{\"Admins\":[123]}");
            Check(await Web.HttpClient.Get<Roles>("https://fixture.invalid") == null, "error body was treated as trusted data");
            Check(UnityWebRequest.Last.Disposed, "request leaked");
        });
        await Test("GET invalid JSON returns no data", async () => {
            Http(UnityWebRequest.Result.Success, "<html>unavailable</html>");
            Check(await Web.HttpClient.Get<Roles>("https://fixture.invalid") == null, "malformed response returned data");
            Check(UnityWebRequest.Last.Disposed, "request leaked");
        });
        await Test("GET success preserves valid data", async () => {
            Http(UnityWebRequest.Result.Success, "{\"Admins\":[123]}");
            var roles = await Web.HttpClient.Get<Roles>("https://fixture.invalid");
            Check(roles.Admins.Count == 1 && roles.Admins[0] == 123, "valid data was lost");
            Check(UnityWebRequest.Last.Disposed, "request leaked");
        });
        await Test("POST errors return failure and have timeout", async () => {
            Http(UnityWebRequest.Result.ProtocolError, "error");
            Check(await Web.HttpClient.Post("https://fixture.invalid", new { test = true }) == null, "error POST looked successful");
            Check(UnityWebRequest.Last.timeout > 0, "POST has no timeout");
            Check(UnityWebRequest.Last.Disposed, "request leaked");
        });
        await Test("failed role request grants no inherited administrators", async () => {
            Http(UnityWebRequest.Result.ConnectionError, "");
            var manager = new RolesManager(); Invoke(manager, "Awake"); manager.TryToInitialize();
            await Until(() => RolesManager.Initialized);
            Check(!manager.CheckIsAdmin(76561198983573782), "original author's account retained administrative access");
        });
        await Test("missing role arrays deny access", () => {
            var manager = new RolesManager(); Set(manager, "Roles", new Roles(null, null, null, null));
            Check(!manager.CheckIsAdmin(1) && !manager.CheckIsHelper(1) && !manager.CheckIsBananaMan(1) && !manager.CheckIsDiscordMan(1), "missing roles granted access");
            return Task.CompletedTask;
        });
        await Test("multiple players' shots processed in same physics step", () => {
            var manager = Shots(); var a = Player(1); var b = Player(2);
            InvokeStatic(typeof(LagCompensationManager), "Shoot", (ushort)1, new Riptide.Message());
            InvokeStatic(typeof(LagCompensationManager), "Shoot", (ushort)2, new Riptide.Message());
            Invoke(manager, "FixedUpdate");
            Check(a.Weapon.Attacks == 1 && b.Weapon.Attacks == 1 && NetworkServerManager.Instance.Hits == 2, "shots remained queued");
            return Task.CompletedTask;
        });
        await Test("disconnected player's queued shot does not consume ammo or broadcast", () => {
            var manager = Shots(); var p = Player(1);
            InvokeStatic(typeof(LagCompensationManager), "Shoot", (ushort)1, new Riptide.Message());
            NetworkServerManager.Instance.Server.Connected.Clear(); Invoke(manager, "FixedUpdate");
            Check(p.Weapon.Attacks == 0 && NetworkServerManager.Instance.Server.Sent == 0, "disconnected player shot was processed");
            return Task.CompletedTask;
        });
        await AdditionalTests();
        await SecurityTests();
        await ProjectChecks();
        System.Console.WriteLine($"{passes} passed, {failures} failed");
        return failures == 0 ? 0 : 1;
    }
    static async Task AdditionalTests()
    {
        await Test("failed writes complete with an exception", async () => {
            var invalid = Path.Combine(Temp(), "missing", "save");
            var task = SaveSystem.WriteToFileAsyncThread(invalid, new byte[] { 1 });
            Check(await Task.WhenAny(task, Task.Delay(2000)) == task, "write hung");
            Check(task.IsFaulted, "write failure was lost");
            try { await task; } catch (IOException) { }
        });
        await Test("callback write truncates and completes once", async () => {
            var p = Temp(); File.WriteAllBytes(p, new byte[] { 1, 2, 3 });
            var done = new TaskCompletionSource<bool>(); int calls = 0;
            SaveSystem.WriteToFileAsyncThread(p, new byte[] { 7 }, () => { calls++; done.TrySetResult(true); });
            Check(await Task.WhenAny(done.Task, Task.Delay(2000)) == done.Task, "callback write hung");
            Check(calls == 1, "callback invoked more than once"); Equal(new byte[] { 7 }, File.ReadAllBytes(p));
        });
        await Test("save formats round trip existing data types", async () => {
            foreach (var value in new object[] { new DateTime(2026, 10, 3), new[] { 2, 4 }, new[] { true, false }, new short[] { 1, 3 }, new[] { "a", "b" }, new List<bool> { true, false } })
            {
                Check(await SaveSystem.SaveDataAsync("round-trip", value), "save failed");
                var routine = SaveSystem.LoadBinaryDataAsync("round-trip"); object result = null;
                while (routine.MoveNext()) { result = routine.Current; await Task.Delay(1); }
                Check(result != null && result.GetType() == value.GetType(), "existing save type changed");
                Check(Newtonsoft.Json.JsonConvert.SerializeObject(result) == Newtonsoft.Json.JsonConvert.SerializeObject(value), "save contents changed");
            }
        });
        await Test("serialization failures return false instead of faulting startup", async () => {
            Check(!await SaveSystem.SaveDataAsync("unsupported", new objectWithNoSerialization()), "unsupported object was saved");
        });
        await Test("binary reads preserve all file bytes", async () => {
            var p = Temp(); var bytes = new byte[150000]; new System.Random(123).NextBytes(bytes); File.WriteAllBytes(p, bytes);
            Equal(bytes, await SaveSystem.ReadByteFromFileAsync(p)); Equal(bytes, SaveSystem.ReadByteFromFile(p));
        });
        await Test("GET send failure returns no data and disposes request", async () => {
            Http(UnityWebRequest.Result.Success, "{}"); UnityWebRequest.SendException = new IOException("fixture transport failure");
            Check(await Web.HttpClient.Get<Roles>("https://fixture.invalid") == null, "send failure returned data");
            Check(UnityWebRequest.Last.Disposed, "request leaked on exception"); UnityWebRequest.SendException = null;
        });
        await Test("POST success returns body and releases request", async () => {
            Http(UnityWebRequest.Result.Success, "ok"); Check(await Web.HttpClient.Post("https://fixture.invalid", null) == "ok", "success body changed");
            Check(UnityWebRequest.Last.Disposed && UnityWebRequest.Last.timeout > 0, "POST leaked or unbounded");
        });
        await Test("manifest roles handle missing lists and string ids", () => {
            var parsed = (Web.Manifest)ParseManifest("{\"roles\":{\"admins\":[\"76561198000000001\"],\"helpers\":null}}");
            var manager = new RolesManager(); Set(manager, "Roles", parsed.Roles);
            Check(manager.CheckIsAdmin(76561198000000001) && !manager.CheckIsAdmin(1) && !manager.CheckIsHelper(76561198000000001), "role permissions incorrect");
            return Task.CompletedTask;
        });
        await Test("malformed manifest is rejected", () => {
            Check(ParseManifest("<html>rate limited</html>") == null && ParseManifest("") == null, "malformed manifest accepted");
            return Task.CompletedTask;
        });
        await Test("preload finishes authentication after malformed response", async () => {
            Http(UnityWebRequest.Result.Success, "<html>error</html>"); var preload = new Preload();
            Set(preload, "menu", new Menu.PreloadMenu()); Invoke(preload, "Awake"); Invoke(preload, "Authenticate");
            await Until(() => Preload.Initialized); Check(Steamworks.NET.SteamManager.CurrentUserBanSummary != null, "fallback ban summary missing");
        });
        await Test("null player summary does not break the menu's ban lookup", async () => {
            Http(UnityWebRequest.Result.Success, "{\"players\":[null]}"); var preload = new Preload();
            Set(preload, "menu", new Menu.PreloadMenu()); Invoke(preload, "Awake"); Invoke(preload, "Authenticate");
            await Until(() => Preload.Initialized);
            Check(Steamworks.NET.SteamManager.CurrentUserBanSummary != null, "null player summary accepted");
        });
        await Test("missing and null ban entries do not crash the menu", async () => {
            foreach (var bans in new[] { "null", "[null]" })
            {
                Http(UnityWebRequest.Result.Success, "{\"players\":[{\"VacBanned\":true,\"Bans\":" + bans + "}]}"); var preload = new Preload();
                Set(preload, "menu", new Menu.PreloadMenu()); Invoke(preload, "Awake"); Invoke(preload, "Authenticate"); await Until(() => Preload.Initialized);
                var summary = Steamworks.NET.SteamManager.CurrentUserBanSummary;
                Check(summary.VacBanned, "valid ban flag was lost");
                Check(summary.Bans != null && summary.Bans.All(ban => ban != null), "invalid ban collection reached menu");
            }
        });
        await Test("valid ban information is preserved", async () => {
            Http(UnityWebRequest.Result.Success, "{\"players\":[null,{\"SteamId\":123,\"VacBanned\":true,\"Bans\":[{\"AppIdMin\":1949740,\"AppIdMax\":1949740}]}]}");
            var preload = new Preload(); Set(preload, "menu", new Menu.PreloadMenu()); Invoke(preload, "Awake"); Invoke(preload, "Authenticate"); await Until(() => Preload.Initialized);
            var summary = Steamworks.NET.SteamManager.CurrentUserBanSummary;
            Check(summary.SteamId == 123 && summary.VacBanned && summary.Bans.Count == 1 && summary.Bans[0].AppIdMax == 1949740, "valid sanctions were discarded");
        });
        await Test("initialization timeout reports error without loading menu", () => {
            var preload = new Preload(); var menu = new Menu.PreloadMenu(); Set(preload, "menu", menu); Invoke(preload, "Awake");
            Quest.QuestManager.Initialized = false; Time.realtimeSinceStartup = 0;
            var stack = new Stack<IEnumerator>(); stack.Push((IEnumerator)Invoke(preload, "Start"));
            Step(stack); Time.realtimeSinceStartup = 1000;
            for (int i = 0; i < 20 && stack.Count > 0; i++) Step(stack);
            Check(stack.Count == 0 && !string.IsNullOrEmpty(menu.Error), "timeout did not stop startup and report failure");
            Check(UnityEngine.SceneManagement.SceneManager.Loads == 0, "loaded partially initialized game");
            Quest.QuestManager.Initialized = true; return Task.CompletedTask;
        });
        await Test("successful initialization still loads menu", () => {
            Http(UnityWebRequest.Result.Success, "{}"); var preload = new Preload(); Set(preload, "menu", new Menu.PreloadMenu()); Invoke(preload, "Awake");
            UnityEngine.SceneManagement.SceneManager.Loads = 0; Time.realtimeSinceStartup = 0;
            var stack = new Stack<IEnumerator>(); stack.Push((IEnumerator)Invoke(preload, "Start"));
            for (int i = 0; i < 20 && stack.Count > 0; i++) Step(stack);
            Check(stack.Count == 0 && UnityEngine.SceneManagement.SceneManager.Loads == 1, "normal startup stopped loading menu"); return Task.CompletedTask;
        });
        await Test("shot queue cannot grow indefinitely", () => {
            var manager = Shots(); Player(1);
            for (var i = 0; i < 10000; i++) InvokeStatic(typeof(LagCompensationManager), "Shoot", (ushort)1, new Riptide.Message());
            var queue = (Queue<ShootLagCompensationData>)Get(manager, "_shootQueue"); Check(queue.Count <= 1024, "queue grew without a limit");
            Invoke(manager, "FixedUpdate"); Check(NetworkServerManager.Instance.Server.Sent <= 128, "physics step has unbounded work"); return Task.CompletedTask;
        });
        await Test("old queued shot cannot hit after connection ID is reused", () => {
            var manager = Shots(); var old = Player(1); InvokeStatic(typeof(LagCompensationManager), "Shoot", (ushort)1, new Riptide.Message());
            Player(1); Invoke(manager, "FixedUpdate"); Check(old.Weapon.Attacks == 0 && NetworkServerManager.Instance.Hits == 0, "stale player shot processed"); return Task.CompletedTask;
        });
    }
    static async Task ProjectChecks()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Packages/manifest.json"))) root = root.Parent;
        if (root == null) throw new DirectoryNotFoundException("Run from the Unity project directory.");
        var assets = Path.Combine(root.FullName, "Assets");
        await Test("all C# files parse for Unity editor and Linux player", () => {
            int files = 0;
            foreach (bool editor in new[] { false, true })
            {
                var symbols = new List<string> { "UNITY_STANDALONE", "UNITY_STANDALONE_LINUX", "UNITY_2021_3_OR_NEWER", "UNITY_2021_2_OR_NEWER", "UNITY_2021_1_OR_NEWER", "UNITY_2020_3_OR_NEWER", "UNITY_2020_2_OR_NEWER", "UNITY_2020_1_OR_NEWER", "UNITY_2019_4_OR_NEWER", "UNITY_2019_3_OR_NEWER", "UNITY_2019_2_OR_NEWER", "UNITY_2019_1_OR_NEWER", "UNITY_2018_4_OR_NEWER", "UNITY_2018_3_OR_NEWER", "UNITY_2018_2_OR_NEWER", "UNITY_2018_1_OR_NEWER", "UNITY_5_3_OR_NEWER", "STEAMWORKS_LIN_OSX" };
                if (editor) { symbols.Add("UNITY_EDITOR"); symbols.Add("UNITY_EDITOR_LINUX"); }
                var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: symbols);
                foreach (var file in Directory.EnumerateFiles(assets, "*.cs", SearchOption.AllDirectories))
                {
                    files++; var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), options, file);
                    var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
                    Check(errors.Length == 0, string.Join("\n", errors.Select(e => e.ToString())));
                    if (!editor && !file.Substring(assets.Length).Split(Path.DirectorySeparatorChar).Contains("Editor"))
                        foreach (var use in tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
                            Check(!use.Name.ToString().StartsWith("UnityEditor"), "player code imports editor namespace: " + file);
                }
            }
            System.Console.WriteLine($"  Parsed {files / 2} files in both configurations (syntax only).");
            return Task.CompletedTask;
        });
        await Test("manifest and lock align with the declared Unity editor", () => {
            var manifest = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root.FullName, "Packages/manifest.json")))["dependencies"];
            var locked = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root.FullName, "Packages/packages-lock.json")))["dependencies"];
            foreach (var dependency in ((Newtonsoft.Json.Linq.JObject)manifest).Properties())
                Check(locked[dependency.Name] != null && locked[dependency.Name]["version"].ToString() == dependency.Value.ToString(), "manifest/lock mismatch: " + dependency.Name);
            foreach (var package in new[] { "com.unity.render-pipelines.universal", "com.unity.render-pipelines.core", "com.unity.shadergraph", "com.unity.visualeffectgraph" })
                Check(locked[package]["version"].ToString().StartsWith("12.1."), "Unity 2021 graphics packages must use the 12.1 family: " + package);
            Check(manifest["com.unity.ugui"].ToString() == "1.0.0", "uGUI incompatible with Unity 2021");
            return Task.CompletedTask;
        });
        await Test("every configured build scene exists", () => {
            foreach (var line in File.ReadLines(Path.Combine(root.FullName, "ProjectSettings/EditorBuildSettings.asset")))
                if (line.Trim().StartsWith("path: ")) Check(File.Exists(Path.Combine(root.FullName, line.Trim().Substring(6))), "missing build scene: " + line);
            return Task.CompletedTask;
        });
    }
    static async Task Test(string name, Func<Task> test)
    {
        try { await test(); passes++; System.Console.WriteLine("PASS " + name); }
        catch (Exception error) { failures++; System.Console.WriteLine("FAIL " + name + ": " + (error.InnerException ?? error).Message); }
    }
    static string Temp() => Path.Combine(Application.persistentDataPath, Guid.NewGuid().ToString());
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Equal(byte[] expected, byte[] actual) { Check(expected.Length == actual.Length, $"expected {expected.Length} bytes, got {actual.Length}"); for (int i = 0; i < expected.Length; i++) Check(expected[i] == actual[i], "byte mismatch"); }
    static void Http(UnityWebRequest.Result result, string body) { UnityWebRequest.NextResult = result; UnityWebRequest.NextBody = body; UnityWebRequest.Stall = false; UnityWebRequest.SendException = null; RolesManager.Initialized = false; }
    static async Task Until(Func<bool> ready) { for (int i = 0; i < 200 && !ready(); i++) await Task.Delay(5); Check(ready(), "initialization hung"); }
    static BindingFlags Flags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static object Invoke(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Flags).Invoke(obj, args);
    static void InvokeStatic(Type type, string method, params object[] args) => type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    static object ParseManifest(string json) => typeof(Web.Manifest).GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { json });
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Flags).SetValue(obj, value);
    static object Get(object obj, string field) => obj.GetType().GetField(field, Flags).GetValue(obj);
    static void Step(Stack<IEnumerator> stack)
    {
        while (stack.Count > 0)
        {
            var current = stack.Peek();
            if (!current.MoveNext()) { stack.Pop(); continue; }
            if (current.Current is IEnumerator child) { stack.Push(child); continue; }
            return;
        }
    }
    static LagCompensationManager Shots() { ServerPlayer.list.Clear(); NetworkServerManager.Instance = new NetworkServerManager(); var manager = new LagCompensationManager(); Invoke(manager, "Awake"); return manager; }
    static ServerPlayer Player(ushort id) { var p = new ServerPlayer { Id = id }; ServerPlayer.list[id] = p; NetworkServerManager.Instance.Server.Connected.Add(id); return p; }
    class objectWithNoSerialization { }
}
