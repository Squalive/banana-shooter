using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Audio
{
    public class AudioSyncLight : AudioSyncer
    {
        [SerializeField] private float beatIntensity = 0;
        [SerializeField] private float restIntensity = 0;
        
        [SerializeField] private Light lighting;
        
        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (IsBeat)
                return;

            lighting.intensity = Mathf.Lerp(lighting.intensity, restIntensity, restSmoothTime * Time.deltaTime);
        }

        public override void OnBeat()
        {
            base.OnBeat();
            
            StopCoroutine(nameof(MoveToScale));
            StartCoroutine(nameof(MoveToScale), beatIntensity);
        }

        private IEnumerator MoveToScale(float target)
        {
            float cur = lighting.intensity;
            float initial = cur;
            float timer = 0;

            while (Math.Abs(cur - target) > 0.01f)
            {
                cur = Mathf.Lerp(initial, target, timer / timeToBeat);
                timer += Time.deltaTime;

                lighting.intensity = cur;

                yield return null;
            }

            IsBeat = false;
        }
    }
}
