
using System;

using Level;
using TMPro;
using UnityEngine;

public class JuicyScore : MonoBehaviour
{
    public static JuicyScore Instance;

    private void Awake()
    {
        Instance = this;
        @group = GetComponent<CanvasGroup>();
    }

    [SerializeField] private TextMeshProUGUI scoreText;
    private CanvasGroup group;

    private float desiredAlpha = 0f;
    private Vector3 desiredSize=Vector3.one;

    private void Update()
    {
        @group.alpha = Mathf.Lerp(@group.alpha, desiredAlpha, Time.deltaTime * 10f);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredSize, Time.deltaTime * 25f);
    }

    private int currentScore = 0;
    
    
    public enum ScoreType
    {
        None,
        Xp,
        Usd
    }

    public void UpdateScore(int score,ScoreType flag)
    {
        CancelInvoke(nameof(Clear));
        currentScore += score;

        desiredAlpha = 1f;
        desiredSize = Vector3.one;
        transform.localScale = Vector3.one*2f;
        switch (flag)
        {
            case ScoreType.Xp:
                scoreText.SetText($"+{currentScore} XP");
                LevelManager.Instance.expWaitToAdd += currentScore;
                break;
            case ScoreType.None:
                scoreText.SetText($"+{currentScore}");
                break;
            case ScoreType.Usd:
                scoreText.SetText($"+{currentScore}$");
                break;
        }
        Invoke(nameof(Clear),1.5f);
    }

    void Clear()
    {
        desiredAlpha = 0;
        currentScore = 0;
    }
}
