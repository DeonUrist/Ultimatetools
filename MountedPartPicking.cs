using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace PartAdjustment
{
    [ES3NonSerializable]
    public sealed class MountedPartPickTarget : MonoBehaviour
    {
        internal GameObject Item;
    }

    // Some native CheckTag recipes disable the pickup shape while attached
    // (gear levers and cassettes). Copy that shape for tool selection only;
    // never enable the native collider or modify its physics recipe.
    internal static class MountedPartPicking
    {
        private sealed class Part
        {
            internal GameObject Item;
            internal readonly List<GameObject> Shapes = new List<GameObject>();
        }
        private static readonly List<Part> parts = new List<Part>();
        private static float nextScan;

        internal static void Update(Camera camera, bool selected)
        {
            if (selected && camera != null && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 1f;
                parts.RemoveAll(p => p.Item == null);
                var owners = new HashSet<Transform>();
                foreach (var collider in Physics.OverlapSphere(camera.transform.position, 6f,
                    (1 << 0) | (1 << 8) | (1 << 14) | (1 << 16), QueryTriggerInteraction.Ignore))
                    if (collider.attachedRigidbody != null) owners.Add(collider.attachedRigidbody.transform);
                foreach (var owner in owners)
                foreach (var fsm in owner.GetComponentsInChildren<PlayMakerFSM>(false))
                {
                    if (fsm.FsmName != "de_Attach" || !UltimateTool.CanDetach(fsm.gameObject)
                        || UltimateTool.Id(fsm.gameObject) == "wheel"
                        || (fsm.transform.position - camera.transform.position).sqrMagnitude > 36f
                        || parts.Any(p => p.Item == fsm.gameObject) || HasNativeCollider(fsm.gameObject)) continue;
                    var part = new Part { Item = fsm.gameObject };
                    foreach (var source in fsm.GetComponentsInChildren<Collider>(true))
                    {
                        if (source.enabled || !source.gameObject.activeInHierarchy || source.GetComponent<UltimateTransient>() != null) continue;
                        var go = new GameObject("UltimateTools.MountedPartPick") { layer = 8 };
                        go.transform.SetParent(source.transform, false);
                        go.AddComponent<UltimateTransient>();
                        go.AddComponent<MountedPartPickTarget>().Item = part.Item;
                        Collider copy = null;
                        if (source is BoxCollider box)
                        {
                            var shape = go.AddComponent<BoxCollider>(); shape.center = box.center; shape.size = box.size; copy = shape;
                        }
                        else if (source is SphereCollider sphere)
                        {
                            var shape = go.AddComponent<SphereCollider>(); shape.center = sphere.center; shape.radius = sphere.radius; copy = shape;
                        }
                        else if (source is CapsuleCollider capsule)
                        {
                            var shape = go.AddComponent<CapsuleCollider>(); shape.center = capsule.center;
                            shape.radius = capsule.radius; shape.height = capsule.height; shape.direction = capsule.direction; copy = shape;
                        }
                        if (copy == null) { Object.Destroy(go); continue; }
                        copy.isTrigger = true; go.SetActive(false); part.Shapes.Add(go);
                    }
                    if (part.Shapes.Count > 0) parts.Add(part);
                }
            }
            foreach (var part in parts)
            {
                bool show = selected && camera != null && UltimateTool.CanDetach(part.Item)
                    && (part.Item.transform.position - camera.transform.position).sqrMagnitude <= 36f
                    && !HasNativeCollider(part.Item);
                foreach (var shape in part.Shapes) if (shape != null && shape.activeSelf != show) shape.SetActive(show);
            }
        }
        private static bool HasNativeCollider(GameObject item) => item.GetComponentsInChildren<Collider>(false)
            .Any(c => c.enabled && c.GetComponent<MountedPartPickTarget>() == null);

        internal static void Clear()
        {
            foreach (var part in parts) foreach (var shape in part.Shapes) if (shape != null) Object.Destroy(shape);
            parts.Clear(); nextScan = 0f;
        }
    }
}
