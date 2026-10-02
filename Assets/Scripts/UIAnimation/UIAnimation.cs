using System;
using UnityEngine;

namespace UIAnimation
{
    public class UIAnimation : MonoBehaviour
    {
        public enum UIAnimationType
        {
            Alpha=0,
            Pos=1,
            Rot=2,
            AlphaWithPos =3,
            AlphaWithRot = 4,
            All=5,
            AlphaSize,
            None,
        }

        public UIAnimationType type = UIAnimationType.All;

        private CanvasGroup _canvasGroup;
        private float desiredAlpha = 0;

        [SerializeField] private Transform targetTransform;

        private Vector3 desiredPos=new Vector3(-80, 0, 0);
        private Vector3 desiredRot = new Vector3(0, -50, 0);

        private Vector3 defaultPos, defaultRot;

        private Vector3 desiredSize,defaultSize;

        [SerializeField] private float speed = 10f,alphaSpeed=10f;

        private void Awake()
        {
            if (targetTransform)
            {
                defaultPos = targetTransform.localPosition;
                defaultRot = targetTransform.localRotation.eulerAngles;
                defaultSize = targetTransform.localScale;
            }

            switch (type)
            {
                case UIAnimationType.Alpha:
                    CreateCanvasGroup();
                    break;
                case UIAnimationType.AlphaWithPos:
                    DisablePos();
                    CreateCanvasGroup();
                    break;
                case UIAnimationType.AlphaWithRot:
                    DisableRot();
                    CreateCanvasGroup();
                    break;
                case UIAnimationType.All:
                    DisablePos();
                    DisableRot();
                    CreateCanvasGroup();
                    break;
                case UIAnimationType.AlphaSize:
                    CreateCanvasGroup();
                    DisableSize();
                    break;
            }
            
        }

        void CreateCanvasGroup()
        {
            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if(_canvasGroup==null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _canvasGroup.alpha = 0f;
        }

        
        private void OnEnable()
        {
            switch (type)
            {
                case UIAnimationType.Alpha:
                    EnableAlpha();
                    break;
                case UIAnimationType.Pos:
                    DisablePos();
                    EnablePos();
                    break;
                case UIAnimationType.Rot:
                    EnableRot();
                    break;
                case UIAnimationType.AlphaWithPos:
                    EnableAlpha();
                    EnablePos();
                    break;
                case UIAnimationType.AlphaWithRot:
                    EnableAlpha();
                    EnableRot();
                    break;
                case UIAnimationType.All:
                    EnableAlpha();
                    EnablePos();
                    EnableRot();
                    break;
                case UIAnimationType.AlphaSize:
                    EnableAlpha();
                    EnableSize();
                    break;
            }
        }

        void EnableSize()
        {
            desiredSize = defaultSize;
        }

        void EnableAlpha()
        {
            desiredAlpha = 1f;
            
        }

        void EnablePos()
        {
            desiredPos=defaultPos;
        }

        void EnableRot()
        {
            desiredRot = defaultRot;
        }
        private void OnDisable()
        {
            switch (type)
            {
                case UIAnimationType.Alpha:
                    DisableAlpha();
                    break;
                case UIAnimationType.Pos:
                    DisablePos();
                    break;
                case UIAnimationType.Rot:
                    DisableRot();
                    break;
                case UIAnimationType.AlphaWithPos:
                    DisableAlpha();
                    DisablePos();
                    break;
                case UIAnimationType.AlphaWithRot:
                    DisableAlpha();
                    DisableRot();
                    break;
                case UIAnimationType.All:
                    DisableAlpha();
                    DisablePos();
                    DisableRot();
                    break;
                case UIAnimationType.AlphaSize:
                    DisableAlpha();
                    DisableSize();
                    break;
            }
        }

        void DisableAlpha()
        {
            desiredAlpha = 0f;
            _canvasGroup.alpha = 0f;
        }

        void DisablePos()
        {
            desiredPos = new Vector3(-80, 0, 0) + defaultPos;
            targetTransform.localPosition = desiredPos;
        }

        void DisableRot()
        {
            desiredRot = new Vector3(0, -50, 0) + defaultRot;
            targetTransform.localRotation=Quaternion.Euler(desiredRot);
        }
        
        void DisableSize()
        {
            desiredSize = defaultSize*2f;
            targetTransform.localScale = desiredSize;
        }
        private void Update()
        {
            switch (type)
            {
                case UIAnimationType.Alpha:
                    UpdateAlpha();
                    break;
                case UIAnimationType.Pos:
                    UpdatePos();
                    break;
                case UIAnimationType.Rot:
                    UpdateRot();
                    break;
                
                case UIAnimationType.AlphaWithPos:
                    UpdatePos();
                    UpdateAlpha();
                    break;
                case UIAnimationType.AlphaWithRot:
                    UpdateRot();
                    UpdateAlpha();
                    break;
                case UIAnimationType.All:
                    UpdatePos();
                    UpdateRot();
                    UpdateAlpha();
                    break;
                case UIAnimationType.AlphaSize:
                    UpdateAlpha();
                    UpdateSize();
                    break;
            }
        }

        void UpdateSize()
        {
            targetTransform.localScale = Vector3.Slerp(targetTransform.localScale, desiredSize, Time.deltaTime * speed);
        }

        void UpdateAlpha()
        {
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, desiredAlpha, Time.deltaTime * alphaSpeed);
        }

        void UpdatePos()
        {
            targetTransform.localPosition = Vector3.Lerp(targetTransform.localPosition,desiredPos,Time.deltaTime*speed);
        }

        void UpdateRot()
        {
            targetTransform.localRotation = Quaternion.Lerp(targetTransform.localRotation, Quaternion.Euler(desiredRot),
                Time.deltaTime * speed);
        }
    }
}
