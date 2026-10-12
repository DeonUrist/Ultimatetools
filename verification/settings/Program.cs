using System;
using PartAdjustment;

static class Program
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static bool Near(float a, float b) => Math.Abs(a - b) < .000001f;
    static void Main()
    {
        var old = "[General]\nEnabled = false\nShowGuide = false\n[Tools]\nPutAwayWithKey = false\nPutAwayKey = Z\n"
            + "[Items]\nMovementStep = 0.01\nRotationAngle = 1\nFastMovementStep = 0.1\nFastRotationAngle = 10\nModifierKey = RightAlt\n"
            + "[DEBUG]\nLiftedOffsetX = -0.1\n[Headlights]\nAttachAnywhere = false\nTailLightChance = 23\n";
        Check(SettingsMigration.NeedsUpgrade(old), "old config requires migration");
        var upgraded = SettingsMigration.Upgrade(old);
        Check(!SettingsMigration.NeedsUpgrade(upgraded), "migrated config has exactly the supported settings");
        Check(upgraded.Contains("Enabled = false") && upgraded.Contains("PutAwayWithKey = false") && upgraded.Contains("PutAwayKey = Z"), "preserve general preferences");
        Check(upgraded.Contains("FastModifierKey = RightAlt") && upgraded.Contains("AttachAnywhere = false") && upgraded.Contains("TailLightChance = 23"), "preserve modifier and headlight preferences");
        Check(upgraded.Contains("MovementRotationSpeed = 100\n") && upgraded.Contains("FastMovementRotationSpeed = 1000\n"), "old speeds retain their exact baseline");
        Check(upgraded.Contains("VisibleGizmosWithoutTool = true") && !upgraded.Contains("ShowGuide") && !upgraded.Contains("[DEBUG]") && !upgraded.Contains("[Items]") && !upgraded.Contains("[Tools]"), "remove obsolete options and default passive highlights on");
        Check(SettingsMigration.Upgrade(upgraded) == upgraded, "migration does not change current preferences twice");
        Check(Near(ToolSettingsMath.Step(false,100f), .01f) && Near(ToolSettingsMath.Step(true,100f), 1f), "normal speed remains 1 cm and 1 degree");
        Check(Near(ToolSettingsMath.Step(false,1000f), .1f) && Near(ToolSettingsMath.Step(true,1000f), 10f), "fast speed remains 10 cm and 10 degrees");
        Check(Near(ToolSettingsMath.Step(false,250f), .025f) && Near(ToolSettingsMath.Step(true,250f), 2.5f), "combined percentage scales movement and rotation equally");
        Check(Near(ToolSettingsMath.Step(false,float.NaN), .01f) && Near(ToolSettingsMath.Step(true,float.PositiveInfinity), 1f), "invalid speed stays finite");
        Check(ToolSettingsMath.ShowSlots(true,true,false,false,true), "default highlights without tool");
        Check(!ToolSettingsMath.ShowSlots(true,false,false,false,true), "option off hides highlights without tool");
        Check(ToolSettingsMath.ShowSlots(true,false,true,false,true), "option off still highlights with tool");
        Check(!ToolSettingsMath.ShowSlots(false,true,true,false,true), "disabled mod hides highlights");
        Check(!ToolSettingsMath.ShowSlots(true,true,true,true,true) && !ToolSettingsMath.ShowSlots(true,true,true,false,false), "hide highlights while driving or outside gameplay");
        Console.WriteLine("PASS: settings migration, percentage speed equivalence/scaling, passive/tool-only visibility and gameplay gates.");
    }
}
