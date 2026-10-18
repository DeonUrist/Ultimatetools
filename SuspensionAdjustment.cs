using System;
using System.Collections.Generic;
using System.Linq;
using ES3Internal;
using HutongGames.PlayMaker;
using NWH.WheelController3D;
using UnityEngine;

namespace PartAdjustment
{
    [ES3NonSerializable]
    public sealed class SuspensionPickTarget : MonoBehaviour
    {
        internal SuspensionAdjustment Adjustment;
        internal Collider PickCollider;
    }

    [ES3NonSerializable]
    public sealed class SuspensionAdjustment : MonoBehaviour
    {
        internal static readonly List<SuspensionAdjustment> All = new List<SuspensionAdjustment>();
        internal static int Revision;
        internal float Width = 1f;
        internal float RideHeight, Balance;
        internal Transform Hinge;
        private Transform model, factoryModel;
        private Vector3 modelPosition, modelScale, hingePosition;
        private readonly List<Mount> mounts = new List<Mount>();
        private readonly List<SuspensionPickTarget> picks = new List<SuspensionPickTarget>();
        private SuspensionBraces braces;
        private SuspensionIndicators indicators;
        private SuspensionWidthGeometry widthGeometry;
        private SuspensionStanceGeometry stanceGeometry;
        private PlayMakerFSM attachment, suspensionCheck;
        private Rigidbody body;
        private bool toolRequested, pickable;
        private bool ready;
        private SuspensionStructure structure;
        private bool lastKit;
        private bool buggyFamily;
        private readonly SuspensionOwnership ownership = new SuspensionOwnership();

        private sealed class Mount
        {
            internal Transform Transform;
            internal Vector3 Baseline;
            internal float Center;
            internal PlayMakerFSM Suspension;
            internal bool Lifted;
            internal bool Front;
            internal WheelController Controller;
            internal Damper Damper;
            internal readonly SuspensionDamping Damping = new SuspensionDamping();
        }

        internal static SuspensionAdjustment Ensure(Transform car)
        {
            if (car == null || !car.gameObject.scene.IsValid()) return null;
            var existing = car.GetComponent<SuspensionAdjustment>();
            if (existing != null) return existing.Supported() ? existing : null;
            Transform model, hinge;
            WheelController[] wheels;
            if (!SuspensionSupport.TryAssembly(car, out model, out hinge, out wheels)) return null;
            var adjustment = car.gameObject.AddComponent<SuspensionAdjustment>();
            adjustment.Initialize(model, hinge, wheels);
            return adjustment;
        }

        private void Initialize(Transform suspensionModel, Transform hinge, WheelController[] wheels)
        {
            model = suspensionModel;
            Hinge = hinge;
            buggyFamily = model.Find("buggy") != null;
            modelPosition = model.localPosition;
            modelScale = model.localScale;
            hingePosition = hinge.localPosition;
            attachment = SuspensionSupport.EnabledFsm(hinge, "vehPart_Attach");
            suspensionCheck = SuspensionSupport.EnabledFsm(hinge, "checkSuspension");
            body = GetComponent<Rigidbody>();
            foreach (var wheel in wheels)
            {
                var suspension = SuspensionSupport.EnabledFsm(wheel.transform, "Suspension");
                mounts.Add(new Mount { Transform = wheel.transform,
                    Baseline = KitFitted ? VanillaPosition(wheel.transform) : wheel.transform.localPosition,
                    Controller = wheel, Damper = wheel.damper, Front = wheel.name.Contains("_F"),
                    Suspension = suspension, Lifted = suspension?.ActiveStateName == "lifted" });
            }
            RestoreFactoryAssembly(KitFitted);
            structure = new SuspensionStructure(model, factoryModel);
            stanceGeometry = new SuspensionStanceGeometry(transform, model, factoryModel);
            RefreshCenters();
            widthGeometry = new SuspensionWidthGeometry(model);
            // Separate trigger targets preserve the layer/tag and attachment child counts of all vanilla hinges.
            // Never parent a selection target under hinge_suspension: GameObjectHasChildren detects the lift kit there.
            foreach (var mesh in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!AxleEndpoints.IsAxle(mesh) || mesh.GetComponent<MeshRenderer>() == null) continue;
                var go = new GameObject("PartAdjustment.SuspensionTarget") { layer = 13, tag = "Hinge" };
                go.transform.SetParent(mesh.transform, false);
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = mesh.sharedMesh.bounds.center;
                var size = mesh.sharedMesh.bounds.size;
                box.size = new Vector3(Mathf.Max(size.x, 0.06f), Mathf.Max(size.y, 0.06f), Mathf.Max(size.z, 0.06f));
                box.enabled = false;
                var target = go.AddComponent<SuspensionPickTarget>();
                target.Adjustment = this;
                target.PickCollider = box;
                picks.Add(target);
            }
            braces = new SuspensionBraces(model);
            indicators = new SuspensionIndicators(this, model);
            ready = true;
            All.Add(this);
            Revision++;
            Plugin.Log.LogDebug("Suspension adjustment ready: " + name + ", " + mounts.Count + " physical mounts.");
            lastKit = KitFitted;
            Apply();
        }

        private void RestoreFactoryAssembly(bool restoreGeometry)
        {
            // Prefer the registered prefab: loaded transforms may already contain 2.1.0 edits.
            var instance = GetComponent<ES3Prefab>();
            var manager = ES3ReferenceMgrBase.Current;
            var prefab = instance == null || manager == null ? null : manager.GetPrefab(instance.prefabId, true);
            var factory = prefab == null ? null : prefab.transform;
            if (factory == null)
                factory = Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => !t.gameObject.scene.IsValid()
                    && t.name == name.Replace("(Clone)", "").Trim() && t.Find("suspension_model") != null);
            if (factory == null || factory == transform) return;
            factoryModel = factory.Find("suspension_model");
            var factoryHinge = factory.Find("hinge_suspension_parent/hinge_suspension");
            if (factoryModel != null)
            {
                modelPosition = factoryModel.localPosition; modelScale = factoryModel.localScale;
                // Older height edits could persist a rear mesh's pose and hidden springs.
                if (restoreGeometry) foreach (var rear in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    var n = rear.name.ToLowerInvariant();
                    if (!n.Contains("suspension") || !n.Contains("rear")) continue;
                    var path = rear.name;
                    for (var p = rear.transform.parent; p != null && p != model; p = p.parent) path = p.name + "/" + path;
                    var original = factoryModel.Find(path);
                    if (original == null) continue;
                    rear.transform.localPosition = original.localPosition;
                    rear.transform.localRotation = original.localRotation;
                    rear.transform.localScale = original.localScale;
                    var mesh = original.GetComponent<MeshFilter>();
                    if (mesh != null) rear.sharedMesh = mesh.sharedMesh;
                    foreach (var spring in rear.GetComponentsInChildren<Transform>(true))
                        if (spring != rear.transform && spring.name.ToLowerInvariant().Contains("spring"))
                        {
                            var source = original.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == spring.name);
                            if (source != null) spring.gameObject.SetActive(source.gameObject.activeSelf);
                        }
                }
            }
            if (factoryHinge != null) hingePosition = factoryHinge.localPosition;
        }

        private static Vector3 VanillaPosition(Transform wheel, string stateName = null)
        {
            var fsm = SuspensionSupport.EnabledFsm(wheel, "Suspension");
            // These constants are authoritative even if ES3 has already restored an adjusted transform.
            var state = fsm?.FsmStates.FirstOrDefault(s => s.Name == (stateName ?? fsm.ActiveStateName))
                ?? fsm?.FsmStates.FirstOrDefault(s => s.Name == "stock");
            var action = SuspensionSupport.MountAction(fsm == null ? null : SuspensionSupport.Actions(fsm.Fsm, state), fsm != null,
                a => fsm.Fsm.GetOwnerDefaultTarget(a.gameObject) == wheel.gameObject);
            return SuspensionSupport.Position(action, wheel.localPosition);
        }

        internal void SetPickable(bool toolOn)
        {
            toolRequested = toolOn;
            SetColliderVisibility(toolOn && Supported() && KitFitted);
            if (!pickable) indicators?.Hide();
        }

        private void SetColliderVisibility(bool value)
        {
            if (pickable == value) return;
            pickable = value;
            foreach (var target in picks)
                if (target != null && target.PickCollider != null) target.PickCollider.enabled = pickable;
        }

        internal void UpdateIndicators(Camera camera)
        {
            SetColliderVisibility(toolRequested && Supported() && KitFitted);
            if (pickable && UltimateTool.Screwdriver) indicators?.Update(camera);
            else indicators?.Hide();
        }

        internal GameObject FittedKit
        {
            get
            {
                if (Hinge == null) return null;
                foreach (Transform child in Hinge)
                    if (child.CompareTag("vehPart") && UltimateTool.Id(child.gameObject) == "suspension"
                        && AdjustmentRunner.Find(child.gameObject, "de_Attach") != null) return child.gameObject;
                return null;
            }
        }

        // 1.2.0: the suspension can only be adjusted while a lift kit sits on hinge_suspension (the game's own checkSuspension looks at
        // the same thing: GameObjectHasChildren). Without one the width is standard, and stays standard.
        internal bool KitFitted
        {
            get
            {
                if (Hinge == null) return false;
                for (int i = 0; i < Hinge.childCount; i++) if (Hinge.GetChild(i).CompareTag("vehPart")) return true;
                return false;
            }
        }

        // 1.2.0: another mod (Apocapatrol's car templates) sets the values; false when the car has no kit or is not supported
        internal bool Set(float width, float height)
        {
            if (!Supported() || !KitFitted) return false;
            Width = AdjustmentMath.Width(width);
            Apply();
            return true;
        }

        internal void Change(float widthDelta, float heightDelta, float balanceDelta, bool reset)
        {
            if (!Supported() || !KitFitted) return;
            float width = reset ? 1f : AdjustmentMath.Width(Width + widthDelta);
            float height = reset ? 0f : RideHeight, balance = reset ? 0f : Balance;
            if (!reset) SuspensionStanceMath.Change(ref height, ref balance, heightDelta, balanceDelta);
            if (!reset && width == Width && height == RideHeight && balance == Balance) return;
            Width = width;
            RideHeight = height; Balance = balance;
            Apply();
        }

        internal void Apply()
        {
            if (!Supported()) { UpdateDamping(false); return; }
            var update = ownership.BeginApply(KitFitted, Width != 1f || RideHeight != 0f || Balance != 0f);
            if (update == SuspensionUpdate.None) { UpdateDamping(false); return; }
            if (update == SuspensionUpdate.RestoreStock)
            {
                Width = 1f;
                RideHeight = Balance = 0f;
                RestoreFactoryAssembly(true);
                foreach (var mount in mounts)
                {
                    mount.Baseline = VanillaPosition(mount.Transform, "stock");
                    mount.Lifted = false;
                    mount.Transform.localPosition = mount.Baseline;
                }
                RefreshCenters();
                Plugin.Log.LogDebug("Suspension of " + name + " back to standard: no lift kit fitted.");
            }
            model.localScale = new Vector3(modelScale.x * Width, modelScale.y, modelScale.z);
            float visualDrop = KitFitted ? SuspensionAlignmentDefaults.ModelDrop(buggyFamily) : 0f;
            // Buggy assemblies retain their existing alignment. Lifted car bases stay at
            // their vanilla body pose; only their axles receive height/alignment offsets.
            model.localPosition = buggyFamily ? modelPosition - Vector3.up * (RideHeight + visualDrop) : modelPosition;
            structure.Apply(modelPosition, modelScale);
            stanceGeometry.Apply(buggyFamily ? 0f : RideHeight + visualDrop, Balance, !buggyFamily);
            // Shields nested under a moving axle also belong to the fixed body.
            structure.Apply(modelPosition, modelScale);
            braces.ShowForWidth(Width);
            // Keep the native kit trigger with the assembly without stretching it.
            if (Hinge != null) Hinge.localPosition = hingePosition - Hinge.parent.InverseTransformVector(transform.TransformVector(Vector3.up * RideHeight));
            foreach (var mount in mounts) ApplyMount(mount);
            UpdateDamping(true);
            if (body != null) body.WakeUp();
        }

        private void FixedUpdate()
        {
            if (!ready) return;
            if (lastKit != KitFitted) { lastKit = KitFitted; Apply(); }
            else UpdateDamping(Supported());
        }

        private void UpdateDamping(bool supported)
        {
            bool active = supported && isActiveAndEnabled && Plugin.Enabled.Value && KitFitted;
            foreach (var mount in mounts)
            {
                var wheel = mount.Controller;
                if (wheel == null) continue;
                if (!ReferenceEquals(mount.Damper, wheel.damper))
                {
                    // A replaced damper is an external edit until a native state completes again.
                    mount.Damper = wheel.damper;
                    mount.Damping.ExternalChanged();
                }
                if (wheel.damper == null) continue;
                float bump, rebound;
                if (!mount.Damping.Update(wheel.DamperBumpRate, wheel.DamperReboundRate,
                    SuspensionDamping.Fraction(RideHeight, Balance, mount.Front), active && mount.Lifted,
                    out bump, out rebound)) continue;
                wheel.DamperBumpRate = bump;
                wheel.DamperReboundRate = rebound;
            }
        }

        internal void WriteSaveVariables(FsmVariables variables)
        {
            SaveVariables.Float(variables, SaveVariables.Width, 1f).Value = Width;
            SaveVariables.Float(variables, SaveVariables.RideHeight, 0f).Value = RideHeight;
            SaveVariables.Float(variables, SaveVariables.Balance, 0f).Value = Balance;
            SaveVariables.RemoveHeight(variables);
            SaveVariables.Vector(variables, SaveVariables.ModelPosition, modelPosition).Value = modelPosition;
            SaveVariables.Vector(variables, SaveVariables.ModelScale, modelScale).Value = modelScale;
            SaveVariables.Vector(variables, SaveVariables.HingePosition, hingePosition).Value = hingePosition;
            structure.Write(variables);
            stanceGeometry.Write(variables);
        }

        internal void PrepareLoad(FsmVariables variables)
        {
            // Reset defaults before every load: old saves omit these fields and must not inherit another slot's values.
            SaveVariables.Float(variables, SaveVariables.Width, 1f).Value = 1f;
            SaveVariables.Float(variables, SaveVariables.RideHeight, 0f).Value = 0f;
            SaveVariables.Float(variables, SaveVariables.Balance, 0f).Value = 0f;
            SaveVariables.Float(variables, SaveVariables.Height, 0f).Value = 0f;
            SaveVariables.Vector(variables, SaveVariables.ModelPosition, modelPosition).Value = modelPosition;
            SaveVariables.Vector(variables, SaveVariables.ModelScale, modelScale).Value = modelScale;
            SaveVariables.Vector(variables, SaveVariables.HingePosition, hingePosition).Value = hingePosition;
            structure.Write(variables);
            stanceGeometry.Write(variables);
        }

        internal void ReadSaveVariables(FsmVariables variables)
        {
            Width = AdjustmentMath.Width(SaveVariables.Float(variables, SaveVariables.Width, 1f).Value);
            RideHeight = SaveVariables.Float(variables, SaveVariables.RideHeight, 0f).Value;
            Balance = SaveVariables.Float(variables, SaveVariables.Balance, 0f).Value;
            SuspensionStanceMath.Clamp(ref RideHeight, ref Balance);
            modelPosition = SaveVariables.Vector(variables, SaveVariables.ModelPosition, modelPosition).Value;
            modelScale = SaveVariables.Vector(variables, SaveVariables.ModelScale, modelScale).Value;
            hingePosition = SaveVariables.Vector(variables, SaveVariables.HingePosition, hingePosition).Value;
            structure.Read(variables);
            stanceGeometry.Read(variables);
            RestoreFactoryAssembly(KitFitted);
            SaveVariables.RemoveHeight(variables);
            foreach (var mount in mounts) if (mount.Transform != null)
            {
                mount.Baseline = KitFitted ? VanillaPosition(mount.Transform) : mount.Transform.localPosition;
                mount.Lifted = mount.Suspension?.ActiveStateName == "lifted";
            }
            RefreshCenters();
            Apply();
        }

        private void ApplyMount(Mount mount)
        {
            // Native stock transitions may happen wheel by wheel. Never overwrite a stock wheel
            // while another wheel's callback is applying the fitted kit's stance.
            if (mount.Transform == null || !KitFitted || !mount.Lifted) return;
            var position = mount.Baseline;
            // Preserve native clearance, Y and Z; widening adds only the axle's lateral growth.
            if (Width != 1f)
            {
                if (widthGeometry.TryOffset(mount.Transform, mount.Baseline, mount.Lifted, modelScale, Width, out var offset))
                    position.x += offset.x;
                else position.x = AdjustmentMath.SpacedX(position.x, mount.Center, Width);
            }
            position.x += SuspensionAlignmentDefaults.Lateral(buggyFamily, mount.Transform.name.EndsWith("L", StringComparison.Ordinal));
            position.y = SuspensionStanceMath.MountY(mount.Baseline.y, RideHeight, Balance, mount.Front);
            if (!mount.Transform.localPosition.Equals(position)) mount.Transform.localPosition = position;
            // NWH WheelController.Step reads this mount to cast the suspension ray, apply forces, and place
            // wheel.visualTransform and wheel.colliderTransform. The wheel's own scale/radius stays unchanged.
        }

        internal void VanillaMountChanged(Transform wheel, string state)
        {
            if (!Supported()) return;
            Mount mount = null;
            foreach (var candidate in mounts) if (candidate.Transform == wheel) { mount = candidate; break; }
            if (mount == null) return;
            mount.Baseline = wheel.localPosition;
            mount.Lifted = state == "lifted";
            if (mount.Controller != null && mount.Controller.damper != null)
            {
                mount.Damper = mount.Controller.damper;
                mount.Damping.NativeChanged(mount.Controller.DamperBumpRate, mount.Controller.DamperReboundRate);
            }
            RefreshCenters();
            Apply();
        }

        private bool Supported()
        {
            if (!ready || !SuspensionSupport.BranchActive(model, transform) || !SuspensionSupport.BranchActive(Hinge, transform)
                || attachment == null || !attachment.enabled || suspensionCheck == null || !suspensionCheck.enabled) return false;
            foreach (var mount in mounts) if (mount.Transform == null || mount.Suspension == null || !mount.Suspension.enabled) return false;
            return true;
        }

        private void RefreshCenters()
        {
            // Match opposite mounts at the same longitudinal position, keeping each axle centered.
            foreach (var mount in mounts)
            {
                Mount opposite = null;
                float nearest = float.PositiveInfinity;
                foreach (var candidate in mounts)
                {
                    if (candidate == mount || Mathf.Sign(candidate.Baseline.x) == Mathf.Sign(mount.Baseline.x)) continue;
                    float distance = Mathf.Abs(candidate.Baseline.z - mount.Baseline.z);
                    if (distance < nearest) { opposite = candidate; nearest = distance; }
                }
                mount.Center = opposite == null ? 0f : (mount.Baseline.x + opposite.Baseline.x) * 0.5f;
            }
        }

        private void OnDisable() => UpdateDamping(false);

        private void OnDestroy()
        {
            UpdateDamping(false);
            indicators?.Destroy();
            All.Remove(this);
            Revision++;
            if (ToolRunner.Active == this) ToolRunner.Active = null;
        }
    }
}
