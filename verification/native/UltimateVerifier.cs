using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using PartAdjustment;
using UnityEngine;

[BepInPlugin("com.denis.ultimatetools.verifier", "Ultimate Tools Native Verifier", "1.0.0")]
[BepInDependency(PartAdjustment.Plugin.Guid)]
public sealed partial class UltimateVerifier : BaseUnityPlugin
{
    const BindingFlags Hidden=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic;
    Type tool;
    int checks;
    readonly List<string> lines=new List<string>();
    readonly List<GameObject> objects=new List<GameObject>();
    string report;
    void Awake()
    {
        var arg=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("-ultimate-tools-report="));
        if(arg==null)return;
        Logger.LogInfo("Ultimate Tools verifier dispatched: "+arg);
        report=Path.GetFullPath(arg.Substring(arg.IndexOf('=')+1));
        StartCoroutine(Run());
    }
    IEnumerator Run()
    {
        Logger.LogInfo("Ultimate Tools verifier coroutine started.");
        Logger.LogInfo("Ultimate Tools verifier exercising fixtures.");
        try
        {
            foreach(var r in Resources.FindObjectsOfTypeAll<AdjustmentRunner>())r.enabled=false;
            foreach(var r in Resources.FindObjectsOfTypeAll<ToolRunner>())r.enabled=false;
            tool=typeof(PartAdjustment.Plugin).Assembly.GetType("PartAdjustment.UltimateTool");
            Check(tool!=null,"new controller loaded");
            Check(typeof(PartAdjustment.Plugin).Assembly.GetName().Version.Major==2,"production assembly version");
            RaySelection();
            DetachAndAdjust();
            Compatibility();
            DriveAndPutAway();
            PatchInstallation();
            SuspensionGeometry();
            lines.Add("PASS: "+checks+" Ultimate Tools native Unity checks.");
        }
        catch(Exception e){lines.Add("ERROR: "+e);}
        finally
        {
            foreach(var go in objects)if(go!=null)Destroy(go);
            File.WriteAllLines(report,lines);Application.Quit();
        }
        yield break;
    }
    void Check(bool value,string message){if(!value)throw new Exception(message);checks++;lines.Add("OK: "+message);}
    object Call(string name,params object[] args)=>tool.GetMethod(name,Hidden).Invoke(null,args);
    void Field(string name,object value)=>tool.GetField(name,Hidden).SetValue(null,value);
    GameObject Node(string name,Vector3 position){var go=new GameObject("UT.Test."+name);go.transform.position=position;objects.Add(go);return go;}
    FsmOwnerDefault Go(GameObject go)=>new FsmOwnerDefault{OwnerOption=OwnerDefaultOption.SpecifyGameObject,GameObject=new FsmGameObject{Value=go}};
    FsmOwnerDefault Owner()=>new FsmOwnerDefault{OwnerOption=OwnerDefaultOption.UseOwner};
    PlayMakerFSM Fsm(GameObject go,string name,params string[] states)
    {
        var component=go.AddComponent<PlayMakerFSM>();component.enabled=false;
        var data=new Fsm{Name=name,StartState=states[0]};
        data.States=states.Select(s=>new FsmState(data){Name=s,Actions=new FsmStateAction[0]}).ToArray();
        component.Fsm=data;return component;
    }
    void Actions(PlayMakerFSM f,string state,params FsmStateAction[] actions)
    {
        f.Fsm.Variables.Reinitialize();
        var s=f.Fsm.GetState(state);s.Actions=actions;s.SaveActions();
    }
    void StartFsm(PlayMakerFSM f){f.Fsm.Reinitialize();f.enabled=true;f.Fsm.Init(f);f.Fsm.Start();f.Fsm.SetState(f.Fsm.StartState);}
    Camera camera;
    GameObject plate,exhaust;
    GameObject item,hand;
    PlayMakerFSM grab;
    void RaySelection()
    {
        var c=Node("Camera",new Vector3(1000,1000,1000));camera=c.AddComponent<Camera>();camera.enabled=false;
        exhaust=Node("Exhaust",c.transform.position+Vector3.forward*1.5f);exhaust.layer=8;exhaust.AddComponent<BoxCollider>().size=Vector3.one*.2f;
        plate=Node("ForegroundPlate",c.transform.position+Vector3.forward*.8f);plate.layer=8;plate.AddComponent<BoxCollider>().size=Vector3.one*.3f;
        Physics.SyncTransforms();
        var hit=(RaycastHit)Call("ExactHit",camera,2f,null);Check(hit.collider.gameObject==plate,"foreground plate blocks exhaust");
        plate.transform.position+=Vector3.right;Physics.SyncTransforms();
        hit=(RaycastHit)Call("ExactHit",camera,2f,null);Check(hit.collider.gameObject==exhaust,"visible object exactly on cursor is picked");
        exhaust.transform.position+=Vector3.right*.35f;Physics.SyncTransforms();
        hit=(RaycastHit)Call("ExactHit",camera,2f,null);Check(hit.collider==null,"no off-cursor nearest-object fallback");
        exhaust.transform.position-=Vector3.right*.35f;plate.transform.position-=Vector3.right;Physics.SyncTransforms();
        hit=(RaycastHit)Call("ExactHit",camera,2f,plate);Check(hit.collider.gameObject==exhaust,"held object excluded from attachment ray");
        var ignored=Node("UnrelatedTrigger",c.transform.position+Vector3.forward*.5f);ignored.layer=8;ignored.AddComponent<BoxCollider>().isTrigger=true;
        Physics.SyncTransforms();hit=(RaycastHit)Call("ExactHit",camera,2f,plate);Check(hit.collider.gameObject==exhaust,"unrelated trigger does not steal target");
        ignored.SetActive(false);plate.SetActive(false);exhaust.SetActive(false);
    }
    void DetachAndAdjust()
    {
        Field("Held",null);
        hand=Node("Hand",new Vector3(1001,1000,1000));hand.transform.rotation=Quaternion.Euler(11,22,33);
        item=Node("Radiator",new Vector3(1000.2f,1000.3f,1001.2f));item.tag="vehPart";item.layer=8;
        item.transform.rotation=Quaternion.Euler(15,45,8);item.transform.localScale=new Vector3(.7f,1.2f,.9f);
        var mount=Node("Mount",new Vector3(1000,1000,1001));item.transform.SetParent(mount.transform,true);
        var collider=item.AddComponent<BoxCollider>();
        var detach=Fsm(item,"de_Attach","idle","de Attach");
        detach.Fsm.GlobalTransitions=new[]{new FsmTransition{FsmEvent=FsmEvent.GetFsmEvent("de_Attach"),ToState="de Attach"}};
        Actions(detach,"de Attach",new SetLayer{gameObject=Owner(),layer=9},new SetTag{gameObject=Owner(),tag="vehPartRemoved"});StartFsm(detach);
        var check=Fsm(item,"CheckTag","vehPart","vehPartRemoved");
        Actions(check,"vehPartRemoved",new SetParent{gameObject=Owner(),parent=new FsmGameObject(),worldPositionStays=true,resetLocalPosition=false,resetLocalRotation=false},
            new AddComponent{gameObject=Owner(),component="UnityEngine.Rigidbody",storeComponent=new FsmObject(),removeOnExit=false});StartFsm(check);
        var host=Node("GrabHost",hand.transform.position);
        grab=Fsm(host,"GrabItem","idle","Grab","ItemInHand");
        var variable=new FsmGameObject("Item");var pos=new FsmVector3("itemPos");var rot=new FsmVector3("itemRot");
        grab.Fsm.Variables.GameObjectVariables=new[]{variable};grab.Fsm.Variables.Vector3Variables=new[]{pos,rot};
        Actions(grab,"Grab",new UseGravity{gameObject=new FsmOwnerDefault{OwnerOption=OwnerDefaultOption.SpecifyGameObject,GameObject=new FsmGameObject("Item"){UseVariable=true}},useGravity=false},
            new SetParent{gameObject=new FsmOwnerDefault{OwnerOption=OwnerDefaultOption.SpecifyGameObject,GameObject=new FsmGameObject("Item"){UseVariable=true}},parent=new FsmGameObject{Value=hand},worldPositionStays=true,resetLocalPosition=false,resetLocalRotation=false},
            new ColliderSetIsTrigger{gameObject=new FsmOwnerDefault{OwnerOption=OwnerDefaultOption.SpecifyGameObject,GameObject=new FsmGameObject("Item"){UseVariable=true}},isTrigger=true,resetOnExit=false});
        grab.Fsm.GetState("Grab").Transitions=new[]{new FsmTransition{FsmEvent=FsmEvent.Finished,ToState="ItemInHand"}};StartFsm(grab);Field("Grab",grab);
        var p=item.transform.position;var q=item.transform.rotation;var s=item.transform.lossyScale;
        lines.Add("Before detach FSMs: "+string.Join(",",item.GetComponents<PlayMakerFSM>().Select(f=>f.FsmName+":"+f.ActiveStateName+":"+f.Fsm.Started)));
        Call("DetachAndGrab",item);
        lines.Add("After detach: parent="+item.transform.parent?.name+" tag="+item.tag+" grab="+grab.ActiveStateName+" check="+check.ActiveStateName+" held="+tool.GetField("Held",Hidden).GetValue(null));
        foreach(var a in grab.Fsm.GetState("Grab").Actions)lines.Add("Grab action: "+a.GetType().Name+" enabled="+a.Enabled+" owner="+a.Owner);
        var parentAction=(SetParent)grab.Fsm.GetState("Grab").Actions[1];
        lines.Add("Grab target="+grab.Fsm.GetOwnerDefaultTarget(parentAction.gameObject)+" parent="+parentAction.parent.Value+" variable="+grab.Fsm.Variables.GetFsmGameObject("Item").Value+" variableName="+parentAction.gameObject.GameObject.Name+" usevar="+parentAction.gameObject.GameObject.UseVariable);
        lines.Add("Detached FSM state="+detach.ActiveStateName+" global="+string.Join(",",detach.Fsm.GlobalTransitions.Select(t=>t.EventName+":"+t.ToState+":"+t.ToFsmState?.Name)));
        Check((item.transform.position-p).sqrMagnitude<.000001f,"detach keeps exact position");
        Check(Quaternion.Angle(item.transform.rotation,q)<.001f,"detach keeps exact rotation");
        Check((item.transform.lossyScale-s).sqrMagnitude<.000001f,"detach keeps scale");
        Check(item.transform.parent==hand.transform,"detached part enters native hand parent");
        Check(grab.ActiveStateName=="ItemInHand","native grab enters held state");
        Check(item.CompareTag("vehPartRemoved")&&item.layer==9,"native detached tag/layer");
        Check(item.GetComponent<Rigidbody>()!=null&&!item.GetComponent<Rigidbody>().useGravity,"held part cannot fall");
        Check(collider.isTrigger,"held collider is a trigger");
        Check((pos.Value-item.transform.localPosition).sqrMagnitude<.000001f,"native local held position captured");
        var type=typeof(PartAdjustment.Plugin).Assembly.GetType("PartAdjustment.AdjustmentSession");
        var session=Activator.CreateInstance(type,Hidden,null,new object[]{item.transform},null);
        Check((bool)type.GetProperty("Valid",Hidden).GetValue(session,null),"held item is adjustable");
        type.GetMethod("Change",Hidden).Invoke(session,new object[]{Vector3.right,.01f,Quaternion.identity});
        Check((item.transform.position-(p+Vector3.right*.01f)).sqrMagnitude<.000001f,"held adjustment moves by configured step");
        Call("CaptureHandPose",item,grab);Check((pos.Value-item.transform.localPosition).sqrMagnitude<.000001f,"adjustment updates native held pose");
        type.GetMethod("Reset",Hidden).Invoke(session,null);Check((item.transform.position-p).sqrMagnitude<.000001f,"reset restores initial held position");
        Field("Screwdriver",true);Call("Finish");Check((GameObject)tool.GetField("Held",Hidden).GetValue(null)==item,"finishing keeps item held");
        var mesh=(Mesh)typeof(PartAdjustment.Plugin).Assembly.GetType("PartAdjustment.ToolVisual").GetMethod("LoadMesh",Hidden).Invoke(null,null);
        Check(mesh.vertexCount==197&&mesh.triangles.Length==342,"embedded source screwdriver geometry loads");
        Check(Mathf.Abs(mesh.bounds.size.y-.24f)<.00001f,"screwdriver is 24 cm long");
        var vertices=mesh.vertices;var normals=mesh.normals;var tris=mesh.triangles;
        Check(Enumerable.Range(0,tris.Length/3).All(i=>Vector3.Dot(Vector3.Cross(vertices[tris[i*3+1]]-vertices[tris[i*3]],vertices[tris[i*3+2]]-vertices[tris[i*3]]),normals[tris[i*3]]+normals[tris[i*3+1]]+normals[tris[i*3+2]])>0),"screwdriver faces agree with outward normals");
    }
    void Compatibility()
    {
        var id=Fsm(item,"ID","ID");id.Fsm.Variables.StringVariables=new[]{new FsmString("ID"){Value="radiator"}};StartFsm(id);
        lines.Add("ID definitions: "+string.Join(",",id.Fsm.Variables.StringVariables.Select(v=>v.Name+":"+v.Value)));
        var node=Node("Socket",Vector3.zero);
        var slot=Fsm(node,"vehPart_Attach","partMissing","checkID","Attach","partAttached");
        Actions(slot,"checkID",new StringCompare{stringVariable="",compareTo="radiator",storeResult=new FsmBool(),everyFrame=false});StartFsm(slot);
        lines.Add("Compatibility: id="+Call("Id",item)+" slot="+slot.FsmName+" active="+slot.isActiveAndEnabled+" comparisons="+string.Join(",",slot.Fsm.GetState("checkID").Actions.OfType<StringCompare>().Select(a=>a.compareTo.Value+":"+a.Enabled)));
        Check((bool)Call("Compatible",slot,item),"radiator matches native radiator recipe");
        var occupant=Node("Occupant",Vector3.zero);occupant.transform.SetParent(node.transform);occupant.tag="vehPart";
        Check(!(bool)Call("Compatible",slot,item),"occupied fixed socket rejected");
        var dash=Fsm(node,"vehPart_Attach","partMissing","checkID","Attach");Actions(dash,"checkID",new StringCompare{stringVariable="",compareTo="radiator",storeResult=new FsmBool(),everyFrame=false});StartFsm(dash);
        Check((bool)Call("Compatible",dash,item),"multi-part dashboard style recipe accepts another part");
        id.Fsm.Variables.GetFsmString("ID").Value="headlight";
        Check(!(bool)Call("Compatible",slot,item),"incompatible item does not offer radiator socket");
    }
    void DriveAndPutAway()
    {
        Field("Active",true);Field("Held",null);
        var go=Node("DriveHost",Vector3.zero);var f=Fsm(go,"DriveUse","Idle","checkHand","checkHand 2","enter","exit","blocked");
        foreach(var state in new[]{"checkHand","checkHand 2"})
        {
            Actions(f,state,new GameObjectHasChildren{gameObject=Go(hand),falseEvent=FsmEvent.GetFsmEvent("empty"),trueEvent=FsmEvent.GetFsmEvent("full"),storeResult=new FsmBool(),everyFrame=false});
            f.Fsm.GetState(state).Transitions=new[]{new FsmTransition{FsmEvent=FsmEvent.GetFsmEvent("empty"),ToState=state=="checkHand"?"enter":"exit"},new FsmTransition{FsmEvent=FsmEvent.GetFsmEvent("full"),ToState="blocked"}};
        }
        StartFsm(f);f.Fsm.SetState("checkHand");Check(f.ActiveStateName=="enter","tool out does not block native car entry");
        f.Fsm.SetState("checkHand 2");Check(f.ActiveStateName=="exit","tool out does not block native car exit");
        Field("Held",item);f.Fsm.SetState("checkHand");Check(f.ActiveStateName=="blocked","real held part retains native hand restriction");
        var box=Fsm(go,"UseToolbox","off 2","offToolbox","off");StartFsm(box);Field("toolbox",box);Field("Active",true);
        var p=item.transform.position;Call("PutAway");
        Check(box.ActiveStateName=="offToolbox","X path uses native tool put-away");
        Check(!(bool)tool.GetField("Active",Hidden).GetValue(null),"put-away disables unified tool");
        Check((item.transform.position-p).sqrMagnitude<.000001f && grab.ActiveStateName=="ItemInHand","put-away preserves grabbed part");
    }
    void PatchInstallation()
    {
        foreach(var pair in new[]{new[]{typeof(MousePick).FullName,"DoMousePick"},new[]{typeof(MousePickEvent).FullName,"DoMousePickEvent"},new[]{typeof(GameObjectHasChildren).FullName,"OnEnter"},new[]{typeof(GetButtonDown).FullName,"OnUpdate"}})
        {
            var t=typeof(MousePick).Assembly.GetType(pair[0]);var method=t.GetMethod(pair[1],Hidden|BindingFlags.Public);
            Check(Harmony.GetPatchInfo(method)?.Owners.Contains(PartAdjustment.Plugin.Guid)==true,pair[0]+"."+pair[1]+" patched");
        }
    }
}


