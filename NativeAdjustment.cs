using UnityEngine;

namespace PartAdjustment
{
    internal static class NativeAdjustment
    {
        private static readonly ExclusiveAdjustment<PlayMakerFSM> selection = new ExclusiveAdjustment<PlayMakerFSM>(Transition);
        internal static PlayMakerFSM Active => selection.Active;

        private static void Transition(PlayMakerFSM hinge, bool activate)
        {
            if (hinge == null) return;
            hinge.SendEvent(activate ? "Activate" : "Deactivate");
            hinge.Fsm.UpdateStateChanges();
        }

        internal static void Begin(GameObject picked)
        {
            var hinge = AdjustmentRunner.Find(picked, "AdjustHinge");
            selection.Begin(hinge != null && hinge.isActiveAndEnabled ? hinge : null);
        }

        internal static void Stop() => selection.Stop();
    }
}
