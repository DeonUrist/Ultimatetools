using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace PartAdjustment
{
    // Chassis meshes belong to the fixed car body even when shipped beneath
    // suspension_model. Match factory paths, including Duke's lifted bottom.
    internal sealed class SuspensionStructure
    {
        private sealed class Piece
        {
            internal Transform Transform;
            internal Vector3 Position, Scale;
            internal Matrix4x4 Pose;
            internal string Key;
            internal bool Factory;
        }
        private readonly Transform model;
        private readonly List<Piece> pieces = new List<Piece>();
        internal SuspensionStructure(Transform model, Transform factory)
        {
            this.model = model;
            foreach (var mesh in model.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mesh.name.ToLowerInvariant();
                if (n != "suspension_lift" && n != "undershield" && n != "shield" && n != "lifted bottom") continue;
                // Rustallion and Outrider carry their axles under suspension_lift.
                // Preserve the chassis first; stance geometry then places its axles independently.
                var path = mesh.name;
                for (var p = mesh.transform.parent; p != model && p != null; p = p.parent) path = p.name + "/" + path;
                var original = factory == null ? null : factory.Find(path);
                var source = original ?? mesh.transform;
                var sourceModel = original == null ? model : factory;
                pieces.Add(new Piece { Transform = mesh.transform, Key = "UltimateTools_Structure_" + path.Replace('/', '_'),
                    Position = sourceModel.InverseTransformPoint(source.position), Scale = source.localScale,
                    Pose = sourceModel.worldToLocalMatrix * source.localToWorldMatrix, Factory = original != null });
            }
        }
        internal void Apply(Vector3 position, Vector3 scale)
        {
            var factory = Matrix4x4.TRS(position, model.localRotation, scale);
            foreach (var p in pieces)
            {
                if (p.Transform == null) continue;
                var pose = p.Transform.parent.worldToLocalMatrix * model.parent.localToWorldMatrix * factory * p.Pose;
                p.Transform.localPosition = pose.MultiplyPoint3x4(Vector3.zero);
                p.Transform.localRotation = pose.rotation;
                p.Transform.localScale = pose.lossyScale;
            }
        }
        internal void Write(FsmVariables variables)
        {
            foreach (var p in pieces)
            {
                SaveVariables.Vector(variables, p.Key + "_Position", p.Position).Value = p.Position;
                SaveVariables.Vector(variables, p.Key + "_Scale", p.Scale).Value = p.Scale;
            }
        }
        internal void Read(FsmVariables variables)
        {
            foreach (var p in pieces)
            {
                // Old saves can contain the displaced base. The prefab is authoritative.
                if (p.Factory) continue;
                p.Position = SaveVariables.Vector(variables, p.Key + "_Position", p.Position).Value;
                var savedScale = SaveVariables.Vector(variables, p.Key + "_Scale", p.Scale).Value;
                p.Pose = Matrix4x4.TRS(p.Position, p.Pose.rotation, Vector3.Scale(p.Pose.lossyScale,
                    new Vector3(savedScale.x / p.Scale.x, savedScale.y / p.Scale.y, savedScale.z / p.Scale.z)));
                p.Scale = savedScale;
            }
        }
    }
}
