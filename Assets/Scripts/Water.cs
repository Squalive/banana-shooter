
using System;
using Audio;
using Manager;
using Movement;
using Pool;
using UnityEngine;

public class Water : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb)
        {
            if (Mathf.Abs(rb.velocity.y) > 6.2f)
                AudioManager.Instance.SoundEffect3D("Water Splash",other.transform.position,0.5f);
            rb.drag = 2f;
            if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            {
                rb.AddForce(Vector3.up*force,ForceMode.Acceleration);
                
                if(GameManager.Instance.setting.spawnParticle)
                {
                    var position = transform.position;
                    ObjectPooler.Instance.SpawnFromPool("Water Splash", other.bounds.ClosestPoint(position),
                        Quaternion.LookRotation(Vector3.up));
                }
            }
            else 
            {
                PlayerMovement.Instance.inWater = true;
                UnderWaterSfx.Instance.SetUnderWater(true);
                if(PlayerMovement.Instance.IsJumping())
                    rb.AddForce(Vector3.up*playerForce,ForceMode.Acceleration);
            }
            
        }
    }
    bool WaterF=true;

    void WaterA()
    {
        WaterF = true;
    }

    private float force = 50f,playerForce = 75f;
    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb)
        {
            if(other.gameObject.layer != LayerMask.NameToLayer("Player"))
                rb.AddForce(force * Vector3.up,ForceMode.Acceleration);
            else 
            {
                PlayerMovement.Instance.inWater = true;
                UnderWaterSfx.Instance.SetUnderWater(true);
                if(PlayerMovement.Instance.IsJumping())
                    rb.AddForce(playerForce * Vector3.up,ForceMode.Acceleration);
                else
                    rb.AddForce(force*0.5f*Vector3.up,ForceMode.Acceleration);
            }
        }
        if ((other.gameObject.layer == LayerMask.NameToLayer("Player") ||other.gameObject.layer == LayerMask.NameToLayer("Bullet"))&& WaterF)
        {
            WaterF = false;
            Invoke(nameof(WaterA),1f);
            // AudioManager.Instance.Play("WaterSplash");
        }
    }

    private void OnTriggerExit(Collider other)
    {
         Rigidbody rb = other.GetComponent<Rigidbody>();
         if (rb)
         {
             rb.drag = 0f;
             rb.AddForce(Vector3.down*force*0.1f,ForceMode.VelocityChange);

             if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
             {
                 rb.AddForce(Vector3.down*playerForce*0.05f,ForceMode.VelocityChange);
                 PlayerMovement.Instance.inWater = false;
                 UnderWaterSfx.Instance.SetUnderWater(false);
             }
         }
    }
}
