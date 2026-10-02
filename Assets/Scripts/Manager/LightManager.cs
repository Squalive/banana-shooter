using System.Collections.Generic;
using Movement;
using UnityEngine;

namespace Manager
{
    public class LightManager : MonoBehaviour
    {
        public float maxDistance=50;

        public List<GameObject> lights = new List<GameObject>();

        private void Update()
        {
            if (!PlayerMovement.Instance) return;
            foreach (var light in lights)
            {
                float dis = Vector3.Distance(light.transform.position,PlayerMovement.Instance.GetRb().position);

                if (dis > maxDistance)
                {
                    if (light.activeSelf)
                    {
                        light.SetActive(false);
                    }
                }
                else
                {
                    if(!light.activeSelf)light.SetActive(true);
                }
            }
        }
    }
}
