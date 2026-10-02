using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Extensions
{
    public static class MyVector3Extensions
    {
        public static List<MyVector3> ToMyVector3List(this List<Vector3> list)
        {
            List<MyVector3> newList = new List<MyVector3>();

            foreach (var vector3 in list)
            {
                newList.Add(new MyVector3(vector3));
            }

            return newList;
        }
        
        public static List<Vector3> ToVector3List(this List<MyVector3> list)
        {
            List<Vector3> newList = new List<Vector3>();

            foreach (var vector3 in list)
            {
                newList.Add(vector3.ToVector3());
            }

            return newList;
        }

        public static MyVector3 ToMyVector3(this Vector3 vector3)
        {
            return new MyVector3(vector3);
        }
    }
}