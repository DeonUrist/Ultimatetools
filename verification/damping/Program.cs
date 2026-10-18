using System;
using PartAdjustment;

static class Program
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    static bool Near(float a, float b) => Math.Abs(a - b) < .002f;
    static float Fraction(float height, float balance = 0f, bool front = true) => SuspensionDamping.Fraction(height, balance, front);
    static void Main()
    {
        Check(Near(Fraction(0), 1), "native height retains damping");
        Check(Near(Fraction(.05f), .75f), "5 cm gives 75%");
        Check(Near(Fraction(.10f), .50f), "10 cm gives 50%");
        Check(Near(Fraction(.15f), .25f), "15 cm gives 25%");
        Check(Near(Fraction(-.15f), 1), "lowering retains native damping");
        Check(Near(Fraction(.05f, .05f), .5f) && Near(Fraction(.05f, .05f, false), 1), "balance treats each axle independently");
        Check(Near(Fraction(0, -.075f), 1) && Near(Fraction(0, -.075f, false), .625f), "negative balance softens rear only");
        foreach (float height in new[] {float.NaN, float.PositiveInfinity, float.NegativeInfinity})
            Check(Near(Fraction(height), 1), "invalid saved height stays native");
        float previous = 1;
        for (int i = 0; i <= 150; i++)
        {
            float fraction = Fraction(i * .001f);
            Check(fraction <= previous + .000001f && fraction >= .25f, "continuous monotonic bounded curve");
            previous = fraction;
        }

        var damping = new SuspensionDamping();
        float bump = 7000, rebound = 6000;
        void Apply(float fraction, bool active = true)
        {
            damping.Update(bump, rebound, fraction, active, out bump, out rebound);
        }
        Apply(Fraction(.15f));
        Check(Near(bump, 1750) && Near(rebound, 1500), "Duke maximum lift preserves original bump/rebound ratio");
        for (int i = 0; i < 1000; i++) Apply(Fraction(.15f));
        Check(Near(bump, 1750) && Near(rebound, 1500), "repeated physics ticks do not compound damping");
        Apply(Fraction(.05f));
        Check(Near(bump, 5250) && Near(rebound, 4500), "lowering recalculates from original baseline");
        Apply(1);
        Check(bump == 7000 && rebound == 6000, "reset restores exact baseline");
        Apply(.25f); Apply(.25f, false);
        Check(bump == 7000 && rebound == 6000, "kit removal or disable restores owned values");

        Apply(.25f);
        bump = 8000; rebound = 8000;
        damping.NativeChanged(bump, rebound); Apply(.25f, false);
        Check(bump == 8000 && rebound == 8000, "completed stock transition retains new stock rates");
        bump = rebound = 7000;
        damping.NativeChanged(bump, rebound); Apply(.25f);
        Check(bump == 1750 && rebound == 1750, "refit captures new lifted rates");
        bump = 3200; rebound = 4100;
        Apply(.25f); Apply(.25f, false); Apply(.5f);
        Check(bump == 3200 && rebound == 4100, "external writes survive changes, disable and reactivation");
        bump = rebound = 4000; damping.NativeChanged(bump, rebound); Apply(.5f);
        Check(bump == 2000 && rebound == 2000, "known native transition can reclaim ownership");
        damping.ExternalChanged(); bump = rebound = 1500; Apply(.25f);
        Check(bump == 1500 && rebound == 1500, "replacement damper yields ownership");

        foreach (float baseline in new[] {1500f, 4000f, 6000f, 7000f})
        {
            bump = rebound = baseline; damping.NativeChanged(bump, rebound); Apply(.25f);
            Check(Near(bump, baseline / 4) && Near(rebound, baseline / 4), "vehicle-specific baseline scales proportionally");
        }
        foreach (float invalid in new[] {0f, -1f, float.NaN, float.PositiveInfinity})
        {
            var bad = new SuspensionDamping();
            Check(!bad.Update(invalid, 4000, .25f, true, out _, out _), "invalid baseline is not written");
        }
        Console.WriteLine("PASS: " + checks + " progressive damping, per-axle balance, lifecycle and external ownership checks.");
    }
}
