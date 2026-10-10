# Project verification

## Checks

- Regression suite in `tools/verification`: **41 passed, 0 failed**.
- Unity 2021.3.45f2 batch import and script compilation: **0 errors**.
- All 682 C# files parse with Unity 2021 editor and Linux player symbols, and player code has no unguarded `UnityEditor` imports.
- Package versions match the lock file and the Unity 2021 graphics package family.
- All 18 build scenes exist.

## Security fixes

| Issue | Reproduced | Fix |
|---|---|---|
| A client could claim any Steam ID (including an administrator's) in `Authorize`. The ID was registered before ticket validation, kept after an invalid ticket, and used by `ClientInit` and `ManageServer`. | Code trace (requires Steam to run end to end) | `ClientAuthentication` binds the claimed ID only when it equals the Steam transport's peer ID and trusts it only after `ValidateAuthTicketResponse_t` OK. `ClientInit` and `ManageServer` require an authorized identity. Invalid tickets disconnect. Bindings are released on every disconnect, which also lets players rejoin after dropping before init. |
| `BinaryFormatter` save loading instantiated any serializable type and ran its callbacks. | Yes | `SaveTypeBinder` allows only the types the game saves (`bool`, `int`, `short`, `string`, `DateTime`, their arrays, `List<bool>`, and enums of the game assembly). Existing saves still load. A format migration is still recommended. |
| A client could make the host copy its whole inventory buffer for every fragment, without a size limit (20,000 fragments took 63.5 s on the host thread). | Yes, with the same algorithm | The declared size is capped at 1 MiB, allocated once, and fragments that exceed it disconnect the client. |
| `BuyWeapon`, `ClientInit` and `ReceivePerks` indexed collections with client values; `BuyWeapon` charged before validating. `SpecterMode` threw for unknown clients. | Code trace | Indices are validated before any state changes. |

## Run the regression suite

Install the .NET 8 SDK and run from the project root:

```bash
dotnet run --project tools/verification/RegressionTests.csproj
```

The runner links the actual production files for saves, HTTP, the manifest, roles, preload and lag compensation. Only unavailable Unity/Steam boundaries have test doubles. Newtonsoft.Json and Roslyn are taken from the SDK, so no external NuGet packages are required. Build outputs go into the ignored `tools/verification/.build` directory; save fixtures go into the operating system's temporary directory.

The tests cover shorter-file overwrites, failed writes, callbacks, legacy save round trips, full binary reads, HTTP failures and malformed JSON, manifest parsing, role permissions, malformed ban summaries, successful startup, initialization timeouts, multiple players' shots, disconnected players, reused connection IDs and queue limits.

## Unity verification

The project was imported and compiled in batch mode with Unity **2021.3.45f2** on Arch Linux: `Assembly-CSharp` and `Assembly-CSharp-Editor` build with **0 errors** (57 pre-existing warnings). Missing fallback shader messages in that log come from `-nographics` and are expected. No player build, play session or live multiplayer test has been run yet.

Use the editor declared in `ProjectSettings/ProjectVersion.txt`: **2021.3.45f2**. URP and VFX Graph are **12.1.15**, matching the core and Shader Graph lock entries; uGUI is **1.0.0**. Unity's [2021.3 package documentation](https://docs.unity3d.com/2021.3/Documentation/Manual/com.unity.render-pipelines.universal.html) identifies the 12.1 URP family and states that core package versions match the editor.

Activate a license in Unity Hub (Preferences → Licenses), then import the project in batch mode:

```bash
"$HOME/Unity/Hub/Editor/2021.3.45f2/Editor/Unity" -batchmode -nographics -quit \
  -projectPath "$PWD" -logFile /tmp/banana-unity-import.log
```

### Running 2021.3 on current Arch Linux

| Symptom | Cause | Fix |
|---|---|---|
| `libxml2.so.2: cannot open shared object file` | Arch ships libxml2 2.14+ (`libxml2.so.16`) | `sudo pacman -S libxml2-legacy` |
| Script compilation fails with `No usable version of libssl was found` | The editor's bundled .NET 5 runtime needs OpenSSL 1.1 | `paru -S openssl-1.1` (add `--mflags --nocheck` if its `afalg` test fails on development kernels) |
| Import hangs at `Starting: .../bee_backend ... --stdin-canary` after compilation succeeded | `bee_backend` never exits while its stdin canary thread is blocked; observed on a 7.3-rc `-next` kernel | Rename `Editor/Data/bee_backend` to `bee_backend.real` and replace it with a script that drops `--stdin-canary` before `exec`-ing the real binary |

Then open `Assets/Scenes/LoadScene.unity` with Steam running and the game in the account library, as described in [project setup](project-setup.md). Verify startup with successful and unavailable web services, a player build, save/load round trips, and a two-client multiplayer match under latency. Core initialization now stops after 60 seconds per stage with an error message; this deadline is adjustable on the Preload component.

The original repository documents omitted audio/effects/models due to licensing. These resources were not recreated. Legacy binary save serialization was retained for compatibility; replacing that format is a separate migration.
