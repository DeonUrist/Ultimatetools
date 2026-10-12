using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace PartAdjustment
{
    internal static class SaveVariables
    {
        internal const string Width = "PartAdjustment_Width";
        internal const string Height = "PartAdjustment_Height";
        internal const string RideHeight = "UltimateTools_RideHeight", Balance = "UltimateTools_SuspensionBalance";
        internal const string ModelPosition = "PartAdjustment_ModelPosition";
        internal const string ModelScale = "PartAdjustment_ModelScale";
        internal const string HingePosition = "PartAdjustment_HingePosition";
        internal const string FloorPosition = "UltimateTools_FloorPosition", FloorScale = "UltimateTools_FloorScale";

        internal static void RemoveHeight(FsmVariables variables)
        {
            if (!variables.FloatVariables.Any(v => v.Name == Height)) return;
            variables.FloatVariables = variables.FloatVariables.Where(v => v.Name != Height).ToArray();
            variables.Reinitialize();
        }

        internal static void Remove(FsmVariables variables)
        {
            if (!variables.FloatVariables.Any(v => v.Name == Width || v.Name == Height || v.Name == RideHeight || v.Name == Balance)
                && !variables.Vector3Variables.Any(v => v.Name == ModelPosition || v.Name == ModelScale || v.Name == HingePosition
                    || v.Name == FloorPosition || v.Name == FloorScale || v.Name.StartsWith("UltimateTools_Structure_") || v.Name.StartsWith("UltimateTools_Axle_"))) return;
            variables.FloatVariables = variables.FloatVariables.Where(v => v.Name != Width && v.Name != Height && v.Name != RideHeight && v.Name != Balance).ToArray();
            variables.Vector3Variables = variables.Vector3Variables.Where(v => v.Name != ModelPosition
                && v.Name != ModelScale && v.Name != HingePosition && v.Name != FloorPosition && v.Name != FloorScale
                && !v.Name.StartsWith("UltimateTools_Structure_") && !v.Name.StartsWith("UltimateTools_Axle_")).ToArray();
            variables.Reinitialize();
        }

        internal static FsmFloat Float(FsmVariables variables, string name, float fallback)
        {
            var value = variables.FloatVariables.FirstOrDefault(v => v.Name == name);
            if (value != null) return value;
            value = new FsmFloat(name) { Value = fallback };
            variables.FloatVariables = variables.FloatVariables.Concat(new[] { value }).ToArray();
            variables.Reinitialize();
            return value;
        }

        internal static FsmVector3 Vector(FsmVariables variables, string name, Vector3 fallback)
        {
            var value = variables.Vector3Variables.FirstOrDefault(v => v.Name == name);
            if (value != null) return value;
            value = new FsmVector3(name) { Value = fallback };
            variables.Vector3Variables = variables.Vector3Variables.Concat(new[] { value }).ToArray();
            variables.Reinitialize();
            return value;
        }
    }
}
