
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Manager;
using Menu;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Interface;
using Multiplayer.Entity.Server;
using Multiplayer.Entity.Server.Enemy;
using Quest;
using Riptide;
using Safe;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Mode
{
    public class Endless : MonoBehaviour
    {
        public static Endless Instance { get; private set; }
        public List<Transform> spawnPos = new List<Transform>();

    

        public ServerEnemy[] enemyPrefabs;

        public bool serverStarted = false;
        public bool clientStarted = false;

        [SerializeField] private Vector3 startPos;

        [SerializeField] private LayerMask whatIsPlayer;

        private Collider[] _colliders = new Collider[40];

        [SerializeField] private LocalizeStringEvent text;
        [SerializeField] private LocalizeStringEvent[] otherTexts;
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
        
            Gizmos.DrawWireCube(startPos,new Vector3(20,5,35));
        }

        private void Awake()
        {
            Instance = this;
        
            text.SetEntry("Endless_tutorial_0");
            text.StringReference.Arguments = new List<object>() {ClientPlayer.list.Count};
            border.material = redGlow;
            text.RefreshString();
            RlKillCount = new SafeInt(0);
            
            Invoke(nameof(VoiceLine),1.5f);
        }

        private void Start()
        {
            NetworkManager.Instance.SetRichPreference("Endless", SceneManager.GetActiveScene().name);
            TutorialInGameUI.Instance.ChangeState(TutorialInGameUI.TutorialState.EndlessStart,startPos);
        }

        void VoiceLine()
        {
            VoiceStoreData data = Menu.VoiceLine.Instance.data;
            data.endlessPlayAmount++;
            GameManager.voiceLineChanged = true;
            int playAmount = data.endlessPlayAmount;
            switch (playAmount)
            {
                case 1:
                    Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_start_A);
                    break;
                case 2:
                    Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_start_B);
                    break;
                case 3:
                    Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_start_C);
                    break;
                case 4:
                    Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_start_D);
                    break;
                default:
                    if(Random.Range(0,10) < 3)
                        Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_start_E);
                    break;
            }
        }

        private int lastCnt=0;

        [SerializeField] private Renderer border;
        [SerializeField] private Material redGlow, greenGlow;

        [SerializeField] private AnimationCurve difficultyCurve,spawnCurve;

        [SerializeField] private Transform[] enemySpawnPos;
        [SerializeField] private float minSpawnOffset=-20, maxSpawnOffset=20;

        private int maxEntityAmount = 35;
        private ushort _round = 0;

        private ushort _spawnCount;

        private float _waveTime;

        private float _waveFresh;

        [SerializeField] private GameObject[] weaponStation;

        private bool endlessKingFlag = false;
        private void Update()
        {
            if (serverStarted)
            {
                serverTime += Time.deltaTime;

                _spawnTime -= Time.deltaTime;
                difficulty += Mathf.Sqrt(Mathf.Clamp(difficultyCurve.Evaluate(Mathf.Sqrt(serverTime / 50000000000f)), 0, 1f)) * 0.5f;
                difficulty = Mathf.Clamp(difficulty, 0, 1f);
                _waveTime -= Time.deltaTime;
                if (_waveTime <= 0)
                {
                    if (_spawnCount > 0)
                    {
                        if (_spawnTime <= 0 && ServerEnemy.list.Count+10<maxEntityAmount)
                        {
                            _spawnCount--;
                            float temp = Mathf.Clamp(spawnCurve.Evaluate(difficulty),1,5);
                            int spawnAmount = (int)Random.Range(_minSpawnCount*temp, _maxSpawnCount*temp);
                            spawnAmount = Mathf.Clamp(spawnAmount, 1, 10);

                            for (int i = 0; i < spawnAmount; i++)
                            {
                                Vector3 tempPos= ServerPlayer.list.ElementAt(Random.Range(0, ServerPlayer.list.Count)).Value.PlayerTransform.position;
                                // bool pl = Random.Range(0, 10) < 7 * difficulty || spawnAmount < 4;
                                // if (pl)
                                // {
                                //     tempPos = ;
                                // }
                                Vector3 spawnPosition = new Vector3(tempPos.x +Random.Range(minSpawnOffset,maxSpawnOffset), tempPos.y, tempPos.z+Random.Range(minSpawnOffset,maxSpawnOffset));

                                RaycastHit hit;
                                while (!Physics.Raycast(spawnPosition,Vector3.down,out hit,1000f,GameManager.Instance.whatIsGround) 
                                       || (Physics.Raycast(tempPos, (spawnPosition - tempPos).normalized,
                                           (spawnPosition - tempPos).magnitude, GameManager.Instance.whatIsGround)))
                                {
                                    tempPos = ServerPlayer.list.ElementAt(Random.Range(0, ServerPlayer.list.Count))
                                        .Value.PlayerTransform.position;
                                    spawnPosition = new Vector3(tempPos.x +Random.Range(minSpawnOffset,maxSpawnOffset), tempPos.y, tempPos.z+Random.Range(minSpawnOffset,maxSpawnOffset));
                                }

                                spawnPosition = hit.point+Vector3.up;

                                int prefab = Random.Range(0, enemyPrefabs.Length);
                                ServerEnemy enemy = Instantiate(enemyPrefabs[prefab], spawnPosition, Quaternion.identity);
                                enemy.Initialize();
                            }

                            temp = (1 - difficulty) + 0.6f;
                            _spawnTime = Random.Range(3.5f*temp, maxSpawnTime*temp);
                            _round++;
                            Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ServerToClientId.EndlessRefresh);
                            message.Add(spawnAmount);
                            message.Add(_round);
                            NetworkServerManager.Instance.Server.SendToAll(message);
                            
                            
                            
                        }

                        
                    }
                    float tp = (1 - difficulty) + 0.6f;
                    //TODO: REFRESH
                    if (_spawnCount <= 0 && ServerEnemy.list.Count<=0)
                    {
                        _waveTime = Random.Range(13*tp, 30*tp);
                        _waveFresh = 1f;
                        _spawnCount = (ushort) Random.Range(3*tp, 6*tp);
                        Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.EndlessWeaponStation);

                        message.Add((ushort)Random.Range(0, weaponStation.Length));
                        message.Add((ushort)_waveTime);
                        
                        NetworkServerManager.Instance.Server.SendToAll(message);
                    }
                }
                else
                {
                    _waveFresh -= Time.deltaTime;
                    if (_waveFresh < 0)
                    {
                        _waveFresh = 1;
                        Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ServerToClientId.Message);
                        message.Add((ushort)MessageType.Wave);
                        message.Add((ushort)_waveTime);
                        NetworkServerManager.Instance.Server.SendToAll(message);
                    }
                }
            }
            if (clientStarted)
            {
                clientTime += Time.deltaTime;
                clientTimeString = clientTime.ToString("F1");
                text.RefreshString();
                foreach (var stringEvent in otherTexts)
                {
                    stringEvent.RefreshString();
                }

                if (clientTime >= 400 && !endlessKingFlag)
                {
                    endlessKingFlag = true;
                    QuestManager.Instance.GetProgress(QuestType.EndlessKing);
                }
                return;
            }

            #region CHECK PLAYER

            int cnt = Physics.OverlapBoxNonAlloc(startPos, new Vector3(10, 2.5f, 17.5f), _colliders, Quaternion.identity,
                whatIsPlayer);

        
            if (lastCnt != cnt)
            {
                if (cnt > lastCnt)
                {
                    AudioManager.Instance.SoundEffect3D("tip",startPos);
                }
                lastCnt = cnt;
                SetText();
            }

            int playerAmount = ClientPlayer.list.Count;
            if (playerAmount == 0) return;
            if (NetworkServerManager.Instance.Server.IsRunning)
            {
                playerAmount = ServerPlayer.list.Count;
                if (lastCnt == playerAmount)
                {
                    serverStartTime -= Time.deltaTime;
                    if (serverStartTime <= 0)
                    {
                        serverStartTime = 0;
                        StartGameServer();
                        
                    }
                }
            }
            if (lastCnt == playerAmount)
            {
                clientStartTime -= Time.deltaTime;
                t -= Time.deltaTime;
                if (t <= 0)
                {
                    AudioManager.Instance.Play("ticking");
                    t = 1f;
                }
                if (clientStartTime <= 0)
                {
                    clientStartTime = 0;
                }
                text.StringReference.Arguments = new List<object>() {clientStartTime.ToString("F0")};
                text.RefreshString();
            }

            #endregion
        
        
        }
        [MessageHandler((ushort) ServerToClientId.EndlessWeaponStation, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void EndlessWeaponStation(Message message)
        {
            ushort id = message.GetUShort();

            ushort time = message.GetUShort();

            for (int i = 0; i < Instance.weaponStation.Length; i++)
            {
                bool flag = i == id;
                Instance.weaponStation[i].SetActive(flag);
                
                if(flag)
                    TutorialInGameUI.Instance.ChangeState(TutorialInGameUI.TutorialState.BuyStation,Instance.weaponStation[i].transform.position);
            }

            Instance.StartCoroutine(Instance.PredictWaveStart(time));
        }

        IEnumerator PredictWaveStart(float time)
        {
            yield return new WaitForSeconds(time);

            TutorialInGameUI.Instance.ChangeState(TutorialInGameUI.TutorialState.None,Vector3.zero);
        }
        private static bool[] fl = new bool[10];
        [MessageHandler((ushort) ServerToClientId.EndlessRefresh, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void EndlessRefresh(Message message)
        {
            int spawnAmount = message.GetInt();
            ushort r = message.GetUShort();
            if (r == 1)
            {
                if (Random.Range(0, 10) < 7 && !fl[0])
                {
                    fl[0] = true;
                    Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_spawn_A);
                }
            }
            else if (spawnAmount == 4&& Random.Range(0, 10) < 2&& !fl[1])
            {
                fl[1] = true;
                Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_spawn_D);
            }
            else if (spawnAmount < 3 && r > 1 && r < 4 && Random.Range(0, 10) < 7&& !fl[2])
            {
                fl[2] = true;
                Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_spawn_B);
            }
            else if (r > 12 && r < 16 && Random.Range(0, 10) < 4&& !fl[3])
            {
                fl[3] = true;
                Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_spawn_C_01);
                Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_spawn_C_02);
            }
            else if (r == 20&& !fl[4])
            {
                fl[4] = true;
                Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_spawn_E);
            }
            else if (r == 30&& !fl[5])
            {
                fl[5] = true;
                Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_spawn_F);
            }
        }
    
        private ushort _minSpawnCount=1, _maxSpawnCount = 3;
        [Range(5,12)][SerializeField] private float maxSpawnTime = 12;
        public float difficulty { get; private set; }= 0;
        private float _spawnTime = 0;

        private float t = 0;
        private float clientStartTime = 5f;
        private float serverStartTime = 5f;
        void SetText()
        {
            int playerAmount = ClientPlayer.list.Count;
            if (lastCnt == playerAmount)
            {
                text.SetEntry("Endless_start");
                border.material = greenGlow;
                clientStartTime = 5f;
                serverStartTime = 5f;
                t = 2f;
            }
            else
            {
                text.SetEntry("Endless_tutorial_0");
                text.StringReference.Arguments = new List<object>() {playerAmount-lastCnt};
                border.material = redGlow;
            }
            text.RefreshString();
        }

        [HideInInspector] public float serverTime=0;
        [HideInInspector] public float clientTime=0;
    
        [HideInInspector] public string clientTimeString="0.0";

        [HideInInspector] public string killCount = "0";
        [HideInInspector] public SafeInt RlKillCount ;
        public void StartGameClient()
        {
            if (clientStarted) return;
            clientStarted = true;
            clientTime = 0;
            killCount = "0";
            RlKillCount = new SafeInt(0);
            clientTimeString = "0.0";
            text.SetEntry("Endless_info");
            Vector3 tempPos= enemySpawnPos[Random.Range(0, enemySpawnPos.Length)].position;
            Vector3 spawnPosition = new Vector3(tempPos.x +Random.Range(minSpawnOffset,maxSpawnOffset), tempPos.y, tempPos.z+Random.Range(minSpawnOffset,maxSpawnOffset));

            while (!Physics.Raycast(spawnPosition,Vector3.down,1000f,GameManager.Instance.whatIsGround))
            {
                tempPos = enemySpawnPos[Random.Range(0, enemySpawnPos.Length)].position;
                spawnPosition = new Vector3(tempPos.x +Random.Range(minSpawnOffset,maxSpawnOffset), tempPos.y, tempPos.z+Random.Range(minSpawnOffset,maxSpawnOffset));
            }

            PlayerMovement.Instance.transform.position = spawnPosition;

            TutorialInGameUI.Instance.ChangeState(TutorialInGameUI.TutorialState.None,Vector3.zero);
            StartCoroutine(StartImpact());
        }

        void StartGameServer()
        {
            serverStarted = true;
            serverTime = 0;
            difficulty = 0;
            _round = 0;
            _waveTime = 0;
            _spawnCount = (ushort) Random.Range(2, 5);
        
            _spawnTime = Random.Range(5f, maxSpawnTime);
            NetworkServerManager.Instance.StartRound();

            foreach (var serverPlayer in ServerPlayer.list.Values)
            {
                serverPlayer.SetWeapon();
            }
        }

        IEnumerator StartImpact()
        {
            AudioManager.Instance.Play("gamestart");
            yield return new WaitForSeconds(0.8f);
            GameStart.Instance.SetSpecialMode();

            yield return new WaitForSeconds(1f);
            if (Random.Range(0, 10) < 7)
            {
                if (Random.Range(0, 10) < 3)
                {
                    Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_start_G_01);
                    Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_start_G_02);
                    Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_start_F);
                }
                else
                {
                    Menu.VoiceLine.Instance.PlayVoice(VoiceKey.gm_endless_start_F);
                }
            }
        }

        private void OnEnable()
        {
            if(NetworkServerManager.Instance.Server.IsRunning)
                GameManager.Instance.ServerPlayerDead += ServerPlayerDead;
        }

        private void OnDisable()
        {
            GameManager.Instance.ServerPlayerDead -= ServerPlayerDead;
        }

        private void ServerPlayerDead(IPlayerServer dead, IPlayerServer from)
        {
            if (serverStarted)
            {
                foreach (var serverPlayer in ServerPlayer.list.Values)
                {
                    if (!serverPlayer.Dead)
                    {
                        dead.Spect(true);
                        return;
                    }
                }
            
                serverStarted = false;
                foreach (var entity in GameManager.Entities)
                {
                    Destroy(entity);
                }
                GameManager.Entities.Clear();

                StartCoroutine(Restart());
                //TODO: Stop
                Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.StopRound);

                message.Add((ushort)NetworkServerManager.ServerType);
            
                NetworkServerManager.Instance.Server.SendToAll(message);
            }
        }

        IEnumerator Restart()
        {
            yield return new WaitForSeconds(2f);
            
            foreach (var serverPlayer in ServerPlayer.list.Values)
            {
                serverPlayer.Spect(false);
            }
        }
        public void ClientStop()
        {
            clientStarted = false;
            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.message.StringReference.Arguments = new List<object>() {this};
                clientTimeString = clientTime.ToString("F1");
                text.RefreshString();
                GameUIManager.Instance.message.SetEntry("endless_stop");
            }
            NetworkManager.Instance.DisplayMessage(1.5f,5f);
            TutorialInGameUI.Instance.ChangeState(TutorialInGameUI.TutorialState.EndlessStart,startPos);
        }
    }
}
