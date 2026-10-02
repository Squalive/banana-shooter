using System;
using Level;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Server;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

namespace Menu
{
    public class PlayerListItem : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler
    {
        private GameUIManager manager;
        public string playerName;
        public ushort connectionId;
        public ulong playerSteamId;

        public LevelSystem levelSystem;

        public ushort kills, deaths;
        
        [SerializeField] public TextMeshProUGUI playerNameText,killsText,deathsText,expText,latencyText,timeText;
        [SerializeField] private RawImage avatar,levelImage;

        public RawImage image;
        private Color desiredColor;
        private Color defaultColor;
        public Color normalColor=Color.white,targetColor=Color.grey;
        public Color redColor=Color.red,redTargetColor=Color.grey;
        public Color blueColor=Color.blue,blueTargetColor=Color.grey;

        public Color infectedColor=Color.green, infectedTargetColor;
        
        public Texture2D clanOwnerVerified,alive,dead,infected;

        [SerializeField] private Gradient latencyColor;
        private void Start()
        {
            manager=GameUIManager.Instance;
        }

        private void Update()
        {
            image.color = Color.Lerp(image.color, desiredColor, Time.unscaledDeltaTime * 15f);
        }

        private bool team = false, red = false;
        public void SetPlayerValues(string _playerName,ushort _connectionId,ulong steamId,int exp,bool isInfected,short ping, PlayerState playerState)
        {
            playerName = _playerName;
            connectionId = _connectionId;
            playerSteamId = steamId;
            playerNameText.SetText(playerName);

            if (playerState.AvatarImage == null)
            {
                NetworkManager.OnAvatarImageLoaded += OnImageLoaded;
            }
            else
            {
                avatar.texture = playerState.AvatarImage;
            }

            team = NetworkManager.ClientGameMode == GameMode.TeamDeathMatch;
            red = ClientPlayer.list[_connectionId].playerState.Team == Team.Rebel;

            levelSystem = new LevelSystem(exp);
            int level = levelSystem.GetLevel();

            Color color = LevelManager.Instance.GetColor(level);
            expText.SetText(level.ToString());
            expText.color = color;
            levelImage.color = color;

            defaultColor = normalColor;
            if (isInfected)
            {
                desiredColor = infectedColor;
                return;
            }
            if (team)
            {
                desiredColor = red ? redColor : blueColor;
            }
            else desiredColor = defaultColor;
        
            latencyText.SetText(ping.ToString());

            latencyText.color = latencyColor.Evaluate(ping / 200f);

            if (NetworkManager.ClientGameMode == GameMode.KingOfTheHill || NetworkManager.ClientGameMode == GameMode.CatchTheBanana)
            {
                timeText.transform.parent.gameObject.SetActive(true);
            }
        
            SetState(PlayerItemState.Alive);
        }

        private void OnDestroy()
        {
            NetworkManager.OnAvatarImageLoaded -= OnImageLoaded;
        }

        void OnImageLoaded(CSteamID steamID, Texture2D avatarImage)
        {
            if (steamID.m_SteamID == playerSteamId)
            {
                avatar.texture = avatarImage;
                NetworkManager.OnAvatarImageLoaded -= OnImageLoaded;
            }
        }

        public void SetKills(ushort kill)
        {
            kills = kill;
            GameUIManager.Instance.SortPlayer();
            killsText.SetText(kill.ToString());
        }
    
        public enum PlayerItemState
        {
            Alive,
            Dead,
            ClanOwner,
            Infected,
        }

        public void SetDeaths(ushort death)
        {
            deaths = death;
            deathsText.SetText(deaths.ToString());
        }
        public void SetWeaponLevel(int weaponLevel)
        {
            // deathsText.SetText(weaponLevel.ToString());
        }
        public int stayTime;
        public void SetTime(int time)
        {
            stayTime = time;
            int h = time / 60;
            int s = time - h*60;
            timeText.SetText(h.ToString("D2") + ":" + s.ToString("D2"));
        }

        private PlayerItemState state=PlayerItemState.Dead;
        [SerializeField] private RawImage stateImage;  

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (state==PlayerItemState.Infected)
            {
                desiredColor = infectedTargetColor;
                return;
            }
            if (team)
                desiredColor = red ? redTargetColor : blueTargetColor;
            else
                desiredColor = targetColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (state==PlayerItemState.Infected)
            {
                desiredColor = infectedColor;
                return;
            }
            if (team)
                desiredColor = red ? redColor : blueColor;
            else desiredColor = defaultColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            manager.currentSelectPlayer = this;
            manager.serverManage.gameObject.SetActive(true);
            manager.serverManage.position = Input.mousePosition;
        
            manager.profileName.SetText(playerName);
            manager.profileAvatar.texture = avatar.texture;
        
            Color color = LevelManager.Instance.GetColor(levelSystem.GetLevel());

            foreach (var raw in manager.levelImages)
            {
                raw.color = color;
            }
        
            manager.levelText.SetText(levelSystem.GetLevel().ToString()); 
            manager.expText.SetText($"{levelSystem.GetExp()} / {levelSystem.GetExpToNext()}");
            manager.expSlider.minValue = levelSystem.GetMinExp();
            manager.expSlider.maxValue = levelSystem.GetExpToNext();
            manager.expSlider.value = levelSystem.GetExp();
        }

        private void OnDisable()
        {
            if (state==PlayerItemState.Infected)
            {
                desiredColor = infectedColor;
                return;
            }
        
            if (team)
                desiredColor = red ? redColor : blueColor;
            else desiredColor = defaultColor;
        }

        private Texture2D currentTexture;
    
        public void SetState(PlayerItemState s)
        {
            state = s;
        
            switch (s)
            {
                case PlayerItemState.Alive:
                    currentTexture = alive;
                    Color color = normalColor;
                    if (team)
                    {
                        color = red ? redColor : blueColor;
                    }
                    defaultColor = color;
                    desiredColor = defaultColor;
                    break;
                case PlayerItemState.Dead:
                    currentTexture = dead;
                    defaultColor = Color.black;
                    desiredColor = defaultColor;
                    break;
                case PlayerItemState.ClanOwner:
                    currentTexture = clanOwnerVerified;
                    break;
                default:
                    currentTexture = alive;
                    break;
            }


            stateImage.texture = currentTexture;
        }

        public void SetInfect(bool infect)
        {
            SetState(infect ? PlayerItemState.Infected : PlayerItemState.Alive);
            if (infect)
            {
                desiredColor = infectedColor;
                return;
            }
            if (team)
                desiredColor = red ? redColor : blueColor;
            else desiredColor = defaultColor;
        }
    }
}
