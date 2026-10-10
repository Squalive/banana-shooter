
using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Manager;
using Menu;
using Mode;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Interface;
using Multiplayer.Entity.Server;
using PVE;
using Quest;
using Riptide;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class GameModes : MonoBehaviour
{
    public bool started, stopped;

    private bool _init = false;

    private bool _winMusic = false;

    public uint StartTick { get; private set; } = 0;
    public uint EndTick { get; private set; } = 0;

    public event Action gameStart;

    public static GameModes Create(GameMode mode, GameObject host)
    {
        switch (mode)
        {
            case GameMode.Brawl: return host.AddComponent<Brawl>();
            case GameMode.TeamDeathMatch: return host.AddComponent<TeamDeathMatch>();
            case GameMode.Infected: return host.AddComponent<Infected>();
            case GameMode.KillConfirm: return host.AddComponent<KillConfirm>();
            case GameMode.Randomizer: return host.AddComponent<Randomizer>();
            case GameMode.KingOfTheHill: return host.AddComponent<KingOfTheHill>();
            case GameMode.GunGame:
                var gunGame = host.AddComponent<GunGame>();
                gunGame.leftTime = 350;
                return gunGame;
            case GameMode.CatchTheBanana: return host.AddComponent<CatchTheBanana>();
            case GameMode.OneShotOneKill: return host.AddComponent<OneShotOneKill>();
            case GameMode.RocketMode: return host.AddComponent<RocketMode>();
            case GameMode.PVE: return host.AddComponent<PVEMode>();
            default: return null;
        }
    }

    private void Start()
    {
        gunKills = new int[NetworkManager.Instance.weaponInfo.Count];
    }

    public void PlayGameStartAudio()
    {
        AudioManager.Instance.Play("gamestart");
    }

    void DisableSound()
    {
        CancelInvoke(nameof(Sound));
    }

    void Sound()
    {
        AudioManager.Instance.Play("ticking");
    }

    public void SetTick()
    {
        StartTick = NetworkServerManager.Instance.CurrentTick + 5 * 50;

        EndTick = StartTick + (uint)(leftTime * 50);

        _init = true;
    }

    private void StartGameServer()
    {
        started = true;

        Init();

        gameStart?.Invoke();

        NetworkServerManager.Instance.StartRound();
        NetworkServerManager.Instance.SetGameState(GameState.MidMatch);
    }

    public void StartGameClient(uint endTick)
    {
        started = true;

        EndTick = endTick;

        GameStart.Instance.SetValues();
        QuestManager.Instance.GetProgress(QuestType.Match);
    }

    protected virtual void Init() { }

    public void StopGameServer()
    {
        stopped = true;

        //TODO: Fix the tdm knockout code and infection code

        Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.StopRound);
        message.Add((ushort)NetworkServerManager.ServerType);
        message.Add((ushort)NetworkServerManager.ServerGameMode);

        List<IPlayerServer> winnerPlayers = new List<IPlayerServer>();
        IPlayerServer[] availablePlayers = GetAvailablePlayers();
        List<ushort> ids = new List<ushort>();

        int count = availablePlayers.Length;

        if (NetworkServerManager.ServerType == ServerType.KnockoutRound)
        {
            int eliminatedIndex = 0;

            bool end = false;

            if (count >= 30)
            {
                eliminatedIndex = Random.Range(18, 22);
            }
            else if (count >= 20)
            {
                eliminatedIndex = Random.Range(8, 12);
            }
            else if (count >= 10)
            {
                eliminatedIndex = count / 2;
            }
            else if (count > 2)
            {
                eliminatedIndex = Random.Range(2, 3);
            }
            else
            {
                eliminatedIndex = 1;
                end = true;
                NetworkServerManager.SetIsPlaying(false);
            }

            int size = count - eliminatedIndex;
            ushort[] eliminatedIds = new ushort[size <= 0 ? 0 : size];
            for (int i = eliminatedIndex; i < count; i++)
            {
                IPlayerServer clientPlayer = availablePlayers[i];
                if (NetworkServerManager.ClientData.TryGetValue(clientPlayer.Id, out var clientData))
                {
                    eliminatedIds[i - eliminatedIndex] = clientPlayer.Id;
                    clientData.SetEliminated(true);

                    clientPlayer.Eliminated = true;
                }
            }

            message.Add(end);
            message.AddUShorts(eliminatedIds);
        }
        else
        {
            NetworkServerManager.SetIsPlaying(false);
        }

        availablePlayers = GetAvailablePlayers();
        count = availablePlayers.Length;

        for (int i = 0; i < (count > 3 ? 3 : count); i++)
        {
            winnerPlayers.Add(availablePlayers[i]);
        }

        foreach (var client in NetworkServerManager.Instance.Server.Clients)
        {
            if (ServerPlayer.list.TryGetValue(client.Id, out var value))
            {
                int rand = Random.Range(0, 10);
                if (winnerPlayers.Contains(value))
                {
                    if (rand >= 6)
                    {
                        ids.Add(client.Id);
                    }
                }
                else
                {
                    if (rand > 7)
                    {
                        ids.Add(client.Id);
                    }
                }
            }

        }

        message.Add(winnerPlayers.Count);

        foreach (var p in winnerPlayers)
        {
            message.Add(p.Id);
            message.Add(p.Username);
            message.Add(p.Description);
        }
        message.Add(ids.ToArray());

        NetworkServerManager.Instance.Server.SendToAll(message);

        NetworkManager.Instance.GameModeChanged?.Invoke(NetworkServerManager.ServerGameMode);

        if (NetworkServerManager.GetAvailableClientCount() <= 1)
        {
            foreach (var clientData in NetworkServerManager.ClientData.Values)
            {
                clientData.SetEliminated(false);
                clientData.SendEliminated();
            }
        }

    }

    IPlayerServer[] GetAvailablePlayers()
    {
        IPlayerServer[] availablePlayers;
        List<IPlayerServer> players;
        switch (NetworkServerManager.ServerGameMode)
        {
            case GameMode.Infected:

                bool infectedWin = true;
                foreach (var serverPlayer in ServerPlayer.list.Values)
                {
                    if (!serverPlayer.IsInfected)
                    {
                        infectedWin = false;
                        break;
                    }
                }

                if (infectedWin)
                {
                    players = ServerPlayer.list.Values.Where(e => !e.Eliminated && e.IsInfected)
                        .OrderByDescending(e => e.Kills).ToList();

                    players.AddRange(ServerPlayer.list.Values.Where(e => !e.Eliminated && !e.IsInfected)
                        .OrderByDescending(e => e.Kills).ToList());
                }
                else
                {
                    players = ServerPlayer.list.Values.Where(e => !e.Eliminated && !e.IsInfected)
                        .OrderByDescending(e => e.Kills).ToList();

                    players.AddRange(ServerPlayer.list.Values.Where(e => !e.Eliminated && e.IsInfected)
                        .OrderByDescending(e => e.Kills).ToList());
                }
                availablePlayers = players.ToArray();
                break;
            case GameMode.TeamDeathMatch:
                int rebelScore = 0, alliancePlayersScore = 0;
                foreach (var serverPlayer in ServerPlayer.list.Values)
                {
                    if (serverPlayer.Team == Team.Rebel)
                    {
                        rebelScore += serverPlayer.Kills;
                    }
                    else
                    {
                        alliancePlayersScore += serverPlayer.Kills;
                    }
                }

                if (rebelScore >= alliancePlayersScore)
                {
                    players = ServerPlayer.list.Values.Where(e => !e.Eliminated && e.Team == Team.Rebel)
                        .OrderByDescending(e => e.Kills).ToList();

                    players.AddRange(ServerPlayer.list.Values.Where(e => !e.Eliminated && e.Team == Team.Alliance)
                        .OrderByDescending(e => e.Kills).ToList());
                }
                else
                {
                    players = ServerPlayer.list.Values.Where(e => !e.Eliminated && e.Team == Team.Alliance)
                        .OrderByDescending(e => e.Kills).ToList();

                    players.AddRange(ServerPlayer.list.Values.Where(e => !e.Eliminated && e.Team == Team.Rebel)
                        .OrderByDescending(e => e.Kills).ToList());
                }

                availablePlayers = players.ToArray();
                break;
            case GameMode.KingOfTheHill:

                availablePlayers = ServerPlayer.list.Values.Where(e => !e.Eliminated)
                    .OrderByDescending(e => e.StayTime).ToArray();

                break;
            case GameMode.CatchTheBanana:

                availablePlayers = ServerPlayer.list.Values.Where(e => !e.Eliminated)
                    .OrderByDescending(e => e.StayTime).ToArray();

                break;
            default:
                availablePlayers = ServerPlayer.list.Values.Where(e => !e.Eliminated)
                    .OrderByDescending(e => e.Kills).ToArray();
                break;
        }

        return availablePlayers;
    }

    public void StopGameClient(ushort[] ids)
    {
        stopped = true;
        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == NetworkManager.Instance.Client.Id)
            {
                InventoryManager.Instance.GetBox();
            }
        }

        if (NetworkManager.ClientGameMode == GameMode.Infected)
        {
            int noninfectedCount = 0;
            foreach (var player in ClientPlayer.list.Values)
            {
                if (!player.playerState.IsInfected)
                    noninfectedCount++;
            }

            if (noninfectedCount == 1 && !ClientPlayer.LocalPlayer.playerState.IsInfected)
            {
                AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.ZOMBIE_SLAYER);
            }
        }


        if (NetworkManager.ClientServerType == ServerType.OneVsOne)
            QuestManager.Instance.GetProgress(QuestType.OneVsOne);

        if (GameUIManager.Instance) GameUIManager.Instance.EndRound();
    }

    [SerializeField] public float leftTime = 300f;

    protected virtual void FixedUpdate()
    {
        ServerUpdate();

#if !UNITY_SERVER
        var tick = NetworkManager.Instance.ServerTick;

        if (EndTick - tick <= MusicManager.Instance.TicksToPlayWin && !_winMusic)
        {
            _winMusic = true;

            MusicManager.Instance.ChangeMusic(MusicManager.MusicType.WinningMusic);
        }

        if (tick > EndTick)
        {
            while (NetworkManager.Instance.boxes.Count > 0)
            {
                NetworkManager.BoxListItem box = NetworkManager.Instance.boxes.Dequeue();

                ReceiveBoxItem item = Instantiate(PrefabManager.Instance.GetPrefab("ReceiveBox"), GameUIManager.Instance.receiveContent).GetComponent<ReceiveBoxItem>();

                if (ClientPlayer.list.TryGetValue(box.playerId, out var player))
                {
                    item.SetValue(
                        Chat.Instance.GetPlayerNameNetwork(player.playerState.Username, player.playerState.SteamId,
                            player.DisplayTag), box.itemdefid);
                }
            }

            return;
        }

        int time = 0;

        bool isWaiting = true;

        uint currentTick = NetworkManager.Instance.ServerTick;

        if (currentTick <= StartTick)
        {

            time = (int)math.ceil((StartTick - currentTick) * 0.02f);
            isWaiting = false;
        }
        else if (currentTick <= EndTick)
        {
            time = (int)math.ceil((EndTick - currentTick) * 0.02f);
            isWaiting = false;
        }

        if (!isWaiting)
        {
            var min = time / 60;
            var seconds = time % 60;

            string m = min < 10 ? $"0{min}" : min.ToString();
            string s = seconds < 10 ? $"0{seconds}" : seconds.ToString();

            GameUIManager.Instance.leftTime.SetText(time.ToString());

            GameUIManager.Instance.scoreBoardLeftTime.SetText($"{m}:{s}");
        }
        else
        {
            GameUIManager.Instance.leftTime.SetText("Waiting...");
            GameUIManager.Instance.scoreBoardLeftTime.SetText("Waiting...");
        }
#endif
    }

    private void ServerUpdate()
    {
        if (!_init)
            return;

        uint currentTick = NetworkServerManager.Instance.CurrentTick;

        if (started && !stopped && currentTick > EndTick)
        {
            NetworkServerManager.Instance.StopGame();
        }
        else if (!started && currentTick > StartTick)
        {
            StartGameServer();
        }
    }

    public int[] gunKills = new int[30];
    public bool complete = false;

    [MessageHandler((ushort)ServerToClientId.BeforeGameStart, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
    private static void BeforeGameStart(Message message)
    {
        uint startTick = message.GetUInt();
        uint endTick = message.GetUInt();

        if (NetworkManager.ClientServerType == ServerType.KnockoutRound)
        {
            bool finalRound = message.GetBool();
            int round = message.GetInt();

            if (GameUIManager.Instance)
            {
                if (finalRound)
                {
                    GameUIManager.Instance.message.SetEntry("Final Round");
                    GameUIManager.Instance.message.StringReference.Arguments = null;
                }
                else
                {
                    GameUIManager.Instance.message.SetEntry("Round X");
                    GameUIManager.Instance.message.StringReference.Arguments = new List<object>() { round };
                }
            }

            NetworkManager.Instance.DisplayMessage(0.5f, 3f);
        }

        if (NetworkManager.Instance.game != null)
            NetworkManager.Instance.game.BeforeGameStart(startTick, endTick);
    }

    void BeforeGameStart(uint startTick, uint endTick)
    {
        StartTick = startTick;
        EndTick = endTick;
        InvokeRepeating(nameof(Sound), 2f, 1f);
        Invoke(nameof(DisableSound), 5f);
        Invoke(nameof(PlayGameStartAudio), 4.2f);
    }

}
