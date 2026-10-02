using CodingDaniel.MapEditor.MECommon;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodingDaniel.MapEditor.Interaction
{
    /// <summary>
    /// One frame's worth of state for drawing a transform handle: where the handle sits, which axis is
    /// highlighted, and which axes are locked. Renamed from RTHDrawingSettings; it was declared inside
    /// MEHandleComponent, which is no longer where the drawing lives.
    /// </summary>
    public class HandleDrawSettings
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public HandleAxis SelectedAxis;
        public LockObject LockObject;
        public bool DrawLocked;

        public HandleDrawSettings()
        {
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Scale = Vector3.one;
            SelectedAxis = HandleAxis.None;
            LockObject = null;
            DrawLocked = true;
        }

        internal MaterialPropertyBlock[] PropertyBlocks;

        internal void Init(int propertyBlocksCount)
        {
            if (PropertyBlocks == null)
            {
                PropertyBlocks = new MaterialPropertyBlock[propertyBlocksCount];
                for (int i = 0; i < propertyBlocksCount; ++i)
                {
                    PropertyBlocks[i] = new MaterialPropertyBlock();
                }
            }
        }
    }
}