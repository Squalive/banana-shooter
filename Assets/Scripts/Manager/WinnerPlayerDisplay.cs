using System;
using System.Collections.Generic;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Cosmetic;
using UnityEngine;

namespace Manager
{
    
    public class WinnerPlayerDisplay : MonoBehaviour
    {
        public static WinnerPlayerDisplay Instance { get; private set; }
        public static LayerMask ClientPlayerLayer;

        private static readonly int Dance = Animator.StringToHash("dance");
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
            ClientPlayerLayer = LayerMask.NameToLayer("Cosmetic");
            
        }

        [Serializable]
        public class PlayerCosmetics
        {
            public List<GameObject> hatCosmetics = new List<GameObject>();
            public List<GameObject> faceCosmetics = new List<GameObject>();
            public List<GameObject> shoeLCosmetics = new List<GameObject>();
            public List<GameObject> shoeRCosmetics = new List<GameObject>();
            public SkinnedMeshRenderer daveHair,clothe,pant;
            public List<GameObject> hairCosmetics = new List<GameObject>();
            public List<GameObject> clothesCosmetics = new List<GameObject>();
            public List<GameObject> pantCosmetics = new List<GameObject>();
            
            [HideInInspector]
            public Transform hatParticle;
            [HideInInspector]
            public Transform faceParticle;
            [HideInInspector]
            public Transform shoeLParticle;
            [HideInInspector]
            public Transform shoeRParticle;
            [HideInInspector]
            public Transform hairParticle;
            [HideInInspector]
            public Transform clotheParticle;
            [HideInInspector]
            public Transform pantParticle;

            public Animator anim;
        }

        public PlayerCosmetics firstCosmetics,secondCosmetics,thirdCosmetics;

        public GameObject firstPlayer, secondPlayer, thirdPlayer;

        public void Enable(GameObject[] obj)
        {
            foreach (var o in obj)
            {
                o.SetActive(true);
            }
        }
        
        public void Disable()
        {
            firstPlayer.SetActive(false);
            secondPlayer.SetActive(false);
            thirdPlayer.SetActive(false);
        }
        
        void SetCosmetics(int index, Color color, float shiny, int particle, List<GameObject> cosmetics,
            ref Transform particleTran,GameObject alreadyHave=null)
        {
            if (cosmetics == null) return;
            foreach (var cosmetic in cosmetics)
            {
                cosmetic.gameObject.SetActive(false);
            }
            if(particleTran) Destroy(particleTran.gameObject);
            if (index < cosmetics.Count && index!=-1)
            {
                GameObject cos = cosmetics[index];
                if (cos != null)
                {
                    if (alreadyHave)
                    {
                        alreadyHave.SetActive(false);
                    }
                    cos.SetActive(true);
                    
                    foreach (var ren in cos.GetComponentsInChildren<Renderer>())
                    {
                        if (color != Color.clear)
                        {
                            ren.material.color = color;

                            if (CosmeticManager.IsGoldenColor(color))
                            {
                                ren.material.SetFloat(MapSaver.Smoothness, .8f);
                                ren.material.SetFloat(MapSaver.Metallic, 0.6f);
                            }
                        }
                        if (shiny != 0)
                        { 
                            ren.material.EnableKeyword("_EMISSION");
                            ren.material.SetColor(CosmeticMenu.EmissionColor, color * shiny);
                        }
                        else
                        {
                            ren.material.DisableKeyword("_EMISSION");
                        }
                    }
                    if (particle != -1 &&CosmeticManager.ItemIdToItem.TryGetValue(particle, out var item))
                    {
                        InventoryManager.ParticleItem particleItem = InventoryManager.Instance.GetParticle(item.tag);
                        particleTran = Instantiate(particleItem.prefab).transform;
                        particleTran.position  = cos.transform.position+new Vector3(0,0.0095f,0);

                        particleTran.localScale = particleItem.cosmeticMenuSize;
                        
                        particleTran.gameObject.layer= ClientPlayerLayer;

                        for (int i = 0; i < particleTran.childCount; i++)
                        {
                            particleTran.GetChild(i).gameObject.layer =ClientPlayerLayer ;
                        }
                        

                        CosmeticVFX cosmeticVFX = particleTran.GetComponent<CosmeticVFX>();

                        if (cosmeticVFX != null)
                        {
                            var filter = cos.GetComponentInChildren<MeshFilter>();
                            if (filter == null)
                            {
                                particleTran.parent = cos.transform;
                                var skin = cos.GetComponentInChildren<SkinnedMeshRenderer>();
                                cosmeticVFX.SetSkinnedMeshRenderer(skin);
                                cosmeticVFX.SetTransform(skin.rootBone,true);
                            }
                            else
                            {
                                particleTran.parent = cos.transform.parent;
                                cosmeticVFX.SetMesh(filter.sharedMesh);
                                cosmeticVFX.SetTransform(filter.transform);
                            }
                        }
                        else
                        {
                            particleTran.parent = cos.transform.parent;
                        }
                    }
                }
                else
                {
                    if(alreadyHave) alreadyHave.SetActive(true);
                }
            }
            else
            {
                if(alreadyHave) alreadyHave.SetActive(true);
            }
        }

        public void SetPlayerCosmetics(PlayerCosmetics playerCosmetics,InventoryManager.CosmeticIndex index)
        {
            SetCosmetics(index.hairIndex,index.hairColor,index.hairShiny,index.hairParticle,playerCosmetics.hairCosmetics,ref playerCosmetics.hairParticle,
                playerCosmetics.daveHair.gameObject);
            
            SetCosmetics(index.pantIndex,index.pantColor,index.pantShiny,index.pantParticle,playerCosmetics.pantCosmetics,ref playerCosmetics.pantParticle,
                playerCosmetics.pant.gameObject);
            
            SetCosmetics(index.faceIndex,index.faceColor,index.faceShiny,index.faceParticle,playerCosmetics.faceCosmetics,ref playerCosmetics.faceParticle);
            
            SetCosmetics(index.clothesIndex,index.clothesColor,index.clothesShiny,index.clothesParticle,playerCosmetics.clothesCosmetics,ref playerCosmetics.clotheParticle,playerCosmetics.clothe.gameObject);
            
            SetCosmetics(index.hatIndex,index.hatColor,index.hatShiny,index.hatParticle,playerCosmetics.hatCosmetics,ref playerCosmetics.hatParticle);
            
            SetCosmetics(index.shoesIndex,index.shoesColor,index.shoesShiny,index.shoesParticle,playerCosmetics.shoeLCosmetics,ref playerCosmetics.shoeLParticle);
            
            SetCosmetics(index.shoesIndex,index.shoesColor,index.shoesShiny,index.shoesParticle,playerCosmetics.shoeRCosmetics,ref playerCosmetics.shoeRParticle);
            playerCosmetics.anim.SetBool(Dance,true);
        }
    }
}
