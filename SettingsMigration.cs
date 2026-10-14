using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PartAdjustment
{
    // Retain the requested preferences while removing obsolete sections and keys.
    internal static class SettingsMigration
    {
        private static readonly string[] general = { "Enabled", "VisibleGizmosWithoutTool", "PutAwayWithKey", "PutAwayKey",
            "MovementRotationSpeed", "FastMovementRotationSpeed", "FastModifierKey", "AbsoluteAdjustmentRotation", "Apocasetter" };
        private static readonly string[] headlights = { "AttachAnywhere", "TailLightChance" };

        private static Dictionary<string, string> Read(string text)
        {
            var values = new Dictionary<string, string>();
            string section = "";
            foreach (var raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("[") && line.EndsWith("]")) { section = line.Substring(1, line.Length - 2); continue; }
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                int equals = line.IndexOf('=');
                if (equals > 0) values[section + "/" + line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim();
            }
            return values;
        }

        internal static bool NeedsUpgrade(string text)
        {
            var values = Read(text);
            return values.Count != general.Length + headlights.Length
                || general.Any(k => !values.ContainsKey("General/" + k))
                || headlights.Any(k => !values.ContainsKey("Headlights/" + k));
        }

        internal static string Upgrade(string text)
        {
            var values = Read(text);
            string Value(string key, string fallback, string oldKey = null) => values.TryGetValue(key, out var value) ? value
                : oldKey != null && values.TryGetValue(oldKey, out value) ? value : fallback;
            string Speed(string key, string oldKey, float fallback)
            {
                if (values.TryGetValue(key, out var current)) return current;
                if (!values.TryGetValue(oldKey, out var old) || !float.TryParse(old, NumberStyles.Float, CultureInfo.InvariantCulture, out var step)
                    || float.IsNaN(step) || float.IsInfinity(step) || step <= 0f) return fallback.ToString(CultureInfo.InvariantCulture);
                return Math.Min(10000f, Math.Max(10f, step / .01f * 100f)).ToString("0.###", CultureInfo.InvariantCulture);
            }
            return "[General]\n"
                + "Enabled = " + Value("General/Enabled", "true") + "\n"
                + "VisibleGizmosWithoutTool = " + Value("General/VisibleGizmosWithoutTool", "true") + "\n"
                + "PutAwayWithKey = " + Value("General/PutAwayWithKey", "true", "Tools/PutAwayWithKey") + "\n"
                + "PutAwayKey = " + Value("General/PutAwayKey", "X", "Tools/PutAwayKey") + "\n"
                + "MovementRotationSpeed = " + Speed("General/MovementRotationSpeed", "Items/MovementStep", 100f) + "\n"
                + "FastMovementRotationSpeed = " + Speed("General/FastMovementRotationSpeed", "Items/FastMovementStep", 1000f) + "\n"
                + "FastModifierKey = " + Value("General/FastModifierKey", "LeftAlt", "Items/ModifierKey") + "\n"
                + "AbsoluteAdjustmentRotation = " + Value("General/AbsoluteAdjustmentRotation", "true") + "\n"
                + "Apocasetter = true\n\n[Headlights]\n"
                + "AttachAnywhere = " + Value("Headlights/AttachAnywhere", "true") + "\n"
                + "TailLightChance = " + Value("Headlights/TailLightChance", "8") + "\n";
        }
    }
}
