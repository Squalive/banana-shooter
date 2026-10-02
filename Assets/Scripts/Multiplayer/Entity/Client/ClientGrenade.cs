using System.Collections;
using System.Collections.Generic;
using Audio;
using Demo;
using Demo.Entity;
using Manager;
using Menu;
using Movement;
using Quest;
using Riptide;
using UnityEngine;

namespace Multiplayer.Entity.Client
{
    public class ClientGrenade : MonoBehaviour
    {
        public static Dictionary<ushort, ClientGrenade> list = new();

        public ushort Id { get; private set; }
        public ushort PlayerId { get; private set; }

        public Vector3 ThrowDirection { get; private set; }

        public Throwable throwable;
        
        public DemoThrowable DemoThrowable { get; private set; }

        private bool expoloded = false;

        [HideInInspector]
        public ThrowObjectMenu.ThrowObjectType type;

        private void Awake()
        {
            if (DemoManager.Replaying)
            {
                Destroy(this);
            }
        }

        [MessageHandler((ushort) ServerToClientId.ThrowObj, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ThrowObj(Message message)
        {
            ushort id = message.GetUShort();
        
            ThrowObjectMenu.ThrowObjectType type = (ThrowObjectMenu.ThrowObjectType) message.GetInt();
            ushort playerId = message.GetUShort();

            Vector3 pos = message.GetVector3();
            Vector3 dir = message.GetVector3();

            ClientGrenade obj = Throwable.InstantiateThrowable(type,pos).GetComponent<ClientGrenade>();

            obj.Initialize(id,playerId,type,dir);
        }

        [MessageHandler((ushort) ServerToClientId.ThrowObjExplode, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ThrowObjExplode(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var value))
            {
                Vector3 pos = message.GetVector3();
                value.Explode(pos);
            }
        }

        [MessageHandler((ushort) ServerToClientId.FlashBang, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void FlashBang(Message message)
        {
            ushort getHitId = message.GetUShort();
            ushort hitId = message.GetUShort();
            float angle = message.GetFloat();
            if (angle == 0) angle = 1;

            if (ClientPlayer.list.ContainsKey(getHitId) && ClientPlayer.list.ContainsKey(hitId))
            {
                if (getHitId == NetworkManager.Instance.Client.Id)
                {
                    GameManager.Instance.SetExposure(Mathf.Min((180f / angle)*2f,10));
                    
                    AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.GET_BLIND_AMOUNT);
                }

                if (getHitId != hitId && hitId == NetworkManager.Instance.Client.Id)
                {
                    AudioManager.Instance.Play("HitMarker");
                    AudioManager.Instance.Play("Hit");
                    AudioManager.Instance.Play("Hit2");
                    AudioManager.Instance.Play("Hit3");
                    AudioManager.Instance.Play("Hit4");
                    HitMarker.Instance.StartHitMarker(Color.cyan);
                    
                    
                    QuestManager.Instance.GetProgress(QuestType.Flash);
                }
            }
        }
        public void Initialize(ushort id,ushort _playerId,ThrowObjectMenu.ThrowObjectType type, Vector3 dir, bool isLocal=false)
        {
            Id = id;

            PlayerId = _playerId;

            this.type = type;

            ThrowDirection = dir;
        
            Destroy(gameObject,15f);

            DemoThrowable = gameObject.GetComponent<DemoThrowable>();
            
            DemoManager.Instance.AddThrowableSpawned(this);

            throwable.Initialize(this.type, dir, isLocal,PlayerMovement.Instance.GetCollider());

            if (PlayerId == NetworkManager.Instance.Client.Id)
            {
                switch (type)
                {
                    case ThrowObjectMenu.ThrowObjectType.Grenade:
                        StartCoroutine(ExplodeByTime(1.2f));
                        break;
                    case ThrowObjectMenu.ThrowObjectType.FlashBang:
                        StartCoroutine(ExplodeByTime(1.5f));
                        break;
                    default:
                        StartCoroutine(ExplodeByTime(10f));
                        break;
                }
                
                
            }
            else
            {
                if (list.ContainsKey(id))
                {
                    Destroy(list[id].gameObject);
                    list.Remove(id);
                }
                list.Add(id,this);
            }
        }

        IEnumerator ExplodeByTime(float time)
        {
            yield return new WaitForSeconds(time);
            
            Explode(transform.position);
        }

        void Explode(Vector3 pos)
        {
            throwable.Explode(pos, true);
        }

        private void OnCollisionEnter(Collision other)
        {
            if (DemoManager.Replaying) return;
            if(expoloded || PlayerId != NetworkManager.Instance.Client.Id)return;
            
            Vector3 normal = other.contacts[0].normal;
            Vector3 pos = other.contacts[0].point;
            
            switch (type)
            {
                case ThrowObjectMenu.ThrowObjectType.MolotovCocktail:
                    if (Vector3.Angle(Vector3.up, normal) < 30)
                    {
                        Explode(pos);
                    }

                    break;
                case ThrowObjectMenu.ThrowObjectType.JumpPad:
                    if (Vector3.Angle(Vector3.up, normal) < 30)
                    {
                        Explode(pos);
                    }

                    break;
                case ThrowObjectMenu.ThrowObjectType.Knife:
                    Explode(pos);

                    break;
            }
        }
        
        private void OnDestroy()
        {
            if (list.ContainsKey(Id))
                list.Remove(Id);
        }
    }
}
