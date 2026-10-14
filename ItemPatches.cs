using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using InsaneSystems.InputManager;
using UnityEngine;

namespace PartAdjustment
{
    [HarmonyPatch(typeof(InputController))]
    internal static class SecondaryInputPatch
    {
        [HarmonyPatch(nameof(InputController.GetKeyActionIsDown)), HarmonyPrefix]
        private static bool Down(string actionName, ref bool __result) => Filter(actionName, ref __result);
        [HarmonyPatch(nameof(InputController.GetKeyActionIsActive)), HarmonyPrefix]
        private static bool Active(string actionName, ref bool __result) => Filter(actionName, ref __result);
        [HarmonyPatch(nameof(InputController.GetKeyActionIsUp)), HarmonyPrefix]
        private static bool Up(string actionName, ref bool __result) => Filter(actionName, ref __result);
        private static bool Filter(string name, ref bool result)
        {
            if (name != Controls.Secondary || !AdjustmentRunner.Gameplay || !AdjustmentRunner.ToolSelected) return true;
            result = false;
            return false;
        }
    }

    // The frame our Use press mounted or removed a headlight, the game's FSMs must not also act on it.
    [HarmonyPatch(typeof(GetButtonDown), nameof(GetButtonDown.OnUpdate))]
    internal static class UseConsumedPatch
    {
        private static bool Prefix(GetButtonDown __instance)
        {
            if (__instance.buttonName?.Value != Controls.Use) return true;
            if (UltimateTool.Active && !UltimateTool.Driving)
            {
                if (__instance.Fsm?.Name == "DriveUse")
                    return !(UltimateTool.HeldId == "seat" && !UltimateTool.Screwdriver)
                        && UltimateTool.ConsumedFrame != Time.frameCount;
                if (__instance.storeResult != null) __instance.storeResult.Value = false;
                return false;
            }
            // Holding a headlight / tail light: the tool never starts a hinge adjustment (UseAdjustTool "over" -> "adjust").
            bool hingeStart = HeadlightMount.HingePressClaimed && __instance.Fsm?.Name == "UseAdjustTool" && __instance.State?.Name == "over";
            if (HeadlightMount.ConsumedUseFrame != Time.frameCount && !hingeStart) return true;
            if (__instance.storeResult != null) __instance.storeResult.Value = false;
            return false;
        }
    }
}
