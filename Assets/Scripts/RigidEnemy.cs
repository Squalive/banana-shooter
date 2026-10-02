using System;
using UnityEngine;

public class RigidEnemy : MonoBehaviour
{
    public enum EnemyState
    {
        Active,
        Tumbling,
        Falling,
        Recovering,
        Dead
    }
    public Transform root;

    public Transform head;

    public Transform torso;

    internal Rigidbody rb;

    private Rigidbody headRb;

    private Rigidbody torsoRb;

    
    public EnemyState state = EnemyState.Recovering;
    [HideInInspector]
    public float standUpForce,standUpMultiForce;
    public bool minOneGrounded;
    [HideInInspector]
    public Rigidbody[] rigs;
    private void Awake()
    {
        rb = root.GetComponent<Rigidbody>();
        if ((bool)head)
        {
            headRb = head.GetComponent<Rigidbody>();
        }
        else
        {
            headRb = rb;
        }
        if ((bool)torso)
        {
            torsoRb = torso.GetComponent<Rigidbody>();
        }
        else
        {
            torsoRb = rb;
        }
        rigs = GetComponentsInChildren<Rigidbody>();
        standUpForce = GetTotalMassInChild();
        ik = GetComponent<IKEnemy>();
        UpdateState(EnemyState.Active);
        nLegs = ik.legs.Length;
        groundChecks = new Transform[nLegs];
        for (int i = 0; i < nLegs; i++)
        {
            groundChecks[i] = ik.legs[i].transform;
        }
        DisableSelfCollision(true);
    }
    private Transform[] groundChecks;
    private int nLegs;
    [HideInInspector]
    public IKEnemy ik;
    public float groundCheckRadius = 0.2f;
    public float upMultiForceFadeSpeed=4000;
    public float legPushForce = 0.55f;
    public float gravityForceMultiplayer = 15000;
    public float gravityFactor = 0;
    
    public float moveSpeed = 10f;

    private float rotationForce = 300f;

    public float maxRotationForce = 0.05f;

    private float stabilizeForce = 1f;

    public float recoverTime = 2f;

    public float recoveryForce = 0.3f;

    public float tumbleAngle = 30f;

    public float fallAngle = 70f;

    public float getupMagT = 0.2f;

    public float getupAng = 15f;

    private bool ragdoll;
    [HideInInspector]
    public bool recovering;
    private void FixedUpdate()
    {
        if (state == EnemyState.Dead)
        {
            return;
        }
        minOneGrounded = false;
        for (int i = 0; i < nLegs; i++)
        {
            if (Physics.CheckSphere(groundChecks[i].position, groundCheckRadius, ik.whatIsGround))
            {
                minOneGrounded = true;
            }
        }
        if (state == EnemyState.Active || state == EnemyState.Tumbling || state == EnemyState.Recovering || state == EnemyState.Falling)
        {
            if (!Physics.Raycast(root.position, Vector3.down, out var hitInfo, ik.heightAboveGround * 3f, ik.whatIsGround))
            {
                UpdateState(EnemyState.Falling);
            }
            else
            {
                headDistanceToGround = hitInfo.distance;
            }
        }
        standUpMultiForce = upMultiForceFadeSpeed * (ik.heightAboveGround-headDistanceToGround);
        float num2 = Vector3.Angle(Vector3.up, root.up);
        if (state == EnemyState.Falling)
        {
            if (headDistanceToGround != 0f && headDistanceToGround < ik.heightAboveGround * 1.5f && num2 < 50f)
            {
                UpdateState(EnemyState.Active);
                CancelInvoke("GetUp");
                ConfigureLegs(false);
                recovering = false;
            }
            else if (!IsInvoking("GetUp"))
            {
                Invoke("GetUp", recoverTime);
            }
            return;
        }
        if (state == EnemyState.Recovering)
        {
            bool flag = Physics.CheckSphere(root.position, 0.5f, ik.whatIsGround);
            if (headDistanceToGround < ik.heightAboveGround || flag)
            {
                headRb.AddForce(Vector3.up * (standUpForce+standUpMultiForce) * recoveryForce * .8f);
                rb.AddForce(Vector3.up * (standUpForce+standUpMultiForce) * recoveryForce * 0.2f);
            }
            if ((num2 < getupAng && torsoRb.velocity.magnitude < getupMagT) || (headDistanceToGround > ik.heightAboveGround * 0.85f && headDistanceToGround < ik.heightAboveGround * 1.85f && num2 < 30f))
            {
                UpdateState(EnemyState.Active);
                CancelInvoke("RecoveryCooldown");
                Invoke("RecoveryCooldown", 2f);
            }
            return;
        }
        if (state == EnemyState.Active && rb.velocity.magnitude < 1f && headDistanceToGround > ik.heightAboveGround && headDistanceToGround < ik.heightAboveGround + ik.heightAboveGround * 0.1f)
        {
            headRb.AddForce(Vector3.up * (standUpForce) * 0.86f);
            return;
        }
       
        float num3 = Mathf.Clamp(1f -headDistanceToGround / ik.heightAboveGround, -1f, 1f);
        if (num2 < tumbleAngle)
        {
            UpdateState(EnemyState.Active);
        }
        else if (num2 < fallAngle)
        {
            UpdateState(EnemyState.Tumbling);
        }
        else if (num2 > fallAngle)
        {
            UpdateState(EnemyState.Falling);
        }
        if (minOneGrounded)
        {
            gravityFactor = 0;
            rb.AddForce(root.up * (standUpForce) *num3*1f);
            rb.AddForce(root.up * (standUpForce) *legPushForce);
        }

        // bool touchGround = false;
        // for (int i = 0; i < rigs.Length; i++)
        // {
        //     if (Physics.CheckSphere(rigs[i].position, groundCheckRadius, ik.whatIsGround))
        //     {
        //         touchGround = true;
        //         break;
        //     }
        // }
        //
        // if (!touchGround)
        // {
        //     gravityFactor += 1f * Time.fixedDeltaTime;
        //     if (gravityFactor>=2)
        //     {
        //         gravityFactor = 2;
        //     }
        //     for (int i = 0; i < rigs.Length; i++)
        //     {
        //         rigs[i].AddForce(Vector3.down * gravityForceMultiplayer * gravityFactor * Time.fixedDeltaTime);
        //     }
        // }
        if (headDistanceToGround < ik.heightAboveGround*2)
        {
            StandBalance();
        }
       
    }
    private void RecoveryCooldown()
    {
        recovering = false;
    }
    private void GetUp()
    {
        if (Physics.CheckSphere(root.position, ik.heightAboveGround * 0.5f, ik.whatIsGround))
        {
            UpdateState(EnemyState.Recovering);
            ConfigureLegs(false);
        }
        else
        {
            Invoke("GetUp", recoverTime);
        }
    }
    void StandBalance()
    {
        headRb.AddForce(Vector3.up*(standUpForce+standUpMultiForce));
        torsoRb.AddForce(Vector3.down*standUpForce);
    }
    float GetTotalMassInChild()
    {
        float mass = 0;
        for (int i = 0; i < rigs.Length; i++)
        {
            mass += rigs[i].mass;
        }

        return mass*(-Physics.gravity.y);
    }
    private void ConfigureLegs(bool makeRagdoll)
    {
        if (makeRagdoll == ragdoll)
        {
            return;
        }
        ragdoll = makeRagdoll;
        for (int i = 0; i < ik.legs.Length; i++)
        {
            ik.legs[i].enabled = !makeRagdoll;
            ik.ForceCurrentPosition(i);
        }
    }
    [HideInInspector]public float headDistanceToGround;
    private void DisableSelfCollision(bool ignore)
    {
        Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
        foreach (var collider1 in componentsInChildren)
        {
            foreach (var collider2 in componentsInChildren)
            {
                Physics.IgnoreCollision(collider1, collider2, ignore);
            }
        }
    }

    private float force;
    private void StabilizingBody()
    {
        headRb.AddForce(Vector3.up * force * stabilizeForce);
        torsoRb.AddForce(Vector3.down * force * stabilizeForce);
    }
    public void Concuss()
    {
        UpdateState(EnemyState.Falling);
        ConfigureLegs(true);
        recovering = true;
        Invoke("GetUp", recoverTime * UnityEngine.Random.Range(0.7f, 1.5f));
    }

    public void UpdateState(EnemyState s)
    {
        if (state != s)
        {
            state = s;
            switch (s)
            {
                case EnemyState.Active:
                    ConfigureRb(5f,5f,1f);
                    break;
                case EnemyState.Tumbling:
                    ConfigureRb(1f, 4f,.1f);
                    break;
                case EnemyState.Falling:
                    ConfigureRb(0f, 0f,0);
                    Concuss();
                    break;
                case EnemyState.Recovering:
                    ConfigureRb(4f, 4f,.15f);
                    break;
                case EnemyState.Dead:
                    HitMarker.Instance.StartHitMarker(Color.red);
                    ConfigureRb(0f, 0f,0);
                    KillRigidEnemy();
                    break;
                default:
                    rb.drag = 0f;
                    rb.angularDrag = 0f;
                    break;
            }
        }
    }
    public void KillRigidEnemy()
    {
        ConfigureLegs(true);
        CancelInvoke();
        ik.CollectGarbage();
        
        Destroy(gameObject,10f);
        Destroy(ik);
        Destroy(GetComponent<Enemy>());
       
    }
    private void ConfigureRb(float drag, float angularDrag,float stabilize)
    {
        for (int i = 0; i < rigs.Length; i++)
        {
            rigs[i].drag = drag;
            rigs[i].angularDrag = angularDrag;
        }
       
        stabilizeForce = stabilize;
    }
    private float moveLegsWithSpeedScale = .2f;
    public Vector3 GetVelocity()
    {
        if (!rb)
        {
            return Vector3.zero;
        }
        Vector3 result = rb.velocity * moveLegsWithSpeedScale;
        if (result.magnitude > 1f)
        {
            return result.normalized;
        }
        return result;
    }
    public void RotateBody(Vector3 dir)
    {
        if (state != EnemyState.Active && state!=EnemyState.Tumbling) return;
        float y = root.transform.eulerAngles.y;
        float y2 = Quaternion.LookRotation(dir).eulerAngles.y;
        float value = Mathf.DeltaAngle(y, y2);
        value = Mathf.Clamp(value, -2f, 2f);
        rb.AddTorque(Vector3.up * value * standUpForce * rotationForce);
    }

    public void MoveBody(Vector3 dir)
    {
        if (state != EnemyState.Active && state!=EnemyState.Tumbling) return;
        rb.AddForce(dir * moveSpeed * rb.mass);
        headRb.AddForce(dir * moveSpeed * headRb.mass);
        torsoRb.AddForce(dir * moveSpeed * torsoRb.mass);
    }
}
