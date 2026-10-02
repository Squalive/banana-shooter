
using System.Collections;
using System.Collections.Generic;

using Cosmetic;
using Manager;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class TournamentMenu : MonoBehaviour
    {
        public static TournamentMenu Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] private GameObject menu;
        [SerializeField] private LocalizeStringEvent nameText;
        [SerializeField] private TextMeshProUGUI desc;
        [SerializeField] private RawImage icon;
        [SerializeField] private Transform prizePoolContent;

        private void Start()
        {
            join.onClick.AddListener(Join);
            StartCoroutine(GetLobby());
        }

        IEnumerator GetLobby()
        {
            yield return new WaitForSeconds(.5f); 
            // LobbyMenu.Instance.GetLobbyList(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide,SearchLobbyType.Tournament);
        }

        class TournamentItem
        {
            public CSteamID LobbyId;
            public string Name;
            public string Desc;
            public int IconIndex;
            public List<int> Prizes;

            public TournamentItem(string name, string desc, int iconIndex, List<int> prizes,CSteamID lobbyId)
            {
                Name = name;
                Desc = desc;
                IconIndex = iconIndex;
                Prizes = prizes;
                LobbyId = lobbyId;
            }
        }

        
        private List<TournamentItem> _tournaments = new List<TournamentItem>();

        private static List<ulong> existTournament = new List<ulong>();
        public void CheckTournament(CSteamID lobbyId)
        {
            if (existTournament.Contains(lobbyId.m_SteamID)) return;
            bool enable = SteamMatchmaking.GetLobbyData(lobbyId, "tournament_enable") == "1";
            if (enable)
            {
                string tourName = SteamMatchmaking.GetLobbyData(lobbyId, "tournament_name");
                string tourDesc = SteamMatchmaking.GetLobbyData(lobbyId, "tournament_desc");
                string tourPrize = SteamMatchmaking.GetLobbyData(lobbyId, "tournament_prize_pool");

                List<int> prizes = new List<int>();
                string[] p = tourPrize.Split(';');
                foreach (var p1 in p)
                {
                    if (int.TryParse(p1, out int i))
                    {
                        prizes.Add(i);
                    }
                }

                TournamentItem item = new TournamentItem(tourName,tourDesc,0,prizes,lobbyId);
                
                _tournaments.Add(item);
                preview.interactable = _currentIndex != 0;
                next.interactable = _currentIndex != _tournaments.Count - 1;
                if (_tournaments.Count == 1)
                {
                    _currentIndex = 0;
                    
                    GenerateItems(item);
                    
                }
                
                existTournament.Add(lobbyId.m_SteamID);
            }
        }

        private int _currentIndex = 0;
        
        public void NextTournament(int i)
        {
            _currentIndex += i;
            if (_currentIndex >= _tournaments.Count) _currentIndex = 0;
            if (_currentIndex < 0) _currentIndex = _tournaments.Count - 1;
            preview.interactable = _currentIndex != 0;
            next.interactable = _currentIndex != _tournaments.Count - 1;
            
            GenerateItems(_tournaments[_currentIndex]);
        }

        [SerializeField] private Button next, preview,join;
        void GenerateItems(TournamentItem tournamentItem)
        {
            for (int i = 0; i < prizePoolContent.childCount; i++)
            {
                Destroy(prizePoolContent.GetChild(i).gameObject);
            }
            menu.SetActive(true);
            nameText.StringReference.Arguments = new List<object>(){tournamentItem.Name};
            nameText.RefreshString();
            desc.SetText(tournamentItem.Desc);

            foreach (var prize in tournamentItem.Prizes)
            {
                CosmeticItem cosmeticItem = CosmeticManager.ItemIdToItem[prize];
                Transform item = Instantiate(PrefabManager.Instance.GetPrefab("Cosmetic"),
                    prizePoolContent).transform;
            
                item.GetChild(2).GetChild(0).GetComponent<RawImage>().texture = cosmeticItem.icon;
                Transform child3 = item.GetChild(3);
                child3.GetComponent<TextMeshProUGUI>().color = cosmeticItem.GetColor();
                Transform child1 = item.GetChild(1);
                Color c = cosmeticItem.GetColor();
                child1.GetComponent<RawImage>().color = c;
                child3.GetComponent<TextMeshProUGUI>().color = c;

                child3.GetComponent<TextMeshProUGUI>().SetText(cosmeticItem.displayName);
            }
        }

        void Join()
        {
            menu.SetActive(false);
            LobbyMenu.Instance.JoinLobby(_tournaments[_currentIndex].LobbyId.m_SteamID);
        }
    }
}
