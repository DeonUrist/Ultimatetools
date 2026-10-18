using System;

namespace PartAdjustment
{
    // Scale a captured baseline, never the previous scaled result. Native state changes refresh it.
    internal sealed class SuspensionDamping
    {
        internal const float MinimumFraction = .25f;
        private float originalBump, originalRebound, writtenBump, writtenRebound;
        private bool captured, owned, blocked;

        internal static float Fraction(float height, float balance, bool front)
        {
            SuspensionStanceMath.Clamp(ref height, ref balance);
            float lift = Math.Max(0f, SuspensionStanceMath.Lift(height, balance, front));
            return 1f - (1f - MinimumFraction) * Math.Min(1f, lift / SuspensionStanceMath.AxleLimit);
        }

        internal void NativeChanged(float bump, float rebound)
        {
            originalBump = bump; originalRebound = rebound;
            captured = Valid(bump) && Valid(rebound);
            owned = blocked = false;
        }

        internal void ExternalChanged()
        {
            captured = owned = false;
            blocked = true;
        }

        internal bool Update(float bump, float rebound, float fraction, bool active,
            out float nextBump, out float nextRebound)
        {
            nextBump = bump; nextRebound = rebound;
            if (owned && (bump != writtenBump || rebound != writtenRebound)) ExternalChanged();
            if (blocked) return false;
            if (!active || fraction >= 1f)
            {
                bool restore = owned;
                if (restore) { nextBump = originalBump; nextRebound = originalRebound; }
                captured = owned = false;
                return restore && (nextBump != bump || nextRebound != rebound);
            }
            if (!Valid(bump) || !Valid(rebound) || float.IsNaN(fraction) || float.IsInfinity(fraction)) return false;
            if (!captured) NativeChanged(bump, rebound);
            fraction = Math.Max(MinimumFraction, Math.Min(1f, fraction));
            nextBump = writtenBump = originalBump * fraction;
            nextRebound = writtenRebound = originalRebound * fraction;
            owned = true;
            return nextBump != bump || nextRebound != rebound;
        }

        private static bool Valid(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
