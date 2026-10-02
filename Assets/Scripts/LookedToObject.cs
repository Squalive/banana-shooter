
using UnityEngine;

public class LookedToObject : MonoBehaviour
{
    public Transform parent;

    private void LateUpdate()
    {
        if (parent != null)
        {
            transform.position = parent.position;
            transform.rotation = parent.rotation;
        }
    }
}
