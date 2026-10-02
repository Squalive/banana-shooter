using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public interface IMaterialPaletteManager
    {
        event Action<Material> MaterialCreated;
        event Action<Material> MaterialAdded;
        event Action<Material> MaterialRemoved;
        event Action<MaterialPalette> PaletteChanged;
        event Action<Material, Material> MaterialReplaced;

        bool IsReady
        {
            get;
        }

        MaterialPalette Palette
        {
            get;
        }
        
        void CreateMaterial();
        void AddMaterial(Material material, bool setUniqueName = false);
        void ReplaceMaterial(Material oldMaterial, Material newMaterial);
        void RemoveMaterial(Material material);
        void ApplyTexture(Material material, Texture texture);
    }
    [DefaultExecutionOrder(-50)]
    public class MaterialPaletteManager : MonoBehaviour,IMaterialPaletteManager
    {
        public static MaterialPaletteManager Instance { private set; get; }
        public event Action<Material> MaterialCreated;
        public event Action<Material> MaterialAdded;
        public event Action<Material> MaterialRemoved;
        public event Action<MaterialPalette> PaletteChanged;
        public event Action<Material, Material> MaterialReplaced;

        public bool IsReady
        {
            get;
            private set;
        }

        private MaterialPalette _palette;
        public MaterialPalette Palette
        {
            get { return _palette; }
        }

        private IME _rte;
        private IProBuilderTool _proBuilderTool;

        private void Awake()
        {
            Instance = this;
            _rte = MEBase.Instance;
            
            _proBuilderTool = ProBuilderTool.Instance;

            InitPalette();
            IsReady = true;
        }
        

        private void InitPalette()
        {
            _palette = FindObjectOfType<MaterialPalette>();
            if (_palette == null)
            {
                GameObject go = new GameObject("MaterialPalette");
                _palette = go.AddComponent<MaterialPalette>();

                Material material = PBBuiltinMaterials.DefaultMaterial;
                _palette.Materials.Add(material);
            }
            CleanPalette();
        }

        public void CreateMaterial()
        {
            Material material = Instantiate(PBBuiltinMaterials.DefaultMaterial);
            material.name = PathHelper.GetUniqueName("Material", _palette.Materials.Select(m => m.name).ToList());
            _palette.Materials.Add(material);

            if(MaterialCreated != null)
            {
                MaterialCreated(material);
            }
        }

        public void AddMaterial(Material material, bool setUniqueName)
        {
            if(setUniqueName)
            {
                material.name = PathHelper.GetUniqueName("Material", _palette.Materials.Select(m => m.name).ToList());
            }

            _palette.Materials.Add(material);
            if(MaterialAdded != null)
            {
                MaterialAdded(material);
            }
        }

        public void ReplaceMaterial(Material oldMaterial, Material newMaterial)
        {
            int index = _palette.Materials.IndexOf(oldMaterial);
            _palette.Materials.RemoveAt(index);
            if(!_palette.Materials.Contains(newMaterial))
            {
                _palette.Materials.Insert(index, newMaterial);
            }
            if(MaterialReplaced != null)
            {
                MaterialReplaced(oldMaterial, newMaterial);
            }
        }

        public void RemoveMaterial(Material material)
        {
            _palette.Materials.Remove(material);
            if(MaterialRemoved != null)
            {
                MaterialRemoved(material);
            }
        }

        public void ApplyTexture(Material material, Texture texture)
        {
            Texture oldTexture = material.MainTexture();
            Texture newTexture = texture;
            material.MainTexture(newTexture);

            _rte.Undo.CreateRecord(record =>
            {
                material.MainTexture(newTexture);
                return true;
            },
            record =>
            {
                material.MainTexture(oldTexture);
                return true;
            });
        }

        private void CleanPalette()
        {
            if (_palette.Materials == null)
            {
                _palette.Materials = new List<Material>();
                return;
            }

            _palette.Materials = _palette.Materials.Where(m => m != null).ToList();
        }


        protected virtual void Update()
        {
            bool select = Input.GetKey(KeyCode.S);
            bool unselect = Input.GetKey(KeyCode.U);

            if (!select && !unselect)
            {
                if (!_proBuilderTool.HasSelection)
                {
                    return;
                }
            }
            
            if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt) && !Input.GetKey(KeyCode.AltGr))
            {
                return;
            }

            for (int keyCode = (int)KeyCode.Alpha0; keyCode <= (int)KeyCode.Alpha0 + 9; ++keyCode)
            {
                if (!Input.GetKeyDown((KeyCode)keyCode))
                {
                    continue;
                }

                int index = keyCode - (int)KeyCode.Alpha1;
                if (index == -1)
                {
                    index = 9;
                }

                if (0 <= index && index < Palette.Materials.Count)
                {
                    Material material = Palette.Materials[index];
                    if (material == null)
                    {
                        break;
                    }

                    if(select)
                    {
                        _proBuilderTool.SelectFaces(material);
                    }
                    else if(unselect)
                    {
                        _proBuilderTool.UnselectFaces(material);
                    }
                    else
                    {
                        _proBuilderTool.ApplyMaterial(material);
                    }
                }
            }
        }
    }
}
