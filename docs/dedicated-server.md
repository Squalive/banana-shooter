# Dedicated server

The dedicated server is the game itself built with Unity's **Dedicated Server** target. That build defines `UNITY_SERVER`, which switches Steam to the GameServer API and starts hosting on boot (`Assets/Scripts/Dedicated/DedicatedServer.cs`). Everything else is the same server code the player-hosted lobbies run.

## Build

Install the **Linux Dedicated Server Build Support** module (or the Mac one) for Unity 2021.3.45f2, then use **Build → Dedicated Server (Linux)** in the editor, or:

```
Unity -batchmode -quit -projectPath . -executeMethod BuildDedicatedServer.Linux
```

The output goes to `Builds/Server/Linux/`, with `steam_appid.txt` copied next to it.

## Run

```
./BananaShooterServer.x86_64 -logFile - +server MyServer +port 27015 +maxplayercount 40
```

| Argument | Default | |
|---|---|---|
| `+server` | `MyServer` | Folder name under `Servers/` for the config |
| `+port` | `27015` | Game port (UDP). The Steam query port is `port + 1` |
| `+maxplayercount` | `40` | Clamped to 2–80 |

Open both UDP ports. On Linux the Steam GameServer API needs `steamclient.so` in `~/.steam/sdk64/`, which comes with [SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD).

## Config

On first start the server writes `Servers/<server>/Config.json` in the working directory. It uses the same keys as the old dedicated server, so existing files can be copied over.

```json
{
  "Server": { "Login_Token": "", "VAC_Secure": true, "DLC_Only": false, "Enable_Update_Restart": true, "Vote_Kick_Ban_Minutes": 30 },
  "Browser": { "Server_Name": "Banana Shooter", "Description_Short": "" },
  "Game": { "Random_Map": false, "Random_GameMode": false, "Minimal_Player_Amount": 2, "Server_Type": 1 }
}
```

- `Login_Token`: a [game server login token](https://steamcommunity.com/dev/managegameservers) for app 1949740. Empty logs on anonymously.
- `Server_Type`: `1` Normal or `5` Knockout. Other types need a host client and fall back to Normal.
- `Enable_Update_Restart`: quits when Steam reports a game update. With players on, it first counts down 3 minutes and warns in chat (3 min, 1 min, 30, 15, 3, 2, 1 s). Run the server under a supervisor that updates and restarts it.
- `Vote_Kick_Ban_Minutes`: how long a vote-kicked player can't rejoin. `0` kicks without a ban. Admin bans last until the server restarts.

## Workshop maps

`Servers/<server>/SteamWorkshopConfig.json` (written on first start, same format as the old server):

```json
{ "enabled": true, "fileUlongIds": [3811095731, 3771974830] }
```

The server downloads the items into `Servers/<server>/Workshop/Content` before it opens for players. While workshop is on, every match is a workshop map, and King of the Hill falls back to Brawl. Items that fail to download are skipped, and the log says which ones.

## Console

Type into the server's stdin: `status`, `kick <id>`, `bans` (list bans and time left), `unban <steamid>`, `start` (skip voting), `restart` (same countdown and chat warnings as an update restart, then quits), `quit`. Linux builds only. `SIGTERM` (`docker stop`, systemd) also shuts down cleanly.

## Not supported yet

Endless, Shooting Range, 1v1, Lua mods, scheduled daily restarts (use a cron job or systemd timer).
