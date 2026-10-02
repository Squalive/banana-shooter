
using Demo.Entity.Enemy;
using Demo.Interface;
using Extensions;
using UnityEngine;
using Utils;

namespace Demo.Entity
{
    public abstract class DemoEntity : MonoBehaviour, ITarget
    {
        public int Id { get; private set; }
        
        public bool IsDestroyed { get; protected set; }
        
        [SerializeField] protected Rigidbody rb;
        [SerializeField] protected Transform selfTrans;
        [SerializeField] protected Transform orientation;
        
        [SerializeField] private Transform[] bones;
        
        Transform[] ITarget.Bones
        {
            get => bones;
            set => bones = value;
        }

        public virtual void Spawn(int id,params object[] obj)
        {
            Id = id;
        }

        private void OnDestroy()
        {
            if (IsEnemy())
            {
                DemoManager.Instance.DestroyEnemy(Id,((DemoEnemy)this).attackerId);
            }
            else
            {
                DemoManager.Instance.DestroyEntity(Id);
            }
        }

        public virtual bool IsPlayer()
        {
            return false;
        }
        
        public virtual bool IsEnemy()
        {
            return false;
        }
        
        public virtual bool IsThrowable()
        {
            return false;
        }

        public virtual void Destroy(params object[] param)
        {
            if (!IsDestroyed)
            {
                IsDestroyed = true;
                gameObject.SetActive(false);
            }
        }

        public virtual void Active()
        {
            IsDestroyed = false;
            gameObject.SetActive(true);
        }

        public Transform GetTransform()
        {
            return selfTrans;
        }

        public Rigidbody GetRb()
        {
            return rb;
        }

        public Vector3 GetPosition()
        {
            return selfTrans.position;
        }

        public float GetEulerAngleY()
        {
            return orientation.rotation.eulerAngles.y;
        }

        public MyVector3 GetVelocity()
        {
            return rb.velocity.ToMyVector3();
        }

        public MyQuaternion GetRotation()
        {
            return orientation.rotation.ToMyQuaternion();
        }

        
    }
}