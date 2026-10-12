namespace PartAdjustment
{
    // Preserve the accepted relative alignment without raising the physical wheel mounts.
    // Vertical calibration belongs to the visible model; wheel Y stays native until adjusted.
    internal static class SuspensionAlignmentDefaults
    {
        internal static float Lateral(bool buggy, bool left) => buggy ? 0f : left ? .10f : -.10f;
        internal static float ModelDrop(bool buggy) => buggy ? .10f : .20f;
    }
}
