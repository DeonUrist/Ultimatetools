using System;
using PartAdjustment;

static class Program
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
    static bool Near(float a, float b) => Math.Abs(a - b) < .000001f;
    static void Bounded(float height, float balance)
    {
        Check(float.IsFinite(height) && float.IsFinite(balance), "finite stance");
        Check(Math.Abs(SuspensionStanceMath.Lift(height, balance, true)) <= .150001f, "front stays within 15 cm");
        Check(Math.Abs(SuspensionStanceMath.Lift(height, balance, false)) <= .150001f, "rear stays within 15 cm");
        Check(Math.Abs(balance) <= .075001f, "balance stays within 7.5 cm");
    }
    static void Main()
    {
        float height = 0, balance = 0;
        SuspensionStanceMath.Change(ref height, ref balance, .01f, 0f);
        Check(Near(SuspensionStanceMath.Lift(height, balance, true), .01f)
            && Near(SuspensionStanceMath.Lift(height, balance, false), .01f), "raise moves both ends equally");
        SuspensionStanceMath.Change(ref height, ref balance, 0f, .01f);
        Check(Near(SuspensionStanceMath.Lift(height, balance, true), .02f)
            && Near(SuspensionStanceMath.Lift(height, balance, false), 0f), "positive balance raises front and lowers rear");
        height = 0; balance = 0;
        SuspensionStanceMath.Change(ref height, ref balance, 0f, -.01f);
        Check(Near(SuspensionStanceMath.Lift(height, balance, true), -.01f)
            && Near(SuspensionStanceMath.Lift(height, balance, false), .01f), "negative balance tilts nose down");
        foreach (float h in new[] { -100f, -.15f, 0f, .15f, 100f, float.NaN, float.PositiveInfinity })
        foreach (float b in new[] { -100f, -.075f, 0f, .075f, 100f, float.NaN, float.NegativeInfinity })
        {
            height = h; balance = b; SuspensionStanceMath.Clamp(ref height, ref balance);
            Bounded(height, balance);
        }
        height = 0; balance = 0;
        var random = new Random(20);
        for (int i = 0; i < 10000; i++)
        {
            SuspensionStanceMath.Change(ref height, ref balance,
                random.Next(-1, 2) * .01f, random.Next(-1, 2) * .01f);
            Bounded(height, balance);
        }
        height = .15f; balance = 0;
        SuspensionStanceMath.Change(ref height, ref balance, .01f, .01f);
        Check(Near(height, .15f) && Near(balance, 0f), "at full lift additional tilt cannot exceed end limit");
        height = .10f; balance = -.04f;
        float savedHeight = height, savedBalance = balance;
        for (int i = 0; i < 10; i++)
        {
            height = savedHeight; balance = savedBalance; SuspensionStanceMath.Clamp(ref height, ref balance);
            Check(Near(height, .10f) && Near(balance, -.04f), "valid saved stance does not drift on reload");
        }
        Console.WriteLine("PASS: raise/lower and pitch signs, corrupt-save bounds, 10,000 input sequences, limits and reload stability.");
    }
}
