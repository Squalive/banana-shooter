
using Manager;
using Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LeaderBoardMenu : MonoBehaviour
{
    public static LeaderBoardMenu Instance;

    private void Awake()
    {
        Instance = this;
    }

    public Transform winContent,parkourContent,targetScoreContent;

    public Button preview, next;
    public TextMeshProUGUI text;
    private int cIndex;

    public LeaderBoardItem winItem, parkourItem, scoreItem;

    public GameObject[] boards;
    private void Start()
    {
        SetLeaderBoard();
        preview.onClick.AddListener(delegate { NextLeaderBoard(-1); });
        next.onClick.AddListener(delegate { NextLeaderBoard(1); });
        if (LeaderboardManager.Instance.winIndex.Count > 0)
        {
            for (int i = 0; i < LeaderboardManager.Instance.winIndex.Count; i++)
            {
                LeaderBoardItem item =
                    Instantiate(PrefabManager.Instance.GetPrefab("LeaderBoardItem"),
                        winContent).GetComponent<LeaderBoardItem>();

                LeaderboardManager.LeaderBoardIndex index = LeaderboardManager.Instance.winIndex[i];
                item.SetValue(index.steamId,index.rank,index.score);
            }
        }

        if (LeaderboardManager.Instance.myWin != null)
        {
            winItem.SetValue(LeaderboardManager.Instance.myWin.steamId,LeaderboardManager.Instance.myWin.rank,LeaderboardManager.Instance.myWin.score);
        }
        
        if (LeaderboardManager.Instance.parkourIndex.Count > 0)
        {
            for (int i = 0; i < LeaderboardManager.Instance.parkourIndex.Count; i++)
            {
                LeaderBoardItem item =
                    Instantiate(PrefabManager.Instance.GetPrefab("LeaderBoardItem"),
                        parkourContent).GetComponent<LeaderBoardItem>();

                LeaderboardManager.LeaderBoardIndex index = LeaderboardManager.Instance.parkourIndex[i];
                item.SetValue(index.steamId,index.rank,index.score);
            }
        }
        
        if (LeaderboardManager.Instance.myParkour != null)
        {
            parkourItem.SetValue(LeaderboardManager.Instance.myParkour.steamId,LeaderboardManager.Instance.myParkour.rank,LeaderboardManager.Instance.myParkour.score);
        }
        
        if (LeaderboardManager.Instance.scoreIndex.Count > 0)
        {
            for (int i = 0; i < LeaderboardManager.Instance.scoreIndex.Count; i++)
            {
                LeaderBoardItem item =
                    Instantiate(PrefabManager.Instance.GetPrefab("LeaderBoardItem"),
                        targetScoreContent).GetComponent<LeaderBoardItem>();

                LeaderboardManager.LeaderBoardIndex index = LeaderboardManager.Instance.scoreIndex[i];
                item.SetValue(index.steamId,index.rank,index.score);
            }
        }
        if (LeaderboardManager.Instance.myScore != null)
        {
            parkourItem.SetValue(LeaderboardManager.Instance.myScore.steamId,LeaderboardManager.Instance.myScore.rank,LeaderboardManager.Instance.myScore.score);
        }
        
    }

    void NextLeaderBoard(int ind)
    {
        EventSystem.current.SetSelectedGameObject(null);
        cIndex += ind;
        if (cIndex < 0) cIndex = boards.Length-1;
        if (cIndex > boards.Length-1) cIndex = 0;
        SetLeaderBoard();
    }

    private string[] leaderboards = new string[]
    {
        "Win",
        "Parkour Time",
        "Target Score"
    };

    void SetLeaderBoard()
    {
        for (int i = 0; i < boards.Length; i++)
        {
            boards[i].SetActive(i==cIndex);
        }
        text.SetText(leaderboards[cIndex]);
    }
}
