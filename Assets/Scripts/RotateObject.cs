
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    private Transform _transform;

    private void Start()
    {
        _transform = transform;
    }

    private void Update()
    {
        float z = Mathf.PingPong(Time.time, 1f);
        Vector3 axis = new Vector3(0, z, 0);
        _transform.Rotate(axis,1f);
    }
}
