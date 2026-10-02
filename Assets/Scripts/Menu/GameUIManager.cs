using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;

using Console;
using Cosmetic;
using Demo;
using Demo.UI;
using Manager;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Server;
using PlayerCameraController;
using Riptide;
using Steamworks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Weapon;

namespace Menu
{
    public class GameUIManager : MonoBehaviour,IPointerDownHandler
    {
        public static GameUIManager Instance;
        public CanvasGroup gameGroup;
        public RawImage afterImage;
        internal float desiredGameAlpha = 1f;
        public bool controlByConsole = false;
        internal float desiredFlashAlpha = 0f;
    
        public GameObject gameScene, gameSceneImportant,gameSceneMisc, menuScene,scoreBoard,scope,settingMenu,weapon,inventory,achievement,mainMenu,voteKickMenu,serverSetting,reportMenu;

        public SpectateCanvas spectateCanvas;

        public Image throwablesFilled;

        public LocalizeStringEvent deathLocalizedText,damageGivenText,damageTakenText;

#if UNITY_EDITOR
        [MenuItem("Dev/ScreenShot")]
        static void ScreenShot()
        {
            //CaptureScreenShot( new Rect( 0, 0, Screen.width*1f, Screen.height*1f));
            ScreenCapture.CaptureScreenshot(Application.persistentDataPath + "/" + SceneManager.GetActiveScene().name+ ".png");
        }
#endif
        
        [SerializeField] public LocalizeStringEvent gameModeMap;

        [SerializeField] public GameObject timeTextObj;
        private void Awake()
        {
            Instance = this;

            deathLocalizedText.StringReference.Arguments = new List<object>() { "null" };
            damageGivenText.StringReference.Arguments = new List<object>() { "null" };
            damageTakenText.StringReference.Arguments = new List<object>() { "null" };

            ClientPlayer.SetGameUIManagerClass(this);

            bool flag = LobbyManager.Instance.lobbyId.IsLobby();
            lobbyIdTextObj.SetActive(flag);
            if (flag)
                lobbyIdText.text = LobbyManager.Instance.lobbyId.m_SteamID.ToString();
        
            switch (NetworkManager.ClientGameMode)
            {
                case GameMode.TeamDeathMatch:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(false);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(true);
                    }
                    // playerMultipleState.SetEntry("Deaths");
                    break;
                case GameMode.Brawl:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(true);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(false);
                    }
                    // playerMultipleState.SetEntry("Deaths");
                    break;
                case GameMode.Infected:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(true);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(false);
                    }
                    // playerMultipleState.SetEntry("Deaths");
                    break;
                case GameMode.KillConfirm:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(true);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(false);
                    }
                    // playerMultipleState.SetEntry("Deaths");
                    break;
                case GameMode.Randomizer:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(true);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(false);
                    }
                    // playerMultipleState.SetEntry("Deaths");
                    break;
                case GameMode.KingOfTheHill:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(true);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(false);
                    }
                    timeTextObj.SetActive(true);
                    break;
                case GameMode.GunGame:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(true);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(false);
                    }
                    // playerMultipleState.SetEntry("WeaponLevel");
                    break;
                case GameMode.CatchTheBanana:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(true);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(false);
                    }
                    timeTextObj.SetActive(true);
                    break;
                case GameMode.OneShotOneKill:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(true);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(false);
                    }
                    break;
                case GameMode.RocketMode:
                    foreach (var ui in playersUI)
                    {
                        ui.SetActive(true);
                    }
                    foreach (var ui in teamPlayersUI)
                    {
                        ui.SetActive(false);
                    }
                    break;
            }
        
            dir.SetActive(GameManager.Instance.setting.showDir);
        
            if(MusicManager.Instance.music == MusicManager.MusicType.WinningMusic)
                Invoke(nameof(StopMusic),3.5f);
            else
                StopMusic();

            PlayerNameRaycast.Instance.SetValue(hitPlayer);
            
            WeaponManager.Instance.throwableManager.SetUIValues(throwObjImage,throwablesFilled,throwObjCount,throwKeyText);
        }

        void StopMusic()
        {
            MusicManager.Instance.ChangeMusic(MusicManager.MusicType.None);
        }
        private void OnEnable()
        {
            if(!DemoManager.Replaying)
                GameManager.InputManager.Player.Respawn.performed += Respawn;
        
            throwKeyText.SetText(GameManager.GetBindingName("Throw", 0));
        }
        private void OnDisable()
        {
            GameManager.InputManager.Player.Respawn.performed -= Respawn;
        }

        private void Start()
        {

            bool flag = RolesManager.Instance.CheckIsAdmin(NetworkManager.Instance.steamId.m_SteamID);
            kick.gameObject.SetActive(flag);
            ban.gameObject.SetActive(flag);
        }

        public TextMeshProUGUI bulletText,healthText ,leftTime,scoreBoardLeftTime;
        public Slider healthSlider;
        public RawImage healthImage,image;
        public Gradient gradient;
        private void Update()
        {
            if (Input.GetButtonDown("Cancel"))
            {
                SetPause();
            }
            
            hurtCanvas.alpha = Mathf.Lerp(hurtCanvas.alpha, desiredAlpha, Time.deltaTime * speed);
            healthCanvas.alpha = Mathf.Lerp(healthCanvas.alpha, desiredAlphaHealth, Time.deltaTime * healthSpeed);
            invincibleCanvas.alpha = Mathf.Lerp(invincibleCanvas.alpha, desiredAlphaInvincibleCanvas, Time.deltaTime * 10);
            speedUpCanvas.alpha = Mathf.Lerp(speedUpCanvas.alpha, desiredAlphaSpeedUp, Time.deltaTime * 10);
        
            killSecuredCanvas.alpha = Mathf.Lerp(killSecuredCanvas.alpha, killSecuredDesiredAlpha, Time.deltaTime * killSecuredSpeed);
            killSecured.localScale = Vector3.Lerp(killSecured.localScale , desiredSize, Time.deltaTime * killSecuredSpeed);
            parkourTimeGroup.alpha = Mathf.Lerp(parkourTimeGroup.alpha, desiredAlphaParkourTime, Time.deltaTime * 10f);
            fireCanvas.alpha = Mathf.Lerp(fireCanvas.alpha, desiredFireAlpha, Time.deltaTime * 15f);

            weaponGroup.alpha = Mathf.Lerp(weaponGroup.alpha, weaponAlpha, Time.deltaTime * 10f);
            afterImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(afterImage.color.a, desiredFlashAlpha, Time.deltaTime));
            if (DemoManager.Replaying) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                OpenScoreBoard();
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                CloseScoreBoard();
            }

            if (!gameEnd&& scoreBoard.activeSelf && Input.GetMouseButtonDown(1))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            gameGroup.alpha = Mathf.Lerp(gameGroup.alpha, desiredGameAlpha, Time.deltaTime);
        
            if (reloadProgressObj.activeSelf)
            {
                currentProgress += Time.deltaTime;
                reloadProgressBar.fillAmount = currentProgress / reloadProgress;
            }

            if (leftTimeToRespawn>0)
            {
                if (leftTimeToRespawn < 4f)
                {
                    deathTimeText.SetText(UIManager.IsItChinese()
                        ? $"{leftTimeToRespawn:f1}s 后重生\n按 {GameManager.GetBindingName("Respawn", 0)} 跳过"
                        : $"{leftTimeToRespawn:f1}s to respawn\nPress {GameManager.GetBindingName("Respawn", 0)} to skip");
                }
                else
                {
                    deathTimeText.SetText(UIManager.IsItChinese()
                        ? $"{leftTimeToRespawn:f1}s 后重生"
                        : $"{leftTimeToRespawn:f1}s to respawn");
                }
            
                leftTimeToRespawn -= Time.deltaTime;
            }

            if (dir.activeSelf)
            {
                Direction();
                wishDir.localRotation = Quaternion.Slerp(wishDir.localRotation, wishDesiredRot, Time.deltaTime * 10f);
                velDir.localRotation = Quaternion.Slerp(velDir.localRotation, velDesiredRot, Time.deltaTime * 10f);
            }

            if (Input.GetMouseButtonDown(1) && gameEnd)
            {
                scoreBoardList.SetActive(!scoreBoardList.activeSelf);
                // scoreBoardPlayer.color = scoreBoardList.activeSelf ? Color.grey : Color.white;
                winFirstText.color = scoreBoardList.activeSelf ? Color.grey : Color.white;
                winSecondText.color = scoreBoardList.activeSelf ? Color.grey : Color.white;
                winThirdText.color = scoreBoardList.activeSelf ? Color.grey : Color.white;
            }

            if (gameEnd)
            {
            
                leftTimeText.SetText(leftTimeToLoad.ToString("F0"));
                leftTimeToLoad -= Time.deltaTime;
                if (leftTimeToLoad < 0)
                    leftTimeToLoad = 0;
            }
        
        
        }

        public GameObject scoreBoardList;
        // public RawImage scoreBoardPlayer;

        
        public void PlayButton()
        {
            AudioManager.Instance.PlayButton();
        }
        private bool scoreBoardT;
        void OpenScoreBoard()
        {
            if ( pause || gameEnd || DeveloperConsoleUI.Instance.uiCanvas.activeSelf ||ReportMenu.Instance.IsReporting()) return;
            Chat.Instance.DisableBlockraycast();
            scoreBoard.SetActive(true);
            gameScene.SetActive(false);
            scope.SetActive(false);
            achievement.SetActive(false);
            serverManage.gameObject.SetActive(false);
            voteKickMenu.SetActive(false);
            if (!scoreBoardT)
            {
                Tutorial.Instance.SetText("ScoreboardTip");
                scoreBoardT = true;
            }
        }
        public void CloseScoreBoard()
        {
            if (DemoManager.Replaying) return;
            if (pause || gameEnd || DeveloperConsoleUI.Instance.uiCanvas.activeSelf) return;
            scoreBoard.SetActive(false);
            if (ReportMenu.Instance.IsReporting()) return;
            gameScene.SetActive(!UpgradeInGameMenu.Instance.Player || !UpgradeInGameMenu.Instance.Player.Dead);
            scope.SetActive(false);
            serverManage.gameObject.SetActive(false);
            if (TeamSelector.Instance.IsSelecting) return;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        [SerializeField] public CanvasGroup hurtCanvas,healthCanvas,invincibleCanvas,speedUpCanvas,fireCanvas;
        private float desiredAlpha = 0,speed=20f;
        private float desiredAlphaHealth = 0,healthSpeed=40f;
        private float desiredAlphaInvincibleCanvas=0;
        private float desiredAlphaSpeedUp=0;
        private float desiredFireAlpha=0;

        public void SetFire(float a)
        {
            desiredFireAlpha = a;
        }

        public void Hurt(int health,int maxhealth)
        {
            if (health == 0)
            {
                desiredAlpha = 1f;
                CancelInvoke(nameof(Clear));
                Invoke(nameof(Clear), 1.2f);
            }
            else desiredAlpha = 1f - (float)(health) / maxhealth;
            // speed = 40f;
            // Invoke("UpSpeed", 0.1f);
            // Invoke("Clear", 0.09f);
        }

        // void UpSpeed()
        // {
        //     speed = 25f;
        // }

        public void Clear()
        {
            desiredAlpha = 0;
        }
    
        public void Health()
        {
            desiredAlphaHealth = 1;
            healthSpeed = 25f;
            Invoke(nameof(UpSpeedHealth), 0.1f);
            Invoke(nameof(ClearHealth), 0.09f);
        }

        void UpSpeedHealth()
        {
            healthSpeed = 15f;
        }

        void ClearHealth()
        {
            desiredAlphaHealth = 0;
        }

        public void SpeedUp()
        {
            desiredAlphaSpeedUp = 1f;
            Invoke(nameof(SpeedUpSecond),0.3f);
        }
        void SpeedUpSecond()
        {
            desiredAlphaSpeedUp = 0.5f;
            Invoke(nameof(SpeedUp),0.3f);
        }
        public void ClearSpeedUp()
        {
            desiredAlphaSpeedUp = 0;
            CancelInvoke(nameof(SpeedUp));
            CancelInvoke(nameof(SpeedUpSecond));
        }
        #region Invincible

        public bool ticking = false;
        public void Ticking()
        {
            CancelInvoke(nameof(ClearTicking));
            if (ticking) return;
            ticking = true;
            desiredAlphaInvincibleCanvas = 1;
        }

        public void ClearTicking()
        {
            ticking = false;
            desiredAlphaInvincibleCanvas = 0;
        }
        public void Invincible()
        {
            desiredAlphaInvincibleCanvas = 1;
            Invoke(nameof(InvincibleSecond),0.3f);
        }

        void InvincibleSecond()
        {
            desiredAlphaInvincibleCanvas = 0.5f;
            Invoke(nameof(Invincible),0.3f);
        }
        public void ClearInvincible()
        {
            if (ticking) return;
            desiredAlphaInvincibleCanvas = 0;
            CancelInvoke(nameof(Invincible));
            CancelInvoke(nameof(InvincibleSecond));
        }

        #endregion
    
        public bool pause;

        private WeaponMenu _weaponMenu;
        
        public void SetPause()
        {
            if (gameEnd) return;
            if (pause && CrateOpenMenu.Instance.IsOpening)
            {
                CrateOpenMenu.Instance.ClosePage();
                return;
            }

            if (BuyWeaponMenu.Instance.Buying)
            {
                BuyWeaponMenu.Instance.CloseMenu();
                return;
            }
            DeveloperConsoleUI.Instance.uiCanvas.SetActive(false);
            Chat.Instance.DisableBlockraycast();
            pause = !pause;
            CosmeticMenu.Instance.DisableCam();
            CosmeticMenu.Instance.DisableItemCam();
            if (_weaponMenu == null)
                _weaponMenu = WeaponMenu.Instance;
            if(_weaponMenu.weaponCam!=null)_weaponMenu.weaponCam.enabled = false;
        
            gameScene.SetActive(!pause && !gameEnd && (!WeaponManager.Instance.CurrentPlayer || WeaponManager.Instance.CurrentPlayer.Health > 0));
            menuScene.SetActive(pause);
            mainMenu.SetActive(pause);
            scoreBoard.SetActive(false);
            reportMenu.SetActive(false);
            settingMenu.SetActive(false);
            weapon.SetActive(false);
            inventory.SetActive(false);
            scope.SetActive(!pause && WeaponManager.Instance.isAiming);
            achievement.SetActive(false);
            voteKickMenu.SetActive(false);
            serverManage.gameObject.SetActive(false);
            serverSetting.SetActive(false);
            
            DemoCanvas.Instance.canvas.SetActive(DemoManager.Replaying && !pause && DemoCanvas.UIEnabled);

            Cursor.visible = pause;
            Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
        }

        
        public void OpenSetting()
        {
            settingMenu.SetActive(true);
            scoreBoard.SetActive(false);
            weapon.SetActive(false);
            inventory.SetActive(false);
            achievement.SetActive(false);
            serverManage.gameObject.SetActive(false);
            voteKickMenu.SetActive(false);
            menuScene.SetActive(false);
            serverSetting.SetActive(false);
        }
        
        public void Achievement()
        {
            settingMenu.SetActive(false);
            scoreBoard.SetActive(false);
            weapon.SetActive(false);
            inventory.SetActive(false);
            achievement.SetActive(true);
            voteKickMenu.SetActive(false);
            serverManage.gameObject.SetActive(false);
            menuScene.SetActive(false);
            serverSetting.SetActive(false);
        
        }
        
        public void OpenInventory()
        {
            settingMenu.SetActive(false);
            scoreBoard.SetActive(false);
            weapon.SetActive(false);
            inventory.SetActive(true);
            achievement.SetActive(false);
            serverManage.gameObject.SetActive(false);
            voteKickMenu.SetActive(false);
            menuScene.SetActive(false);
            serverSetting.SetActive(false);
        }
        
        public void ServerSetting()
        {
            settingMenu.SetActive(false);
            scoreBoard.SetActive(false);
            weapon.SetActive(false);
            inventory.SetActive(false);
            achievement.SetActive(false);
            serverManage.gameObject.SetActive(false);
            voteKickMenu.SetActive(false);
            menuScene.SetActive(false);
            serverSetting.SetActive(true);
        }
        
        public void Libary()
        {
            settingMenu.SetActive(false);
            scoreBoard.SetActive(false);
            weapon.SetActive(true);
            inventory.SetActive(false);
            achievement.SetActive(false);
            serverManage.gameObject.SetActive(false);
            voteKickMenu.SetActive(false);
            menuScene.SetActive(false);
            serverSetting.SetActive(false);
        }
        
        public void Menu()
        {
            weapon.SetActive(false);
            scoreBoard.SetActive(false);
            inventory.SetActive(false);
            scope.SetActive(false);
            achievement.SetActive(false);
            LoadingManager.Instance.menuType = LoadingManager.MenuType.Normal;
            if (NetworkManager.Instance.Client.IsConnected)
            {
            
                LobbyManager.Instance.LeaveLobby();
            }
            else
            {
                LoadingManager.Instance.Menu();
            }

            serverManage.gameObject.SetActive(false);
            voteKickMenu.SetActive(false);
            menuScene.SetActive(false);
            serverSetting.SetActive(false);
        }

        public Transform voteKickPlayerContent;
        public ToggleGroup voteKickPlayerGroup;
        
        public void VoteKickMenu()
        {
            for (int i = 0; i < voteKickPlayerContent.childCount; i++)
            {
                Destroy(voteKickPlayerContent.GetChild(i).gameObject);
            }

            ushort index = 0;
            foreach (var client in ClientPlayer.list.Values)
            {
                VoteKickPlayerItem item =
                    Instantiate(PrefabManager.Instance.GetPrefab("VoteKickPlayer"), voteKickPlayerContent)
                        .GetComponent<VoteKickPlayerItem>();
            
                item.SetPlayerValues(client.playerState.Username,client.Id,client.playerState.SteamId,client.playerState.AvatarImage);

                item.transform.GetChild(3).GetComponent<Toggle>().group = voteKickPlayerGroup;
                item.transform.GetChild(3).GetComponent<Toggle>().onValueChanged.AddListener(delegate
                {
                    SelectVotePlayer(client.Id);
                });
                if(index==0) item.transform.GetChild(3).GetComponent<Toggle>().onValueChanged.Invoke(false);
                index++;
            }
            settingMenu.SetActive(false);
            scoreBoard.SetActive(false); 
            weapon.SetActive(false);
            inventory.SetActive(false);
            achievement.SetActive(false);
            serverManage.gameObject.SetActive(false);
            voteKickMenu.SetActive(true);
            menuScene.SetActive(false);
            serverSetting.SetActive(false);
        }

        private ushort votePlayerId = 0;
        public void AddVote(VoteKicking vote)
        {
            // KickVote voteKick = Instantiate(PrefabManager.Instance.GetPrefab("KickVote"), voteContent).GetComponent<KickVote>();

            KickVote.Instance.SetPlayerValues(PlayerList[vote.PlayerId].playerName, vote,
                PlayerList[vote.FromClient].playerName);

            Tutorial.Instance.SetText("VoteTip");
        
            // KeyTip.Instance.SetText("F1",t);
            // KeyTip.Instance.SetText("F2",t);
        }

        public void AgreedVote()
        {
            KickVote.VoteKicking.agreeCount++;
            KickVote.Instance.agreeCountText.SetText(KickVote.VoteKicking.agreeCount.ToString());
        }
        public void DisAgreedVote()
        {
            KickVote.VoteKicking.disAgreeCount++;
            KickVote.Instance.disAgreeCountText.SetText(KickVote.VoteKicking.disAgreeCount.ToString());
        }
        void SelectVotePlayer(ushort id)
        {
            votePlayerId = id;
        }
        public void ClearKick()
        {
            if (KickVote.VoteKicking == null) return;
            if (ClientPlayer.list.ContainsKey(KickVote.VoteKicking.FromClient)&&ClientPlayer.list.ContainsKey(KickVote.VoteKicking.PlayerId))
            {
                Chat.Instance.AddMessage(
                    $"{PlayerList[KickVote.VoteKicking.FromClient].playerName} Kick {PlayerList[KickVote.VoteKicking.PlayerId].playerName} Failed",
                    Color.yellow);
            }
        
        
            KickVote.Instance.Clear();
        }
        public void KickFinish()
        {
            Chat.Instance.AddMessage(
                $"{PlayerList[KickVote.VoteKicking.FromClient].playerName} Kick {PlayerList[KickVote.VoteKicking.PlayerId].playerName} Finish",
                Color.yellow);
        
            KickVote.Instance.Clear();
        }

        
        public void VoteKick()
        {
            if (KickVote.VoteKicking != null)
            {
                FailedWindow window = Instantiate(PrefabManager.Instance.failedWindow, transform)
                    .GetComponent<FailedWindow>();
                window.SetTitle("Eat shit");
                window.SetReason(UIManager.IsItChinese() ? "现在已有投票在场" : "Theres already has vote in game");
                return;
            }

            if (PlayerList.TryGetValue(votePlayerId, out var playerListItem))
            {
                ulong steamId = playerListItem.playerSteamId;
                if (RolesManager.Instance.CheckIsAdmin(steamId)
                    || RolesManager.Instance.CheckIsHelper(steamId) ||
                    RolesManager.Instance.CheckIsDiscordMan(steamId)
                    || RolesManager.Instance.CheckIsBananaMan(steamId))
                {
                    FailedWindow window = Instantiate(PrefabManager.Instance.failedWindow, transform)
                        .GetComponent<FailedWindow>();
                    window.SetTitle("Eat shit");
                    window.SetReason("Sorry he is not cheating , he is a verified person in the game");
                    return;
                }
            }
            if (votePlayerId == NetworkManager.Instance.Client.Id)
            {
                FailedWindow window = Instantiate(PrefabManager.Instance.failedWindow, transform)
                    .GetComponent<FailedWindow>();
                window.SetTitle("Eat shit");
                window.SetReason(UIManager.IsItChinese() ? "你不能踢自己" : "You cant kick youself");
                return;
            }
            Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.ManageToKick);

            message.Add(votePlayerId);
            NetworkManager.Instance.SendByte += message.WrittenLength;
        
            NetworkManager.Instance.Client.Send(message);
        }
        public Dictionary<ushort, PlayerListItem> PlayerList = new Dictionary<ushort, PlayerListItem>();
        [SerializeField] public Transform playerListContent;

        public void AddPlayerToContent(string name,ushort id,ulong steamId,bool ownedDlc,ushort kills,ushort deaths,bool isReady,int exp,bool isInfected,int weaponLevel,int stayTime,short ping,bool displayTag, PlayerState playerState)
        {
            Transform content = playerListContent;
            switch (NetworkManager.ClientGameMode)
            {
                case GameMode.TeamDeathMatch:
                    if (ClientPlayer.list.ContainsKey(id))
                    {
                        content = ClientPlayer.list[id].playerState.Team == Team.Rebel ? redTeamContent : blueTeamContent;
                    }
                    break;
            }
            PlayerListItem item = Instantiate(PrefabManager.Instance.playerListPrefab, content)
                .GetComponent<PlayerListItem>();

            string playerName = Chat.Instance.GetPlayerNameNetwork(name,steamId,displayTag, ownedDlc);
            item.SetPlayerValues(playerName+ " " + ClientPlayer.list[id].GetGroupName(),id,steamId,exp,isInfected,ping, playerState);
            item.SetKills(kills);
            switch (NetworkManager.ClientGameMode)
            {
                case GameMode.KingOfTheHill:
                    item.SetTime(stayTime);
                    break;
                case GameMode.GunGame:
                    item.SetWeaponLevel(weaponLevel);
                    break;
                default:
                    item.SetDeaths(deaths);
                    break;
            
            }

            Debug.Log($"name is {name}, id is {id}");

            if (PlayerList.TryGetValue(id,out var playerListItem))
            {
                Destroy(playerListItem.gameObject);
                PlayerList.Remove(id);
            }
            PlayerList.Add(id,item);
        }

        [SerializeField] public GameObject death;
        [SerializeField] public TextMeshProUGUI deathName,deathTimeText;
        [SerializeField] public RawImage deathAvatar;

        public void SetDeath(ushort id,Texture2D weaponTexture2D)
        {
            leftTimeToRespawn = 5;
            if (!ClientPlayer.list.TryGetValue(id,out var player) || !PlayerList.ContainsKey(id)) return;
            currentSelectPlayer = PlayerList[id];
            deathName.SetText(player.playerState.Username);
            deathAvatar.texture = player.playerState.AvatarImage;
            deathWeapon.texture = weaponTexture2D;
            serverManage.gameObject.SetActive(false);
            deathLocalizedText.StringReference.Arguments[0] = weaponTexture2D.name;
            deathLocalizedText.RefreshString();
            death.SetActive(true);

            if (ClientPlayer.list.TryGetValue(NetworkManager.Instance.Client.Id, out var localPlayer))
            {
                damageTakenText.StringReference.Arguments[0] = localPlayer.DamageTracker.GetDamageTaken(id);
                damageGivenText.StringReference.Arguments[0] = localPlayer.DamageTracker.GetDamageGiven(id);
                
                damageTakenText.RefreshString();
                damageGivenText.RefreshString();
            }

            GameManager.InputManager.Player.Report.performed += ReportMenu.Instance.QuickReport;
        }

        public RawImage deathWeapon;
        private float leftTimeToRespawn = 0;
        public void SetDeath(ushort id,string enemyName,Texture2D texture2D,Texture2D weaponTexture2D)
        {
            deathName.SetText(enemyName);
            deathAvatar.texture = texture2D;
            deathWeapon.texture = weaponTexture2D;
            serverManage.gameObject.SetActive(false);
            leftTimeToRespawn = 5;
            deathLocalizedText.StringReference.Arguments[0] = weaponTexture2D.name;
            deathLocalizedText.RefreshString();
            death.SetActive(true);
            
            if (ClientPlayer.list.TryGetValue(NetworkManager.Instance.Client.Id, out var localPlayer))
            {
                damageTakenText.StringReference.Arguments[0] = localPlayer.DamageTracker.GetDamageTaken(id);
                damageGivenText.StringReference.Arguments[0] = localPlayer.DamageTracker.GetDamageGiven(id);
                
                damageTakenText.RefreshString();
                damageGivenText.RefreshString();
            }
        }
        public void DeleteDeath()
        {
            GameManager.InputManager.Player.Report.performed -= ReportMenu.Instance.QuickReport;
            death.SetActive(false);
        }

   

        public List<WeaponUI> weaponUis = new();


        [SerializeField] public Transform killSecured;
        [SerializeField] public CanvasGroup killSecuredCanvas;
        Vector3 desiredSize =  Vector3.one;
        private float killSecuredDesiredAlpha=0;
        [SerializeField] public TextMeshProUGUI killSecuredText;
        private float killSecuredSpeed = 25f;
        public void KillSecured(string killPlayer)
        {
            killSecuredText.SetText(killPlayer);
            desiredSize = new Vector3(1.2f, 1.2f, 1.2f);
            killSecuredDesiredAlpha = 1;
            Invoke("KillSecuredSmall",0.2f);
            Invoke("UpKillSecuredSpeed",1f);
            Invoke("ClearKillSecured",0.99f);
        }
    
        void UpKillSecuredSpeed()
        {
            killSecuredSpeed = 15f;
        }

        void ClearKillSecured()
        {
            killSecuredDesiredAlpha = 0;
        }

        void KillSecuredSmall()
        {
            desiredSize = Vector3.one;
        }



        [Header("Lobby Id")]
        [SerializeField] public GameObject lobbyIdTextObj;
        public TextMeshProUGUI lobbyIdText;

        
        public void CopyLobbyId()
        {
            GUIUtility.systemCopyBuffer = LobbyManager.Instance.lobbyId.m_SteamID.ToString();
        }

        public LocalizeStringEvent parkourComplete;
        public CanvasGroup parkourTimeGroup;

        private float desiredAlphaParkourTime=0;

        public void SetParkourTime(float timer,string key)
        {
            parkourComplete.SetEntry(key);
            parkourComplete.StringReference.Arguments = new List<object>() {timer.ToString("F2")};
            parkourComplete.RefreshString();
        
            desiredAlphaParkourTime = 1f;
            Invoke(nameof(ClearParkourAlpha),3f);
        }

        void ClearParkourAlpha()
        {
            desiredAlphaParkourTime = 0;
        }

        public GameObject crossHair, reloadProgressObj;
        public Image reloadProgressBar;

        private float reloadProgress,currentProgress;
        private bool previusState = true;

        public void RecordPreviusState()
        {
            previusState = crossHair.activeSelf;
        }
    

        public void Reload(float reloadTime)
        {
            reloadProgress = reloadTime;
            currentProgress = 0;
            reloadProgressBar.fillAmount = 0;
            crossHair.SetActive(false);
            reloadProgressObj.SetActive(true);

        }

        public void StopReload()
        {
            currentProgress = reloadProgress;
            crossHair.SetActive(previusState);
            reloadProgressObj.SetActive(false);
        }

        public GameObject hitMarker;

        // public void HitMarker(Color color)
        // {
        //     // HitMarker2 hitMarker2= Instantiate(hitMarker, gameScene.transform).GetComponent<HitMarker2>();
        //     // hitMarker2.color = color;
        //     // if(color==Color.red)
        //     //     hitMarker2.transform.localScale = Vector3.one *0.4f;
        //     // else
        //     //     hitMarker2.transform.localScale = Vector3.one * 1.1f;
        // }

        public TextMeshProUGUI hitPlayer;

        public RectTransform serverManage;

        public TextMeshProUGUI profileName;
        public RawImage profileAvatar;

        public RawImage[] levelImages;
        public TextMeshProUGUI levelText, expText;
        public Slider expSlider;
    
        public void OnPointerDown(PointerEventData eventData)
        {
            serverManage.gameObject.SetActive(false);
        }

        public PlayerListItem currentSelectPlayer;
        
        public void KickOrBan(bool ban)
        {
            serverManage.gameObject.SetActive(false);
            if (currentSelectPlayer == null || !RolesManager.Instance.CheckIsAdmin(NetworkManager.Instance.steamId.m_SteamID)) return;
            if (currentSelectPlayer.connectionId == NetworkManager.Instance.Client.Id)
            {
                FailedWindow window = Instantiate(PrefabManager.Instance.failedWindow, transform)
                    .GetComponent<FailedWindow>();
                window.SetTitle("Eat shit");
                window.SetReason(UIManager.IsItChinese() ? "你不能踢自己" : "you cant kick yourself");
                return;
            }
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ClientToServerId.ManageServer);

            ManageType type = ban ? ManageType.Ban : ManageType.Kick;
            //

            message.Add((ushort) type);
            message.Add(currentSelectPlayer.connectionId);
            if(type == ManageType.Ban)
                message.Add(currentSelectPlayer.playerSteamId);
        
            NetworkManager.Instance.SendByte += message.WrittenLength;
            NetworkManager.Instance.Client.Send(message);
        
        }


        
        public void OpenSteamProfile()
        {
            SteamFriends.ActivateGameOverlayToUser("steamid",(CSteamID)currentSelectPlayer.playerSteamId);
        }
    

        public void SetInfected(bool flag, bool selfControl)
        {
            infectedCanvas.SetActive(flag);
            upgradeTab.SetActive(!flag);

            if (selfControl)
            {
                UpgradeInGameMenu.Instance.dashIndex = flag ? 1 : UpgradeInGameMenu.Instance.dashIndex;
                UpgradeInGameMenu.Instance.dashSlider.gameObject.SetActive(UpgradeInGameMenu.Instance.dashIndex  > 0);

                UpgradeInGameMenu.Instance.doubleJumpIndex = flag ? 10 : UpgradeInGameMenu.Instance.doubleJumpIndex;
                PlayerMovement.Instance.maxJumpCount=UpgradeInGameMenu.Instance.doubleJumpIndex+1;
                PlayerMovement.Instance.jumpLeft = PlayerMovement.Instance.maxJumpCount;
        
                UpgradeInGameMenu.Instance.moveSpeedIndex= flag ? 10 : UpgradeInGameMenu.Instance.moveSpeedIndex;
                PlayerMovement.Instance.moveSpeedFactor = 1f + 0.1f*UpgradeInGameMenu.Instance.moveSpeedIndex;
            }
        }
    
    

    
    

        public TextMeshProUGUI gameModeText;

        public GameObject[] playersUI, teamPlayersUI;

        public Transform redTeamContent, blueTeamContent;

        public GameObject micIcon, micSpeak;

        public void SetMicSpeak()
        {
            micSpeak.SetActive(true);
            CancelInvoke(nameof(ClearMicSpeak));
            Invoke(nameof(ClearMicSpeak),0.3f);
        }

        void ClearMicSpeak()
        {
            micSpeak.SetActive(false);
        }
    
        void Respawn(InputAction.CallbackContext obj)
        {
            if (DemoManager.Replaying) return;
            if (ClientPlayer.list.TryGetValue(NetworkManager.Instance.Client.Id,out var player)&&player.Dead && !player.specting)
            {
                if (leftTimeToRespawn < 4f)
                {
                    Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.Respawn);
                    NetworkManager.Instance.SendByte += message.WrittenLength;
                    NetworkManager.Instance.Client.Send(message);
                }
            }
        }

        public RectTransform wishDir,velDir;
        private Quaternion wishDesiredRot, velDesiredRot;

        void Direction()
        {
            Vector3 wish = Vector3.zero;
        
            if (!PlayerMovement.Instance)
            {
            
                return;
            }

            wish = PlayerMovement.Instance.orientation.right * PlayerMovement.Instance.x +
                   PlayerMovement.Instance.orientation.forward * PlayerMovement.Instance.y;
            wish.Normalize();
            Vector3 vel = PlayerMovement.Instance.GetRb().velocity.normalized;
            Quaternion tRot = Quaternion.identity;
            if (vel != Vector3.zero)
                tRot = Quaternion.LookRotation(vel);
            tRot.z = -tRot.y;
            tRot.x = 0f;
            tRot.y = 0f;
            Vector3 northDir = new Vector3(0, 0, PlayerMovement.Instance.orientation.eulerAngles.y);
            if(tRot==Quaternion.identity)
                velDesiredRot = Quaternion.Euler(0,0,0);
            else velDesiredRot = tRot * Quaternion.Euler(northDir);
        
        
            tRot = Quaternion.identity;
            if (wish != Vector3.zero)
                tRot = Quaternion.LookRotation(wish);
            tRot.z = -tRot.y;
            tRot.x = 0f;
            tRot.y = 0f;
            if(tRot==Quaternion.identity)
                wishDesiredRot = Quaternion.Euler(0,0,0);
            else wishDesiredRot= tRot * Quaternion.Euler(northDir);
        
        }

        public GameObject dir;
        public TextMeshProUGUI throwObjCount,throwKeyText;

        public void SetThrowableCount()
        {
            if (NetworkManager.ClientGameMode == GameMode.SpecialGameMode)
            {
                throwObjCount.SetText("∞");
            }
            else
            {
                throwObjCount.SetText(WeaponManager.Instance.throwableManager.TacticalThrowCount.ToString());
            }
        }
        public RawImage throwObjImage;

        public CanvasGroup weaponGroup;
        private float weaponAlpha;

        public GameObject infectedCanvas;
        public GameObject messagePanel;
        public LocalizeStringEvent message;

        public void ShowMessage(float clearTimer)
        {
            CancelInvoke(nameof(ClearMessage));
            messagePanel.SetActive(true);
            Invoke(nameof(ClearMessage),clearTimer);
        }

        void ClearMessage()
        {
            message.SetEntry("");
            messagePanel.SetActive(false);
        }

        public void SetWeaponAlpha()
        {
            CancelInvoke(nameof(ClearWeaponAlpha));
            weaponAlpha = 1f;
            Invoke(nameof(ClearWeaponAlpha),1.5f);
        }

        void ClearWeaponAlpha()
        {
            weaponAlpha = 0f;
        }
    
        public GameObject kick, ban;

        public GameObject firstWinner,secondWinner,thirdWinner;

        [SerializeField] public GameObject winnerObj;

        public TextMeshProUGUI winFirstText,winSecondText,winThirdText;
        void EndGame()
        {
            scoreBoard.SetActive(true);
            gameScene.SetActive(false);
            scope.SetActive(false);
            achievement.SetActive(false);
            serverManage.gameObject.SetActive(false);
            voteKickMenu.SetActive(false);
            scoreBoardList.SetActive(false);
            receiveScroll.SetActive(true);
            leftTimeText.gameObject.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        
            winnerObj.SetActive(true);
        
            Color color;

            switch (NetworkManager.ClientServerType)
            {
                case ServerType.KnockoutRound:
                    if (GameManager.finalRound)
                    {
                        color = GameManager.win ? new Color(30/255f,144/255f,255/255f) : Color.red;
                        end.SetEntry(GameManager.win ? "Win" : "Lose");
                    }
                    else
                    {
                        color = GameManager.survive ? new Color(74/255f,156/255f,0) : Color.red;
                        end.SetEntry(GameManager.survive ? "Survive" : "Eliminate");
                    }
                    break;
                default:
                    color = GameManager.win ? new Color(30/255f,144/255f,255/255f) : Color.red;
                    end.SetEntry(GameManager.win ? "Win" : "Lose");
                    break;
            }
            color.a = 172 / 255f;
            scoreBoardBG.color =color;

            end.gameObject.SetActive(true);
        }

        public bool gameEnd = false;
        public void EndRound()
        {
            gameEnd = true;
            Invoke(nameof(EndGame),3f);
            Invoke(nameof(CloseEndScreen),8f);
        }

        void CloseEndScreen()
        {
            EndScreenUI.Instance.CloseEndScreen();
            scoreBoardTip.gameObject.SetActive(true);
        }

        public GameObject upgradeTab;

        public TextMeshProUGUI leftTimeText;
        private float leftTimeToLoad = 15f;

        public Transform receiveContent;
        public GameObject receiveScroll;

        public TextMeshProUGUI scoreBoardTip;

        public void SortPlayer()
        {
            if (NetworkManager.ClientGameMode != GameMode.TeamDeathMatch)
            {

                var items = PlayerList.Values.ToArray();
            
                Array.Sort(items,new ComparePlayerByKill());

                if (items.Length > 0)
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i].transform.SetSiblingIndex(i);
                    }
                }
            }
        }

        
        public void CopyPlayersSteamId()
        {
            serverManage.gameObject.SetActive(false);
            string id = currentSelectPlayer.playerSteamId.ToString();
            GUIUtility.systemCopyBuffer = id;
        }

        
        public void MutePlayer()
        {
            serverManage.gameObject.SetActive(false);
            ushort id = currentSelectPlayer.connectionId;
        }

        public RawImage scoreBoardBG;
        public LocalizeStringEvent end;

        [SerializeField] private Transform enemyContent;
        [SerializeField] private EnemyUITracker enemyUiPrefab;
        public void AddEnemyDot(Transform enemy,ushort id)
        {
            if (_enemyUITrackers.ContainsKey(id))
            {
                Destroy(_enemyUITrackers[id].gameObject);
                _enemyUITrackers.Remove(id);
            }
            EnemyUITracker enemyUITracker = Instantiate(enemyUiPrefab,enemyContent);

            enemyUITracker.target = enemy;
        
            _enemyUITrackers.Add(id,enemyUITracker);
        }

        public void RemoveEnemyDot(ushort id)
        {
            if (_enemyUITrackers.ContainsKey(id))
            {
                Destroy(_enemyUITrackers[id].gameObject);
                _enemyUITrackers.Remove(id);
            }
        }

        private Dictionary<ushort, EnemyUITracker> _enemyUITrackers = new Dictionary<ushort, EnemyUITracker>();
        private Dictionary<ushort, EnemyUITracker> _playerUITrackers = new Dictionary<ushort, EnemyUITracker>();
    
        [SerializeField] private EnemyUITracker playerUiPrefab;
    
        public void AddPlayerDot(Transform enemy,ushort id,Vector3 offset,string n,ClientPlayer player)
        {
            if (_playerUITrackers.ContainsKey(id))
            {
                Destroy(_playerUITrackers[id].gameObject);
                _playerUITrackers.Remove(id);
            }
            EnemyUITracker enemyUITracker = Instantiate(playerUiPrefab,enemyContent);

            enemyUITracker.target = enemy;
            enemyUITracker.offset = offset;
            enemyUITracker.text.text = $"{player.Cash}$\n{n}";
        
            _playerUITrackers.Add(id,enemyUITracker);
        }

        public void SetPlayerDotName(ushort id, ClientPlayer player)
        {
            if (_playerUITrackers.TryGetValue(id, out var uiTracker))
            {
                uiTracker.text.text = $"{player.Cash}$\n{player.playerState.Username}";
            }
        }
        public void DisplayPlayerDot(ushort id,bool flag)
        {
            if (_playerUITrackers.TryGetValue(id,out var dot))
            {
                dot.gameObject.SetActive(flag);
            } 
        }

        public void RemovePlayerDot(ushort id)
        {
            if (_playerUITrackers.ContainsKey(id))
            {
                Destroy(_playerUITrackers[id].gameObject);
                _playerUITrackers.Remove(id);
            }
        }

    }

    public class ComparePlayerByKill : IComparer<PlayerListItem>
    {
        public int Compare(PlayerListItem x, PlayerListItem y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            if (x.kills > y.kills) return -1;
            if (x.kills < y.kills) return 1;
            return 0;
        }
    }
}