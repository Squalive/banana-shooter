namespace CodingDaniel.MapEditor.Interaction
{
    /// <summary>
    /// How the scene camera is aligned to the handle's axis frame. When the camera looks straight down
    /// one axis the handle is in a 2D mode and must not act on that axis.
    /// </summary>
    public enum ViewMode
    {
        XYZ3D,
        XY2D,
        XZ2D,
        YZ2D,
    }
}