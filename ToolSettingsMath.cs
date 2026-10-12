namespace PartAdjustment
{
    internal static class ToolSettingsMath
    {
        internal static float Step(bool rotating, float percent)
        {
            if (float.IsNaN(percent) || float.IsInfinity(percent) || percent <= 0f) percent = 100f;
            return (rotating ? 1f : .01f) * percent / 100f;
        }
        internal static bool ShowSlots(bool enabled, bool withoutTool, bool toolActive, bool driving, bool gameplay) =>
            enabled && (withoutTool || toolActive) && !driving && gameplay;
    }
}
