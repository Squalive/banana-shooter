using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.Interaction;

namespace CodingDaniel.MapEditor.Interaction.TransformTools
{
    /// <summary>
    /// What distinguishes one transform tool from another.
    ///
    /// Today that is exactly one thing: which axes each camera-aligned 2D view has to lock. Looking
    /// straight down an axis and dragging must not move or rotate about the axis pointing at the
    /// camera. Each tool used to hard-code its own answer in a SharedLockObject property setter; the
    /// answers are data here and the mechanism lives once in BaseHandle.
    ///
    /// Grid size and snapping deliberately stay on the handle components: GridSize is serialized scene
    /// configuration there, so duplicating it here would create two sources of truth.
    /// </summary>
    public sealed class TransformToolDefinition
    {
        public EditorTool Tool;
        public string DisplayName;

        /// <summary>Whether the V key drives vertex snapping while this tool is active.</summary>
        public bool SupportsVertexSnapping;

        public AxisLockFlags XY2DLocks;
        public AxisLockFlags XZ2DLocks;
        public AxisLockFlags YZ2DLocks;

        /// <summary>Locks this tool forces in the given view mode. XYZ3D forces none.</summary>
        public AxisLockFlags LocksFor(ViewMode mode)
        {
            switch (mode)
            {
                case ViewMode.XY2D: return XY2DLocks;
                case ViewMode.XZ2D: return XZ2DLocks;
                case ViewMode.YZ2D: return YZ2DLocks;
                default: return AxisLockFlags.None;
            }
        }
    }
}