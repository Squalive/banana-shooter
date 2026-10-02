using Audio;
using Manager;
using Movement;
using Riptide;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Client
{
    public class ClientJumpPad : MonoBehaviour
    {
        public ushort ID { private set; get; }
        
        public Transform pad;

        private Vector3 target,startPos;
        private Quaternion  startRot;
        private float rot = 1f;

        private AudioSource source;

        private Quaternion targetRot;
        private bool toTarget;

        [SerializeField]private float offset=0.12f;
        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        private void Start()
        {
            startPos = pad.localPosition;
            startRot = pad.localRotation; 
        
            target = new Vector3(startPos.x, startPos.y + 0.15f, startPos.z);
            targetRot = Quaternion.Euler(Random.Range(-rot, rot), 0, Random.Range(-rot, rot));
        }
        private void Update()
        {
            if (source.enabled && !PlayerMovement.Instance)
            {
                source.enabled = false;
                return;
            }
            if (!source.enabled && PlayerMovement.Instance) source.enabled = true;
            Vector3 _target = toTarget ? target :startPos;
            if (Mathf.Abs(_target.y-pad.localPosition.y)<offset && !toTarget)
            {
                _target = target;
                toTarget = true;
            }
            else if (Mathf.Abs(target.y-pad.localPosition.y)<offset && toTarget)
            {
                toTarget = false;
            }

            pad.localRotation=Quaternion.Slerp(pad.localRotation,targetRot,Time.deltaTime*10f);
        
            pad.localPosition=Vector3.Slerp(pad.localPosition,_target,Time.deltaTime*5f);
            if (targetRot == pad.localRotation)
            {
                targetRot = Quaternion.Euler(Random.Range(-rot, rot), 0, Random.Range(-rot, rot));
            }
        }
        [SerializeField] private float force=50f;
        public AudioClip hit;
        private bool ready = true;
        public Vector3 direction = Vector3.up;
        private void OnCollisionEnter(Collision other)
        {
            if (other.collider.gameObject.layer == LayerMask.NameToLayer("Ground") || !ready) return;
        
            for (int i = 0; i < other.contactCount; i++)
            {
                if(!other.collider.GetComponent<Rigidbody>())continue;
                ready = false;
                other.collider.GetComponent<Rigidbody>().AddForce(direction*force,ForceMode.Impulse);

                PlayHitAudio();
            
                Invoke(nameof(GetReady),0.1f);
            
                if(other.gameObject.layer==LayerMask.NameToLayer("RagDoll"))Destroy(other.transform.root.gameObject);
            
                break;
            }
        }

        void GetReady()
        {
            ready = true;
        }

        private void PlayHitAudio()
        {
            // AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            // audioSource.pitch = 1f + Random.Range(-0.5f, 0.5f);
            // audioSource.spatialBlend = 0.2f;
            // audioSource.PlayOneShot(hit);
            //
            // Destroy(audioSource,1f);
            AudioManager.Instance.SoundEffect3D("jump_pad_hit", transform.position, 1f, 4f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
        
            Gizmos.DrawRay(transform.position,direction);
        }

        private void Initialize(ushort id)
        {
            ID = id;

            var particlePrefab = PrefabManager.Instance.GetPrefab("JumpPadParticle");

            Instantiate(particlePrefab, transform.position, Quaternion.identity);
            
            PlayHitAudio();
        }

        [MessageHandler((ushort)ServerToClientId.JumpPadSpawned, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void JumpPadSpawned(Message message)
        {
            ushort id = message.GetUShort();

            Vector3 pos = message.GetVector3();

            Quaternion rot = message.GetQuaternion();

            float destroyTime = message.GetFloat();

            var prefab = PrefabManager.Instance.GetPrefab("ClientJumpPad").GetComponent<ClientJumpPad>();

            var instance = Object.Instantiate(prefab, pos, rot);
            
            Object.Destroy(instance.gameObject, destroyTime);
            
            instance.Initialize(id);
        }
    }
}
