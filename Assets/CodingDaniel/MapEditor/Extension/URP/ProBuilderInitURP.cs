using System;
using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using CodingDaniel.MapEditor.MECommon;
using UnityEngine;

namespace CodingDaniel.MapEditor.Extension.URP
{
    [DefaultExecutionOrder(-92)]
    public class ProBuilderInitURP : MonoBehaviour
    {
        private RenderersCache _selectionPickerCache;
        private void Awake()
        {
            _selectionPickerCache = gameObject.AddComponent<RenderersCache>();
            PBSelectionPicker.Renderer = new PBSelectionPickerRendererURP(_selectionPickerCache);
        }

        private void OnDestroy()
        {
            Destroy(_selectionPickerCache);
            _selectionPickerCache = null;
        }
    }
}
