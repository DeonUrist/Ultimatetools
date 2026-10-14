using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using UnityEngine;

public sealed partial class UltimateVerifier
{
    object Construct(string name, params object[] args) => Activator.CreateInstance(
        typeof(PartAdjustment.Plugin).Assembly.GetType("PartAdjustment." + name), Hidden, null, args, null);
    void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    static bool Axle(Transform t) => t.name.ToLowerInvariant().Contains("suspension")
        && (t.name.ToLowerInvariant().Contains("front") || t.name.ToLowerInvariant().Contains("rear"));
    static bool FixedPiece(Transform t) => new[] { "suspension_lift", "undershield", "shield", "lifted bottom" }.Contains(t.name);
    static void SamePose(Transform t, Matrix4x4 expected, Action<bool,string> check, string label)
    {
        // Vertex positions check scale and orientation as well as the mesh origin.
        foreach (var p in new[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.forward })
            check((t.TransformPoint(p) - expected.MultiplyPoint3x4(p)).sqrMagnitude < .00000001f, label);
    }
    void SuspensionGeometry()
    {
        var fixtures = new List<string[]>();
        using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("UltimateTools.SuspensionFixtures.csv")))
        {
            reader.ReadLine(); string line;
            while ((line = reader.ReadLine()) != null) fixtures.Add(line.Split(','));
        }
        int cars = 0;
        foreach (var group in fixtures.GroupBy(r => r[0]))
        {
            var factory = Node(group.Key + ".Factory", Vector3.zero).transform;
            foreach (var row in group)
            {
                var path = row[1]; int slash = path.LastIndexOf('/');
                var parent = slash < 0 ? factory : factory.Find(path.Substring(0,slash));
                var go = new GameObject(slash < 0 ? path : path.Substring(slash+1));
                var t = go.transform; t.SetParent(parent,false);
                var v = row.Skip(3).Take(10).Select(s => float.Parse(s,CultureInfo.InvariantCulture)).ToArray();
                t.localPosition = new Vector3(v[0],v[1],v[2]);
                t.localRotation = new Quaternion(v[3],v[4],v[5],v[6]);
                t.localScale = new Vector3(v[7],v[8],v[9]);
                if (row[13] != "")
                {
                    var m = new Mesh { name = row[13], vertices = new[] { Vector3.zero,Vector3.right,Vector3.up }, triangles = new[] {0,1,2} };
                    go.AddComponent<MeshFilter>().sharedMesh = m;
                }
                go.SetActive(bool.Parse(row[2]));
            }
            var car = Instantiate(factory.gameObject).transform; objects.Add(car.gameObject);
            // Nontrivial car pose verifies that offsets are measured in car axes.
            car.position = new Vector3(50,30,10); car.rotation = Quaternion.Euler(7,23,-4);
            var model = car.Find("suspension_model"); var nativeModel = factory.Find("suspension_model");
            bool buggy = model.Find("buggy") != null;
            var structure = Construct("SuspensionStructure",model,nativeModel);
            var stance = Construct("SuspensionStanceGeometry",car,model,nativeModel);
            var baselines = new Dictionary<Transform,Matrix4x4>();
            foreach (var row in group)
            {
                var t = car.Find(row[1]); var original = factory.Find(row[1]);
                baselines[t] = car.localToWorldMatrix * factory.worldToLocalMatrix * original.localToWorldMatrix;
            }
            // Simulate stale saved chassis and axle positions. Prefab geometry must win.
            var vars = new FsmVariables(); Invoke(structure,"Write",vars); Invoke(stance,"Write",vars);
            foreach (var v in vars.Vector3Variables) v.Value += Vector3.up * .37f;
            Invoke(structure,"Read",vars); Invoke(stance,"Read",vars);
            foreach (float width in new[] {1f,1.5f,1.025f,1f})
            foreach (var values in new[] {new Vector2(0,0), new Vector2(.06f,.04f), new Vector2(-.06f,-.04f), new Vector2(0,0)})
            {
                float h = values.x, balance = values.y, drop = buggy ? .1f : .2f;
                model.localScale = Vector3.Scale(nativeModel.localScale,new Vector3(width,1,1));
                model.localPosition = nativeModel.localPosition - (buggy ? Vector3.up*(h+drop) : Vector3.zero);
                Invoke(structure,"Apply",nativeModel.localPosition,nativeModel.localScale);
                Invoke(stance,"Apply",buggy ? 0f : h+drop,balance,!buggy);
                Invoke(structure,"Apply",nativeModel.localPosition,nativeModel.localScale);
                foreach (var row in group.Where(r => r[13] != ""))
                {
                    var t = car.Find(row[1]); var original = factory.Find(row[1]);
                    if (FixedPiece(t)) SamePose(t,baselines[t],Check,group.Key+" base/shield stays vanilla at width/height/balance "+width+"/"+h+"/"+balance);
                    bool lifted = row[1].Contains("/lifted/") || row[1].Contains("/buggy/");
                    bool separateSpring = t.name.Contains("spring") && !t.GetComponentsInParent<MeshFilter>(true).Any(m => m.transform != t && Axle(m.transform));
                    if (!lifted || (!Axle(t) && (!separateSpring || buggy))) continue;
                    var expected = model.localToWorldMatrix * nativeModel.worldToLocalMatrix * original.localToWorldMatrix;
                    var delta = car.TransformVector(Vector3.up * (buggy ? 0f : h+drop));
                    delta += car.TransformVector(Vector3.up * (t.name.Contains("front") ? balance : -balance));
                    expected.SetColumn(3, expected.GetColumn(3) - new Vector4(delta.x,delta.y,delta.z,0));
                    SamePose(t,expected,Check,group.Key+" axle/spring follows suspension only "+t.name);
                }
                if (!buggy) Check(model.localPosition == nativeModel.localPosition,group.Key+" suspension root retains vanilla base height");
            }
            cars++;
        }
        Check(cars==9,"all nine vanilla suspension geometries checked; six lifted cars and three unchanged buggy families");
    }
}
