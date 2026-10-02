using System;
using System.Collections;
using System.Collections.Generic;
using Cosmetic;
using Manager;
using UnityEngine;

public class PlayerRagdoll : MonoBehaviour
{
    public static List<PlayerRagdoll> ragdolls = new List<PlayerRagdoll>();

    public List<Rigidbody> rbs = new List<Rigidbody>();

    public List<Transform> bones = new List<Transform>();
    
    public List<GameObject> hatCosmetics = new List<GameObject>();
    public List<GameObject> faceCosmetics = new List<GameObject>();
    public List<GameObject> shoeLCosmetics = new List<GameObject>();
    public List<GameObject> shoeRCosmetics = new List<GameObject>();
    public Renderer daveHair,clothe,pant;
    public List<GameObject> hairCosmetics = new List<GameObject>();
    public List<GameObject> clothesCosmetics = new List<GameObject>();
    public List<GameObject> pantCosmetics = new List<GameObject>();
    
    public InventoryManager.CosmeticIndex cosmeticIndex;
    public SkinnedMeshRenderer[] models;
    public bool isInfected=false;

    private void Start()
    {
        _layerMask = LayerMask.NameToLayer("RagDoll");

        Collider[] colliders = GetComponentsInChildren<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            for (int j = 0; j < colliders.Length; j++)
            {
                if (i != j)
                {
                    Physics.IgnoreCollision(colliders[i],colliders[j],true);
                }
            }
        }
    }

    public void SetCosmetic(InventoryManager.CosmeticIndex cosmeticItem)
    {
        if(!ragdolls.Contains(this))
            ragdolls.Add(this);

        if (ragdolls.Count > GameManager.RagdollLimited)
        {
            for (int i = 0; i < ragdolls.Count-GameManager.RagdollLimited; i++)
            {
                Destroy(ragdolls[i].gameObject);
            }
        }
        
        cosmeticIndex = cosmeticItem;
        if (cosmeticIndex.hatIndex < hatCosmetics.Count && cosmeticIndex.hatIndex!=-1)
        {
            Color color = cosmeticIndex.hatColor;
            float shiny = cosmeticIndex.hatShiny;
            int particle = cosmeticIndex.hatParticle;
            SetCosmetics(cosmeticIndex.hatIndex, color, shiny, particle, hatCosmetics, ref _hatParticle);
        }
        if (cosmeticIndex.faceIndex < faceCosmetics.Count && cosmeticIndex.faceIndex!=-1)
        {
            Color color = cosmeticIndex.faceColor;
            float shiny = cosmeticIndex.faceShiny;
            int particle = cosmeticIndex.faceParticle;
            SetCosmetics(cosmeticIndex.faceIndex, color, shiny, particle, faceCosmetics, ref _faceParticle);
        }
        if (cosmeticIndex.shoesIndex < shoeLCosmetics.Count && cosmeticIndex.shoesIndex!=-1)
        {
            Color color = cosmeticIndex.shoesColor;
            float shiny = cosmeticIndex.shoesShiny;
            int particle = cosmeticIndex.shoesParticle;
            SetCosmetics(cosmeticIndex.shoesIndex, color, shiny, particle, shoeLCosmetics, ref _shoeLParticle);
            SetCosmetics(cosmeticIndex.shoesIndex, color, shiny, particle, shoeRCosmetics, ref _shoeRParticle);
        }
        if (daveHair!=null &&cosmeticIndex.hairIndex < hairCosmetics.Count && cosmeticIndex.hairIndex!=-1)
        {
            Color color = cosmeticIndex.hairColor;
            float shiny = cosmeticIndex.hairShiny;
            int particle = cosmeticIndex.hairParticle;
            SetCosmetics(cosmeticIndex.hairIndex, color, shiny, particle, hairCosmetics, ref _hairParticle,daveHair.gameObject);
            
        }
        else if (cosmeticIndex.hairIndex == -1)
        {
            if(daveHair)
                daveHair.gameObject.SetActive(true);
        }
        if (cosmeticIndex.clothesIndex < clothesCosmetics.Count && cosmeticIndex.clothesIndex!=-1)
        {
            Color color = cosmeticIndex.clothesColor;
            float shiny = cosmeticIndex.clothesShiny;
            int particle = cosmeticIndex.clothesParticle;
            SetCosmetics(cosmeticIndex.clothesIndex, color, shiny, particle, clothesCosmetics, ref _clotheParticle,clothe.gameObject);
        }
        
        if (cosmeticIndex.pantIndex < pantCosmetics.Count && cosmeticIndex.pantIndex!=-1)
        {
            Color color = cosmeticIndex.pantColor;
            float shiny = cosmeticIndex.pantShiny;
            int particle = cosmeticIndex.pantParticle;
            SetCosmetics(cosmeticIndex.pantIndex, color, shiny, particle, pantCosmetics, ref _pantParticle,pant.gameObject);
        }
    }
    Transform _hatParticle;
    Transform _faceParticle;
    Transform _shoeLParticle;
    Transform _shoeRParticle;
    Transform _hairParticle;
    Transform _clotheParticle;
    Transform _pantParticle;

    private LayerMask _layerMask;
    void SetCosmetics(int index, Color color, float shiny, int particle, List<GameObject> cosmetics,
        ref Transform particleTran, GameObject alreadyHave = null)
    {
        foreach (var cosmetic in cosmetics)
        {
            cosmetic.SetActive(false);
        }
        if(particleTran)
            Destroy(particleTran.gameObject);
        GameObject cos = cosmetics[index] ;
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
                    ren.material.color = color;
                if (shiny != 0)
                { 
                    ren.material.EnableKeyword("_EMISSION");
                    ren.material.SetColor(CosmeticMenu.EmissionColor,color*shiny);
                }
                else
                {
                    ren.material.DisableKeyword("_EMISSION");
                }
            }
            if (particle != -1 && CosmeticManager.ItemIdToItem.TryGetValue(particle, out var item))
            {
                InventoryManager.ParticleItem particleItem = InventoryManager.Instance.GetParticle(item.tag);
                particleTran = Instantiate(particleItem.prefab).transform;
                particleTran.position  = cosmetics[index].transform.position+new Vector3(0,0.0095f,0);

                particleTran.localScale = particleItem.inGameSize;
                        
                particleTran.gameObject.layer= _layerMask;

                for (int i = 0; i < particleTran.childCount; i++)
                {
                    particleTran.GetChild(i).gameObject.layer =_layerMask;
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
            if(alreadyHave)
                alreadyHave.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if(ragdolls.Contains(this))
            ragdolls.Remove(this);
    }

    public void SetBones(List<Transform> bone)
    {
        if (bones.Count < bone.Count) return;
        for (int i = 0; i < bone.Count; i++)
        {
            bones[i].position = bone[i].position;
            bones[i].rotation = bone[i].rotation;
        }
    }

    public void SetInfected(bool flag)
    {
        isInfected = flag;
        if (isInfected)
        {
            foreach (var meshRenderer in models)
            {
                meshRenderer.material.color = new Color(119 / 255f, 154 / 255f, 91 / 255f);
            }
        }
    }
}
