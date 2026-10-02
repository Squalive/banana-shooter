

using Movement;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Multiplayer.Interface;
using Steamworks.NET;
using UnityEngine;
using UnityEngine.Audio;

public class AudioFreqController : MonoBehaviour
{
    public static AudioFreqController Instance;

    private void Awake()
    {
        if(Instance==null)
            Instance = this;
    }

    [SerializeField] AudioMixer mixer;

    private ClientPlayer player;

    private void Update()
    {
        if (!SteamManager.Initialized) return;
        if (!NetworkManager.Instance.Client.IsConnected) return;
        if (player == null)
        {
            if (ClientPlayer.list.TryGetValue(NetworkManager.Instance.Client.Id,out var p))
            {
                player = p;
            }
        }
        else
        {
            float hz;
            int health = player.playerState.Health;
            if (health <= 0)
            {
                hz = 1f;
            }
            else
            {
                float thr = 0.9f;
                int maxHealth = player.playerState.MaxHealth;
                hz =  ((float) health /  maxHealth) <=  thr ? ((float) health / (maxHealth))*thr : 1f;
            }
            if (PlayerMovement.Instance.inWater)
                hz = 0.05f;
            mixer.GetFloat("LowpassFre",out float currentFre);
            mixer.SetFloat("LowpassFre",Mathf.Lerp(currentFre, 22000f * hz, Time.deltaTime * 8f));
            // filter.cutoffFrequency = Mathf.Lerp(filter.cutoffFrequency, 22000f * hz, Time.deltaTime * 8f);
        }
    }

    public void ResetStat()
    {
        mixer.SetFloat("LowpassFre",22000f);
        // filter.cutoffFrequency = 22000f;
    }
}
