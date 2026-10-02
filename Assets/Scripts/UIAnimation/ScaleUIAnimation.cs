using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIAnimation
{
    public class ScaleUIAnimation : BaseUIAnimation
    {
        [Header("Transforms which will be used to scale")]
        [SerializeField] private Transform[] transforms = Array.Empty<Transform>();
        
        [Header("Based on the amount of transform provided, the default scale")]
        [SerializeField] private Vector3[] defaultScale = Array.Empty<Vector3>();
        [Header("Scale When hover Each index is the same as transform")]
        [SerializeField] private Vector3[] hoverScale = Array.Empty<Vector3>();
        [Header("Scale When Click Down")]
        [SerializeField] private Vector3[] clickScale = Array.Empty<Vector3>();

        [Header("Speed of scaling")]
        [SerializeField] private float speed = 15f;


        private Vector3[] _desiredScale;

        protected override void Awake()
        {
            base.Awake();
            _desiredScale = new Vector3[transforms.Length];

            for (int i = 0; i < _desiredScale.Length; i++)
            {
                _desiredScale[i] = defaultScale[i];
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (isToggle && IsSelected)
            {
                return;
            }
            base.OnPointerEnter(eventData);
            for (int i = 0; i < _desiredScale.Length; i++)
            {
                _desiredScale[i] = hoverScale[i];
            }
        }
        
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (isToggle && IsSelected)
            {
                return;
            }
            for (int i = 0; i < _desiredScale.Length; i++)
            {
                _desiredScale[i] = defaultScale[i];
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            for (int i = 0; i < _desiredScale.Length; i++)
            {
                _desiredScale[i] = clickScale[i];
            }
        }
        
        public override void OnPointerUp(PointerEventData eventData)
        {
            if (isToggle && IsSelected)
            {
                return;
            }
            base.OnPointerUp(eventData);
            for (int i = 0; i < _desiredScale.Length; i++)
            {
                _desiredScale[i] = defaultScale[i];
            }
        }

        public override void DeSelect()
        {
            base.DeSelect();
            for (int i = 0; i < _desiredScale.Length; i++)
            {
                _desiredScale[i] = defaultScale[i];
            }
        }

        private void Update()
        {
            for (int i = 0; i < _desiredScale.Length; i++)
            {
                transforms[i].localScale =
                    Vector3.Slerp(transforms[i].localScale, _desiredScale[i], Time.deltaTime * speed);
            }
        }
    }
}