using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class PlayerDisplay : PlayerRender
    {
        public static PlayerDisplay Instance{ get;private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if(Instance!=this)
            {
                Destroy(gameObject);
            }
        }
    }
}
