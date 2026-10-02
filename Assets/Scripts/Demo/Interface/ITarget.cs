using UnityEngine;

namespace Demo.Interface
{
    public interface ITarget
    {
        public Transform[] Bones { get;protected set; }
    }
}