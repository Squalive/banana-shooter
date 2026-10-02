
using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Manager;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using PlayerCameraController;
using Pool;
using Steamworks;
using UnityEngine;
using Weapon;

public class Bullet : MonoBehaviour,IPooledObject
{
    public enum BulletType
    {
        Normal,
        Explosive,
    }

    public BulletType type=BulletType.Normal;
    private float speed;
    private int damage;

    private Transform selfTrans;
    [SerializeField] private Transform smokeTrans;
    private GameObject _smokeTrail;

    public bool initialized = false;
    private Vector3 startPos;
    private Rigidbody rb;


    public bool isLocal = true;
    private Vector3 dir;

    private TrailRenderer _renderer;

    void Init()
    {
        rb=GetComponent<Rigidbody>();
        selfTrans = transform;
        _renderer = selfTrans.GetComponentInChildren<TrailRenderer>();
    }

    public void Initialization(Vector3 dir, float bulletSpeed, int damage, LayerMask layerMask, bool local,
        bool useGravity, bool enableSmokeTrail = false)
    {
        hited = false;
        speed = bulletSpeed;
        this.dir = dir;
        selfTrans.rotation = Quaternion.LookRotation(dir);
        isLocal = local;

        rb.velocity = selfTrans.forward * speed;

        rb.useGravity = useGravity;

        this.damage = damage;

        enemies.Clear();
        clients.Clear();
        shootingTarget.Clear();
        shootingTarget2.Clear();

        startPos = selfTrans.position;
        gameObject.layer = layerMask;

        _renderer.Clear();
        initialized = true;
        Invoke(nameof(Disable), 5f);

        if (enableSmokeTrail)
        {
            if (_smokeTrail != null)
            {
                _smokeTrail.transform.SetParent(null);
                _smokeTrail.SetActive(false);
            }

            _smokeTrail = ObjectPooler.Instance.SpawnFromPool("SmokeTrail", smokeTrans.position, Quaternion.identity);
        
            _smokeTrail.GetComponent<TrailRenderer>().Clear();
        
            _smokeTrail.transform.SetParent(smokeTrans);
        }
            
    }

    private bool hited;
    List<Enemy> enemies = new();
    List<ClientPlayer> clients = new();
    List<ShootingTarget> shootingTarget = new();
    List<ShootingTarget2> shootingTarget2 = new();
    private Collider[] colliders = new Collider[80];
     private void OnHit(Vector3 point,Vector3 normal,Collider hit,bool wall)
     { 
         if (hited) return; 
         Rigidbody rb=hit.GetComponent<Rigidbody>();

         ShootingTarget target;
         ShootingTarget2 target2;
         Transform root;
         switch (type)
         {
             case BulletType.Normal:
                 root = hit.transform.root;
                 target = root.GetComponent<ShootingTarget>();
                 target2 =root.GetComponent<ShootingTarget2>();
                 if (rb && !rb.isKinematic)
                 {
                     rb.velocity += Vector3.one;
                     while (rb.velocity.sqrMagnitude>500)
                     {
                         rb.velocity *= 0.5f;
                     }

                     if (hit.CompareTag("Metal"))
                     {
                         AudioManager.Instance.SoundEffect3D("metal_hit", point);
                     }
                     
                     if (GameManager.Instance.setting.spawnParticle)
                     {
                         ObjectPooler.Instance.SpawnFromPool("BulletHit",point,Quaternion.LookRotation(normal));
                     }
                 }
                 if (hit.gameObject.layer == LayerMask.NameToLayer("Ground") || hit.gameObject.layer == LayerMask.NameToLayer("Interact"))
                 {
                     if (GameManager.Instance.setting.spawnParticle)
                     {
                         ObjectPooler.Instance.SpawnFromPool("BulletHit",point,Quaternion.LookRotation(normal));
                     }
                 }
                 else if (hit.gameObject.layer == LayerMask.NameToLayer("ShootingTarget") && ((target != null &&
                     !shootingTarget.Contains(target)) || (target2 != null && !shootingTarget2.Contains(target2))))
                 {
                     if (target != null)
                     {
                         target.Hit();
                         shootingTarget.Add(target);
                     }
                     else if (target2 != null)
                     {
                         target2.SetTargetPos();
                         shootingTarget2.Add(target2);
                     }
                     
                     
                     HitMarker.Instance.StartHitMarker(Color.white);

                     if (GameManager.Instance.setting.spawnParticle)
                     {
                         ObjectPooler.Instance.SpawnFromPool("BulletHit",point,Quaternion.LookRotation(normal));
                     }
                     HitMarker3D h = ObjectPooler.Instance.SpawnFromPool("HitMarker",point, Quaternion.LookRotation(ListenerManager.Instance.cameraTransform.position-point)).GetComponent<HitMarker3D>();
                     h.text.SetText(damage.ToString());
                 }
                 break;
             case BulletType.Explosive:
                 if (wall) break;
                 if(GameManager.Instance.setting.spawnParticle)Instantiate(PrefabManager.Instance.GetPrefab("ExplosionParticle"),point+Vector3.up,Quaternion.LookRotation(normal));
                 AudioManager.Instance.SoundEffect3D("Explosion",point, 1f, 20f);
                 int cnt= Physics.OverlapSphereNonAlloc(point, 18f,colliders);
                 for(int i=0;i<cnt;i++)
                 {
                     Collider col = colliders[i];
                     root = col.transform.root;
                     target = root.GetComponent<ShootingTarget>();
                     target2 = root.GetComponent<ShootingTarget2>();
                     if (col.gameObject.layer == LayerMask.NameToLayer("ShootingTarget") && ((target != null &&
                         !shootingTarget.Contains(target)) || (target2 != null && !shootingTarget2.Contains(target2))))
                     {
                         if (target != null)
                         {
                             target.Hit();
                             shootingTarget.Add(target);
                         }
                         else if (target2 != null)
                         {
                             target2.SetTargetPos();
                             shootingTarget2.Add(target2);
                         }
                         HitMarker.Instance.StartHitMarker(Color.white);
                         if (GameManager.Instance.setting.spawnParticle)
                         {
                             ObjectPooler.Instance.SpawnFromPool("BulletHit",point,Quaternion.LookRotation(normal));
                         }
                         HitMarker3D h = ObjectPooler.Instance.SpawnFromPool("HitMarker",point, Quaternion.LookRotation(MoveCamera.Instance.transform.position-point)).GetComponent<HitMarker3D>();
                         h.text.SetText(damage.ToString());
                     }
                     
                     // else if (col.gameObject.layer == LayerMask.NameToLayer("Client") && hit.transform.root.CompareTag("Enemy")  &&
                     //          !clientEnemies.Contains(clientEnemy))
                     // {
                     //     clientEnemies.Add(clientEnemy);
                     //     NetworkManager.Instance.HitEnemy(clientEnemy.Id,(ushort)weapon.weaponIndex,wall);
                     // }
                     // else if (col.gameObject.layer == LayerMask.NameToLayer("ClientPlayer") && !clients.Contains(player))
                     // {
                     //     clients.Add(player);
                     //     // bool isHead = col.gameObject.CompareTag("Head");
                     //     // int actualDamage = isHead ? damage * 2 : damage;
                     //     if(GameManager.Instance.setting.spawnParticle)Instantiate(PrefabManager.Instance.GetPrefab("Blood"), point, Quaternion.LookRotation(normal));
                     //     // if (NetworkManager.Instance.IsTeamMode(player))
                     //     // {
                     //     //     Disable();
                     //     //     return;
                     //     // }
                     //     // HitMarker.Instance.StartHitMarker(isHead?Color.yellow:Color.white);
                     //     
                     //     // NetworkManager.Instance.TakeDamage(player.Id,isHead,false,(ushort)weapon.weaponIndex);
                     //
                     //
                     //
                     //     // HitMarker3D h = ObjectPooler.Instance.SpawnFromPool("HitMarker", point, Quaternion.LookRotation(MoveCamera.Instance.transform.position-point)).GetComponent<HitMarker3D>();
                     //     // h.text.SetText(actualDamage.ToString());
                     //
                     // }
                     // if (rb&& ( col.gameObject.layer != LayerMask.NameToLayer("ServerPlayer")&& col.gameObject.layer != LayerMask.NameToLayer("ClientPlayer")) && !isLocal )
                     // {
                     //     rb.AddExplosionForce(2300f,point,25f);
                     // }
                 }
                 break;
         }
         
         Disable();

         Invoke(nameof(DestroySmoke), 5f);
     }

     void DestroySmoke()
     {
         if (_smokeTrail != null)
         {
             _smokeTrail.transform.SetParent(null);
             _smokeTrail.SetActive(false);
         }
     }

     void Disable()
     {
         if(_smokeTrail!=null)
            _smokeTrail.transform.SetParent(null);
         gameObject.SetActive(false);
         initialized = false;
     }


     public float penetrationAmount = 15;
     private RaycastHit[] hits = new RaycastHit[20];
     private void OnCollisionEnter(Collision other)
     {
         if ( hited  || !initialized) return;
         //if (other.collider.gameObject.layer == LayerMask.NameToLayer("Weapon")) return;
         
         OnHit(other.contacts[0].point,other.contacts[0].normal,other.collider,false);
         hits = new RaycastHit[20];
         int cnt = Physics.RaycastNonAlloc(other.contacts[0].point + dir * penetrationAmount, -dir,hits, penetrationAmount);
         return;
         if (cnt > 0)
         {
             for(int i= 0;i<cnt;i++)
             {
                 RaycastHit hitInfo = hits[i];
                 if (hitInfo.collider == other.collider) continue;
                 OnHit(hitInfo.point,hitInfo.normal,hitInfo.collider,true);
             }
             // Debug.DrawLine(other.contacts[0].point+dir*penetrationAmount,other.contacts[0].point ,Color.green,10f);
         }
         hited = true;
     }

     public void OnObjectSpawn()
     {
         
     }

     public void OnObjectInit()
     {
         Init();
     }
}
