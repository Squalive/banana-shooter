
using System;
using System.Collections.Generic;
using Audio;
using EZCameraShake;
using Manager;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using PlayerCameraController;
using Pool;
using Riptide;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using Weapon;
using Random = UnityEngine.Random;

public class InfectedHand : MonoBehaviour
{
    public static InfectedHand Instance;

    private void Awake()
    {
        Instance = this;
    }

    SkinnedMeshRenderer arm;

    public Rig armRig;
    public bool isInfected=false;
    public GameObject clawKnife;
    public Transform fpsArm;

    private Animator armAnim;
    private static readonly int IsInfected = Animator.StringToHash("IsInfected");
    private void Start()
    {
        arm = WeaponManager.Instance.arm;
        armAnim = WeaponManager.Instance.armAnimator;

        playerCam = MoveCamera.Instance.camTransform;
    }
    private static readonly int WeaponIndex = Animator.StringToHash("WeaponIndex");
    public void SetInfect(bool flag)
    {
        isInfected = flag;

        if (arm == null)
            arm = WeaponManager.Instance.arm;
        if(armAnim==null)
            armAnim = WeaponManager.Instance.armAnimator;
        
        armAnim.SetBool(IsInfected,flag);
        armAnim.SetInteger(WeaponIndex,flag || WeaponManager.Instance.CurrentWeapon==null ? -1 : WeaponManager.Instance.CurrentWeapon.weaponIndex);
        arm.material.color = flag ? Color.green : Color.white;
        
        arm.enabled = flag && GameManager.Instance.setting.useArm;
        clawKnife.SetActive(flag);

        armRig.weight = flag ? 0 : 1;
    }

    private void Update()
    {
        if (!isInfected || WeaponManager.Instance.CurrentPlayer.Health <= 0) return;
        MovementBob();
        SpeedBob();

        Vector3 defaultPos = startPos;
        
        bool cursorVisible = Cursor.visible;
            
        float mx = cursorVisible ? 0 : Input.GetAxis("Mouse X");
        float my = cursorVisible ? 0 : Input.GetAxis("Mouse Y");
        
        if (!WeaponManager.Instance.CurrentPlayer.selfControlled)
        {
            mx = WeaponManager.Instance.CurrentPlayer.GetDeltaX();
            my = WeaponManager.Instance.CurrentPlayer.GetDeltaY();
            // Debug.Log(mx.ToString("F2") + " " + my.ToString("F2"));
        }

        float b = -mx * gunDrag * currentGunDragMultiplier*drag;
        float b2 = -my * gunDrag * currentGunDragMultiplier*drag;
        desX = Mathf.Lerp(desX, b, Time.unscaledDeltaTime * 10f);
        desY = Mathf.Lerp(desY, b2, Time.unscaledDeltaTime * 10f);
        Rotation(new Vector2(desX, desY));
        Vector3 b3=defaultPos+ desiredBob + speedBob +new Vector3(desX, desY, 0f);
        
        fpsArm.localPosition = Vector3.Lerp(fpsArm.localPosition, b3, Time.unscaledDeltaTime * 15f);
        if (NetworkManager.Instance.CantPlay())
        {
            return;
        }
        if (WeaponManager.Instance.CurrentPlayer.selfControlled && Mouse.current.leftButton.wasPressedThisFrame && !attacking)
        {
            Attack();
        }
    }

    private RaycastHit[] hits = new RaycastHit[5];
    private Transform playerCam;
    List<ClientPlayer> hitPlayer = new List<ClientPlayer>();

    private bool thump = false;
    void Attack()
    {
        attacking = true;
        AttackVisual();
        Invoke(nameof(StopAttack),0.23f);
        
        Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.Shoot);

        message.Add(NetworkManager.Instance.InterpolationTick);
        message.Add(playerCam.forward);
        message.Add(playerCam.position);

        NetworkManager.Instance.SendByte += message.WrittenLength;
        NetworkManager.Instance.Client.Send(message);
        hitPlayer.Clear();
        #region AttackHit

        int cnt = Physics.RaycastNonAlloc(playerCam.position,
            playerCam.forward,hits, 5f,PrefabManager.Instance.whatIsHittable);
        
        
        if (cnt > 0)
        {
            for(int i= 0;i<cnt;i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ClientPlayer"))
                {
                    ClientPlayer clientPlayer = hit.transform.root.GetComponent<ClientPlayer>();
                    if (hitPlayer.Contains(clientPlayer)) continue;
                    hitPlayer.Add(clientPlayer);
                }
            
                Hit(hit,false,thump);
            }
            return;
        }
        cnt = Physics.SphereCastNonAlloc(playerCam.position, 1,
            playerCam.forward,hits, 5f,PrefabManager.Instance.whatIsHittable);

        for(int i=0;i<cnt;i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ClientPlayer"))
            {
                ClientPlayer clientPlayer = hit.transform.root.GetComponent<ClientPlayer>();
                if (hitPlayer.Contains(clientPlayer)) continue;
                hitPlayer.Add(clientPlayer);
            }

            Hit(hit,false,thump);
        }

        
        // Message msg = Message.Create(MessageSendMode.unreliable,(ushort)ClientToServerId.Shoot);
        //
        // msg.Add(bulletCount);
        // msg.Add(hits.Length>0?hits[0].point: Vector3.zero);
        //
        // NetworkManager.Instance.Client.Send(msg);
        //

        #endregion
    }

    public void AttackVisual()
    {
        CameraShaker.Instance.ShakeOnce(3f, 3f, 0.1f, 0.4f);
        AudioManager.Instance.Play("knifeAttack");
        thump = !thump;
        armAnim.SetTrigger(Attack1);
        armAnim.SetBool(Thump,thump);
    }

    public int damage = 50;
    public int thumpDamage = 100;
    
    void Hit(RaycastHit hit,bool wall,bool thump)
    {
        Rigidbody rb = hit.collider.gameObject.GetComponent<Rigidbody>();
        int actualDamage = thump? thumpDamage : damage;
        if (rb&& hit.collider.gameObject.layer != LayerMask.NameToLayer("ClientPlayer"))
        {
            rb.AddForce(-hit.normal*1000f,ForceMode.Impulse);
        }
        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") || hit.collider.gameObject.layer == LayerMask.NameToLayer("EnemySpawner"))
        {
            if (GameManager.Instance.setting.spawnParticle)
            {
                ObjectPooler.Instance.SpawnFromPool("BulletHit",hit.point,Quaternion.LookRotation(hit.normal));
            }
            int rand = Random.Range(0, PrefabManager.Instance.broadSwordHit.Length );
            AudioManager.Instance.PlayGunReload(PrefabManager.Instance.broadSwordHit[rand]);
        }
        else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ShootingTarget"))
        {
            ShootingTarget target = hit.transform.root.GetComponent<ShootingTarget>();
            ShootingTarget2 target2 =  hit.transform.root.GetComponent<ShootingTarget2>();
            if(target!=null)
                target.Hit();
            else if(target2!=null)
                target2.SetTargetPos();
            if (GameManager.Instance.setting.spawnParticle)
            {
                ObjectPooler.Instance.SpawnFromPool("BulletHit",hit.point,Quaternion.LookRotation(hit.normal));
            }
            HitMarker.Instance.StartHitMarker(Color.white);
            HitMarker3D h = ObjectPooler.Instance.SpawnFromPool("HitMarker",hit.point, Quaternion.LookRotation(MoveCamera.Instance.transform.position-hit.point)).GetComponent<HitMarker3D>();
            h.text.SetText(actualDamage.ToString());
        }
        else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ClientPlayer"))
        {
            ClientPlayer player = hit.transform.root.GetComponent<ClientPlayer>();
            if (GameManager.Instance.setting.enableGore&&GameManager.Instance.setting.spawnParticle)
            {
                ObjectPooler.Instance.SpawnFromPool("Blood", hit.point, Quaternion.LookRotation(hit.normal));
            }
            if (NetworkManager.Instance.IsTeamMode(player.playerState) || (NetworkManager.ClientGameMode==GameMode.Infected && player.playerState.IsInfected==isInfected)) return;
            HitMarker.Instance.StartHitMarker(Color.green);
            // NetworkManager.Instance.TakeDamage(player.Id,thump,wall,1001);

            // HitMarker3D h = ObjectPooler.Instance.SpawnFromPool("HitMarker",hit. point, Quaternion.LookRotation(MoveCamera.Instance.transform.position-hit.point)).GetComponent<HitMarker3D>();
            // h.text.SetText(actualDamage.ToString());
 
        }
    }

    void StopAttack()
    {
        attacking = false;
    }

    private bool attacking;

    private void Rotation(Vector2 offset)
    {
        float num = offset.magnitude * 0.03f;
        if (offset.x < 0f)
        {
            num = 0f - num;
        }
        float y = offset.y;
        Vector3 euler = new Vector3(y: (0f - offset.x) * 40f, x: y * 80f , z: num * 50f);
        try
        {
            if (!(Time.deltaTime <= 0f))
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(euler), Time.deltaTime * 20f);
            }
        }
        catch (Exception)
        {
        }
    }

    private void MovementBob()
    {
        if (Mathf.Abs(WeaponManager.Instance.CurrentPlayer.GetVelocity().magnitude) < 4f || !WeaponManager.Instance.CurrentPlayer.Grounded || WeaponManager.Instance.CurrentPlayer.IsCrouching)
        {
            desiredBob = Vector3.zero;
            return;
        }
        float x = Mathf.PingPong(Time.time * bobSpeed, xBob) - xBob / 2f;
        float y = Mathf.PingPong(Time.time * bobSpeed, yBob) - yBob / 2f;
        float z = Mathf.PingPong(Time.time * bobSpeed, zBob) - zBob / 2f;
        desiredBob = new Vector3(x, y, z);
    }
    private void SpeedBob()
    {
        Vector2 vector = WeaponManager.Instance.CurrentPlayer.FindVelRelativeToLook()*drag;
        Vector3 vector2 = new Vector3(vector.x, WeaponManager.Instance.CurrentPlayer.GetVelocity().y, vector.y);
        vector2 *= -0.01f;
        vector2 = Vector3.ClampMagnitude(vector2, 0.1f);
        speedBob = Vector3.Lerp(speedBob, vector2, Time.deltaTime * 10f);
    }
    private Vector3 startPos=new Vector3(0,-0.5f,0);
    private float drag = 0.3f;
    private Vector3 desiredBob;

    private float xBob = 0.12f;

    private float yBob = 0.08f;

    private float zBob = 0.1f;

    private float bobSpeed = 0.45f;
    

    private float rVel;

    private float rPVel;

    private float gunDrag = 0.2f;

    public float currentGunDragMultiplier = .5f;

    private float desX;

    private float desY;

    private Vector3 speedBob;

    private float rotationOffset;

    private Vector3 prevRotation;
    private static readonly int Attack1 = Animator.StringToHash("Attack");
    private static readonly int Thump = Animator.StringToHash("Thump");
}
