using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using Movement;
using UnityEngine;

namespace Menu
{
    public class HitDetection : MonoBehaviour
    {
        public static HitDetection Instance;

        
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            enabled = PerkManager.Instance.HasPerk(Perk.HitDetection);
        }

        public void SetProperty(PlayerMovement playerMovement)
        {
            orientation = playerMovement.orientation;
        }

        private Transform orientation;

        [SerializeField] private GameObject indicator;
        [SerializeField] private Transform gameUI;

        public void CreateIndicator(Vector3 dir)
        {
            if (!enabled) return;
            StartCoroutine(LerpRedArrow(dir));
        }

        public List<GameObject> list = new List<GameObject>();
        IEnumerator LerpRedArrow(Vector3 dir)
        {
            GameObject redArrow = Instantiate(indicator, Vector3.zero, Quaternion.identity, gameUI);
            list.Add(redArrow);
            CanvasGroup redArrowGroup = redArrow.GetComponent<CanvasGroup>();
            redArrowGroup.alpha = 1;
            RectTransform arrowRot = redArrow.GetComponent<RectTransform>();
            arrowRot.localPosition = Vector3.zero;
            RectTransform arrowImage = redArrow.GetComponentInChildren<RectTransform>();
            Quaternion tRot = Quaternion.LookRotation(dir-orientation.position);
            tRot.z = -tRot.y;
            tRot.x = 0f;
            tRot.y = 0f;

            Vector3 northDir = new Vector3(0, 0, orientation.eulerAngles.y);
            arrowRot.localRotation = tRot * Quaternion.Euler(northDir);
            arrowImage.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            float scale = 0.5f;
            Vector3 desiredSize = new Vector3(scale, scale, scale);
            while ((Vector3.Distance(arrowImage.localScale,desiredSize)>0.2f) 
                   || redArrowGroup.alpha!=0f)
            {
                tRot = Quaternion.LookRotation(dir-orientation.position);
                tRot.z = -tRot.y;
                tRot.x = 0f;
                tRot.y = 0f;

                northDir = new Vector3(0, 0, orientation.eulerAngles.y);
            
                arrowRot.localRotation = tRot * Quaternion.Euler(northDir);
                arrowImage.localScale = Vector3.Lerp(arrowImage.localScale,desiredSize,Time.deltaTime*3f);
                redArrowGroup.alpha = Mathf.Lerp(redArrowGroup.alpha, 0f, Time.deltaTime*1.5f);
                yield return null;
            }
        
            Destroy(redArrow);
        }
    }
}