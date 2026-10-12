using UnityEngine;

namespace PartAdjustment
{
    // Accepted calibration from 2.1.3; X is mirrored outward/inward per side.
    internal static class SuspensionAlignmentDefaults
    {
        internal static Vector3 For(bool buggy, bool left) => buggy
            ? new Vector3(0f, .10f, 0f)
            : new Vector3(left ? .10f : -.10f, .20f, 0f);
    }
}
