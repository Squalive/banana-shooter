using UnityEngine;

namespace CodingDaniel.MapEditor.Interaction.Rendering
{
    /// <summary>
    /// Handle colours.
    ///
    /// Extracted from MEHandleComponent (where it was called RTHColors - an asset-store name) so the
    /// palette is a value the renderer and gizmos can be handed rather than something they reach for
    /// through a singleton. Field names are unchanged, and the component still serializes it as
    /// <c>_colors</c>, so the palette configured in MapEditor.unity survives.
    /// </summary>
    [System.Serializable]
    public class GizmoPalette
    {
        public Color32 DisabledColor = new Color32(128, 128, 128, 128);
        public Color32 XColor = new Color32(187, 70, 45, 255);
        public Color32 YColor = new Color32(139, 206, 74, 255);
        public Color32 ZColor = new Color32(55, 115, 244, 255);
        public Color32 AltColor = new Color32(192, 192, 192, 224);
        public Color32 AltColor2 = new Color32(0x38, 0x38, 0x38, 224);
        public Color32 SelectionColor = new Color32(239, 238, 64, 255);
        public Color32 SelectionAltColor = new Color(0, 0, 0, 0.1f);
        public Color32 BoundsColor = new Color(0.0f, 1, 0.0f, 0.75f);
        public Color32 GridColor = new Color(1, 1, 1, 0.1f);
        public Color32 ObjectSelectionColor = Color.red;
    }
}
