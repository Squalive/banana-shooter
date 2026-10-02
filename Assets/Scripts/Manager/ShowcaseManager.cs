using System;
using Cosmetic;
using UnityEngine;

namespace Manager
{
    public class ShowcaseManager : MonoBehaviour
    {
        
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Transform itemTransform;
        
        Vector3 _defaultPos = Vector3.zero;

        private CosmeticItem _lastShowcaseItem;

        public void Showcase(CosmeticItem item)
        {
            _lastShowcaseItem = item;
             
            meshFilter.mesh = item.mesh;

            meshRenderer.materials = item.materials;

            itemTransform.localScale = item.GetSize();

            itemTransform.rotation = Quaternion.Euler(item.defaultRotation);

            itemTransform.localPosition = _defaultPos + item.offset;
        }

        public bool IsThisItem(CosmeticItem item)
        {
            return _lastShowcaseItem != null && _lastShowcaseItem == item;
        }
    }
}
