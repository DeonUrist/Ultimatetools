using System;
using System.Collections;
using BepInEx.Configuration;
using NWH.WheelController3D;
using PartAdjustment;
using UnityEngine;

public sealed partial class UltimateVerifier
{
    void ProgressiveDamping()
    {
        var type = typeof(SuspensionAdjustment);
        var car = Node("Damping.Car", Vector3.zero);
        car.AddComponent<Rigidbody>().isKinematic = true;
        var adjustment = car.AddComponent<SuspensionAdjustment>();
        var hinge = Node("Damping.Hinge", Vector3.zero).transform;
        hinge.SetParent(car.transform, false);
        type.GetField("Hinge", Hidden).SetValue(adjustment, hinge);
        var kit = Node("Damping.Kit", Vector3.zero);
        kit.tag = "vehPart"; kit.transform.SetParent(hinge, false);
        var mountType = type.GetNestedType("Mount", System.Reflection.BindingFlags.NonPublic);
        var mounts = (IList)type.GetField("mounts", Hidden).GetValue(adjustment);
        var wheels = new WheelController[2];
        var states = new object[2];
        for (int i = 0; i < 2; i++)
        {
            var go = Node("Damping.Wheel" + i, Vector3.zero);
            go.SetActive(false); go.transform.SetParent(car.transform, false);
            var wheel = wheels[i] = go.AddComponent<WheelController>();
            wheel.enabled = false; go.SetActive(true);
            wheel.DamperBumpRate = 7000; wheel.DamperReboundRate = 6000;
            var mount = Activator.CreateInstance(mountType, true);
            foreach (var pair in new[] {new {Name="Controller", Value=(object)wheel}, new {Name="Damper", Value=(object)wheel.damper},
                new {Name="Transform", Value=(object)go.transform}, new {Name="Front", Value=(object)(i == 0)}, new {Name="Lifted", Value=(object)true}})
                mountType.GetField(pair.Name, Hidden).SetValue(mount, pair.Value);
            states[i] = mountType.GetField("Damping", Hidden).GetValue(mount);
            mounts.Add(mount);
        }
        var enabled = (ConfigEntry<bool>)typeof(PartAdjustment.Plugin).GetField("Enabled", Hidden).GetValue(null);
        bool previous = enabled.Value;
        void Stance(float height, float balance = 0)
        {
            type.GetField("RideHeight", Hidden).SetValue(adjustment, height);
            type.GetField("Balance", Hidden).SetValue(adjustment, balance);
            Invoke(adjustment, "UpdateDamping", true);
        }
        void Rates(float front, float rear, string label)
        {
            Check(Math.Abs(wheels[0].DamperBumpRate - 7000 * front) < .01f
                && Math.Abs(wheels[0].DamperReboundRate - 6000 * front) < .01f
                && Math.Abs(wheels[1].DamperBumpRate - 7000 * rear) < .01f
                && Math.Abs(wheels[1].DamperReboundRate - 6000 * rear) < .01f, label);
        }
        try
        {
            enabled.Value = true;
            Stance(0); Rates(1, 1, "zero height keeps native NWH rates");
            Stance(.05f); Rates(.75f, .75f, "5 cm applies 75% to actual NWH properties");
            Stance(.10f); Rates(.5f, .5f, "10 cm applies 50%");
            Stance(.15f); Rates(.25f, .25f, "15 cm applies 25%");
            for (int i = 0; i < 100; i++) Stance(.15f);
            Rates(.25f, .25f, "repeated production updates do not compound rates");
            Stance(.05f, .05f); Rates(.5f, 1, "balance adjusts actual front and rear dampers independently");
            Stance(-.10f); Rates(1, 1, "lowering restores native rates");
            Stance(.15f); Stance(0); Rates(1, 1, "stance reset restores exact rates");
            Stance(.15f); kit.transform.SetParent(null); Stance(.15f);
            Rates(1, 1, "kit removal restores damping");
            kit.transform.SetParent(hinge, false); Stance(.15f);
            Rates(.25f, .25f, "kit refit resumes progressive damping");
            enabled.Value = false; Stance(.15f); Rates(1, 1, "global disable restores damping");
            enabled.Value = true; Stance(.15f); adjustment.enabled = false;
            Rates(1, 1, "component OnDisable restores damping");
            adjustment.enabled = true; Stance(.15f);
            Rates(.25f, .25f, "component reenable resumes damping");
            wheels[0].DamperBumpRate = 3210; wheels[0].DamperReboundRate = 4320;
            Stance(.10f); Stance(0);
            Check(wheels[0].DamperBumpRate == 3210 && wheels[0].DamperReboundRate == 4320,
                "external NWH rate changes are respected during tuning and reset");
            wheels[1].damper = new Damper { bumpRate = 2222, reboundRate = 3333 };
            Stance(.15f);
            Check(wheels[1].DamperBumpRate == 2222 && wheels[1].DamperReboundRate == 3333,
                "replacement damper is not overwritten");
            for (int i = 0; i < 2; i++)
            {
                wheels[i].DamperBumpRate = 8000; wheels[i].DamperReboundRate = 8000;
                Invoke(states[i], "NativeChanged", 8000f, 8000f);
                mountType.GetField("Lifted", Hidden).SetValue(mounts[i], false);
            }
            Stance(.15f);
            Check(wheels[0].DamperBumpRate == 8000 && wheels[1].DamperReboundRate == 8000,
                "stock native transition preserves fresh stock rates");
            Check(wheels[0].spring.maxLength == .35f && wheels[0].spring.maxForce == 16000
                && wheels[0].damper.bumpDivisionVelocity == .06f && wheels[0].damper.reboundDivisionVelocity == .05f
                && wheels[0].damper.slowBump == 1.4f && wheels[0].damper.fastRebound == .6f,
                "spring support, travel and damper curve factors remain unchanged");
            UnityEngine.Object.DestroyImmediate(adjustment);
            Check(wheels[0].DamperBumpRate == 8000, "destroy cleanup preserves native stock rates");
        }
        finally { enabled.Value = previous; }
    }
}
