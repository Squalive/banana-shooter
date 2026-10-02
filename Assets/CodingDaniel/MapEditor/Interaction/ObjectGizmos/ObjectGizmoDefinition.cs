using System;
using System.Reflection;
using UnityEngine;

namespace CodingDaniel.MapEditor.Interaction.ObjectGizmos
{
    /// <summary>
    /// Everything a concrete object gizmo used to express by existing as its own MonoBehaviour subclass:
    /// which component it belongs to, how it is shaped, which values a drag writes, what undo must
    /// record, which inspector properties to push back, and its colours.
    ///
    /// Definitions are data; <see cref="ObjectGizmoBehaviour"/> is the single interpreter.
    /// </summary>
    public sealed class ObjectGizmoDefinition
    {
        private static readonly MemberInfo[] NoMembers = new MemberInfo[0];
        private static readonly UiBinding[] NoBindings = new UiBinding[0];

        /// <summary>Stable identity, so a bound gizmo survives a domain reload.</summary>
        public string Id = string.Empty;

        /// <summary>The component type this gizmo is attached to.</summary>
        public Type ComponentType;

        /// <summary>Optional extra restriction, for components that share a type (for example Light.type).</summary>
        public Func<Component, bool> Applies;

        public ObjectGizmoGeometry Geometry = ObjectGizmoGeometry.Sphere;

        /// <summary>False for display-only shapes that used to accept a drag that did nothing.</summary>
        public bool AllowDrag = true;

        /// <summary>Redraw when the scene camera moves. True for every screen-stable shape.</summary>
        public bool RefreshOnCameraChanged;

        public GizmoColors Colors = GizmoColors.Default;

        // ----- geometry, in the target's local space ---------------------------------------------

        public Func<Component, Vector3> LocalCenter = _ => Vector3.zero;

        /// <summary>Sphere and cone rim radius.</summary>
        public Func<Component, float> Radius;

        /// <summary>Cone length. The apex handle drags this.</summary>
        public Func<Component, float> Height;

        /// <summary>Box geometry only.</summary>
        public Func<Component, Bounds> LocalBounds;

        // ----- values a drag writes --------------------------------------------------------------

        /// <summary>The value handle 0 drags: sphere radius, cone height.</summary>
        public Func<Component, float> ReadPrimary;
        public Action<Component, float> WritePrimary;

        /// <summary>The value the cone rim handles drag: the rim radius.</summary>
        public Func<Component, float> ReadSecondary;
        public Action<Component, float> WriteSecondary;

        // ----- undo ------------------------------------------------------------------------------

        /// <summary>
        /// Exactly the members the writers above mutate, including derived ones (a spot drag writes
        /// spotAngle and innerSpotAngle, an audio drag writes minDistance and maxDistance). Declaring
        /// them here is what keeps undo symmetric with the writes.
        /// </summary>
        public Func<Component, MemberInfo[]> UndoMembers = _ => NoMembers;

        /// <summary>Inspector properties refreshed after a drag writes them.</summary>
        public UiBinding[] UiBindings = NoBindings;

        public sealed class UiBinding
        {
            public string Key;
            public Func<Component, float> Current;
        }
    }
}
