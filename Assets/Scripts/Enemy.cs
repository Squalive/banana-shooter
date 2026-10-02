
using Audio;
using DitzelGames.FastIK;
using Manager;
using Movement;
using Pool;
using Steamworks;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    public int hp=100;
    private RigidEnemy ikController;
    Vector3 target;

    private void Awake()
    {
        ikController = GetComponent<RigidEnemy>();
    }

    private void Start()
    {
        if(PlayerMovement.Instance)
            target = PlayerMovement.Instance.playerCam.position-PlayerMovement.Instance.GetVelocity().normalized;
    }

    public float minDistance=15;
    public FastIKFabric handIk;
    public NavMeshAgent agent;

    Vector3 GetNavMeshPos()
    {
        agent.enabled = true;

        if (agent.isOnNavMesh)
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(PlayerMovement.Instance.transform.position, path))
            {
                agent.path = path;
            }
        }
        
            

        Vector3 pos = agent.steeringTarget;

        agent.enabled = false;

        return pos;
    }
    private void FixedUpdate()
    {
        
        if (ikController.root.transform.position.y < -50f)
        {
            TakeDamage(2000);
        }
        if (hp < 1 || ikController.state!= RigidEnemy.EnemyState.Active)
        {
            if (handIk.enabled) handIk.enabled = false;
            return;
        }

        if (PlayerMovement.Instance)
            target = PlayerMovement.Instance.playerCam.position - PlayerMovement.Instance.GetVelocity().normalized;
        else
            return;
        var position = ikController.root.position;
        Vector3 normalized = (target - position).normalized;
        Vector3 pos = GetNavMeshPos();
        float num = Vector3.Distance(target, position);
        Debug.DrawRay(position,(pos- position).normalized,Color.red);
        MoveLogic((pos - position).normalized, num);
        ikController.RotateBody(normalized);
        handIk.enabled =Mathf.Abs( num - minDistance) < 5f || num<minDistance;
        if (handIk.enabled)
        {
            handIk.Target.position = target;
            Attack((target - tip.position).normalized);
        }
    }

    public void TakeDamage(int damage)
    {
        int score = 25;
        hp -= damage;
        if (hp <= 0 && ikController.state != RigidEnemy.EnemyState.Dead)
        {
            score += 100;
            hp = 0;
            Destroy(handIk);
            ikController.UpdateState(RigidEnemy.EnemyState.Dead);
            AudioManager.Instance.Play("KillSecured");
        }
        JuicyScore.Instance.UpdateScore(score,JuicyScore.ScoreType.None);
    }
    private void MoveLogic(Vector3 moveDir, float distanceFromTarget)
    {
        int num = 1;
        if (distanceFromTarget < minDistance)
        {
            num = -1;
        }
        ikController.MoveBody(moveDir * num);
    }
    private bool readyToAttack = true;
    public Transform tip;
    public AudioClip shootClip;
    public AudioSource source;
    public ParticleSystem particleSystem;
    public int damage = 20;
    [Range(2, 100)] public float recoilRange = 5;
    private void Attack(Vector3 dir)
    {
        if (readyToAttack)
        {
            readyToAttack = false;
            source.PlayOneShot(shootClip);
            particleSystem.Play();
            float off = 0.05f;
            Invoke("GetReadyToAttack", UnityEngine.Random.Range(0.7f, 1f));
            Vector3 offset = new Vector3(Random.Range(-off,off), Random.Range(-off, off), 0);
            dir += offset;
            Bullet bullet = ObjectPooler.Instance.SpawnFromPool("Bullet", tip.position,
                Quaternion.LookRotation(dir)).GetComponent<Bullet>();

            bullet.Initialization(dir,200f,damage,LayerMask.NameToLayer("EnemyBullet"),false,false);
            ikController.rb.AddForce(-dir*recoilRange,ForceMode.Impulse);
        }
    }
    private void GetReadyToAttack()
    {
        readyToAttack = true;
    }
}
