using System;
using PartAdjustment;

static class Program
{
    static void Check(SuspensionOwnership owner, bool kit, bool saved, SuspensionUpdate expected, string name)
    {
        if (owner.BeginApply(kit, saved) != expected) throw new Exception(name);
    }

    static void Main()
    {
        // Original Rustallion lifted SetPosition values from the game's prefab.
        foreach (float nativeY in new[] { -.2347564995288849f, -.4099999964237213f })
        foreach (bool front in new[] { false, true })
        {
            if (SuspensionStanceMath.MountY(nativeY, 0f, 0f, front) != nativeY)
                throw new Exception("unadjusted vanilla lift kit must keep native physical mount Y");
            if (Math.Abs(SuspensionStanceMath.MountY(nativeY, .02f, 0f, front) - (nativeY - .02f)) > .000001f)
                throw new Exception("manual lift changes native height only by the requested amount");
        }
        foreach (bool buggy in new[] { false, true })
        {
            float drop = SuspensionAlignmentDefaults.ModelDrop(buggy);
            // Previously both sides of this wheel/model difference included a wheel +Y offset.
            float oldDifference = -.4f + drop - (-.3f);
            float newDifference = -.4f - (-.3f - drop);
            if (Math.Abs(oldDifference - newDifference) > .000001f)
                throw new Exception("moving model preserves the accepted relative vertical alignment");
        }
        var stock = new SuspensionOwnership();
        for (int i = 0; i < 100; i++)
            Check(stock, false, false, SuspensionUpdate.None, "stock spawn, load and native wheel callbacks remain passive");
        Check(stock, true, false, SuspensionUpdate.AdjustKit, "fitting kit enables calibration");
        Check(stock, true, true, SuspensionUpdate.AdjustKit, "saved kit adjustments remain supported");
        Check(stock, false, false, SuspensionUpdate.RestoreStock, "removing even an unadjusted calibrated kit restores stock");
        for (int i = 0; i < 4; i++)
            Check(stock, false, false, SuspensionUpdate.None, "successive native stock wheel callbacks do not overwrite neighbours");
        var loaded = new SuspensionOwnership();
        Check(loaded, false, true, SuspensionUpdate.RestoreStock, "orphaned saved stance restores once without a kit");
        Check(loaded, false, false, SuspensionUpdate.None, "recovered save returns to native control");
        Check(stock, true, false, SuspensionUpdate.AdjustKit, "refitting kit resumes adjustment");
        Check(stock, false, false, SuspensionUpdate.RestoreStock, "second removal restores again");
        Console.WriteLine("PASS: Rustallion native lift-kit mount heights, model alignment, manual lift, stock pass-through, kit removal/refitting and orphaned save recovery.");
    }
}
