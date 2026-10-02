using UnityEngine;
using UnityEngine.EventSystems;

namespace Menu
{
    public class Item3DViewer : MonoBehaviour,IDragHandler
    {
    
        private void Awake()
        {
            _targetRot = obj.rotation;
        }

        private Quaternion _targetRot;
        public Transform obj;

        private float interpolationSpeed = 5f;
        
        public void OnDrag(PointerEventData eventData)
        {
            if (Input.GetMouseButton(0))
            {
                if (obj != null)
                {
                    eventData.delta = new Vector2(eventData.delta.x, 0) * 0.2f;
                    var axis = Quaternion.AngleAxis(-90f, Vector3.forward) * eventData.delta;

                    _targetRot = Quaternion.AngleAxis(eventData.delta.magnitude, axis) * _targetRot;
                }
            }
        
        }

        private void Update()
        {
            obj.rotation = Quaternion.Slerp(obj.rotation,_targetRot,interpolationSpeed*Time.deltaTime);
        }

        public void SetObj(Transform t)
        {
            _targetRot = t.rotation;
            obj = t;
        }
    }
}
