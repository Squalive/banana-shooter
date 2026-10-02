
using Audio;
using Manager;
using Multiplayer;
using Steamworks;
using TMPro;
using UnityEngine;
using Weapon;
using Random = UnityEngine.Random;

public class ShootingTarget2 : MonoBehaviour
{
    public static ShootingTarget2 Instance;

    private void Awake()
    {
        Instance = this;
    }

    public float width = 10, height = 10;

    private void Start()
    {
        renderer.material.color = Color.blue;
        highScoreText.SetText($"Highest Score: {LeaderboardManager.Instance.targetHighScore}\nRank: {LeaderboardManager.Instance.targetRank}");
        leaderBoard.SetText(LeaderboardManager.Instance.target2LeaderBoard);
    }

    

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawLine(new Vector3(transform.position.x - width / 2, transform.position.y + height / 2, transform.position.z),
            new Vector3(transform.position.x + width / 2, transform.position.y + height / 2, transform.position.z));
        
        Gizmos.DrawLine(new Vector3(transform.position.x - width / 2, transform.position.y - height / 2, transform.position.z),
            new Vector3(transform.position.x + width / 2, transform.position.y - height / 2, transform.position.z));
        
        Gizmos.DrawLine(new Vector3(transform.position.x - width / 2, transform.position.y + height / 2, transform.position.z),
            new Vector3(transform.position.x - width / 2, transform.position.y - height / 2, transform.position.z));
        
        Gizmos.DrawLine(new Vector3(transform.position.x + width / 2, transform.position.y + height / 2, transform.position.z),
            new Vector3(transform.position.x + width / 2, transform.position.y - height / 2, transform.position.z));
    }

    public Transform target;

    private int lastScore = 0;
    public void SetTargetPos()
    {
        target.localPosition =
            new Vector3(Random.Range(-width / 2f, width / 2f), Random.Range(-height / 2f, height / 2f));
        if (!started)
        {
            started = true;
            score = 0;
            lastScore = 0;
            time = 10;
            renderer.material.color = Color.red;
        }

        if (Mathf.Abs(lastScore - score) < 2)
        {
            lastScore = score;
            score++;
        }
        
        if (score > LeaderboardManager.Instance.targetHighScore)
        {
            LeaderboardManager.Instance.targetHighScore = score;
            highScoreText.SetText($"Highest Score: {LeaderboardManager.Instance.targetHighScore}\nRank: {LeaderboardManager.Instance.targetRank}");
        }

        if (WeaponManager.Instance.CurrentWeapon != null && WeaponManager.Instance.CurrentWeapon.currentAmmo.GetValue()<WeaponManager.Instance.CurrentWeapon.maxAmmo)
        {
            WeaponManager.Instance.CurrentWeapon.currentAmmo++;
        }
    }

    public MeshRenderer renderer;

    public TextMeshProUGUI text,highScoreText,leaderBoard;

    public bool started = false;

    private float time;
    private int score;

    private void Update()
    {
        if (started)
        {
            time -= Time.deltaTime;

            if (time <= 0)
            {
                started = false;
                renderer.material.color = Color.blue;
                time = 0;
                if ( LeaderboardManager.Instance.targetHighScore > 200) return;
                if (Mathf.Abs(lastScore -  LeaderboardManager.Instance.targetHighScore) > 2)
                {
                    LeaderboardManager.Instance.targetHighScore = lastScore;
                }

                AchievementManager.Instance.SetStat(AchievementManager.EStats.TARGET_SCORE,
                    AchievementManager.StatsType.Int, LeaderboardManager.Instance.targetHighScore);
                LeaderboardManager.Instance.UploadTargetScore();

                AudioManager.Instance.Play(score > LeaderboardManager.Instance.targetHighScore
                    ? "ShootingTargetSuccess"
                    : "ShootingTargetFailed");
                LeaderboardManager.Instance.GetRank();
            }
            text.SetText($"{time.ToString("F2")} Score {score}");
        }
        
    }
}
