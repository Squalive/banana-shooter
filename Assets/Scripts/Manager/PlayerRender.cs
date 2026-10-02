using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public abstract class PlayerRender : MonoBehaviour
    {
        public Transform player;

        public Animator anim;

        public List<GameObject> hatCosmetics = new List<GameObject>();
        public List<GameObject> faceCosmetics = new List<GameObject>();
        public List<GameObject> shoeLCosmetics = new List<GameObject>();
        public List<GameObject> shoeRCosmetics = new List<GameObject>();
        public SkinnedMeshRenderer daveHair,clothe,pant;
        public List<GameObject> hairCosmetics = new List<GameObject>();
        public List<GameObject> clothesCosmetics = new List<GameObject>();
        public List<GameObject> pantCosmetics = new List<GameObject>();

        public bool useCam = true;
    
        public Camera cam;
    }
}