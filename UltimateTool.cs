using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace PartAdjustment
{
    // One input owner for both native toolboxes. Native recipes still own attachment,
    // CheckTag state, cooling/light behavior and saving; this controller chooses their target.
    internal static class UltimateTool
    {
        internal static bool Active, Screwdriver;
        internal static PlayMakerFSM Adjust, Grab, Drive;
        internal static GameObject Held, Target;
        internal static PlayMakerFSM TargetHinge, TargetSlot;
        internal static RaycastHit Hit;
        internal static bool Driving, DriveOffered;
        internal static int ConsumedFrame = -1;
        private static PlayMakerFSM toolbox, adjustBox, utility;
        private static GameObject owner;
        private static bool forcedAdjust;
        private static float nextSlots;
        private static readonly List<PlayMakerFSM> slots = new List<PlayMakerFSM>();
        private static readonly HashSet<Transform> nearbyVehicles = new HashSet<Transform>();
        private static readonly List<UltimateMountMarker> nearbyMarkers = new List<UltimateMountMarker>();
        private static bool markersShown;
        internal static readonly List<UltimateMountMarker> Markers = new List<UltimateMountMarker>();
        internal static bool NativeEditing => Active && Adjust != null && Adjust.ActiveStateName == "adjust";
        internal static bool Editing => AdjustmentRunner.Session != null || NativeEditing;
        internal static bool Holding => Held != null;
        internal static string HeldId => Id(Held);

        internal static string Id(GameObject item) => item == null ? null : AdjustmentRunner.Find(item, "ID")?.FsmVariables.GetFsmString("ID")?.Value;
        internal static bool InHandState(string s) => s == "ItemInHand" || s == "Rotate" || s == "Forward" || s == "Backward";
        internal static bool BoxOut(PlayMakerFSM fsm) => fsm != null && fsm.isActiveAndEnabled && (fsm.ActiveStateName == "off 2" || fsm.ActiveStateName == "over 2");

        internal static void Tick(Camera camera)
        {
            if (camera == null) { Clear(); return; }
            if (owner != camera.gameObject)
            {
                Clear(); owner = camera.gameObject;
                Adjust = AdjustmentRunner.Find(owner, "UseAdjustTool"); Grab = AdjustmentRunner.Find(owner, "GrabItem");
                Drive = AdjustmentRunner.Find(owner, "DriveUse"); utility = AdjustmentRunner.Find(owner, "UseTool");
                toolbox = AdjustmentRunner.Find(owner, "UseToolbox"); adjustBox = AdjustmentRunner.Find(owner, "UseAdjustToolBox");
            }
            bool outNow = Plugin.Enabled.Value && (BoxOut(toolbox) || BoxOut(adjustBox));
            var previousHeld = Held;
            Held = Grab != null && InHandState(Grab.ActiveStateName) ? HeadlightMount.ItemRoot(Grab.FsmVariables.GetFsmGameObject("Item")?.Value) : null;
            if (Held != previousHeld) nextSlots = 0;
            Driving = Drive != null && new[] { "inCar", "over 2", "TooFast", "checkHand 2", "enter", "exit" }.Contains(Drive.ActiveStateName);
            DriveOffered = Drive != null && (Drive.ActiveStateName == "over" || Drive.ActiveStateName == "over 2");
            if (!outNow)
            {
                if (Active) { Finish(); SetAdjustmentMarkers(false); if (forcedAdjust && Adjust != null) Adjust.enabled = false; }
                Active = false; forcedAdjust = false; Target = null; TargetHinge = TargetSlot = null;
                if (camera.isActiveAndEnabled) RefreshSlots(camera); else HideMarkers();
                ToolVisual.Update(false, Screwdriver, toolbox, adjustBox); return;
            }
            if (!Active) { Screwdriver = false; slots.Clear(); nextSlots = 0; }
            Active = true;
            // Keep the adjustment FSM available even when the utility toolbox supplied the arm.
            if (Adjust != null && !Adjust.enabled) { Adjust.enabled = true; forcedAdjust = BoxOut(toolbox); }
            if (!AdjustmentRunner.Gameplay || !camera.isActiveAndEnabled) { HideMarkers(); return; }
            if (Driving) { Finish(); HideMarkers(); Target = null; TargetHinge = TargetSlot = null; }
            else
            {
                RefreshSlots(camera);
                Pick(camera);
            }
            if (Controls.Pressed(Controls.Secondary))
            {
                Finish(); Screwdriver = !Screwdriver; AdjustmentRunner.ConsumedSecondaryFrame = Time.frameCount;
            }
            ToolVisual.Update(true, Screwdriver, toolbox, adjustBox);
            SetAdjustmentMarkers(Screwdriver && !Driving);
            if (Driving) return; // Native DriveUse retains the complete exit/speed safety flow.
            if (Controls.Pressed(Controls.Use))
            {
                if (DriveOffered && !Holding) { Finish(); return; }
                ConsumedFrame = HeadlightMount.ConsumedUseFrame = Time.frameCount;
                if (Editing) { Finish(); return; }
                string id = Id(Target);
                if (id == "Toolbox" || id == "PartAdjusterTools") { PutAway(); return; }
                if (Screwdriver) BeginAdjust();
                else if (Held != null) AttachHeld();
                else if (Target != null && Target.CompareTag("vehPart") && AdjustmentRunner.Find(Target, "de_Attach") != null) DetachAndGrab(Target);
            }
            if (AdjustmentRunner.Session != null && !AdjustmentRunner.Session.Valid) AdjustmentRunner.Stop();
        }

        // The same center ray resolves a part, a mount and a foreground blocker.
        // Held items, player arms and unrelated triggers are not interaction occluders.
        internal static RaycastHit ExactHit(Camera camera, float reach, GameObject held)
        {
            if (camera == null) return default(RaycastHit);
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            int mask = (1 << 0) | (1 << 7) | (1 << 8) | (1 << 9) | (1 << 13) | (1 << 16) | (1 << 14);
            // The rolling wheel hub can use a physics layer outside the normal item mask.
            var hits = Physics.RaycastAll(ray, reach, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var c = hit.collider;
                if (c == null || (held != null && c.transform.IsChildOf(held.transform))) continue;
                var wheel = c.GetComponentInParent<NWH.WheelController3D.WheelController>();
                if ((mask & (1 << c.gameObject.layer)) == 0 && wheel == null) continue;
                var marker = c.GetComponentInParent<UltimateMountMarker>();
                if (marker != null)
                {
                    if (held != null && Compatible(marker.Slot, held)) return hit;
                    continue;
                }
                if (c.transform.IsChildOf(camera.transform) || c.GetComponentInParent<UltimateTransient>() != null) continue;
                // Adjustment hinges remain physical triggers even when their
                // wrench visuals are hidden. They must not block wrench pickup.
                if (!Screwdriver && c.gameObject.layer == 13)
                {
                    var suspension = c.GetComponentInParent<SuspensionPickTarget>();
                    if (held != null || suspension?.Adjustment?.FittedKit == null) continue;
                }
                if (c.isTrigger && c.gameObject.layer != 13 && c.gameObject.layer != 7 && wheel == null) continue;
                return hit;
            }
            return default(RaycastHit);
        }

        private static void Pick(Camera camera)
        {
            Hit = ExactHit(camera, 2f, Held); Target = null; TargetHinge = TargetSlot = null;
            if (Held != null && !Screwdriver) Hit = AttachmentHit(camera, Plugin.AttachmentReach);
            if (Screwdriver && Held == null) Hit = AdjustmentHit(camera, 2f);
            // Native Drive's collider can be on a separate physics object and
            // overlap the entire seat socket. Resolve the socket by ancestry,
            // including the native Drive target when its prompt owns the aim.
            if (HeldId == "seat" && !Screwdriver && DriveOffered)
            {
                var driveTarget = Drive?.FsmVariables.GetFsmGameObject("MousePickGO")?.Value;
                TargetSlot = DriverSeatMount(driveTarget == null ? null : driveTarget.transform);
                if (TargetSlot != null) return;
            }
            if (Hit.collider == null) return;
            var marker = Hit.collider.GetComponentInParent<UltimateMountMarker>();
            if (marker != null) { TargetSlot = marker.Slot; return; }
            var transform = Hit.collider.transform;
            // These hinges own both FSMs: resolve attachment before adjustment.
            if (Held != null)
            {
                TargetSlot = CompatibleSlotAt(transform);
                if (TargetSlot != null) return;
            }
            // Drive and the driver-seat socket overlap. Holding a seat turns the
            // exact Drive target into the empty driver-seat mount on that vehicle.
            if (HeldId == "seat" && transform.gameObject.layer == 7)
            {
                TargetSlot = DriverSeatMount(transform);
                if (TargetSlot != null) return;
            }
            var suspension = transform.GetComponentInParent<SuspensionPickTarget>();
            if (suspension != null)
            {
                if (suspension.Adjustment != null && suspension.Adjustment.KitFitted)
                    Target = Screwdriver ? suspension.gameObject : suspension.Adjustment.FittedKit;
                return;
            }
            var wheel = MountedWheel(transform);
            if (wheel != null) { Target = wheel; return; }
            var directlyAimedHinge = AdjustmentRunner.Find(transform.gameObject, "AdjustHinge");
            if (Screwdriver && directlyAimedHinge != null) { Target = transform.gameObject; TargetHinge = directlyAimedHinge; return; }
            // Stop at the first ID root; never walk past an unrelated body part into the vehicle.
            Target = HeadlightMount.ItemRoot(transform.gameObject);
            var root = Target == null ? transform : Target.transform;
            for (var t = root; t != null; t = t.parent)
            {
                if (TargetHinge == null) TargetHinge = AdjustmentRunner.Find(t.gameObject, "AdjustHinge");
                var candidate = AdjustmentRunner.Find(t.gameObject, "vehPart_Attach");
                if (Held != null && Compatible(candidate, Held)) TargetSlot = candidate;
                if (TargetHinge != null || candidate != null || t.GetComponent<Rigidbody>() != null) break;
            }
        }

        private static GameObject MountedWheel(Transform hit)
        {
            var controller = hit.GetComponentInParent<NWH.WheelController3D.WheelController>();
            if (controller == null) return null;
            foreach (Transform child in controller.transform)
                if (child.CompareTag("vehPart") && Id(child.gameObject) == "wheel") return child.gameObject;
            return null;
        }

        private static PlayMakerFSM CompatibleSlotAt(Transform hit)
        {
            for (var t = hit; t != null; t = t.parent)
            {
                var slot = AdjustmentRunner.Find(t.gameObject, "vehPart_Attach");
                if (slot != null) return Compatible(slot, Held) ? slot : null;
                if (t.GetComponent<Rigidbody>() != null) break;
            }
            return null;
        }

        private static PlayMakerFSM DriverSeatMount(Transform driveTarget)
        {
            for (var t = driveTarget; t != null; t = t.parent)
            {
                var seat = t.name == "hinge_seat_driver" ? t : t.Find("hinge_seat_driver");
                var slot = seat == null ? null : AdjustmentRunner.Find(seat.gameObject, "vehPart_Attach");
                if (Compatible(slot, Held) && AdjustmentRunner.PlayerCamera != null
                    && Vector3.Distance(seat.position, AdjustmentRunner.PlayerCamera.transform.position) <= Plugin.AttachmentReach)
                    return slot;
            }
            return null;
        }

        private static RaycastHit AttachmentHit(Camera camera, float reach)
        {
            var surface = default(RaycastHit);
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            int mask = (1 << 0) | (1 << 7) | (1 << 8) | (1 << 9) | (1 << 13) | (1 << 16) | (1 << 14);
            var hits = Physics.RaycastAll(ray, reach, mask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var c = hit.collider;
                if (c == null || c.transform.IsChildOf(camera.transform) || c.transform.IsChildOf(Held.transform)) continue;
                var marker = c.GetComponentInParent<UltimateMountMarker>();
                if (marker != null)
                {
                    if (Compatible(marker.Slot, Held)) return hit;
                    continue;
                }
                if (c.GetComponentInParent<UltimateTransient>() != null) continue;
                if (CompatibleSlotAt(c.transform) != null) return hit;
                if (HeldId == "seat" && c.gameObject.layer == 7 && DriverSeatMount(c.transform) != null) return hit;
                if (c.isTrigger) continue;
                var body = c.attachedRigidbody;
                if (body != null && body.GetComponents<PlayMakerFSM>().Any(f => f.FsmName == "RpmGear" || f.FsmName == "CrashDamage" || f.FsmName == "TrailerAttached"))
                {
                    if (surface.collider == null) surface = hit;
                    continue;
                }
                // Scenery and loose objects still block the exact cursor ray.
                return surface.collider != null ? surface : hit;
            }
            // Keep normal targets for free attachment and the seat/Drive overlap.
            return surface.collider != null ? surface : ExactHit(camera, reach, Held);
        }

        private static Transform VehicleOf(Transform part)
        {
            var body = part.GetComponentInParent<Rigidbody>();
            return body != null ? body.transform : part.root;
        }

        // Adjustment deliberately looks through vehicle bodywork, but stays on
        // the center ray and stops at scenery or another loose foreground object.
        private static RaycastHit AdjustmentHit(Camera camera, float reach)
        {
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            var hits = Physics.RaycastAll(ray, reach, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var c = hit.collider;
                if (c == null || c.transform.IsChildOf(camera.transform) || c.GetComponentInParent<UltimateTransient>() != null) continue;
                var suspension = c.GetComponentInParent<SuspensionPickTarget>();
                if (suspension != null)
                {
                    if (suspension.Adjustment != null && suspension.Adjustment.KitFitted) return hit;
                    continue;
                }
                var item = HeadlightMount.ItemRoot(c.gameObject);
                if (item != null && item.CompareTag("vehPart") && AdjustmentRunner.Find(item, "de_Attach") != null) return hit;
                for (var t = c.transform; t != null; t = t.parent)
                {
                    var hinge = AdjustmentRunner.Find(t.gameObject, "AdjustHinge");
                    if (hinge != null && hinge.isActiveAndEnabled) return hit;
                    if (t.GetComponent<Rigidbody>() != null) break;
                }
                if (c.isTrigger) continue;
                var body = c.attachedRigidbody;
                if (body != null && body.GetComponents<PlayMakerFSM>().Any(f => f.FsmName == "RpmGear" || f.FsmName == "CrashDamage" || f.FsmName == "TrailerAttached")) continue;
                return hit;
            }
            return default(RaycastHit);
        }

        private static void BeginAdjust()
        {
            if (Held != null) { AdjustmentRunner.Session = new AdjustmentSession(Held.transform); return; }
            if (Target == null) return;
            var suspension = Target.GetComponent<SuspensionPickTarget>();
            if (suspension != null && suspension.Adjustment != null && suspension.Adjustment.KitFitted && Adjust != null)
            {
                Adjust.FsmVariables.GetFsmGameObject("Hinge").Value = Target;
                Adjust.Fsm.SetState("adjust"); return;
            }
            if (TargetHinge != null && TargetHinge.isActiveAndEnabled && Adjust != null)
            {
                Adjust.FsmVariables.GetFsmGameObject("Hinge").Value = TargetHinge.gameObject;
                Adjust.Fsm.SetState("adjust"); return;
            }
            if (Target.CompareTag("vehPart") && AdjustmentRunner.Find(Target, "de_Attach") != null)
                AdjustmentRunner.Session = new AdjustmentSession(Target.transform);
        }

        internal static void Finish()
        {
            UltimateAdjustmentHighlight.Clear();
            AdjustmentRunner.Stop();
            if (NativeEditing && Adjust != null) Adjust.Fsm.SetState("finish");
            ToolRunner.Active = null;
        }

        internal static void DetachAndGrab(GameObject item)
        {
            if (item == null || Grab == null || Held != null) return;
            var detach = AdjustmentRunner.Find(item, "de_Attach");
            var check = AdjustmentRunner.Find(item, "CheckTag");
            if (detach == null || check == null || check.Fsm.GetState("vehPartRemoved") == null) return;
            var t = item.transform; var position = t.position; var rotation = t.rotation;
            // Mounted wheel items are invisible; the hub carries their displayed mesh.
            var hub = Id(item) == "wheel" && t.parent != null ? t.parent.Find("wheel_hub") : null;
            if (hub != null) { position = hub.position; rotation = hub.rotation; }
            detach.SendEvent("de_Attach");
            detach.Fsm.UpdateStateChanges();
            // CheckTag normally does this on its next Update. Run that transition now,
            // before any physics step or native Grab action can observe the old mount.
            check.Fsm.SetState("vehPartRemoved");
            t.SetPositionAndRotation(position, rotation);
            Grab.FsmVariables.GetFsmGameObject("Item").Value = item;
            Grab.Fsm.SetState("Grab");
            Grab.Fsm.UpdateStateChanges();
            t.SetPositionAndRotation(position, rotation);
            CaptureHandPose(item, Grab);
            var body = item.GetComponent<Rigidbody>();
            if (body != null) { body.useGravity = false; body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            Held = item;
            Plugin.Log.LogInfo("Detached and grabbed " + item.name + " without changing its world pose.");
        }

        internal static void CaptureHandPose(GameObject item, PlayMakerFSM grab)
        {
            if (item == null || grab == null) return;
            var p = grab.FsmVariables.GetFsmVector3("itemPos"); if (p != null) p.Value = item.transform.localPosition;
            var r = grab.FsmVariables.GetFsmVector3("itemRot"); if (r != null) r.Value = item.transform.localEulerAngles;
        }

        internal static bool Compatible(PlayMakerFSM slot, GameObject item)
        {
            if (item == null || !SuitableSlot(slot)) return false;
            string id = Id(item); if (string.IsNullOrEmpty(id)) return false;
            return slot.Fsm.GetState("checkID").Actions.OfType<StringCompare>()
                .Any(a => a.Enabled && a.compareTo != null && a.compareTo.Value == id);
        }

        internal static bool SuitableSlot(PlayMakerFSM slot)
        {
            if (slot == null || slot.FsmName != "vehPart_Attach" || !slot.isActiveAndEnabled
                || slot.Fsm.GetState("Attach") == null) return false;
            // Dashboard recipes accept multiple gauges; fixed sockets have a partAttached state.
            if (slot.Fsm.GetState("partAttached") != null)
                foreach (Transform child in slot.transform) if (child.CompareTag("vehPart")) return false;
            var state = slot.Fsm.GetState("checkID"); if (state == null) return false;
            return state.Actions.OfType<StringCompare>().Any(a => a.Enabled && a.compareTo != null && !string.IsNullOrEmpty(a.compareTo.Value));
        }

        internal static bool FreeSurface(out Transform parent, out Transform vehicle)
        {
            parent = vehicle = null;
            if (Held == null) return false;
            bool free = AdjustmentRunner.Find(Held, "Attach") != null || (Plugin.AttachAnywhere.Value && HeadlightMount.IsHeadlight(Held));
            if (!free) return false;
            // Native attachables offer mounting when any edge touches layer 8,
            // independently of the camera ray. Keep that contact recipe alive.
            var attach = AdjustmentRunner.Find(Held, "Attach");
            var contact = attach != null && attach.isActiveAndEnabled && attach.ActiveStateName == "Over"
                ? attach.FsmVariables.GetFsmGameObject("CarPart")?.Value : null;
            var surface = contact != null ? contact.transform : Hit.collider == null ? null : Hit.collider.transform;
            if (surface == null) return false;
            var rigid = surface.GetComponentInParent<Rigidbody>();
            if (rigid == null) return false;
            if (!rigid.GetComponents<PlayMakerFSM>().Any(f=>f.FsmName == "RpmGear" || f.FsmName == "CrashDamage" || f.FsmName == "TrailerAttached")) return false;
            vehicle = rigid.transform; parent = surface;
            for (var t=parent; t!=null && t!=vehicle; t=t.parent)
                if (t.name.StartsWith("hinge_wheel", StringComparison.Ordinal) || t.GetComponent<NWH.WheelController3D.WheelController>() != null) return false;
            while (parent != null && parent != vehicle && (parent.name.StartsWith("hinge", StringComparison.Ordinal)
                || AdjustmentRunner.Find(parent.gameObject, "vehPart_Attach") != null || HeadlightMount.IsHeadlight(parent.gameObject))) parent = parent.parent;
            return parent != null;
        }

        private static void AttachHeld()
        {
            if (Held == null) return;
            if (Compatible(TargetSlot, Held))
            {
                var item = Held;
                TargetSlot.FsmVariables.GetFsmGameObject("Item").Value = item;
                TargetSlot.Fsm.SetState("Attach");
                if (HeadlightMount.IsHeadlight(item))
                    AdjustmentRunner.Find(item, "Headlight")?.SendEvent(HeadlightMount.LightsOn(VehicleOf(TargetSlot.transform)) ? "HeadlightsON" : "HeadlightsOFF");
                Held = null; return;
            }
            if (!FreeSurface(out var parent, out var vehicle)) return;
            var attach = AdjustmentRunner.Find(Held, "Attach");
            if (attach != null && attach.Fsm.GetState("Attach") != null)
            {
                var target = attach.FsmVariables.GetFsmGameObject("CarPart");
                if (target == null) return;
                target.Value = parent.gameObject; attach.Fsm.SetState("Attach");
            }
            else HeadlightMount.Attach(Grab, Adjust, Held, parent, Hit.point, Hit.normal, vehicle);
            Held = null;
        }

        private static void RefreshSlots(Camera camera)
        {
            if (Held == null || AdjustmentRunner.Find(Held, "de_Attach") == null || string.IsNullOrEmpty(HeldId)
                || !ToolSettingsMath.ShowSlots(Plugin.Enabled.Value, Plugin.VisibleGizmosWithoutTool.Value, Active, Driving, AdjustmentRunner.Gameplay))
            { HideMarkers(); return; }
            if (Time.unscaledTime >= nextSlots)
            {
                nextSlots = Time.unscaledTime + 1f;
                slots.Clear();
                slots.AddRange(UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>().Where(f=>f.FsmName == "vehPart_Attach"));
                // Discover and filter at most once per second; only cached nearby icons update per frame.
                nearbyVehicles.Clear();
                foreach (var slot in slots)
                    if (slot != null && (slot.transform.position-camera.transform.position).sqrMagnitude <= Plugin.AttachmentReach * Plugin.AttachmentReach)
                        nearbyVehicles.Add(VehicleOf(slot.transform));
                foreach (var slot in slots)
                    if (slot != null && nearbyVehicles.Contains(VehicleOf(slot.transform)) && Compatible(slot, Held)
                        && !Markers.Any(m=>m != null && m.Slot == slot)) UltimateMountMarker.Create(slot);
                nearbyMarkers.Clear();
                foreach (var marker in Markers)
                    if (marker != null)
                    {
                        bool nearby = marker.Slot != null && nearbyVehicles.Contains(VehicleOf(marker.Slot.transform));
                        if (nearby) nearbyMarkers.Add(marker); else marker.Refresh(camera, false, Held);
                    }
            }
            foreach (var marker in nearbyMarkers)
                if (marker != null) marker.Refresh(camera, Compatible(marker.Slot, Held), Held);
            markersShown = true;
        }

        internal static void HideMarkers()
        {
            if (!markersShown) return;
            foreach (var m in Markers) if (m != null) m.gameObject.SetActive(false);
            markersShown = false;
        }
        internal static void PutAway()
        {
            Finish();
            SetAdjustmentMarkers(false);
            if (BoxOut(toolbox)) toolbox.Fsm.SetState("offToolbox");
            if (BoxOut(adjustBox)) adjustBox.Fsm.SetState("offToolbox");
            if (forcedAdjust && Adjust != null) Adjust.enabled = false;
            Active = false; forcedAdjust = false; HideMarkers(); ToolVisual.Update(false, Screwdriver, toolbox, adjustBox);
        }
        internal static void Clear()
        {
            if (Active) SetAdjustmentMarkers(false);
            Finish(); Active = false; Held = Target = null; TargetHinge = TargetSlot = null;
            if (forcedAdjust && Adjust != null) Adjust.enabled = false;
            foreach(var m in Markers) if(m!=null) UnityEngine.Object.Destroy(m.gameObject);
            Markers.Clear(); slots.Clear(); owner = null; Adjust = Grab = Drive = toolbox = adjustBox = utility = null;
            nearbyMarkers.Clear(); nearbyVehicles.Clear(); markersShown = false;
            forcedAdjust = false; Driving = DriveOffered = false; ToolVisual.Clear();
        }
        private static void SetAdjustmentMarkers(bool show)
        {
            var flag=FsmVariables.GlobalVariables.GetFsmBool("AdjustToolBool");
            if(flag!=null)flag.Value=show;
        }
        internal static string Label(GameObject go)
        {
            if (go == null) return "";
            string n=go.name; int clone=n.IndexOf("(Clone)",StringComparison.Ordinal); if(clone>=0)n=n.Substring(0,clone);
            return n.Replace('_',' ').Replace("hinge ","");
        }
    }
}
