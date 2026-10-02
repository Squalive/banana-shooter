
using System;
using System.Collections.Generic;
using Audio;
using Manager;
using Menu;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using Steamworks;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShootingRange : MonoBehaviour
{
    public static ShootingRange Instance;
    public List<Transform> spawnPos = new List<Transform>();

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < poses.Count; i++)
        {
            
            Vector3 offset = Vector3.zero;
            for (int j = 0; j < movingTargets[i].count; j++)
            {
                Transform t = Instantiate(PrefabManager.Instance.GetPrefab("Target"), poses[i] + offset,
                    PrefabManager.Instance.GetPrefab("Target").transform.rotation).transform;
                offset += new Vector3(5,0,0);
                movingTargets[i].targets.Add(t);
            }
           
            goLeft.Add(Random.Range(0,2)==0);
        }

        SteamFriends.SetRichPresence("steam_display", "#ShotingRange");
    }

    private void Start()
    {
        parkourRank.SetText( $"{LeaderboardManager.Instance.parkourTime.ToString("F2")}\nRank: {LeaderboardManager.Instance.parkourRank.ToString()}");
        leaderBoardParkour.SetText(string.IsNullOrEmpty(LeaderboardManager.Instance.leaderBoardParkour)
            ? ""
            : LeaderboardManager.Instance.leaderBoardParkour);
    }

    private void OnEnable()
    {
        GameManager.Instance.ClientLocalPlayerDead += PlayerDead;
    }

    private void OnDisable()
    {
        GameManager.Instance.ClientLocalPlayerDead -= PlayerDead;
    }

    void PlayerDead(ClientPlayer local, ClientPlayer from)
    {
        StopTimer();
    }

    public float speed = 1f;
    private void Update()
    {
        for (int i = 0; i < poses.Count; i++)
        {
            Vector3 offset = new Vector3(width / 2f, 0f);
            if (goLeft[i])
            {
                offset = -offset;
            }

            foreach (var target in movingTargets[i].targets)
            {
                var position = target.position;
                position = Vector3.Lerp(position,
                    poses[i] + offset,
                    Time.deltaTime * speed / Vector3.Distance(poses[i]  + offset,
                        position));
                target.position = position;
                if (Vector3.Distance(poses[i] + offset, position) < 1f)
                    goLeft[i] = !goLeft[i];
            }
        }

        if (!startedTimer)
        {
            bool started = Physics.CheckBox(startBox, startSize, Quaternion.identity, whatIsPlayer);
            if (started)
            {
                StartTimer();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.F5) && ClientPlayer.list.ContainsKey(NetworkManager.Instance.Client.Id) &&!ClientPlayer.list[NetworkManager.Instance.Client.Id].Dead)
            {
                StopTimer();
                PlayerMovement.Instance.transform.position = recyclePos;
                return;
            }
            bool stopped = Physics.CheckBox(stopBox, stopSize, Quaternion.identity, whatIsPlayer);
            if (stopped)
            {
                StopTimer();
            }
            timer += Time.deltaTime;
            for (int i = 0; i < timerText.Count; i++)
            {
                timerText[i].SetText($"{timer.ToString("F2")}\nScore: {score}");
            }
        }

        
    }

    public Vector3 recyclePos;
    public List<TextMeshProUGUI> timerText = new List<TextMeshProUGUI>();
    private bool startedTimer;
    public LayerMask whatIsPlayer;

    public float width = 20;

    public List<Vector3> poses = new List<Vector3>();
    public List<bool> goLeft = new List<bool>();

    public List<MovingTarget> movingTargets = new List<MovingTarget>();
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        for (int i = 0; i < poses.Count; i++)
        {
            Gizmos.DrawLine(poses[i]-new Vector3(width/2,0),poses[i]+new Vector3(width/2,0));
        }
        
        Gizmos.color = Color.red;
        
        Gizmos.DrawWireCube(startBox,startSize);
        Gizmos.DrawWireCube(stopBox,stopSize);
        
        Gizmos.DrawWireCube(recyclePos,new Vector3(5,5,5));
    }

    public Vector3 startBox,startSize=new Vector3(15f,5f,15f);
    public Vector3 stopBox,stopSize=new Vector3(15f,5f,15f);
    [Serializable]
    public class MovingTarget
    {
        public List<Transform> targets = new List<Transform>();
        public int count=6;
    }

    public List<ShootingTarget> shootingTargets = new List<ShootingTarget>();

    private float timer = 0;
    private int score = 0;

    public void PlusScore()
    {
        ++score;
    }
    void StartTimer()
    {
        Tutorial.Instance.SetText("ShootingRangeTip");
        score = 0;
        for (int i = 0; i < shootingTargets.Count; i++)
        {
            shootingTargets[i].Clear();
        }
        startedTimer = true;
        timer = 0;
    }

    public void StopTimer()
    {
        
        
        int actualScore = 0;
        for (int i = 0; i < shootingTargets.Count; i++)
        {
            if (shootingTargets[i].hitted) actualScore++;
        }

        if (actualScore != score) return;
        if (timer < 0) return;
        if (startedTimer)
        {
            if (score >= shootingTargets.Count)
            {
                Debug.Log("complete");
                if (SteamUserStats.RequestCurrentStats())
                {
                    SteamUserStats.GetStat("PARKOUR_TIME", out float parkourTime);
                    if (parkourTime > timer || Math.Abs(parkourTime - (-1)) < .1f)
                    {
                        AchievementManager.Instance.SetStat(AchievementManager.EStats.PARKOUR_TIME,AchievementManager.StatsType.Float,timer);
                        LeaderboardManager.Instance.parkourTime = timer;
                        parkourRank.SetText( $"{LeaderboardManager.Instance.parkourTime.ToString("F2")}\nRank: {LeaderboardManager.Instance.parkourRank.ToString()}");
                        LeaderboardManager.Instance.UploadParkourTime();
                    }
                }
                AudioManager.Instance.Play("ShootingTargetSuccess");
            }
            else
            {
                // Debug.Log("failed");
                AudioManager.Instance.Play("ShootingTargetFailed");
            }
        }
        

        string complete = score >= shootingTargets.Count & startedTimer  ? "parkour_complete" : "parkour_uncomplete";
        GameUIManager.Instance.SetParkourTime(timer,complete);
        startedTimer = false;
    }

    public TextMeshProUGUI parkourRank,leaderBoardParkour;
}
