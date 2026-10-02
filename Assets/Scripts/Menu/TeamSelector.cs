using System.Collections;
using Audio;

using Multiplayer;
using Multiplayer.Entity.Server;
using Multiplayer.Interface;
using Multiplayer.Server;
using Riptide;
using TMPro;
using UnityEngine;

namespace Menu
{
    public class TeamSelector : MonoBehaviour
    {
        public static TeamSelector Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] private GameObject menu,rebelSelected,allianceSelected;

        [SerializeField] private TextMeshProUGUI timerText;
        private float _timer=0;
        public bool IsSelecting { get; private set; } = false;
        void SetPage(int time)
        {
            GameVoteMenu.Instance.IsVoting = false;
            IsSelecting = true;
            _timer = time;
            timerText.SetText(_timer.ToString("F"));
        
            menu.SetActive(true);
            GameUIManager.Instance.gameScene.SetActive(false);
        
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            StartCoroutine(Ticking(time));
        }
        IEnumerator Ticking(int time)
        {
            yield return new WaitForSeconds(time - 3);
            Tick();
            
            yield return new WaitForSeconds(1);
            Tick();
            
            yield return new WaitForSeconds(1);
            Tick();
        }
        void Tick()
        {
            AudioManager.Instance.Play("ticking");
        }
    
        private void Update()
        {
            if (IsSelecting)
            {
                _timer -= Time.deltaTime;
                timerText.SetText(_timer.ToString("F0"));
            }
        }

        
        public void Select(int t)
        {
            Team team = (Team) t;
            rebelSelected.SetActive(team == Team.Rebel);
            allianceSelected.SetActive(team != Team.Rebel);
        
            Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ClientToServerId.DoSelectTeam);
            message.Add(t);
            NetworkManager.Instance.SendByte += message.WrittenLength;
            NetworkManager.Instance.Client.Send(message);
        }

        [MessageHandler((ushort) ClientToServerId.DoSelectTeam, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void DoSelectTeam(ushort fromClient, Message message)
        {
            if (NetworkServerManager.ClientData.ContainsKey(fromClient))
            {
                int team =  message.GetInt();

                NetworkServerManager.ClientData[fromClient].DesiredTeam = team;
            }
        }
    }
}
