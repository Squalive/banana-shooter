namespace CodingDaniel.MapEditor.Interaction.ObjectGizmos
{
    /// <summary>
    /// Shape drawn for an object gizmo.
    ///
    /// This replaces the geometry class hierarchy that used to carry one MonoBehaviour per shape
    /// (SphereGizmo, ConeGizmo, BoxGizmo and their concrete subclasses).
    /// </summary>
    public enum ObjectGizmoGeometry
    {
        /// <summary>Wire sphere plus a handle at each of the six axis directions.</summary>
        Sphere,

        /// <summary>Wire cone; handle 0 moves the apex, the rest resize the rim.</summary>
        Cone,

        /// <summary>Screen-scaled directional-light indicator.</summary>
        DirectionalLight,

        /// <summary>Wire box around a local-space bounds.</summary>
        Box,
    }
}
