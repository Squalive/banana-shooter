
using System;
using Level;
using UnityEngine;

[Serializable]
public class LevelSystemAnimated
{
    public event EventHandler OnLevelChanged;
    public event EventHandler OnAnimateStop,OnAnimateStart;
    public event EventHandler OnExpChanged;
    
    
    public LevelSystem levelSystem;
    private bool isAnimating;

    private int experience,level,experienceToNextLevel;

    private float updateTimer, updateTimerMax=0.016f,updateTimerMin=0.001f,updateTimerLimit;

    private float _desiredVolume = 0;

    public LevelSystemAnimated(LevelSystem levelSystem)
    {
        SetLevelSystem(levelSystem);

        updateTimerLimit = updateTimerMin;
    }

    void SetLevelSystem(LevelSystem levelSystem)
    {
        this.levelSystem = levelSystem;

        level = levelSystem.GetLevel();
        experience = levelSystem.GetExp();
        experienceToNextLevel = levelSystem.GetExpToNext();

        levelSystem.OnExpChanged += LevelSystem_OnExpChanged;
        levelSystem.OnLevelChanged += LevelSystem_OnLevelChanged;
    }

    private void LevelSystem_OnLevelChanged(object sender, EventArgs e)
    {
        isAnimating = true;
    }

    private void LevelSystem_OnExpChanged(object sender, EventArgs e)
    {
        _desiredVolume = 1f;
        isAnimating = true;
        OnAnimateStart?.Invoke(this,EventArgs.Empty);
        updateTimerLimit = updateTimerMin;
    }

    public int GetLevel()
    {
        return level;
    }

    public int GetMinExp()
    {
        float total = 0;
        for (int i = 1; i < level+1; i++)
        {
            total += Mathf.Floor(i + 300 * Mathf.Pow(2, i / 7f));
        }
        return level==0?0: (int)total;
    }


    public int GetExp()
    {
        return experience;
    }

    public int GetExpToNext()
    {
        return experienceToNextLevel;
    }

    public AudioSource source;
    public void Update()
    {
        source.volume = Mathf.Lerp(source.volume, _desiredVolume, Time.deltaTime * 15f);

        if (isAnimating)
        {
            float desiredLimit = Mathf.Abs(levelSystem.GetExp() - experience) < 500 ? updateTimerMax : updateTimerMin;

            updateTimerLimit = Mathf.Lerp(updateTimerLimit, desiredLimit, Time.deltaTime * 10f);
            
            updateTimer += Time.deltaTime;
            while (updateTimer > updateTimerLimit)
            {
                updateTimer -= updateTimerLimit;
                UpdateAddExp();
            }
            
        }
    }

    void UpdateAddExp()
    {
        if (level < levelSystem.GetLevel())
        {
            AddExp();
        }
        else
        {
            if (experience < levelSystem.GetExp())
            {
                AddExp();
            }
            else
            {
                isAnimating = false;
                _desiredVolume = 0f;
                experience = levelSystem.GetExp();
                if(OnExpChanged!=null) OnExpChanged(this,EventArgs.Empty);
                if(OnAnimateStop!=null) OnAnimateStop(this,EventArgs.Empty);
            }
        }
    }

    private int speed=32;

    void AddExp()
    {
        experience+=speed;
        if (experience >= experienceToNextLevel)
        {
            level++;
            experienceToNextLevel += (int) Mathf.Floor(level + 1 + 300 * Mathf.Pow(2, (level + 1) / 7f));
            if(OnLevelChanged!=null) OnLevelChanged(this,EventArgs.Empty);
        }
        if(OnExpChanged!=null) OnExpChanged(this,EventArgs.Empty);
    }
}