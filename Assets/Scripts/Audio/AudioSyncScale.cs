using System.Collections;
using UnityEngine;

namespace Audio
{
    public class AudioSyncScale : AudioSyncer
    {
        public Vector3 beatScale;
        public Vector3 restScale;

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (IsBeat)
                return;

            transform.localScale = Vector3.Lerp(transform.localScale, restScale, restSmoothTime * Time.deltaTime);
        }

        public override void OnBeat()
        {
            base.OnBeat();
            
            StopCoroutine(nameof(MoveToScale));
            StartCoroutine(nameof(MoveToScale), beatScale);
        }

        private IEnumerator MoveToScale(Vector3 target)
        {
            Vector3 cur = transform.localScale;
            Vector3 initial = cur;
            float timer = 0;

            while (cur != target)
            {
                cur = Vector3.Lerp(initial, target, timer / timeToBeat);
                timer += Time.deltaTime;

                transform.localScale = cur;

                yield return null;
            }

            IsBeat = false;
        }
    }
}