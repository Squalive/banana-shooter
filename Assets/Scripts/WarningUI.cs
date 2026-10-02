
using System;
using MapEditor;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using TMPro;
using UnityEngine;

public class WarningUI : MonoBehaviour
{
    public static WarningUI Instance;
    [SerializeField] private CanvasGroup group;
    private GameObject groupObj;

    private float desiredAlpha = 0f;

    [SerializeField]private TextMeshProUGUI timerText;
    private float timer;
    public static float time = 3f;
    private void Awake()
    {
        Instance = this;

        groupObj = @group.gameObject;
    }
    

    private float maxY,maxX,minX,maxZ,minZ;

    private MapBound _bound;
    public bool playerSpawn;
    private ClientPlayer _player;

    public ClientPlayer Player
    {
        get
        {
            if (NetworkManager.Instance.Client.Connection != null&&_player == null && ClientPlayer.list.ContainsKey(NetworkManager.Instance.Client.Id))
            {
                return _player = ClientPlayer.list[NetworkManager.Instance.Client.Id];
            }
            return _player;
        }
    }
    private Transform _playerTransform;

    public Transform PlayerTransform
    {
        get
        {
            if (_playerTransform == null && ClientPlayer.list.ContainsKey(NetworkManager.Instance.Client.Id))
            {
                return _playerTransform = ClientPlayer.list[NetworkManager.Instance.Client.Id].player.transform;
            }
            return _playerTransform;
        }
    }

    void GetPlayer()
    {
        playerSpawn= Player != null;
        if(!playerSpawn)
            Invoke(nameof(GetPlayer),1f);
    }

    private void Start()
    {
        _bound = MapBound.Instance;
        maxY = _bound.maxY;
        
        maxX = _bound.maxX;
        maxZ = _bound.maxZ;
        
        minX = _bound.minX;
        minZ = _bound.minZ;
        
        Invoke(nameof(GetPlayer),1f);
    }

    private bool alreadyWarn = false;

    private void Update()
    {
        if (!playerSpawn)
        {
            return;
        }

        

        if (!Player.Dead)
        {
            var position = PlayerTransform.position;
            float x = position.x;
            float z = position.z;
            if (!NetworkManager.Instance.IsWorkshopMap)
            {
                if (position.y >= maxY || x >= maxX || x <= minX || z >= maxZ || z <= minZ)
                {
                    if (!alreadyWarn)
                    {
                        alreadyWarn = true;
                        desiredAlpha = 1;
                        groupObj.SetActive(true);
                        timer = time;
                    }

                    timer -= Time.deltaTime;
                    if (timer < 0) timer = 0;
                }
                else
                {
                    if (alreadyWarn)
                    {
                        alreadyWarn = false;
                        desiredAlpha = 0;
                        CancelInvoke(nameof(DisableGroup));
                        Invoke(nameof(DisableGroup),1.2f);
                    }
                }
            }
            
        }
        else
        {
            if (alreadyWarn)
            {
                alreadyWarn = false;
                desiredAlpha = 0;
                CancelInvoke(nameof(DisableGroup));
                Invoke(nameof(DisableGroup),1.2f);
            }
        }

        @group.alpha = Mathf.Lerp(@group.alpha, desiredAlpha, Time.deltaTime * 15f);
        
        timerText.SetText(timer.ToString("F2"));
        
    }

    void DisableGroup()
    {
        groupObj.SetActive(false);
    }
}
