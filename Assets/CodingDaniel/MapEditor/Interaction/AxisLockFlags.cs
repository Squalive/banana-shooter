using System;
using CodingDaniel.MapEditor.MECommon;

namespace CodingDaniel.MapEditor.Interaction
{
    /// <summary>
    /// A set of axis locks, mirroring LockObject's flags. Used as tool data so a tool can declare which
    /// axes a view mode has to lock without hard-coding the answer in a property setter.
    /// </summary>
    [Flags]
    public enum AxisLockFlags
    {
        None = 0,
        PositionX = 1 << 0,
        PositionY = 1 << 1,
        PositionZ = 1 << 2,
        RotationX = 1 << 3,
        RotationY = 1 << 4,
        RotationZ = 1 << 5,
        RotationFree = 1 << 6,
        RotationScreen = 1 << 7,
        ScaleX = 1 << 8,
        ScaleY = 1 << 9,
        ScaleZ = 1 << 10,
    }

    public static class AxisLockFlagsExtensions
    {
        /// <summary>Sets the flags on a lock object. Expects a fresh clone, not a shared instance.</summary>
        public static void ApplyTo(this AxisLockFlags flags, LockObject target)
        {
            if (target == null)
            {
                return;
            }

            if ((flags & AxisLockFlags.PositionX) != 0) { target.PositionX = true; }
            if ((flags & AxisLockFlags.PositionY) != 0) { target.PositionY = true; }
            if ((flags & AxisLockFlags.PositionZ) != 0) { target.PositionZ = true; }
            if ((flags & AxisLockFlags.RotationX) != 0) { target.RotationX = true; }
            if ((flags & AxisLockFlags.RotationY) != 0) { target.RotationY = true; }
            if ((flags & AxisLockFlags.RotationZ) != 0) { target.RotationZ = true; }
            if ((flags & AxisLockFlags.RotationFree) != 0) { target.RotationFree = true; }
            if ((flags & AxisLockFlags.RotationScreen) != 0) { target.RotationScreen = true; }
            if ((flags & AxisLockFlags.ScaleX) != 0) { target.ScaleX = true; }
            if ((flags & AxisLockFlags.ScaleY) != 0) { target.ScaleY = true; }
            if ((flags & AxisLockFlags.ScaleZ) != 0) { target.ScaleZ = true; }
        }
    }
}