using System;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public class MyQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public MyQuaternion(float x, float y, float z,float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public MyQuaternion(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }
}