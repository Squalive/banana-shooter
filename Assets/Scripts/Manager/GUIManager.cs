
using System;
using System.Text;
using Manager.Interface;
using Steamworks;
using UnityEngine;

namespace Manager
{
    public class GUIManager : MonoBehaviour
    {
        [SerializeField] private Font font;
        public IGui Gui { get; } = new EnableGui();
        
        private string _betaName = String.Empty;
        private bool _isBeta = false;

        private void Start()
        {
            _isBeta = SteamApps.GetCurrentBetaName(out _betaName, 128);
        }

        private void OnGUI()
        {
            // Define the base resolution that your GUI is designed for
            float baseWidth = 1920f;
            float baseHeight = 1080f;
            float baseFontSize = 12f;

            // Calculate the scaling factors based on the current screen size
            float scaleX = Screen.width / baseWidth;
            float scaleY = Screen.height / baseHeight;
            float scaleFont = Mathf.Min(scaleX, scaleY);
            
            // Begin a group to scale and position the GUI elements
            GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));

            GUI.skin.font = font;

            if (_isBeta)
            {
                string version = Application.version + $" [{_betaName}] - DLC";
                Gui.DrawText(version, Color.grey, new Rect(0, Screen.height - (20f * scaleY), 500f * scaleX, 50f * scaleY), Mathf.RoundToInt(baseFontSize * scaleFont), FontStyle.Normal);
            }

            // long gcCollectionCount = GC.CollectionCount(0);
            //
            // Gui.DrawText(gcCollectionCount.ToString(), Color.white, new Rect(0, Screen.height - (35f * scaleY), 500f * scaleX, 50f * scaleY), Mathf.RoundToInt(baseFontSize * scaleFont), FontStyle.Normal);
            
            GUI.EndGroup();
        }
    }
}
