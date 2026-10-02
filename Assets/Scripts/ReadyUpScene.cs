
using System.Collections.Generic;
using Manager;
using Menu;
using Multiplayer;
using TMPro;
using UnityEngine;

public class ReadyUpScene : MonoBehaviour
{
    public static ReadyUpScene Instance;

    private void Awake()
    {
        Instance = this;
        

        LobbyManager.Instance.SetLobbyGameMode();
        
        Invoke(nameof(Voice),1.5f);
    }

    public TextMeshProUGUI killLeaderBoard;
    void Voice()
    {
        int lastKill = GameManager.lastKill;
        int lastDie = GameManager.lastDie;
        bool getBox = GameManager.getBox;
        bool win = GameManager.win;

        if (getBox)
        {
            //ready_D
            VoiceLine.Instance.PlayVoice(VoiceKey.ready_D_01);
            VoiceLine.Instance.PlayVoice(VoiceKey.ready_D_02);
        }
        else if( win || (lastKill >= 40 && lastDie <= 15))
        {
            //ready_B
            VoiceLine.Instance.PlayVoice(VoiceKey.ready_B);
        }
        else if (lastKill <= 10 && lastDie > lastKill)
        {
            //ready_C
            VoiceLine.Instance.PlayVoice(VoiceKey.ready_C_01);
            VoiceLine.Instance.PlayVoice(VoiceKey.ready_C_02);
        }
        
        else
        {
            //ready_A
            VoiceLine.Instance.PlayVoice(VoiceKey.ready_A);
        }
    }

    public List<Transform> spawnPos = new List<Transform>();
}
