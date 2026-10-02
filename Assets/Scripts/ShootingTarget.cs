
using UnityEngine;

public class ShootingTarget : MonoBehaviour
{
    public enum TargetType
    {
        Normal,
        Parkour,
    }

    public TargetType type;
    private Vector3 desiredRot;

    private Vector3 defaultRot;
    public Vector3 targetRot = new Vector3(90, 0, 0);
    public bool hitted;
    private void Start()
    {
        defaultRot = transform.localRotation.eulerAngles;
        desiredRot=defaultRot;
    }

    public void Hit()
    {
        if (hitted) return;
        hitted = true;
        desiredRot = targetRot;
        if(type==TargetType.Normal)
            Invoke("Clear",4f);
        else if(type==TargetType.Parkour)
        {
            if (ShootingRange.Instance)
            {
                ShootingRange.Instance.PlusScore();
            }
        }
        JuicyScore.Instance.UpdateScore(25,JuicyScore.ScoreType.None);
    }

    public void Clear()
    {
        desiredRot = defaultRot;
        hitted = false;
    }

    private void Update()
    {
        transform.localRotation = Quaternion.Lerp(transform.localRotation,
            Quaternion.Euler(desiredRot),
            Time.deltaTime * 15f);
    }
}
