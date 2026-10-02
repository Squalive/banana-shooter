namespace CodingDaniel.MapEditor.Interaction
{
    /// <summary>
    /// The axis (or screen-space mode) a transform handle is acting on.
    ///
    /// Renamed from RuntimeHandleAxis: "RTH" was the Asset Store package's Runtime Handle vocabulary.
    /// Values are unchanged, including the flag combinations XY/XZ/YZ.
    /// </summary>
    public enum HandleAxis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        XY = X | Y,
        XZ = X | Z,
        YZ = Y | Z,
        Screen = 8,
        Free = 16,
        Snap = 32,
        Custom = 65536
    }
}