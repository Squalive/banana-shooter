
using Audio;
using CodingDaniel.MapEditor.UI;
using Demo;
using EZCameraShake;
using Manager;
using Menu;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using Quest;
using Riptide;
using UnityEngine;
using UnityEngine.InputSystem;

public class Grappling : MonoBehaviour
{
    public PlayerMovement playerMovement;
    private LineRenderer lr;
    private Vector3 grapplePoint;
    public LayerMask whatIsGrappleable,whatIsPlayer;
    private float maxDistance=100f;
    private SpringJoint joint;

    private void Start()
    {
        _isplayerMovementNotNull = playerMovement != null;
    }

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        spring = new Spring();
        spring.SetTarget(0);

        _player = GetComponentInParent<PlayerState>();
    }

    private void OnDisable()
    {
        GameManager.InputManager.Player.Grapple.started -= StartGrapple;
        GameManager.InputManager.Player.Grapple.canceled -= StopGrapple;
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            GameManager.InputManager.Player.Grapple.started +=  StartGrapple;
            GameManager.InputManager.Player.Grapple.canceled += StopGrapple;
        }
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    public bool hitGrapplePoint;

    private bool tutorial=false;
    RaycastHit hit;
    private void Update()
    {
        if (TabHolder.Instance!=null) return;
        if (_isplayerMovementNotNull && _player.Health > 0&& !NetworkManager.Instance.CheckMultiplayerGameModeStarted())
        {
            if (!tutorial&&(Physics.Raycast(playerMovement.playerCam.position, playerMovement.playerCam.forward, maxDistance,
                whatIsGrappleable) || Physics.SphereCast(playerMovement.playerCam.position,3.5f, playerMovement.playerCam.forward,out  hit, maxDistance,
                whatIsGrappleable)))
            {
                
                Tutorial.Instance.SetText("GrapplingTip",5);
                tutorial = true;
            }


            if ((Physics.Raycast(playerMovement.playerCam.position, playerMovement.playerCam.forward,out hit, maxDistance, whatIsGrappleable) || Physics.SphereCast(playerMovement.playerCam.position, 3.5f, playerMovement.playerCam.forward, out hit, maxDistance, whatIsGrappleable)))
            {
                Vector3 hitPoint = hit.transform.position;
                if (Physics.Raycast(playerMovement.playerCam.position, (hitPoint - playerMovement.playerCam.position).normalized, hit.distance,whatIsPlayer))
                {
               
                    hitGrapplePoint = false;
                    GrappleHint.Instance.DisableTarget();
                    return;
                }
                hitGrapplePoint = true;
                GrappleHint.Instance.SetTargetPos(hitPoint);
            }
            else
            {
                hitGrapplePoint = false;
                GrappleHint.Instance.DisableTarget();
            }
        }
        
    }

    float force = 5500f;
    private void FixedUpdate()
    {
        if (IsGrappling() && _player.selfControlled)
        {
            playerMovement.GetRb().AddForce(force*Time.deltaTime*(grapplePoint-player.position).normalized,ForceMode.Acceleration);
        }
    }

    public Transform player;
    private Vector3 currentGrapplePosition;
    void StartGrapple(InputAction.CallbackContext ctx)
    {
        if (TabHolder.Instance!=null||(playerMovement != null && _player.Health > 0 && !NetworkManager.Instance.CheckMultiplayerGameModeStarted()))
        {
            RaycastHit hit;
            if (Physics.Raycast(playerMovement.playerCam.position, playerMovement.playerCam.forward, out hit, maxDistance,
                whatIsGrappleable))
            {
                Grapple(hit);

            }
            else if (Physics.SphereCast(playerMovement.playerCam.position,3.5f, playerMovement.playerCam.forward, out hit, maxDistance,
                whatIsGrappleable))
            {
                Grapple(hit);
            }
        }
        
    }

    private GameObject lastObj;
    // private bool canGrappleExpert = true;

    // void ReadyToExpert()
    // {
    //     canGrappleExpert = true;
    // }
    private bool grappled=false;
    void Grapple(RaycastHit hit)
    {
        if (!grappled)
        {
            grappled = true;
            QuestManager.Instance.GetProgress(QuestType.GrappleMan);
        }
        isGrappling = true;
        if(GrappleHint.Instance)
            GrappleHint.Instance.DisableTarget();
        CameraShaker.Instance.ShakeOnce(.5f, .5f, 0.1f, 0.2f);
        grapplePoint = hit.point;
        joint = playerMovement.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = grapplePoint;
            
        float distanceFromPoint = Vector3.Distance(playerMovement.transform.position, grapplePoint);

        //The distance grapple will try to keep from grapple point. 
        joint.maxDistance = distanceFromPoint * 0.65f;
        joint.minDistance = distanceFromPoint * 0.25f;

        //Adjust these values to fit your game.
        joint.spring = 4.5f;
        joint.damper = 7f;
        joint.massScale = 4.5f;
        AudioManager.Instance.Play("Grapple");
        
        if (TabHolder.Instance != null) return;
        
        Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.StartGrapple);

        message.Add(GetGrapplePoint());
        
        NetworkManager.Instance.SendByte += message.WrittenLength;
        NetworkManager.Instance.Client.Send(message);
        
        DemoManager.Instance.AddPlayerGrapple(ClientPlayer.LocalPlayer.demoPlayer.Id,true,grapplePoint);
        
        if (GrapplingAchivement.Instance)
        {
            if (!playerMovement.IsGrounded() && (lastObj==null || hit.collider.gameObject!=lastObj))
            {
                lastObj = hit.collider.gameObject;
                GrapplingAchivement.Instance.currentGrapplePoint++;
            }
        }
    }

    public void StopGrapple(InputAction.CallbackContext ctx)
    {
        isGrappling = false;
        Destroy(joint);
        if (TabHolder.Instance != null) return;
        Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.StopGrapple);
        NetworkManager.Instance.SendByte += message.WrittenLength;
        
        NetworkManager.Instance.Client.Send(message);
        
        DemoManager.Instance.AddPlayerGrapple(ClientPlayer.LocalPlayer.demoPlayer.Id,false,Vector3.zero);
    }
    private Spring spring;
    public int quality;
    public float damper;
    public float strength;
    public float velocity;
    public float waveCount;
    public float waveHeight;
    public AnimationCurve affectCurve;

    void DrawRope() {
        //If not grappling, don't draw rope
        if (!IsGrappling()) {
            currentGrapplePosition = player.position;
            spring.Reset();
            if (lr.positionCount > 0)
                lr.positionCount = 0;
            return;
        }

        if (lr.positionCount == 0) {
            spring.SetVelocity(velocity);
            lr.positionCount = quality + 1;
        }
        
        spring.SetDamper(damper);
        spring.SetStrength(strength);
        spring.Update(Time.deltaTime);

        Vector3 grapplePoint = GetGrapplePoint();

        var gunTipPosition = player.position;
        var up = Quaternion.LookRotation((grapplePoint - gunTipPosition).normalized) * Vector3.right;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, grapplePoint, Time.deltaTime * 12f);

        for (var i = 0; i < quality + 1; i++) {
            var delta = i / (float) quality;
            var offset =waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta)* up;
            
            lr.SetPosition(i, Vector3.Lerp(gunTipPosition, currentGrapplePosition, delta) + offset);
        }
    }

    public bool isGrappling = false;
    public bool IsGrappling()
    {
        return isGrappling;
    }

    private PlayerState _player;
    private bool _isplayerMovementNotNull;

    public Vector3 GetGrapplePoint() {
        return grapplePoint;
    }

    [MessageHandler((ushort) ServerToClientId.StartGrapple, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
    private static void StartGrappleClient(Message message)
    {
        ushort id = message.GetUShort();
        Vector3 grapplePoint = message.GetVector3();
        if (ClientPlayer.list.TryGetValue(id,out var player))
        {
            DemoManager.Instance.AddPlayerGrapple(player.demoPlayer.Id,true,grapplePoint);
            player.playerState.grappling.StartGrapple(grapplePoint);
        }
    }

    [MessageHandler((ushort) ServerToClientId.StopGrapple, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
    private static void StopGrappleClient(Message message)
    {
        ushort id = message.GetUShort();
        if (ClientPlayer.list.TryGetValue(id, out var player))
        {
            DemoManager.Instance.AddPlayerGrapple(player.demoPlayer.Id,false,Vector3.zero);
            player.playerState.grappling.StopGrapple();
        }
    }

    public void StartGrapple(Vector3 point)
    {
        AudioManager.Instance.SoundEffect3D("Grapple", transform.position);
        grapplePoint = point;
        isGrappling = true;
    }

    public void StopGrapple()
    {
        isGrappling = false;
    }
}

public class Spring {
    private float strength;
    private float damper;
    private float target;
    private float velocity;
    private float value;
 
    public void Update(float deltaTime) {
        var direction = target - value >= 0 ? 1f : -1f;
        var force = Mathf.Abs(target - value) * strength;
        velocity += (force * direction - velocity * damper) * deltaTime;
        value += velocity * deltaTime;
    }
 
    public void Reset() {
        velocity = 0f;
        value = 0f;
    }
        
    public void SetValue(float value) {
        this.value = value;
    }
        
    public void SetTarget(float target) {
        this.target = target;
    }
 
    public void SetDamper(float damper) {
        this.damper = damper;
    }
        
    public void SetStrength(float strength) {
        this.strength = strength;
    }
 
    public void SetVelocity(float velocity) {
        this.velocity = velocity;
    }
        
    public float Value => value;
}


