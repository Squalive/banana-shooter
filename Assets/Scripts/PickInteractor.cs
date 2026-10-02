
using System;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Riptide;
using UnityEngine;
using UnityEngine.InputSystem;

public class PickInteractor : MonoBehaviour
{
    public static PickInteractor Instance { get; private set; }
    private static bool _initialized = false;
    
    public LayerMask interactableLayer = 29;
    public ClientPickable interactable = null;
    RaycastHit[] hit = new RaycastHit[5];
    private Transform _transform;

    private PlayerState _player;
    private void Awake()
    {
        Instance = this;
        _transform = transform;
    }
    private void OnEnable()
    {
        GameManager.InputManager.Player.Interact.performed += Interact;
    }

    private void OnDisable()
    {
        GameManager.InputManager.Player.Interact.performed -= Interact;
    }

    public void InitializePlayer(PlayerState player)
    {
        _player = player;
        _initialized = true;
    }

    public void DeInitialize()
    {
        _initialized = false;
    }

    private void Interact(InputAction.CallbackContext obj)
    {
        if (!_initialized) return;
        if (interactable == null || _player.Health <= 0) return;
        if (GameUIManager.Instance.pause || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || NetworkManager.Instance.CantPlay()) return;
        
        Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ClientToServerId.PickPickable);
        message.Add(interactable.Id);
        NetworkManager.Instance.Client.Send(message);
        
        PickUI.Instance.DisablePickObj();
    }

    [SerializeField] private float radius = 5f;
    [SerializeField] private float distance = 6f;

    private float lastTime = -30;
    private void Update()
    {
        if (!_initialized) return;
        if (_player.Health <= 0) return;
        Ray ray = new Ray(_transform.position, _transform.forward);
        int cnt = Physics.SphereCastNonAlloc(ray, radius, hit, distance, interactableLayer, QueryTriggerInteraction.Collide);
        
        if (cnt>0)
        {
            for (int i = 0; i < cnt; i++)
            {
                GameObject obj = hit[i].collider.gameObject;
                if(!obj.CompareTag("Pickable"))continue;
                ClientPickable inter = obj.GetComponent<ClientPickable>();

                if (inter == null) return;

                if (interactable == null)
                {
                    interactable = inter;
                    PickUI.Instance.EnablePickObj(inter);
                    if (Time.time - lastTime > 30)
                    {
                        lastTime = Time.time;
                        Tutorial.Instance.SetText("InteractTip",5);
                    }
                }
                else if (inter != interactable)
                {
                    interactable = inter;
                    PickUI.Instance.EnablePickObj(inter);
                
                }
            }
            
        }
        else
        {
            if (interactable)
            {
                PickUI.Instance.DisablePickObj();
                
                interactable = null;
            }
        }
    }
    
    public bool HasInteractable()
    {
        return interactable != null;
    }
}
