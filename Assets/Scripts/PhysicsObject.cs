
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

public class PhysicsObject : MonoBehaviour
{
    private bool ready = true;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }



    private void OnCollisionEnter(Collision other)
    {
        if (ready)
        {
            ready = false;
            AudioSource source = Instantiate(PrefabManager.Instance.impactAudio, base.transform.position, Quaternion.identity).GetComponent<AudioSource>();
            
            source.clip = PrefabManager.Instance.hitWall[Random.Range(0, PrefabManager.Instance.hitWall.Length)];
			
            Destroy(source.gameObject,5);
            
            Invoke(nameof(GetReady), 0.05f);
        }
    }
    
    private void GetReady()
    {
        ready = true;
    }
}
