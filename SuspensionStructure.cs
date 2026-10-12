using System.Collections.Generic;
using System.Linq;
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
            internal string Key;
        }
        private readonly Transform model;
        private readonly List<Piece> pieces = new List<Piece>();
        internal SuspensionStructure(Transform model, string vehicle)
        {
            this.model = model;
            var factory = Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => t.name == "suspension_model"
                && !t.gameObject.scene.IsValid() && t.parent != null && t.parent.name == vehicle);
            foreach (var mesh in model.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mesh.name.ToLowerInvariant();
                if (n != "suspension_lift" && n != "undershield" && n != "shield" && n != "lifted bottom") continue;
                // An unusual chassis carrying an axle must not counter-move that axle.
                if (mesh.GetComponentsInChildren<MeshFilter>(true).Any(m => m != mesh && AxleEndpoints.IsAxle(m))) continue;
                var path = mesh.name;
                for (var p = mesh.transform.parent; p != model && p != null; p = p.parent) path = p.name + "/" + path;
                var original = factory == null ? null : factory.Find(path);
                pieces.Add(new Piece { Transform = mesh.transform, Key = "UltimateTools_Structure_" + path.Replace('/', '_'),
                    Position = original == null ? model.InverseTransformPoint(mesh.transform.position) : factory.InverseTransformPoint(original.position),
                    Scale = original == null ? mesh.transform.localScale : original.localScale });
            }
        }
        internal void Apply(Vector3 position, Vector3 scale, float width)
        {
            var factory = Matrix4x4.TRS(position, model.localRotation, scale);
            foreach (var p in pieces)
            {
                if (p.Transform == null) continue;
                p.Transform.position = model.parent.TransformPoint(factory.MultiplyPoint3x4(p.Position));
                p.Transform.localScale = new Vector3(p.Scale.x / width, p.Scale.y, p.Scale.z);
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
                p.Position = SaveVariables.Vector(variables, p.Key + "_Position", p.Position).Value;
                p.Scale = SaveVariables.Vector(variables, p.Key + "_Scale", p.Scale).Value;
            }
        }
    }
}
