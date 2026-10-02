using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    [DefaultExecutionOrder(-106)]
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get;private set; }

        public List<Map> maps = new List<Map>();

        public Dictionary<string, Map> NameToMap { get; } = new Dictionary<string, Map>();

        private void Awake()
        {
            Instance = this;

            foreach (var map in maps)
            {
                NameToMap.Add(map.name,map);
            }
        }
        
        public Texture2D GetMapTexture(string n)
        {
            foreach (var map in maps)
            {
                if (n == map.name)
                {
                    return map.texture;
                }
            }

            return null;
        } 
        public MapExternal GetMapExternal(string n)
        {
            foreach (var map in maps)
            {
                if (n == map.name)
                {
                    return map.external;
                }
            }

            return MapExternal.None;
        }
    }
}