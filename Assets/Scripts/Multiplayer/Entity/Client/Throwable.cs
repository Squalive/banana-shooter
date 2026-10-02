using Audio;
using Manager;
using Menu;
using Multiplayer.Entity.Server;
using Pool;
using UnityEngine;

namespace Multiplayer.Entity.Client
{
    public class Throwable : MonoBehaviour
    {
        ThrowObjectMenu.ThrowObjectType Type { get; set; }
        
        private Rigidbody _rb=null;
        public static GameObject InstantiateThrowable(ThrowObjectMenu.ThrowObjectType type, Vector3 pos)
        {
            GameObject obj = Instantiate(PrefabManager.Instance.throwables[(int)type], pos, Quaternion.identity);

            return obj;
        }

        public void Initialize(ThrowObjectMenu.ThrowObjectType type ,Vector3 dir,bool isLocal, Collider col)
        {
            Type = type;
            _rb = GetComponent<Rigidbody>();
            
            var speed = ServerGrenade.forces[(int) type];
            
            _rb.velocity = dir.normalized * speed + Vector3.up * speed / 2f * ServerGrenade.gra[(int)type];
            // rigidbody.AddForce(dir*35f+Vector3.up*10f,ForceMode.Impulse);
            _rb.AddTorque(Vector3.right*1000f,ForceMode.Impulse);

            if (!isLocal)
            {
                AudioManager.Instance.SoundEffect3D("Swing",transform.position);
            }
            if(col)
                Physics.IgnoreCollision(GetComponent<Collider>(), col,true);
        }
        
        public void Explode(Vector3 pos,bool destroy)
        {
            // ReplayManager.Instance.RecordThrowableExplode(Id,pos);
            
            transform.position = pos;

            switch (Type)
            {
                case ThrowObjectMenu.ThrowObjectType.Grenade :
                    GrenadeExplode();
                    break;
                case ThrowObjectMenu.ThrowObjectType.Knife:
                    KnifeExplode();
                    break;
                case ThrowObjectMenu.ThrowObjectType.FlashBang:
                    FlashBangExplode();
                    break;
                case ThrowObjectMenu.ThrowObjectType.MolotovCocktail:
                    MolotovCocktailExplode();
                    break;
                case ThrowObjectMenu.ThrowObjectType.JumpPad:
                    break;
            }
        
            if(destroy)
                Destroy(gameObject);
        }
        
        private void KnifeExplode()
        {
            var position = transform.position;
            if (GameManager.Instance.setting.spawnParticle)
            {
                ObjectPooler.Instance.SpawnFromPool("BulletHit",position,Quaternion.LookRotation(Vector3.up));
                Instantiate(PrefabManager.Instance.GetPrefab("SmallExplode"), position, Quaternion.identity);
            }
            AudioManager.Instance.SoundEffect3D("KnifeHit",position, 1f);
            AudioManager.Instance.SoundEffect3D("bulletimpact_robot2",position, 1f);
            
            
            Collider[] cols = new Collider[100];

            int cnt = Physics.OverlapSphereNonAlloc(position, 4f, cols);

            for (int i = 0; i < cnt; i++)
            {
                Rigidbody rb = cols[i].GetComponent<Rigidbody>();
                
                if (rb)
                {
                    rb.AddExplosionForce(70f,position,4f, 2f);
                }
            }
        }
        
        void MolotovCocktailExplode()
        {
            AudioManager.Instance.SoundEffect3D("molotovExplode",transform.position);

            if (GameManager.Instance.setting.spawnParticle)
                Instantiate(PrefabManager.Instance.GetPrefab("MolotovExplosion"), transform.position, Quaternion.LookRotation(Vector3.up));
        }
    
        void FlashBangExplode()
        {
            if (GameManager.Instance.setting.spawnParticle)
            {
                Instantiate(PrefabManager.Instance.GetPrefab("FlashBangExplode"), transform.position, Quaternion.identity);
            }
            AudioManager.Instance.SoundEffect3D("flashbang",transform.position, 1f);
        }
    
        void GrenadeExplode()
        {
            if (GameManager.Instance.setting.spawnParticle)
                Instantiate(PrefabManager.Instance.GetPrefab("ExplosionParticle"), transform.position + Vector3.up,
                    Quaternion.LookRotation(Vector3.up));
       
            AudioManager.Instance.SoundEffect3D("Explosion",transform.position, 1f);

            Collider[] cols = new Collider[100];

            int cnt = Physics.OverlapSphereNonAlloc(transform.position, 15f, cols);

            for (int i = 0; i < cnt; i++)
            {
                Rigidbody rb = cols[i].GetComponent<Rigidbody>();
                
                if (rb)
                {
                    rb.AddExplosionForce(150f,transform.position,15f, 2.5f);
                }
            }
        }
    }
}