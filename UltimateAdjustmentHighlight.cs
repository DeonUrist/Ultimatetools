using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace PartAdjustment
{
    // BlendZBuffer has no material tint and the native mesh is read-only.
    // Use the same textured tintable overlay as compatible socket icons.
    internal static class UltimateAdjustmentHighlight
    {
        private sealed class Icon
        {
            internal MeshRenderer Renderer;
            internal Material Original, Green;
        }

        private static GameObject selected;
        private static readonly List<Icon> icons = new List<Icon>();

        internal static void Update()
        {
            var hinge = UltimateTool.NativeEditing && UltimateTool.Screwdriver
                ? UltimateTool.Adjust.FsmVariables.GetFsmGameObject("Hinge")?.Value : null;
            if (hinge == null && UltimateTool.Active && UltimateTool.Screwdriver && AdjustmentRunner.Session != null && !UltimateTool.Holding)
                for (var t = AdjustmentRunner.Session.Target; t != null; t = t.parent)
                    if (AdjustmentRunner.Find(t.gameObject, "AdjustHinge") != null) { hinge = t.gameObject; break; }
            if (hinge == selected && (hinge == null || icons.Count > 0)) return;
            Clear();
            selected = hinge;
            if (selected == null) return;
            foreach (var follow in Resources.FindObjectsOfTypeAll<PlayMakerFSM>())
            {
                if (follow.FsmName != "HingeFollow" || !follow.gameObject.scene.IsValid()) continue;
                bool matches = follow.FsmStates.Any(s => SuspensionSupport.Actions(follow.Fsm, s).OfType<GetPosition>()
                    .Any(a => follow.Fsm.GetOwnerDefaultTarget(a.gameObject) == selected));
                if (!matches && selected.name.StartsWith("hinge_", System.StringComparison.Ordinal))
                {
                    var vehicle = selected.GetComponentInParent<Rigidbody>();
                    matches = vehicle != null && follow.transform.IsChildOf(vehicle.transform)
                        && follow.name == selected.name.Substring(6) + "HingeVisible";
                }
                if (!matches) continue;
                Tint(follow.GetComponent<MeshFilter>());
            }
            var suspension = selected.GetComponent<SuspensionPickTarget>();
            if (suspension != null)
                foreach (var pick in Resources.FindObjectsOfTypeAll<SuspensionPickTarget>())
                    if (pick.Adjustment == suspension.Adjustment)
                        foreach (var filter in pick.GetComponentsInChildren<MeshFilter>(true)) Tint(filter);
        }

        private static void Tint(MeshFilter filter)
        {
            var renderer = filter == null ? null : filter.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null || icons.Any(i => i.Renderer == renderer)) return;
            var original = renderer.sharedMaterial;
            var green = UltimateMountMarker.GreenIconMaterial(original, "UltimateTools.ActiveAdjustment");
            renderer.sharedMaterial = green;
            icons.Add(new Icon { Renderer = renderer, Original = original, Green = green });
        }

        internal static void Clear()
        {
            foreach (var icon in icons)
            {
                if (icon.Renderer != null && icon.Renderer.sharedMaterial == icon.Green) icon.Renderer.sharedMaterial = icon.Original;
                if (icon.Green != null) Object.Destroy(icon.Green);
            }
            icons.Clear();
            selected = null;
        }
    }
}
