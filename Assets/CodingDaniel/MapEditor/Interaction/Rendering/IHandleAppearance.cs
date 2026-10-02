using UnityEngine;

namespace CodingDaniel.MapEditor.Interaction.Rendering
{
    /// <summary>
    /// What a transform handle renderer or hit tester needs to know about how handles look and how
    /// forgiving picking should be. Implemented by the scene component that used to be reached through
    /// a static Instance.
    /// </summary>
    public interface IHandleAppearance
    {
        GizmoPalette Palette { get; }

        float HandleScale { get; }

        /// <summary>Selection margin in world units, already multiplied by the handle scale.</summary>
        float SelectionMargin { get; }

        /// <summary>Selection margin in pixels.</summary>
        float SelectionMarginPixels { get; }

        bool InvertZAxis { get; }

        bool PositionHandleArrowOnly { get; }

        Vector3 Forward { get; }

        /// <summary>
        /// The handle component's own transform. The rotation drag-plane fallback planes against it,
        /// exactly as the original code did when this logic lived on a MonoBehaviour.
        /// </summary>
        Transform HostTransform { get; }
    }
}