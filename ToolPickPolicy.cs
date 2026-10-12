namespace PartAdjustment
{
    internal static class ToolPickPolicy
    {
        internal static bool ToolRemoves(string id) => id != "cassette";
        internal static bool AcceptTrigger(int layer, bool wheel, bool removablePart) =>
            layer == 7 || layer == 13 || wheel || removablePart;

        internal static bool GiveUseToDrive(bool driveOffered, bool holding, bool editing, bool aimedPart) =>
            driveOffered && !holding && !editing && !aimedPart;
    }
}
