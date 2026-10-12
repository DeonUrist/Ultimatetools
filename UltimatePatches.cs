using System.Collections.Generic;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace PartAdjustment
{
    [HarmonyPatch(typeof(MousePick), "DoMousePick")]
    internal static class NativeToolPickPatch
    {
        private static bool Prefix(MousePick __instance)
        {
            if (!UltimateTool.Active || !ToolFsm(__instance.Fsm?.Name)) return true;
            if(__instance.storeGameObject != null) __instance.storeGameObject.Value=null;
            if(__instance.storeDidPickObject != null) __instance.storeDidPickObject.Value=false;
            __instance.Finish(); return false;
        }
        internal static bool ToolFsm(string name) => name=="UseAdjustTool" || name=="UseTool" || name=="UseAttachableTool";
    }
    [HarmonyPatch(typeof(MousePickEvent), "DoMousePickEvent")]
    internal static class NativeToolHoverPatch
    {
        private static bool Prefix(MousePickEvent __instance) => !UltimateTool.Active || !NativeToolPickPatch.ToolFsm(__instance.Fsm?.Name);
    }
    [HarmonyPatch(typeof(GameObjectHasChildren), nameof(GameObjectHasChildren.OnEnter))]
    internal static class ToolDriveHandPatch
    {
        private static bool Prefix(GameObjectHasChildren __instance)
        {
            if(!UltimateTool.Active || UltimateTool.Holding || __instance.Fsm?.Name!="DriveUse"
                || (__instance.State?.Name!="checkHand" && __instance.State?.Name!="checkHand 2")) return true;
            if(__instance.storeResult!=null)__instance.storeResult.Value=false;
            __instance.Fsm.Event(__instance.falseEvent); __instance.Finish(); return false;
        }
    }
    [HarmonyPatch(typeof(ES3Types.ES3Type_GameObject), nameof(ES3Types.ES3Type_GameObject.GetChildren))]
    internal static class UltimateTransientSavePatch
    {
        private static void Postfix(List<GameObject> __result) => __result.RemoveAll(g=>g!=null && g.GetComponent<UltimateTransient>()!=null);
    }
}
