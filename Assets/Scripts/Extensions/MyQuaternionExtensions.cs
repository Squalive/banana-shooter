using UnityEngine;
using Utils;

namespace Extensions
{
    public static class MyQuaternionExtensions
    {
        public static MyQuaternion ToMyQuaternion(this Quaternion quaternion)
        {
            return new MyQuaternion(quaternion);
        }
    }
}