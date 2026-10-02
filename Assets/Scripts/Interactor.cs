
using System;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Client;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    public static Interactor Instance;

    private static bool _initialized = false;
    
    public LayerMask interactableLayer = 19;
    public Interactable interactable = null;
    RaycastHit hit;
    private Transform _transform;

    private float lastTime = -30;
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
        
        interactable.interactEvent?.Invoke();
    }

    private void Update()
    {
        if (!_initialized) return;
        if (_player.Health <= 0) return;
        Ray ray = new Ray(_transform.position, _transform.forward);
        if (Physics.SphereCast(ray,1f, out hit, 3f, interactableLayer)||
            Physics.Raycast(ray, out hit, 3f, interactableLayer))
        {
            Interactable inter = hit.collider.GetComponent<Interactable>();

            if (inter == null) return;

            if (interactable == null)
            {
                interactable = inter;
                interactable.highLightEvent?.Invoke();
                InteractKeyTip.Instance.EnableObj(inter.transform.position);
                if (Time.time - lastTime > 30)
                {
                    lastTime = Time.time;
                    Tutorial.Instance.SetText("InteractTip");
                }
            }
            else if (inter != interactable)
            {
                interactable.deSelectEvent?.Invoke();
                interactable = inter;
                interactable.highLightEvent?.Invoke();
                InteractKeyTip.Instance.EnableObj(inter.transform.position);
            }
        }
        else
        {
            if (interactable)
            {
                interactable.deSelectEvent?.Invoke();
                interactable = null;
                InteractKeyTip.Instance.DisableObj();
            }
        }
    }
    
    public bool HasInteractable()
    {
        return interactable != null;
    }
}
