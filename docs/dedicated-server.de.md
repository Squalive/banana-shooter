# Dedizierte Server

Der dedizierte Server ist das Spiel selbst, das mit Unitys **Dedicated Server** Ziel gebaut wurde. Dieser Build definiert `UNITY_SERVER`, was Steam auf die GameServer-API umschaltet und das Hosting beim Start aktiviert (`Assets/Scripts/Dedicated/DedicatedServer.cs`). Alles andere ist derselbe Server Code der auch in den von Spielern gehosteten Lobbys verwendet wird.

## Build

Installiere das **Linux Dedizierte Server Build Support** Modul (oder das für Mac) für Unity 2021.3.45f2 und benutze dann im Editor **Build → Dedicated Server (Linux)**, oder:

```
Unity -batchmode -quit -projectPath . -executeMethod BuildDedicatedServer.Linux
```

Die Ausgabe geht nach `Builds/Server/Linux/`, wobei `steam_appid.txt` daneben kopiert wird.

## Starten

```
./BananaShooterServer.x86_64 -logFile - +server MyServer +port 27015 +maxplayercount 40
```

| Argument | Standart | |
|---|---|---|
| `+server` | `MyServer` | Ordnername unter `Servers/` für die Konfiguration |
| `+port` | `27015` | Spiel port (UDP). Der Steam query port ist `port + 1` |
| `+maxplayercount` | `40` | Auf 2–80 begrenzt |

Öffne beide UDP Ports. Auf Linux benötigt die Steam GameServer API `steamclient.so` in `~/.steam/sdk64/`, das mitgeliefert wird von [SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD).

## Konfiguration

Beim ersten Start schreibt der Server `Servers/<server>/Config.json` im Arbeitsverzeichnis. Er verwendet die gleichen Schlüssel wie der alte dedizierte Server, also können bestehende Dateien einfach übernommen werden.

```json
{
  "Server": { "Login_Token": "", "VAC_Secure": true, "DLC_Only": false, "Enable_Update_Restart": true, "Vote_Kick_Ban_Minutes": 30 },
  "Browser": { "Server_Name": "Banana Shooter", "Description_Short": "" },
  "Game": { "Random_Map": false, "Random_GameMode": false, "Minimal_Player_Amount": 2, "Server_Type": 1 }
}
```

- `Login_Token`: ein [game server login token](https://steamcommunity.com/dev/managegameservers) für app 1949740. Leer meldet anonym an.
- `Server_Type`: `1` Normal oder `5` Knockout. Andere Typen brauchen einen Host-Client und fallen zurück auf Normal.
- `Enable_Update_Restart`: Beendet das Spiel, wenn Steam ein Update meldet. Wenn Spieler online sind, wird zunächst 3 Minuten heruntergezählt und im Chat gewarnt (3 min, 1 min, 30, 15, 3, 2, 1 s). Lasst den Server unter einem Supervisor laufen, der ihn aktualisiert und neu startet.
- `Vote_Kick_Ban_Minutes`: wie lange ein per Vote gekickter Spieler nicht wieder beitreten kann. `0` kickt ohne Bann. Admin-Banns gelten bis zum Neustart des Servers.

## Workshop maps

`Servers/<server>/SteamWorkshopConfig.json` (Beim ersten Start geschrieben, im gleichen Format wie der alte Server):

```json
{ "enabled": true, "fileUlongIds": [3811095731, 3771974830] }
```

Der Server lädt die Items in `Servers/<server>/Workshop/Content` herunter, bevor er für die Spieler geöffnet wird. Wenn der Workshop aktiviert ist, ist jedes Match eine Workshop Karte, und King of the Hill geht auf Brawl zurück. Items die nicht heruntergeladen werden, werden übersprungen und im Log steht welche es sind.

## Konsole

Tippe in das stdin des Servers: `status`, `kick <id>`, `bans` (Banns und Restzeit anzeigen), `unban <steamid>`, `start` (Voting überspringen), `restart` (gleicher Countdown und Chat-Warnungen wie bei einem Update-Neustart, dann beendet sich das Programm), `quit`. Nur Linux-Versionen. `SIGTERM` (`docker stop`, systemd) fährt den Server ebenfalls sauber herunter.

## Noch nicht unterstützt

Endlos, Schießstand, 1 gegen 1, Lua-Mods, geplante tägliche Neustarts (verwende einen Cron-Job oder Systemd-Timer).
