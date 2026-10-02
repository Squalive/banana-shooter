using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pool
{
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance;
        private void Awake()
        {
            Instance = this;
        }

        [Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
        
        }

        public List<Pool> pools;

        public Dictionary<string, Queue<GameObject>> poolDictionary=new Dictionary<string, Queue<GameObject>>();

        private void Start()
        {
            foreach (var pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab);
                    obj.SetActive(false);
                    DontDestroyOnLoad(obj);
                    objectPool.Enqueue(obj);
                    
                    IPooledObject pooledObject = obj.GetComponent<IPooledObject>();
                    if(pooledObject!=null)
                        pooledObject.OnObjectInit();
                }

                poolDictionary.Add(pool.tag,objectPool);
            }
        }

        public GameObject SpawnFromPool(string tag, Vector3 pos, Quaternion rot)
        {
            if (!poolDictionary.ContainsKey(tag)) return null;
        
            GameObject objToSpawn= poolDictionary[tag].Dequeue();
        
            objToSpawn.SetActive(true);
            objToSpawn.transform.position = pos;
            objToSpawn.transform.rotation = rot;

            IPooledObject pooledObject = objToSpawn.GetComponent<IPooledObject>();

            if (pooledObject != null)
            {
                pooledObject.OnObjectSpawn();
            }

            poolDictionary[tag].Enqueue(objToSpawn);

            return objToSpawn;
        }
    }
}
