using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Manager;
using Save;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Quest
{
    public class QuestManager : MonoBehaviour
    {
        private static QuestManager _instance;

        public static QuestManager Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null)
                {
                    _instance = value;
                }
                else if (_instance != value)
                {
                    Debug.Log($"{nameof(QuestManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        public static bool Initialized = false;

        public Action RefreshQuest;
        public Action OnClaimed;
        public Action OnProgressChanged;
        //Store quests date
        [SerializeField] private List<QuestObject> quests = new List<QuestObject>();

        public Quest[] _currentQuest = new Quest[5];
        private int[] _questIndex = new int[5];
        int[] _progresses = new int[5];
        bool[] _claimed = new bool[5];

        public static readonly int delay = 86400;
        public DateTime LastRecordTime,FirstPlayTime;
        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            string path = Application.persistentDataPath + "/quest/";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            CoroutineWithData cd = new CoroutineWithData(this, SaveSystem.LoadBinaryDataAsync("quest/quest_first_time"));
            yield return cd.coroutine;
            
            Preload.Instance.NextStep();
            
            if (cd.result is DateTime time)
            {
                FirstPlayTime = time;
            }
            else
            {
                FirstPlayTime = DateTime.Now;
                var task = SaveSystem.SaveDataAsync("quest/quest_first_time", FirstPlayTime);

                while (!task.IsCompleted)
                {
                    yield return null;
                }
            }
            cd = new CoroutineWithData(this, SaveSystem.LoadBinaryDataAsync("quest/quest_index"));
            yield return cd.coroutine;
            
            Preload.Instance.NextStep();
            
            //Get Quest Index
            if (cd.result is int[] index)
            {
                _questIndex = index;
                
                cd = new CoroutineWithData(this, SaveSystem.LoadBinaryDataAsync("quest/quest_claimed"));
                yield return cd.coroutine;
                
                Preload.Instance.NextStep();
                
                if (cd.result is bool[] achieved)
                {
                    _claimed = achieved;
                }
                
                cd = new CoroutineWithData(this, SaveSystem.LoadBinaryDataAsync("quest/quest_progress"));
                yield return cd.coroutine;
                
                Preload.Instance.NextStep();
                
                if (cd.result is int[] progresses)
                {
                    _progresses = progresses;
                }
                
                cd = new CoroutineWithData(this, SaveSystem.LoadBinaryDataAsync("quest/quest_time"));
                yield return cd.coroutine;
                
                Preload.Instance.NextStep();
                
                if (cd.result is DateTime data)
                {
                    LastRecordTime = data;
                    
                    Preload.Instance.NextStep(2);

                    if ((DateTime.Now - LastRecordTime).TotalSeconds >= delay)
                    {
                        GenerateQuest();
                        Initialized = true;
                        yield break;
                    }
                    
                    InvokeRepeating(nameof(CheckTime),5,5);
                }
                else
                {
                    var task = SaveSystem.SaveDataAsync("quest/quest_time", DateTime.Now);
                    
                    while (!task.IsCompleted)
                    {
                        yield return null;
                    }
                    
                    Preload.Instance.NextStep(2);
                }

                for (int i = 0; i < 5; i++)
                {
                    int id = index[i];

                    if (quests.Count <= id)
                        continue;   
                        
                    int progress = _progresses[i];
                    _currentQuest[i] = new Quest(i,_claimed[i],progress,quests[id]);
                    _currentQuest[i].ProgressChanged += ProgressChanged;
                    _currentQuest[i].OnClaim += OnClaim;
                }
            }
            else
            {
                GenerateQuest();
            }

            Initialized = true;
        }

        void CheckTime()
        {
            if ((DateTime.Now - LastRecordTime).Seconds >= delay)
            {
                GenerateQuest();
                RefreshQuest?.Invoke();
            }
        }

        public void GetProgress(QuestType questType)
        {
            bool bns = true;
            int idx = 0;
            foreach (var quest in _currentQuest)
            {
                if (idx < 3 && !quest.IsClaim)
                {
                    bns = false;
                }

                if (!bns && idx > 2) break;
                quest.AddProgress(questType);
                idx++;
            }
        }
        void OnClaim(int id)
        {
            _claimed[id] = true;
            _currentQuest[id].IsClaim = true;
            GetProgress(QuestType.CompleteQuest);
            SaveSystem.SaveData("quest/quest_claimed", _claimed);
            OnClaimed?.Invoke();
        }
        void ProgressChanged(int id,int progress)
        {
            _progresses[id] = progress;
            SaveSystem.SaveData("quest/quest_progress", _progresses);
            OnProgressChanged?.Invoke();
        }
        void GenerateQuest()
        {
            int questCount = quests.Count;
            bool[] exist = new bool[questCount];

            for (int i = 0; i < 3; i++)
            {
                int id = Random.Range(0, questCount);
                while (exist[id] || quests[id].bonus)
                {
                    id = Random.Range(0, questCount);
                }

                exist[id] = true;
                _claimed[i] = false;
                _questIndex[i] = id;
                _currentQuest[i] = new Quest(i,false,0,quests[id]);
                _currentQuest[i].ProgressChanged += ProgressChanged;
                _currentQuest[i].OnClaim += OnClaim;
                _progresses[i] = 0;
            }
            exist = new bool[questCount];

            for (int i = 3; i < 5; i++)
            {
                int id = Random.Range(0, questCount);
                while (exist[id] || !quests[id].bonus)
                {
                    id = Random.Range(0, questCount);
                }

                exist[id] = true;
                _claimed[i] = false;
                _questIndex[i] = id;
                _currentQuest[i] = new Quest(i,false,0,quests[id]);
                _currentQuest[i].ProgressChanged += ProgressChanged;
                _currentQuest[i].OnClaim += OnClaim;
                _progresses[i] = 0;
            }

            CancelInvoke(nameof(CheckTime));
            InvokeRepeating(nameof(CheckTime),20,20);
            DateTime now = DateTime.Now;
            var day = (now - FirstPlayTime).Days;
            // LastRecordTime = (new DateTime(time.Year + FirstPlayTime.Year,time.m + FirstPlayTime.Day, time.Days + FirstPlayTime.Day,FirstPlayTime.Hour,FirstPlayTime.Minute,FirstPlayTime.Second));
            TimeSpan timeSpan = new TimeSpan(day, 0, 0, 0);
            LastRecordTime = FirstPlayTime + timeSpan;
            SaveSystem.SaveData("quest/quest_progress", _progresses);
            SaveSystem.SaveData("quest/quest_index", _questIndex);
            SaveSystem.SaveData("quest/quest_claimed", _claimed);
            SaveSystem.SaveData("quest/quest_time", LastRecordTime);
        }
        private void OnDisable()
        {
            foreach (var quest in _currentQuest)
            {
                if (quest == null)
                    continue;   
                
                quest.ProgressChanged -= ProgressChanged;
                quest.OnClaim -= OnClaim;
            }
        }

    }
}
