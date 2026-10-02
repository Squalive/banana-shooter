using System;
using UnityEngine;
using UnityEngine.VFX;

namespace Pool
{
    public class PoolParticle : MonoBehaviour,IPooledObject
    {
        [SerializeField] private bool isVFX = false;
        private ParticleSystem _particle;
        private VisualEffect _visualEffect;
        public void OnObjectSpawn()
        {
            if(isVFX)
                _visualEffect.Play();
            else _particle.Play();
        }

        public void OnObjectInit()
        {
            if (isVFX)
                _visualEffect = GetComponent<VisualEffect>();
            else _particle = GetComponent<ParticleSystem>();
        }

        private void OnParticleSystemStopped()
        {
            gameObject.SetActive(false);
        }
    }
}