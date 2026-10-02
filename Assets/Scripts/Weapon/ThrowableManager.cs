
using System.Collections.Generic;
using Audio;
using CodingDaniel.MapEditor.UI;
using EZCameraShake;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Server;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Weapon
{
    public class ThrowableManager : MonoBehaviour
    {
        public static readonly float ThrowableIncrease=0.05f;

        public int TacticalThrowCount { get; set; } = 1;
        public bool IsThrowing { get; set; } = false;

        private RawImage _throwableIconImage;
        private Image _throwableValueFilled;
        private TextMeshProUGUI _throwableAmountText, _throwableKeyText;

        private bool _setThrowable = false;
        
        private float _throwablePower;
        
        private GameObject _parabolaTarget;
        private int _throwIndex = 0;
        private Collider[] _col = new Collider[1];

        public int numPoints = 50;
        private float _timeBetweenPoints = 0.01f;
        
        private LineRenderer _line;

        private PlayerState _currentPlayer;

        public void InitializePlayer(PlayerState currentPlayer)
        {
            _currentPlayer = currentPlayer;
        }

        public void SetUIValues(RawImage throwableIconImage, Image throwableValueFilled, TextMeshProUGUI throwableAmountText, TextMeshProUGUI throwableKeyText)
        {
            // _throwableIconImage = GameUIManager.Instance.throwObjImage;
            // _throwableValueFilled = GameUIManager.Instance.throwablesFilled;
            // _throwableAmountText = GameUIManager.Instance.throwObjCount;
            // _throwableKeyText = GameUIManager.Instance.throwKeyText;
            _throwableIconImage = throwableIconImage;
            _throwableValueFilled = throwableValueFilled;
            _throwableAmountText = throwableAmountText;
            _throwableKeyText = throwableKeyText;
            
            _throwableAmountText.SetText(NetworkManager.ClientGameMode == GameMode.SpecialGameMode
                ? "∞"
                : TacticalThrowCount.ToString());
            _throwableIconImage.texture = !InfectedHand.Instance.isInfected ?
                PrefabManager.Instance.throwObjTexture[(int) GameManager.Instance.tacticalProp] : PrefabManager.Instance.throwObjTexture[1] ;

            _throwIndex = !InfectedHand.Instance.isInfected ? (int)GameManager.Instance.tacticalProp : 1;
            _throwableValueFilled.fillAmount = _throwablePower;
        }

        private void OnEnable()
        {
            GameManager.InputManager.Player.Throw.started += ThrowStart;
            GameManager.InputManager.Player.Throw.canceled += ThrowTactical;
        }

        private void OnDisable()
        {
            GameManager.InputManager.Player.Throw.started -= ThrowStart;
            GameManager.InputManager.Player.Throw.canceled -= ThrowTactical;
        }

        private void Start()
        {
            if (_parabolaTarget == null)
            {
                _parabolaTarget = Instantiate(PrefabManager.Instance.GetPrefab("ParabolaTarget"),
                    transform.position, Quaternion.identity);
                
                _parabolaTarget.SetActive(false);
                
                DontDestroyOnLoad(_parabolaTarget);
            }
            
            _line = GetComponent<LineRenderer>();
            _line.enabled = IsThrowing;

            TacticalThrowCount = 1;
        }

        private void Update()
        {
            if (TacticalThrowCount <= 0)
            {
                _throwablePower += ThrowableIncrease * Time.deltaTime;
                if (_setThrowable)
                {
                    _throwableIconImage.color = Color.grey;
                    GameUIManager.Instance.throwObjCount.color = Color.grey;
                    GameUIManager.Instance.throwKeyText.color = Color.grey;
                }

                _throwableValueFilled.fillAmount = _throwablePower;

                if (_throwablePower >= 1)
                {
                    TacticalThrowCount++;
                    _throwableAmountText.SetText(TacticalThrowCount.ToString());
                    _throwableAmountText.color = Color.white;
                    _throwableKeyText.color = Color.white;
                    _throwableIconImage.color = Color.white;
                    _throwableValueFilled.fillAmount = 0;
                    _throwablePower = 0;
                    _setThrowable = true;
                }
            }
            else
            {
                if (_throwablePower > 0)
                {
                    _throwableAmountText.color = Color.white;
                    _throwableKeyText.color = Color.white;
                    _throwableIconImage.color = Color.white;
                    _throwableValueFilled.fillAmount = 0;
                    _throwablePower = 0;
                }
            }
        }

        private void LateUpdate()
        {
            _line.enabled = IsThrowing;
            if (IsThrowing)
            {
                _line.positionCount = numPoints;
                List<Vector3> points = new List<Vector3>();
                var transform1 = transform;
                Vector3 startPos = transform1.position-transform1.right*0.5f;
                Vector3 startVel = transform1.forward * ServerGrenade.forces[_throwIndex] + Vector3.up*ServerGrenade.forces[_throwIndex] /2f * ServerGrenade.gra[_throwIndex];
                for (float t = 0; t < numPoints; t += _timeBetweenPoints)
                {
                    Vector3 newPoint = startPos + t * startVel;
                    newPoint.y = startPos.y + startVel.y * t + Physics.gravity.y / 2f * t * t * ServerGrenade.gra[_throwIndex];
                    points.Add(newPoint);
                    int cnt = Physics.OverlapSphereNonAlloc(newPoint, 0.5f,_col, PrefabManager.Instance.whatIsHittable,QueryTriggerInteraction.Ignore);
                    if (cnt > 0)
                    {
                        _line.positionCount = points.Count;
                            
                        _parabolaTarget.transform.position = _col[0].bounds.ClosestPoint(newPoint);
                        break;
                    }
                }

                _line.SetPositions(points.ToArray());
            }
        }

        #region Input

        private void ThrowTactical(InputAction.CallbackContext obj)
        {
            if(TabHolder.Instance!=null)return;
            if (!_currentPlayer.selfControlled) return;
            if (NetworkManager.Instance.CantPlay() || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || _currentPlayer.Health<= 0 || !IsThrowing) return;
            if (TacticalThrowCount <= 0 && NetworkManager.ClientGameMode != GameMode.SpecialGameMode) return;
            IsThrowing = false;
            
            var transform1 = transform;
            Vector3 pos = transform1.position - transform1.right * 0.5f;
            Vector3 dir = transform1.forward;
        
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ClientToServerId.ThrowObj);
            message.Add(NetworkManager.Instance.ServerTick);
            message.Add(_throwIndex);
            message.Add(dir);
            message.Add(pos);
            NetworkManager.Instance.SendByte += message.WrittenLength;
            NetworkManager.Instance.Client.Send(message);

            ClientGrenade throwable = Throwable.InstantiateThrowable((ThrowObjectMenu.ThrowObjectType)_throwIndex, pos).GetComponent<ClientGrenade>();
            
            throwable.Initialize(3000,NetworkManager.Instance.Client.Id,(ThrowObjectMenu.ThrowObjectType)_throwIndex,dir,true);
            
            GameUIManager.Instance.SetThrowableCount();
            if (NetworkManager.ClientGameMode != GameMode.SpecialGameMode)
            {
                TacticalThrowCount--;
            }
            if (GameManager.Instance.setting.cameraShake) CameraShaker.Instance.ShakeOnce(2, 2f, 0.1f, 0.5f);
            AudioManager.Instance.Play("Swing");
        }

        private void ThrowStart(InputAction.CallbackContext obj)
        {
            if(TabHolder.Instance!=null)return;
            if (!_currentPlayer.selfControlled) return;

            if (NetworkManager.Instance.CantPlay() || TacticalThrowCount <= 0 || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || _currentPlayer.Health <= 0) return;

            _throwIndex = !InfectedHand.Instance.isInfected ? (int)GameManager.Instance.tacticalProp : 1;
            IsThrowing = true;
        }

        #endregion
    }
}