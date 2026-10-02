using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class PlayerDisplayMenu : PlayerRender
    {
        public static PlayerDisplayMenu Instance { get;private set; }

        private void Awake()
        {
            Instance = this;
        }
        
    }
}
