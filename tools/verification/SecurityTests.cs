using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Multiplayer;
using Save;

partial class Program
{
    static async Task SecurityTests()
    {
        await Test("claimed Steam ID that differs from the connection is rejected", () => {
            var auth = new ClientAuthentication();
            Check(!auth.TryBind(5, 111, 76561198983573782), "forged administrator identity was bound");
            Check(!auth.TryBind(5, 0, 0), "connection without a Steam identity was bound");
            Check(!auth.TryGetAuthorizedSteamId(5, out _), "forged identity is trusted");
            return Task.CompletedTask;
        });
        await Test("identity is trusted only after Steam validates the ticket", () => {
            var auth = new ClientAuthentication();
            Check(auth.TryBind(5, 111, 111), "matching identity rejected");
            Check(!auth.TryGetAuthorizedSteamId(5, out _), "identity trusted before validation");
            Check(auth.TryAuthorize(111, out var client) && client == 5, "validation did not reach the connection");
            Check(auth.TryGetAuthorizedSteamId(5, out var id) && id == 111, "validated identity not trusted");
            auth.Revoke(111);
            Check(!auth.TryGetAuthorizedSteamId(5, out _), "revoked ticket still trusted");
            return Task.CompletedTask;
        });
        await Test("account can rejoin after a connection dropped before init", () => {
            var auth = new ClientAuthentication();
            auth.TryBind(5, 111, 111); auth.TryAuthorize(111, out _);
            Check(auth.TryBind(7, 111, 111), "rejoin rejected");
            Check(!auth.TryGetAuthorizedSteamId(7, out _), "new connection inherited the old session's trust");
            Check(auth.TryAuthorize(111, out var client) && client == 7, "validation sent to stale connection");
            Check(!auth.TryGetAuthorizedSteamId(5, out _), "stale connection keeps the identity");
            Check(auth.Remove(7, out var removed) && removed == 111 && auth.SteamIds.Count == 0, "disconnect left the identity bound");
            return Task.CompletedTask;
        });
        await Test("save files cannot instantiate types the game never saves", async () => {
            NotASaveType.Ran = false;
            using (var file = File.Create(Save.SaveSystem.GetPath("tampered")))
                new BinaryFormatter().Serialize(file, new NotASaveType());
            var routine = Save.SaveSystem.LoadBinaryDataAsync("tampered"); object result = null;
            while (routine.MoveNext()) { result = routine.Current; await Task.Delay(1); }
            Check(result as string == "Failed", "tampered save was loaded");
            Check(!NotASaveType.Ran, "tampered save ran deserialization code");
        });
        await Test("saved game enums still load", async () => {
            Check(await Save.SaveSystem.SaveDataAsync("power", Manager.GameManager.PowerType.Healer), "save failed");
            var routine = Save.SaveSystem.LoadBinaryDataAsync("power"); object result = null;
            while (routine.MoveNext()) { result = routine.Current; await Task.Delay(1); }
            Check(result is Manager.GameManager.PowerType power && power == Manager.GameManager.PowerType.Healer, "enum save not restored");
        });
        await Test("failed write keeps the previous file", async () => {
            var p = Temp(); File.WriteAllBytes(p, new byte[] { 1, 2, 3 }); Directory.CreateDirectory(p + ".tmp");
            try { await Save.SaveSystem.WriteToFileAsync(p, new byte[] { 9 }); throw new Exception("write into a blocked temporary path succeeded"); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
            Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(p));
        });
        await Test("writes replace files without leaving temporary files", async () => {
            var p = Temp(); File.WriteAllBytes(p, new byte[] { 1, 2, 3, 4 });
            await Save.SaveSystem.WriteToFileAsync(p, new byte[] { 5 });
            Equal(new byte[] { 5 }, File.ReadAllBytes(p)); Check(!File.Exists(p + ".tmp"), "temporary file left behind");
            var text = Temp(); await Save.SaveSystem.WriteToFileAsync(text, "mapa ñ {\"a\":1}");
            Check(Save.SaveSystem.ReadFile(text) == "mapa ñ {\"a\":1}", "encrypted text did not round trip");
        });
        await Test("inventory declared size is bounded", () => {
            Check(!ClientInputValidation.TryStartInventory(-1, new byte[0], out _, out _), "negative size accepted");
            Check(!ClientInputValidation.TryStartInventory(int.MaxValue, new byte[0], out _, out _), "unbounded size accepted");
            Check(!ClientInputValidation.TryStartInventory(10, new byte[11], out _, out _), "first chunk larger than declared size accepted");
            Check(!ClientInputValidation.TryStartInventory(10, null, out _, out _), "missing chunk accepted");
            return Task.CompletedTask;
        });
        await Test("inventory fragments cannot exceed the declared size", () => {
            Check(ClientInputValidation.TryStartInventory(600, new byte[256], out var buffer, out var received), "valid start rejected");
            Check(ClientInputValidation.TryAppendInventory(buffer, ref received, new byte[256]), "valid fragment rejected");
            Check(!ClientInputValidation.TryAppendInventory(buffer, ref received, new byte[256]), "overflowing fragment accepted");
            Check(ClientInputValidation.TryAppendInventory(buffer, ref received, new byte[88]) && received == 600, "final fragment rejected");
            return Task.CompletedTask;
        });
        await Test("largest allowed inventory assembles in linear time", () => {
            var chunk = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray(); var watch = Stopwatch.StartNew();
            Check(ClientInputValidation.TryStartInventory(ClientInputValidation.MaxInventoryBytes, chunk, out var buffer, out var received), "start rejected");
            while (received < buffer.Length) Check(ClientInputValidation.TryAppendInventory(buffer, ref received, chunk), "fragment rejected");
            Check(watch.ElapsedMilliseconds < 1000, $"assembly took {watch.ElapsedMilliseconds} ms");
            Check(buffer[ClientInputValidation.MaxInventoryBytes - 1] == 255, "inventory bytes corrupted");
            return Task.CompletedTask;
        });
        await Test("weapon purchases and loadouts reject out-of-range indices", () => {
            Check(ClientInputValidation.IsValidPurchase(3, 1, 20, 4), "valid purchase rejected");
            foreach (var (weapon, slot) in new[] { (-1, 0), (20, 0), (3, -1), (3, 4), (short.MinValue, 0) })
                Check(!ClientInputValidation.IsValidPurchase(weapon, slot, 20, 4), $"purchase {weapon}/{slot} accepted");
            Check(ClientInputValidation.IsValidLoadout(new short[] { 0, 3, -1 }, 3, 20), "valid loadout rejected");
            Check(!ClientInputValidation.IsValidLoadout(new short[] { 0, 20, -1 }, 3, 20), "unknown weapon accepted");
            Check(!ClientInputValidation.IsValidLoadout(new short[] { 0, 1 }, 3, 20) && !ClientInputValidation.IsValidLoadout(null, 3, 20), "wrong loadout size accepted");
            Check(ClientInputValidation.IsValidPerks(new ushort[] { 0, 1, 7 }, 3, 7), "valid perks rejected");
            Check(!ClientInputValidation.IsValidPerks(new ushort[] { 0, 1, 2, 3 }, 3, 7) && !ClientInputValidation.IsValidPerks(new ushort[] { 8 }, 3, 7), "invalid perks accepted");
            return Task.CompletedTask;
        });
    }
    [Serializable] class NotASaveType { public static bool Ran; [OnDeserialized] void After(StreamingContext context) => Ran = true; }
}
