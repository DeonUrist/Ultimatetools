using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace PartAdjustment
{
    // Move each lifted axle with its wheels; body/underbody compensation runs afterwards.
    internal sealed class SuspensionStanceGeometry
    {
        private sealed class Axle
        {
            internal Transform Transform;
            internal Vector3 Position;
            internal bool Front, Factory;
            internal string Key;
        }
        private readonly Transform car;
        private readonly List<Axle> axles = new List<Axle>();

        internal SuspensionStanceGeometry(Transform car, Transform model, Transform factoryModel)
        {
            this.car = car;
            foreach (var mesh in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!AxleEndpoints.IsAxle(mesh)) continue;
                var branch = mesh.transform;
                while (branch.parent != null && branch.parent != model) branch = branch.parent;
                if (branch.parent != model || (branch.name != "lifted" && branch.name != "buggy")) continue;
                string path = mesh.name;
                for (var p = mesh.transform.parent; p != null && p != model; p = p.parent) path = p.name + "/" + path;
                var original = factoryModel == null ? null : factoryModel.Find(path);
                axles.Add(new Axle { Transform = mesh.transform, Front = mesh.name.ToLowerInvariant().Contains("front"),
                    Position = original == null ? mesh.transform.localPosition : original.localPosition,
                    Factory = original != null, Key = "UltimateTools_Axle_" + path.Replace('/', '_') + "_Position" });
            }
        }

        internal void Apply(float balance)
        {
            foreach (var axle in axles)
                if (axle.Transform != null)
                    axle.Transform.localPosition = axle.Position + axle.Transform.parent.InverseTransformVector(
                        car.TransformVector(Vector3.up * (axle.Front ? -balance : balance)));
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
