using System;
using HutongGames.PlayMaker.Actions;
using NWH.WheelController3D;
using PartAdjustment;
using UnityEngine;

public sealed partial class UltimateVerifier
{
    void ManualSuspension()
    {
        foreach (bool buggy in new[] { false, true })
        {
            var car = Node("ManualSuspension." + buggy, Vector3.zero);
            car.AddComponent<Rigidbody>().isKinematic = true;
            var model = Node("suspension_model", Vector3.zero).transform;
            model.name = "suspension_model"; model.SetParent(car.transform, false);
            var branch = Node(buggy ? "buggy" : "lifted", Vector3.zero).transform;
            branch.name = buggy ? "buggy" : "lifted"; branch.SetParent(model, false);
            var axle = Node("suspension front", Vector3.zero).transform;
            axle.SetParent(branch, false);
            axle.localPosition = new Vector3(0, -.3f, 1);
            axle.gameObject.AddComponent<MeshFilter>().sharedMesh = new Mesh { name = "ManualAxle",
                vertices = new[] { new Vector3(-1,0,0), new Vector3(1,0,0), Vector3.up }, triangles = new[] {0,1,2} };
            var hingeParent = Node("hinge_suspension_parent", Vector3.zero).transform;
            hingeParent.name = "hinge_suspension_parent"; hingeParent.SetParent(car.transform, false);
            var hinge = Node("hinge_suspension", Vector3.zero).transform;
            hinge.name = "hinge_suspension"; hinge.SetParent(hingeParent, false);
            StartFsm(Fsm(hinge.gameObject, "vehPart_Attach", "idle"));
            StartFsm(Fsm(hinge.gameObject, "checkSuspension", "idle"));
            var kit = Node("Kit", Vector3.zero); kit.tag = "vehPart"; kit.transform.SetParent(hinge, false);
            var wheels = new WheelController[4];
            var baseline = new Vector3[4];
            for (int i = 0; i < wheels.Length; i++)
            {
                var go = Node("hinge_wheel_" + new[] {"FL", "FR", "RL", "RR"}[i], Vector3.zero);
                go.name = "hinge_wheel_" + new[] {"FL", "FR", "RL", "RR"}[i];
                go.SetActive(false); go.transform.SetParent(car.transform, false);
                wheels[i] = go.AddComponent<WheelController>(); wheels[i].enabled = false;
                go.SetActive(true);
                baseline[i] = new Vector3(i % 2 == 0 ? -1 : 1, -.25f, i < 2 ? 1 : -1);
                var fsm = Fsm(go, "Suspension", "lifted", "stock");
                foreach (string state in new[] { "lifted", "stock" })
                    Actions(fsm, state, new SetPosition { gameObject = Owner(), vector = baseline[i],
                        x = new HutongGames.PlayMaker.FsmFloat { UseVariable = true },
                        y = new HutongGames.PlayMaker.FsmFloat { UseVariable = true },
                        z = new HutongGames.PlayMaker.FsmFloat { UseVariable = true }, space = Space.Self });
                StartFsm(fsm);
                wheels[i].DamperBumpRate = 7000; wheels[i].DamperReboundRate = 6000;
                wheels[i].spring.maxLength = .35f;
            }
            var adjustment = car.AddComponent<SuspensionAdjustment>();
            Invoke(adjustment, "Initialize", model, hinge, wheels);
            void CheckStance(float width, float height, float balance)
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    var expected = baseline[i] - Vector3.up * (height + (i < 2 ? balance : -balance));
                    // Synthetic axle has endpoints -1/+1; widening adds only lateral growth.
                    expected.x *= width;
                    Check((wheels[i].transform.localPosition - expected).sqrMagnitude < .0000001f,
                        "manual wheel pose, no calibration: " + buggy + "/" + i + "/" + width + "/" + height);
                    Check(wheels[i].DamperBumpRate == 7000 && wheels[i].DamperReboundRate == 6000
                        && wheels[i].spring.maxLength == .35f, "manual stance leaves native damping and travel unchanged");
                }
                Check((axle.position - (car.transform.TransformPoint(new Vector3(0, -.3f - height - balance, 1)))).sqrMagnitude < .0000001f,
                    "visible axle follows only requested height/balance");
            }
            CheckStance(1, 0, 0);
            Invoke(adjustment, "Change", .25f, .10f, .02f, false);
            CheckStance(1.25f, .10f, .02f);
            Invoke(adjustment, "Change", 0f, 0f, 0f, true);
            CheckStance(1, 0, 0);
            Invoke(adjustment, "Change", .25f, .10f, 0f, false);
            kit.transform.SetParent(null);
            Invoke(adjustment, "Apply");
            CheckStance(1, 0, 0);
        }
    }
}
