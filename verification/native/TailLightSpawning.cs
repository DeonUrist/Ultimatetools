using System;
using BepInEx.Configuration;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

public sealed partial class UltimateVerifier
{
    void TailLightSpawning()
    {
        var chance=(ConfigEntry<int>)typeof(PartAdjustment.Plugin).GetField("TailLightChance",Hidden).GetValue(null);
        int previous=chance.Value;
        try
        {
            chance.Value=100;
            foreach(var variant in new[]{"Headlight","Headlight_2","Headlight_3"})
            {
                var made=SpawnLight(variant,true,true);
                Check(made.name=="tail_light"+variant.Substring("Headlight".Length)+"(Clone)123",
                    variant+": shared input/output variable converts at 100% and keeps unique suffix (actual: "+made.name+")");
                Check(made.GetComponentInChildren<Light>(true).color.r==1f
                    && made.GetComponentInChildren<Light>(true).color.g<.2f,
                    variant+": spawned light is red");
            }
            Check(SpawnLight("Headlight",false,true).name=="tail_light(Clone)123",
                "Separate input/output variables also convert");
            chance.Value=0;
            Check(SpawnLight("Headlight",true,true).name=="Headlight(Clone)123","0% leaves white headlight unchanged");
            chance.Value=100;
            Check(SpawnLight("Headlight",true,false).name=="Headlight(Clone)123","Non-spawner CreateObject is unchanged");
            Check(SpawnLight("OtherItem",true,true).name=="OtherItem(Clone)123","Other items are unchanged");
        }
        finally { chance.Value=previous; }
    }

    GameObject SpawnLight(string sourceName,bool shared,bool spawner)
    {
        var source=Node("Source",Vector3.zero);source.name=sourceName;source.SetActive(false);
        var id=Fsm(source,"ID","ID");
        id.Fsm.Variables.StringVariables=new[]{new FsmString("ID"){Value="headlight"}};
        var lamp=Node("Light",Vector3.zero);lamp.transform.SetParent(source.transform,false);
        lamp.AddComponent<Light>();
        var owner=Node("Fixture",Vector3.zero);
        var fsm=Fsm(owner,spawner?"ItemSpawner":"Other","idle");
        var input=new FsmGameObject{Value=source};
        var output=shared?input:new FsmGameObject();
        var create=new CreateObject();create.Reset();create.gameObject=input;create.storeObject=output;
        create.parent=new FsmGameObject();create.spawnPoint=new FsmGameObject();
        StartFsm(fsm);
        create.Init(fsm.Fsm.GetState("idle"));create.OnEnter();
        var made=output.Value;
        Check(made!=null && made!=source,"CreateObject creates an independent clone");
        objects.Add(made);
        Check(source.name==sourceName && lamp.GetComponent<Light>().color==Color.white,
            "Source prefab keeps its original name and light colour");
        // Exercise a later vanilla rename that still carries the original white item's name.
        var setName=new SetName{gameObject=Go(made),name=new FsmString{Value=sourceName+"(Clone)123"}};
        setName.Init(fsm.Fsm.GetState("idle"));setName.OnEnter();
        return made;
    }
}
