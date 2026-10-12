using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace PartAdjustment
{
    [ES3NonSerializable]
    public sealed class UltimateTransient : MonoBehaviour { }
    [ES3NonSerializable]
    public sealed class UltimateMountMarker : MonoBehaviour
    {
        internal PlayMakerFSM Slot;
        private Material tint;
        private Collider pickCollider;
        private static Mesh mesh;
        private static Material material;
        internal static void Create(PlayMakerFSM slot)
        {
            if(mesh==null || material==null)
            {
                var source=Resources.FindObjectsOfTypeAll<MeshFilter>().FirstOrDefault(m=>m.name=="engineHingeVisible" && m.sharedMesh!=null);
                if(source==null || source.GetComponent<MeshRenderer>()==null)return;
                mesh=source.sharedMesh; material=source.GetComponent<MeshRenderer>().sharedMaterial;
            }
            var go=new GameObject("UltimateTools.AttachmentPoint"){layer=13,tag="Hinge"};
            go.transform.SetParent(slot.transform.parent,false);
            go.AddComponent<UltimateTransient>();
            var marker=go.AddComponent<UltimateMountMarker>(); marker.Slot=slot;
            var collider=go.AddComponent<SphereCollider>(); collider.radius=.085f/.22f; collider.isTrigger=true;
            collider.enabled=false; marker.pickCollider=collider;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var visual=go.AddComponent<MeshRenderer>();
            marker.tint=GreenIconMaterial(material,"UltimateTools.CompatibleMount");
            visual.sharedMaterial=marker.tint; visual.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.localScale=Vector3.one*.22f;
            UltimateTool.Markers.Add(marker); go.SetActive(false);
        }
        internal static Material GreenIconMaterial(Material source,string name)
        {
            var tint=new Material(source){name=name};
            // The native wrench is a textured quad; retain its alpha mask.
            var texture=source.mainTexture;
            var textureScale=source.mainTextureScale;
            var textureOffset=source.mainTextureOffset;
            var overlay=Shader.Find("GUI/Text Shader");
            if(overlay!=null)
            {
                tint.shader=overlay;
                tint.mainTexture=texture;
                tint.mainTextureScale=textureScale;
                tint.mainTextureOffset=textureOffset;
                tint.renderQueue=4000;
            }
            if(tint.HasProperty("_Color"))tint.color=new Color(.6f,1f,.6f,1f);
            if(tint.HasProperty("_TintColor"))tint.SetColor("_TintColor",new Color(.6f,1f,.6f,1f));
            if(tint.HasProperty("_EmissionColor"))tint.SetColor("_EmissionColor",new Color(.6f,1f,.6f,1f));
            return tint;
        }
        internal void Refresh(Camera camera,bool compatible,GameObject held)
        {
            if(Slot==null){Destroy(gameObject);return;}
            bool visible=compatible;
            if(pickCollider!=null)pickCollider.enabled=visible && UltimateTool.Active && held!=null;
            if(gameObject.activeSelf!=visible)gameObject.SetActive(visible);
            if(!visible)return;
            transform.position=UltimateTool.SlotPoint(Slot,held); transform.LookAt(camera.transform,Vector3.up);
        }
        private void OnDestroy(){UltimateTool.Markers.Remove(this);if(tint!=null)Destroy(tint);}
    }
}
