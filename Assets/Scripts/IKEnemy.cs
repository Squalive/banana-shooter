
using DitzelGames.FastIK;
using UnityEngine;
using Object = UnityEngine.Object;

public class IKEnemy : MonoBehaviour
{
    public LayerMask whatIsGround;
    public float heightAboveGround;
    
    public FastIKFabric[] legs;
    
    private Transform[] legTargets;

    private Vector3[] targetPositions;

    private Vector3[] currentPositions;

    public Vector3 legTargetOffset;

    public Transform root;

    private float thresholdDistance;

    private float[] legProgress;

    private RigidEnemy rigidEnemy;

    public float legSpeed = 10f;

    private Vector3 currentVelocity;

    public float upAmount = 2f;
    
    private void Start()
    {
        rigidEnemy = GetComponent<RigidEnemy>();
        legTargets = new Transform[legs.Length];
        targetPositions = new Vector3[legs.Length];
        currentPositions = new Vector3[legs.Length];
        legProgress = new float[legs.Length];
        InitLegTargets();
        thresholdDistance = legs[0].CompleteLength;
        UpdateLegTargets();
        UpdateCurrentLegPosition(0);
        UpdateCurrentLegPosition(1);
        InvokeRepeating("SlowUpdate", 1f, 1f);
    }
    
    private void SlowUpdate()
    {
        UpdateCurrentLegPositions(thresholdDistance * 0.2f);
    }
    private void Update()
    {
        currentVelocity = rigidEnemy.GetVelocity() * thresholdDistance;
        UpdateLegTargets();
        UpdateCurrentLegPositions(thresholdDistance);
        LerpLegs();
    }
    private void InitLegTargets()
    {
        for (int i = 0; i < legs.Length; i++)
        {
            int num = legs[i].ChainLength;
            Transform parent = legs[i].transform;
            while (num > 0)
            {
                parent = parent.parent;
                num--;
            }
            legTargets[i] = parent;
        }
    }

    private void UpdateLegTargets()
    {
        for (int i = 0; i < legTargets.Length; i++)
        {
            float f = 0.2f;
            Vector3 offset = i == 0 ? new Vector3(-f, 0, 0) : new Vector3(f, 0, 0);
            Vector3 vector = legTargets[i].position - root.position;
            Vector3 raycastPoint = legTargets[i].position + legTargetOffset.x * vector + currentVelocity + Vector3.up+offset;
            if (Physics.Raycast(raycastPoint, Vector3.down, out var hitInfo, 50f, whatIsGround))
            {
                targetPositions[i] = hitInfo.point;
            }
        }
    }

    private void UpdateCurrentLegPositions(float threshold)
    {
        for (int i = 0; i < legs.Length && (OppositeLegGrounded(i) || !(legProgress[i] < 0.01f) || !(CheckDistanceFromTargetPoint(i) < 4f)); i++)
        {
            if (CheckDistanceFromTargetPoint(i) > threshold)
            {
                UpdateCurrentLegPosition(i);
            }
        }
    }
    private bool OppositeLegGrounded(int leg)
    {
        int num = (leg + 1) % legs.Length;
        return legProgress[num] < 0.01f;
    }

    private float CheckDistanceFromTargetPoint(int leg)
    {
        return Vector3.Distance(currentPositions[leg], targetPositions[leg]);
    }

    private void UpdateCurrentLegPosition(int leg)
    {
        currentPositions[leg] = targetPositions[leg];
        legProgress[leg] = 1f;
    }

    private void LerpLegs()
    {
        for (int i = 0; i < legs.Length; i++)
        {
            Transform target = legs[i].Target;
            legProgress[i] = Mathf.Lerp(legProgress[i], 0f, Time.deltaTime * legSpeed);
            Vector3 vector = Vector3.up * upAmount * legProgress[i];
            target.position = Vector3.Lerp(target.position, currentPositions[i] + vector, Time.deltaTime * legSpeed);
        }
    }

    public void CollectGarbage()
    {
        FastIKFabric[] array = legs;
        for (int i = 0; i < array.Length; i++)
        {
            Object.Destroy(array[i].Target.gameObject);
        }
    }

    public void ForceCurrentPosition(int i)
    {
        if (legProgress != null)
        {
            _ = legProgress[i];
            legProgress[i] = 1f;
            legs[i].Target.position = legs[i].transform.position;
        }
    }

}
