using System;
using TMPro;
using UnityEngine;

namespace Menu
{
    public class EndScreenUI : MonoBehaviour
    {
        public static EndScreenUI Instance { get; private set; }
        
        public GameObject endScreen;
        public TextMeshProUGUI winnerNameText;

        private void Awake()
        {
            Instance = this;
        }

        public void SetEndScreenValue(string playerName,string description)
        {
            endScreen.SetActive(true);
            winnerNameText.SetText(playerName + " : "+ description);
        }

        public void CloseEndScreen()
        {
            endScreen.SetActive(false);
        }
    }
}
