using System;
using UnityEngine;

namespace Multiplayer.Server
{
    public class ServerPlayerCollision : MonoBehaviour
    {
        private Rigidbody _rb;
        private Transform _transform;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _transform = transform;
        }

        [SerializeField] private float kickForce = 0.008f;

        private void OnCollisionEnter(Collision other)
        {
            Rigidbody rb = other.rigidbody;
            Vector3 dir = -other.contacts[0].normal;

            if (dir!=Vector3.down && Vector3.Angle(dir, _rb.velocity) < 30)
            {
                dir +=Vector3.up;
                rb.AddForce(0.01f*kickForce*Time.deltaTime*dir,ForceMode.Impulse);
            }
        }

        private void OnCollisionStay(Collision other)
        {
            Rigidbody rb = other.rigidbody;
            Vector3 dir = -other.contacts[0].normal;

            if (dir!=Vector3.down && Vector3.Angle(dir, _rb.velocity) < 30)
            {
                rb.AddForce(kickForce*Time.deltaTime*dir,ForceMode.Force);
            }
        }
    }
}
