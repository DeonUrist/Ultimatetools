using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PartAdjustment
{
    internal static class AxleEndpoints
    {
        private static readonly Dictionary<string, Vector3[]> points = Read();
        private static Dictionary<string, Vector3[]> Read()
        {
            var result = new Dictionary<string, Vector3[]>();
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("UltimateTools.AxleEndpoints.csv")))
            {
                reader.ReadLine();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = line.Split(',');
                    var p = new float[6];
                    for (int i = 0; i < 6; i++) p[i] = float.Parse(values[i + 1], CultureInfo.InvariantCulture);
                    result[values[0]] = new[] { new Vector3(p[0], p[1], p[2]), new Vector3(p[3], p[4], p[5]) };
                }
            }
            return result;
        }
        internal static void Get(Mesh mesh, out Vector3 left, out Vector3 right)
        {
            if (points.TryGetValue(mesh.name.Replace(" Instance", ""), out var pair)) { left = pair[0]; right = pair[1]; return; }
            var b = mesh.bounds;
            left = b.center - Vector3.right * b.extents.x;
            right = b.center + Vector3.right * b.extents.x;
        }
        internal static bool IsAxle(MeshFilter mesh)
        {
            string name = mesh.name.ToLowerInvariant();
            return mesh.sharedMesh != null && name.Contains("suspension") && (name.Contains("front") || name.Contains("rear"));
        }
    }
}
