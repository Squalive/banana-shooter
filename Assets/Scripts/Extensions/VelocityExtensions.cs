using UnityEngine;

namespace Extensions
{
    public static class VelocityExtensions
    {
        public static Vector2 FindVelRelativeToLook(float lookAngle, Vector3 velocity) {
            // float lookAngle = orientation.eulerAngles.y;
            // var velocity = rb.velocity;
            float moveAngle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;

            float u = Mathf.DeltaAngle(lookAngle, moveAngle);
            float v = 90 - u;

            float magnitue =velocity.magnitude;
            float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
            float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);
        
            return new Vector2(xMag, yMag);
        }
    }
}