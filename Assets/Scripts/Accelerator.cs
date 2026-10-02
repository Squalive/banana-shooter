
using System;
using Menu;
using UnityEngine;

public class Accelerator : MonoBehaviour
{
    private Material mat;
    private void Awake()
    {
        mat = GetComponent<MeshRenderer>().material;
    }

    private float desiredOffset = -1;

    public float speed = 2.5f,force=60f;
    private void Update()
    {
        mat.mainTextureOffset = new Vector2(0, mat.mainTextureOffset.y - Time.deltaTime*speed);
        if (Mathf.Abs(mat.mainTextureOffset.y - (int)mat.mainTextureOffset.y) < 0.01f)
        {
            mat.mainTextureOffset = Vector2.zero;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (rb)
        {
            rb.AddForce(transform.forward*force,ForceMode.Acceleration);
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.SpeedUp();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.ClearSpeedUp();
            }
        }
    }
}
