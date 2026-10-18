using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

public sealed partial class UltimateVerifier
{
    void PutAwayDrops()
    {
        foreach(var boxName in new[]{"UseToolbox","UseAdjustToolBox"})
        foreach(var state in new[]{"ItemInHand","Rotate","Forward","Backward","idle"})
        {
            Call("Clear");
            var host=Node("PutAwayHost",new Vector3(2000,2000,2000));
            var grip=Node("PutAwayHand",host.transform.position);
            grip.transform.rotation=Quaternion.Euler(12,23,34);
            var part=Node("PutAwayPart",host.transform.position+Vector3.forward);
            part.transform.SetParent(grip.transform,true);
            part.transform.rotation=Quaternion.Euler(23,45,67);
            part.transform.localScale=new Vector3(.7f,1.2f,.9f);
            var body=part.AddComponent<Rigidbody>();body.useGravity=false;
            var collider=part.AddComponent<BoxCollider>();collider.isTrigger=true;
            var colliders=PutAwayReceiver(part,"Colliders","CollidersActivate");
            var useCan=PutAwayReceiver(part,"UseCan","OutofHand");
            var useCan2=PutAwayReceiver(part,"UseCan_2","OutofHand");
            var lockPhysics=PutAwayReceiver(part,"LockPhysics","LockPhysics_OFF");
            var grabber=Fsm(host,"GrabItem","idle","ItemInHand","Rotate","Forward","Backward","Drop");
            grabber.Fsm.Variables.GameObjectVariables=new[]{new FsmGameObject("Item"){Value=part}};
            var velocity=new Vector3(3,0,-2);
            // Match the native Drop actions: notify item FSMs, restore physics,
            // inherit vehicle velocity and unparent at the current world pose.
            var setVelocity=new SetVelocity();setVelocity.Reset();
            setVelocity.gameObject=Go(part);setVelocity.vector=velocity;setVelocity.space=Space.World;
            var setSpin=new SetAngularVelocity();setSpin.Reset();
            setSpin.gameObject=Go(part);setSpin.x=0;setSpin.y=0;setSpin.z=0;
            Actions(grabber,"Drop",PutAwaySend(colliders,"CollidersActivate"),
                PutAwaySend(useCan,"OutofHand"),PutAwaySend(useCan2,"OutofHand"),
                PutAwaySend(lockPhysics,"LockPhysics_OFF"),
                new ColliderSetIsTrigger{gameObject=Go(part),isTrigger=false,resetOnExit=false},
                new UseGravity{gameObject=Go(part),useGravity=true},setSpin,setVelocity,
                new SetParent{gameObject=Go(part),parent=new FsmGameObject(),worldPositionStays=true,resetLocalPosition=false,resetLocalRotation=false},
                new NextFrameEvent{sendEvent=FsmEvent.Finished});
            grabber.Fsm.GetState("Drop").Transitions=new[]{new FsmTransition{FsmEvent=FsmEvent.Finished,ToState="idle"}};
            StartFsm(grabber);grabber.Fsm.SetState(state);
            var box=Fsm(host,boxName,"off 2","offToolbox");
            // The native holster sends OffItem to its tool FSM synchronously.
            // The item must already be free before any holster action executes.
            box.Fsm.Variables.GameObjectVariables=new[]{new FsmGameObject("ParentAtHolster"){Value=grip}};
            Actions(box,"offToolbox",new GetParent{gameObject=Go(part),storeResult=new FsmGameObject("ParentAtHolster"){UseVariable=true}});
            StartFsm(box);Field(boxName=="UseToolbox"?"toolbox":"adjustBox",box);
            Field("Grab",grabber);Field("Active",true);
            // X executes before the controller's next Tick; Held can still be stale.
            Field("Held",state=="Forward"?null:part);
            var position=part.transform.position;var rotation=part.transform.rotation;var scale=part.transform.lossyScale;
            Call("PutAway");
            foreach(var receiver in new[]{colliders,useCan,useCan2,lockPhysics})receiver.Fsm.UpdateStateChanges();
            string label=boxName+" / "+state+": ";
            Check(box.ActiveStateName=="offToolbox",label+"native holster runs");
            Check(!(bool)tool.GetField("Active",Hidden).GetValue(null),label+"tool becomes inactive");
            Check(tool.GetField("Held",Hidden).GetValue(null)==null,label+"held cache cleared");
            if(state=="idle")
            {
                Check(grabber.ActiveStateName=="idle"&&part.transform.parent==grip.transform,label+"hovered item is not dropped");
                Check(new[]{colliders,useCan,useCan2,lockPhysics}.All(f=>f.ActiveStateName=="waiting"),label+"hovered item receives no release events");
            }
            else
            {
                Check(grabber.ActiveStateName=="Drop",label+"grab leaves held state immediately");
                Check(box.FsmVariables.GetFsmGameObject("ParentAtHolster").Value==null,label+"drop precedes holster");
                Check(part.transform.parent==null&&part.activeInHierarchy,label+"item released into world");
                Check(!collider.isTrigger,label+"solid collider restored");
                Check(body.useGravity,label+"gravity restored");
                Check((body.velocity-velocity).sqrMagnitude<.000001f,label+"drop inherits vehicle velocity");
                Check(new[]{colliders,useCan,useCan2,lockPhysics}.All(f=>f.ActiveStateName=="released"),label+"all native release events sent");
                Check((part.transform.position-position).sqrMagnitude<.000001f&&Quaternion.Angle(part.transform.rotation,rotation)<.001f
                    &&(part.transform.lossyScale-scale).sqrMagnitude<.000001f,label+"world pose and scale preserved");
            }
            // Empty hands must also be safe, even with a stale controller cache.
            grabber.Fsm.SetState("ItemInHand");grabber.FsmVariables.GetFsmGameObject("Item").Value=null;
            Field("Held",part);Field("Active",true);box.Fsm.SetState("off 2");Call("PutAway");
            Check(grabber.ActiveStateName=="ItemInHand"&&box.ActiveStateName=="offToolbox",label+"empty hands holster safely");
        }
        Call("Clear");
    }
    PlayMakerFSM PutAwayReceiver(GameObject part,string name,string eventName)
    {
        var receiver=Fsm(part,name,"waiting","released");
        receiver.Fsm.GlobalTransitions=new[]{new FsmTransition{FsmEvent=FsmEvent.GetFsmEvent(eventName),ToState="released"}};
        StartFsm(receiver);return receiver;
    }
    SendEvent PutAwaySend(PlayMakerFSM receiver,string eventName)=>new SendEvent
    {
        eventTarget=new FsmEventTarget{target=FsmEventTarget.EventTarget.FSMComponent,fsmComponent=receiver},
        sendEvent=FsmEvent.GetFsmEvent(eventName),delay=0,everyFrame=false
    };
}
