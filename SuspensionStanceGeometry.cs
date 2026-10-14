using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace PartAdjustment
{
    // Place lifted axles independently of fixed chassis parents, after preserving the base.
    internal sealed class SuspensionStanceGeometry
    {
        private sealed class Axle
        {
            internal Transform Transform;
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal Vector3 Scale;
            internal Matrix4x4 ParentPose;
            internal bool Front, Factory;
            internal string Key;
        }
        private readonly Transform car;
        private readonly Transform model;
        private readonly List<Axle> axles = new List<Axle>();

        internal SuspensionStanceGeometry(Transform car, Transform model, Transform factoryModel)
        {
            this.car = car;
            this.model = model;
            foreach (var mesh in model.GetComponentsInChildren<MeshFilter>(true))
            {
                bool axle = AxleEndpoints.IsAxle(mesh);
                // Duke and Rustallion have rear springs beside the axle, rather than beneath it.
                bool spring = mesh.name.ToLowerInvariant().Contains("spring")
                    && !mesh.transform.GetComponentsInParent<MeshFilter>(true).Any(m => m != mesh && AxleEndpoints.IsAxle(m));
                if (!axle && !spring) continue;
                var branch = mesh.transform;
                while (branch.parent != null && branch.parent != model) branch = branch.parent;
                if (branch.parent != model || (branch.name != "lifted" && branch.name != "buggy")) continue;
                string path = mesh.name;
                for (var p = mesh.transform.parent; p != null && p != model; p = p.parent) path = p.name + "/" + path;
                var original = factoryModel == null ? null : factoryModel.Find(path);
                var source = original ?? mesh.transform;
                var sourceModel = original == null ? model : factoryModel;
                axles.Add(new Axle { Transform = mesh.transform, Front = mesh.name.ToLowerInvariant().Contains("front"),
                    Position = source.localPosition, Rotation = source.localRotation, Scale = source.localScale,
                    ParentPose = sourceModel.worldToLocalMatrix * source.parent.localToWorldMatrix,
                    Factory = original != null, Key = "UltimateTools_Axle_" + path.Replace('/', '_') + "_Position" });
            }
        }

        internal void Apply(float height, float balance, bool independent)
        {
            foreach (var axle in axles)
                if (axle.Transform != null)
                {
                    if (!independent)
                    {
                        // Preserve the existing buggy behavior; child springs follow their axle.
                        if (!AxleEndpoints.IsAxle(axle.Transform.GetComponent<MeshFilter>())) continue;
                        axle.Transform.localPosition = axle.Position + axle.Transform.parent.InverseTransformVector(
                            car.TransformVector(Vector3.up * (axle.Front ? -balance : balance)));
                        continue;
                    }
                    var pose = axle.Transform.parent.worldToLocalMatrix * model.localToWorldMatrix
                        * axle.ParentPose * Matrix4x4.TRS(axle.Position, axle.Rotation, axle.Scale);
                    axle.Transform.localPosition = pose.MultiplyPoint3x4(Vector3.zero)
                        - axle.Transform.parent.InverseTransformVector(car.TransformVector(Vector3.up
                            * SuspensionStanceMath.Lift(height, balance, axle.Front)));
                    axle.Transform.localRotation = pose.rotation;
                    axle.Transform.localScale = pose.lossyScale;
                }
        }

        internal void Write(FsmVariables variables)
        {
            foreach (var axle in axles) SaveVariables.Vector(variables, axle.Key, axle.Position).Value = axle.Position;
        }

        internal void Read(FsmVariables variables)
        {
            foreach (var axle in axles)
                if (!axle.Factory) axle.Position = SaveVariables.Vector(variables, axle.Key, axle.Position).Value;
        }
    }
}
