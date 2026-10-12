using HutongGames.PlayMaker;
using UnityEngine;

namespace PartAdjustment
{
    [ES3NonSerializable, DefaultExecutionOrder(-10000)]
    public sealed class AdjustmentRunner : MonoBehaviour
    {
        internal static AdjustmentSession Session;
        internal static Transform Hover;
        internal static bool HoverRemovable;
        internal static Camera PlayerCamera;
        internal static int ConsumedSecondaryFrame = -1;
        private static Vector3 direction;
        private static float step;
        private static bool reset;
        internal static PlayMakerFSM Find(GameObject go, string name)
        {
            if (go == null) return null;
            foreach(var f in go.GetComponents<PlayMakerFSM>()) if(f.FsmName == name) return f;
            return null;
        }
        internal static bool Gameplay => Plugin.Enabled.Value && Time.timeScale > 0f && Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
        internal static bool ToolSelected => UltimateTool.Active;
        private void Update()
        {
            direction = Vector3.zero; reset = false;
            var go=FsmVariables.GlobalVariables.GetFsmGameObject("PlayerCamera")?.Value;
            PlayerCamera=go == null ? null : go.GetComponent<Camera>();
            UltimateTool.Tick(PlayerCamera);
            Hover=UltimateTool.Target == null ? null : UltimateTool.Target.transform;
            HoverRemovable=ToolSelected && !UltimateTool.Screwdriver && !UltimateTool.Holding && UltimateTool.CanDetach(UltimateTool.Target);
            if (!ToolSelected) { Stop(); return; }
            if (!Gameplay || Session==null) return;
            if (Controls.Pressed(Controls.Mode)) Session.Rotating=!Session.Rotating;
            reset=Controls.Pressed(Controls.Reset);
            direction=Controls.Direction(Session.Rotating);
            step=Controls.Step(Session.Rotating,Controls.Fast);
        }
        internal static void Apply()
        {
            if (!ToolSelected || !UltimateTool.Screwdriver) { Stop(); return; }
            if (Session==null || !Gameplay || PlayerCamera==null) return;
            if (!Session.Valid) { Stop(); return; }
            if (reset) Session.Reset();
            else Session.Change(direction,step,PoseMath.ItemReference(Session.Target));
            if (UltimateTool.Held != null) UltimateTool.CaptureHandPose(UltimateTool.Held,UltimateTool.Grab);
        }
        internal static void Stop() { Session=null; Hover=null; HoverRemovable=false; direction=Vector3.zero; reset=false; }
        internal static void SceneChanged() { UltimateTool.Clear(); Stop(); HeadlightMount.SceneChanged(); PlayerCamera=null; ConsumedSecondaryFrame=-1; }
        private void OnDestroy() { SceneChanged(); }
    }
    [ES3NonSerializable, DefaultExecutionOrder(10001)]
    public sealed class AdjustmentLateRunner : MonoBehaviour
    {
        private void LateUpdate() { AdjustmentRunner.Apply(); }
    }
}
