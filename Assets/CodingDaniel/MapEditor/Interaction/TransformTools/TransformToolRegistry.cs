using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.Interaction;

namespace CodingDaniel.MapEditor.Interaction.TransformTools
{
    /// <summary>
    /// The transform tools, as data. Values are carried over unchanged from the three property setters
    /// that used to hold them.
    /// </summary>
    public static class TransformToolRegistry
    {
        private static readonly TransformToolDefinition[] _definitions =
        {
            Move(),
            Rotate(),
            Scale(),
        };

        public static TransformToolDefinition For(EditorTool tool)
        {
            for (int i = 0; i < _definitions.Length; ++i)
            {
                if (_definitions[i].Tool == tool)
                {
                    return _definitions[i];
                }
            }

            return null;
        }

        private static TransformToolDefinition Move()
        {
            return new TransformToolDefinition
            {
                Tool = EditorTool.Move,
                DisplayName = "Move",
                SupportsVertexSnapping = true,
                XY2DLocks = AxisLockFlags.PositionZ,
                XZ2DLocks = AxisLockFlags.PositionY,
                YZ2DLocks = AxisLockFlags.PositionX,
            };
        }

        private static TransformToolDefinition Rotate()
        {
            // Rotation also locks Free and Screen, because those modes have no meaningful in-plane
            // interpretation while looking straight down an axis.
            return new TransformToolDefinition
            {
                Tool = EditorTool.Rotate,
                DisplayName = "Rotate",
                XY2DLocks = AxisLockFlags.RotationX | AxisLockFlags.RotationY | AxisLockFlags.RotationFree | AxisLockFlags.RotationScreen,
                XZ2DLocks = AxisLockFlags.RotationX | AxisLockFlags.RotationZ | AxisLockFlags.RotationFree | AxisLockFlags.RotationScreen,
                YZ2DLocks = AxisLockFlags.RotationY | AxisLockFlags.RotationZ | AxisLockFlags.RotationFree | AxisLockFlags.RotationScreen,
            };
        }

        private static TransformToolDefinition Scale()
        {
            return new TransformToolDefinition
            {
                Tool = EditorTool.Scale,
                DisplayName = "Scale",
                XY2DLocks = AxisLockFlags.ScaleZ,
                XZ2DLocks = AxisLockFlags.ScaleY,
                YZ2DLocks = AxisLockFlags.ScaleX,
            };
        }
    }
}