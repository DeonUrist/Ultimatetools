using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace PartAdjustment
{
    internal static class ToolVisual
    {
        private sealed class Original { internal MeshFilter Filter; internal Mesh Mesh; internal MeshRenderer Renderer; internal Material[] Materials; }
        private static readonly List<Original> originals=new List<Original>();
        private static Mesh driverMesh;
        private static Material rust;
        private static bool swapped;
        private static float nextFind;
        internal static Mesh LoadMesh()
        {
            if(driverMesh!=null)return driverMesh;
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("UltimateTools.Screwdriver.mesh"))
            using(var reader=new BinaryReader(stream))
            {
                if(reader.ReadUInt32()!=0x55544D31)throw new InvalidDataException("Invalid screwdriver mesh.");
                int count=reader.ReadInt32(),indices=reader.ReadInt32();
                var positions=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++){positions[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());normals[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());uv[i]=new Vector2(reader.ReadSingle(),reader.ReadSingle());}
                var tris=new int[indices];for(int i=0;i<indices;i++)tris[i]=reader.ReadInt32();
                driverMesh=new Mesh{name="UltimateTools.Screwdriver"};driverMesh.vertices=positions;driverMesh.normals=normals;driverMesh.uv=uv;driverMesh.triangles=tris;driverMesh.RecalculateBounds();
            }
            return driverMesh;
        }
        private static void Discover(PlayMakerFSM fsm)
        {
            var state=fsm?.Fsm.GetState("useToolbox");if(state==null)return;
            foreach(var action in state.Actions.OfType<ActivateGameObject>())
            {
                var arm=fsm.Fsm.GetOwnerDefaultTarget(action.gameObject);if(arm==null)continue;
                foreach(var mf in arm.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(mf.name!="Tool_1" || mf.sharedMesh==null || originals.Any(o=>o.Filter==mf))continue;
                    var renderer=mf.GetComponent<MeshRenderer>();if(renderer==null)continue;
                    originals.Add(new Original{Filter=mf,Mesh=mf.sharedMesh,Renderer=renderer,Materials=renderer.sharedMaterials});
                }
            }
        }
        internal static void Update(bool active,bool driver,PlayMakerFSM utilityBox,PlayMakerFSM adjustBox)
        {
            if(active && Time.unscaledTime>=nextFind && originals.Count<2)
            {
                nextFind=Time.unscaledTime+1f;Discover(utilityBox);Discover(adjustBox);
                if(rust==null)rust=Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m=>m.name=="metal_rusted_3");
            }
            bool want=active&&driver;if(swapped==want && originals.Count>0)return;
            foreach(var o in originals)
            {
                if(o.Filter==null || o.Renderer==null)continue;
                o.Filter.sharedMesh=want?LoadMesh():o.Mesh;
                o.Renderer.sharedMaterials=want?new[]{rust??o.Materials[0]}:o.Materials;
            }
            swapped=want;
        }
        internal static void Clear(){foreach(var o in originals){if(o.Filter!=null)o.Filter.sharedMesh=o.Mesh;if(o.Renderer!=null)o.Renderer.sharedMaterials=o.Materials;}originals.Clear();swapped=false;nextFind=0f;}
    }
}
