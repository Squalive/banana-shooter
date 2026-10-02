
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

public class Recoil : MonoBehaviour
{
    public static Recoil Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetFactor(PerkManager.Instance.HasPerk(Perk.Bot) ? 0.5f : 1.1f);
    }

    internal Vector3 currentRotation, targetRotation;

    [SerializeField] private float snappiness, returnSpeed;

    float recoilFactor = 1f;

    public void SetFactor(float factor)
    {
        recoilFactor = factor;
    }

    public void CalculateRecoil()
    {
        targetRotation = Vector3.Lerp(targetRotation,Vector3.zero, returnSpeed*Time.fixedDeltaTime);
        currentRotation = Vector3.Slerp(currentRotation,targetRotation,Time.fixedDeltaTime*snappiness);
    }
    public void RecoilFir(float x, float y, float z)
    {
        targetRotation += recoilFactor*new Vector3(x, Random.Range(-y, y), Random.Range(-z, z));
    }
}
