
using UnityEngine;
using Weapon;

public class SmokeTrail : MonoBehaviour
{
    public Firearms weapon;
    public Transform parent;

    private void Update()
    {
        if (parent == null) return;
        if (!weapon.gameObject.activeSelf) return;
        transform.position = parent.position;
    }
}
