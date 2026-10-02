using System;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public class MyVector3
    {
        public float x;
        public float y;
        public float z;

        public MyVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public MyVector3(Vector3 p)
        {
            x = p.x;
            y = p.y;
            z = p.z;
        }
        public MyVector3()
        {
            
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    public static class VectorUtils
    {
        public static bool CheckIsTooLarge(this Vector3 pos)
        {
            if (Mathf.Abs(pos.x) > 120000 || Mathf.Abs(pos.y) > 120000 || Mathf.Abs(pos.z) > 120000) return true;
            return false;
        }
    }
}