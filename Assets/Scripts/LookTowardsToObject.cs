
using UnityEngine;

public class LookTowardsToObject : MonoBehaviour
{
    public Transform obj;

    private void Update()
    {
        if (obj)
        {
            transform.LookAt(obj);
        }
    }
}
