using System.Collections.Generic;
using Manager;
using Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapEditor
{
    public class MapBound : MonoBehaviour
    {
        public static MapBound Instance;
        public List<Transform> spawnPos = new List<Transform>();
    
        [SerializeField] public Vector3 center=Vector3.zero,size=Vector3.one;

        [HideInInspector] public float maxY,maxX,minX,maxZ,minZ;

        [SerializeField] public List<Transform> hills = new();

        private void Awake()
        {
            Instance = this;
            maxY = center.y + size.y / 2f;
        
            maxX = center.x + size.x / 2f;
            minX = center.x - size.x / 2f;
        
            maxZ = center.z + size.z / 2f;
            minZ = center.z - size.z / 2f;
        
            string n = SceneManager.GetActiveScene().name;
            if (GameManager.Instance.mapPlayed.TryGetValue(n,out bool f) && !f)
            {
                GameManager.Instance.mapPlayed[n] = true;

                List<bool> l = new List<bool>();
                foreach (var b in GameManager.Instance.mapPlayed.Values)
                {
                    l.Add(b);
                }

                SaveSystem.SaveData("map_travel", l);

                AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.MAP_PLAY_AMOUNT);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center,size);
        }
    }
}
