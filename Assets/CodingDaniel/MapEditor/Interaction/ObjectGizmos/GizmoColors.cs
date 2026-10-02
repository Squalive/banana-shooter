using UnityEngine;

namespace CodingDaniel.MapEditor.Interaction.ObjectGizmos
{
    /// <summary>
    /// Colour set for one gizmo kind, replacing the per-class <c>ResetObject()</c> overrides that
    /// used to assign LineColor/HandlesColor/SelectionColor field by field.
    /// </summary>
    public struct GizmoColors
    {
        public Color Line;
        public Color Handle;
        public Color Selection;

        /// <summary>The colours BaseGizmo started with, before any ResetObject override.</summary>
        public static readonly GizmoColors Default = new GizmoColors
        {
            Line = new Color(0f, 1f, 0f, 0.75f),
            Handle = new Color(0f, 1f, 0f, 0.75f),
            Selection = new Color(1f, 1f, 0f, 1f),
        };

        /// <summary>Point, spot and directional lights (was PointLightGizmo/SpotlightGizmo/DirectionalLightGizmo.ResetObject).</summary>
        public static readonly GizmoColors Warm = new GizmoColors
        {
            Line = new Color(1f, 1f, 0.5f, 0.5f),
            Handle = new Color(1f, 1f, 0.35f, 0.95f),
            Selection = new Color(1f, 1f, 0f, 1f),
        };

        /// <summary>Audio sources (was AudioSourceGizmo.ResetObject).</summary>
        public static readonly GizmoColors Audio = new GizmoColors
        {
            Line = new Color(1f / 255f, 156f / 255f, 195f / 255f, 0.5f),
            Handle = new Color(1f / 255f, 156f / 255f, 195f / 255f, 0.95f),
            Selection = new Color(1f / 255f, 167f / 255f, 195f / 255f, 1f),
        };

        /// <summary>Decals (was DecalGizmo's serialized LineColor default).</summary>
        public static readonly GizmoColors Neutral = new GizmoColors
        {
            Line = new Color(1f, 1f, 1f, 0.75f),
            Handle = new Color(1f, 1f, 1f, 0.75f),
            Selection = new Color(1f, 1f, 0f, 1f),
        };
    }
}
