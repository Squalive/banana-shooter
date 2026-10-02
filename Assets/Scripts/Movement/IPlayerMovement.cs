using System;
using UnityEngine;

namespace Movement
{
    public interface IPlayerMovement
    {
        //Assingables
        PlayerMovement PlayerMovement { get; set; }
        Transform PlayerTransform { get; set; }
        Transform PlayerCam { get;set; }
        Transform Orientation{ get;set; }
        GameObject PlayerSmokeFx{ get;set; }
        //Other
        Rigidbody Rb{ get;set; }
        CapsuleCollider Collider { get; set; }
        
        void MyAwake(PlayerMovement playerMovement,Transform playerCam,Transform orientation,GameObject playerSmokeFx,Rigidbody rb,CapsuleCollider collider,Transform playerTransform);

        void MyOnEnable();
        void MyOnDisable();
        
        void MyFixedUpdate();
        void MyUpdate();
        void MyLateUpdate();

        void MyOnCollisionEnter(Collision other);
        void MyOnCollisionStay(Collision other);

        void DeInitialize();
    }
}