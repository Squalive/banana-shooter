
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShakeObject : MonoBehaviour
{
    public float magnitude=.1f;
    private Vector3 originalPos;

    private void Start()
    {
        originalPos = transform.localPosition;
        desiredPos = originalPos;
    }

    private Vector3 desiredPos;

    private void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition,desiredPos,Time.deltaTime*20f);
        while (true)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude;
            Vector3 offset = new Vector3(x, y, z);

            desiredPos = transform.localPosition + offset;
            if (Vector3.Distance(originalPos, desiredPos) < 0.02f)
            {
                break;
            }
        }
        
        
    }
}
