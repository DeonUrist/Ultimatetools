using System;

namespace PartAdjustment
{
    internal static class SuspensionStanceMath
    {
        internal const float Step = .01f, AxleLimit = .15f, BalanceLimit = .075f;

        internal static void Clamp(ref float height, ref float balance)
        {
            balance = FiniteClamp(balance, BalanceLimit);
            height = FiniteClamp(height, AxleLimit - Math.Abs(balance));
        }

        internal static void Change(ref float height, ref float balance, float heightDelta, float balanceDelta)
        {
            Clamp(ref height, ref balance);
            height = FiniteClamp(height + heightDelta, AxleLimit - Math.Abs(balance));
            balance = FiniteClamp(balance + balanceDelta, Math.Min(BalanceLimit, AxleLimit - Math.Abs(height)));
        }

        internal static float Lift(float height, float balance, bool front) => height + (front ? balance : -balance);
        private static float FiniteClamp(float value, float limit) => float.IsNaN(value) || float.IsInfinity(value)
            ? 0f : Math.Max(-limit, Math.Min(limit, value));
    }
}
