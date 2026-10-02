using System.Collections;
using Menu;
using UnityEngine;

namespace PlayerCameraController
{
    public class DeadCamera : MonoBehaviour
    {
        private static DeadCamera _instance;

        public static DeadCamera GetInstance()
        {
            return _instance;
        }

        private void Awake()
        {
            _instance = this;
            // deadCameraTransform = transform;
        }

        [SerializeField] private Transform deadCameraTransform;

        private bool _dead = false;

        private void Update()
        {
            // if (NetworkManager.Instance.CantPlay()) return;

            if (_dead)
            {
                if ( target != null)
                    targetPos = target.position;
                if (Vector3.Distance(deadCameraTransform.position, targetPos) > 1f)
                {
                    deadCameraTransform.position = Vector3.Slerp(deadCameraTransform.position,targetPos+_offset,Time.unscaledDeltaTime*10f);
                }
            
                deadCameraTransform.rotation = Quaternion.LookRotation(targetPos+_offset - deadCameraTransform.position);
            }
        }
        
        private Vector3 _offset;
        public void Dead(Vector3 pos,Vector3 offset,Transform head=null)
        {
            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.gameScene.SetActive(false);
            }
            _dead = false;
            _offset = offset;
            deadCameraTransform.rotation = Quaternion.LookRotation(pos - deadCameraTransform.position);
            deadCameraTransform.localPosition = new Vector3(0, -2, -5);
            // _parent.rotation = ;
            target = head;
            _coroutine= StartCoroutine(LookToTarget());
        }

        private Vector3 targetPos;

        private Transform target;
        IEnumerator LookToTarget()
        {
            yield return new WaitForSeconds(1f);
            _dead = true;
        }

        private Coroutine _coroutine;

        public void Respawn()
        {
            if(_coroutine!=null)
                StopCoroutine(_coroutine);
            _dead = false;
            if(GameUIManager.Instance) GameUIManager.Instance.gameScene.SetActive(true);
            deadCameraTransform.localRotation = Quaternion.identity;
            deadCameraTransform.localPosition = Vector3.zero;
        }
    }
}
