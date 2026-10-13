using System;
using PartAdjustment;

int checks = 0;
void Check(bool result, string message) { if (!result) throw new Exception(message); checks++; }
Check(ToolPickPolicy.AcceptTrigger(8, false, true), "Mounted turbo trigger is a valid wrench target");
Check(ToolPickPolicy.AcceptTrigger(16, false, true), "Mounted steering wheel trigger is a valid wrench target");
Check(!ToolPickPolicy.AcceptTrigger(8, false, false), "Unrelated body trigger cannot steal the part target");
Check(ToolPickPolicy.AcceptTrigger(2, true, false), "Rolling wheel hub keeps its native special targeting");
Check(!ToolPickPolicy.GiveUseToDrive(true, false, false, true), "Aimed steering wheel takes Use over overlapping Drive");
Check(ToolPickPolicy.GiveUseToDrive(true, false, false, false), "Drive remains available when no part is targeted");
Check(!ToolPickPolicy.GiveUseToDrive(true, true, false, false), "Held seat mounting keeps priority over Drive");
Check(!ToolPickPolicy.GiveUseToDrive(true, false, true, false), "Finish adjustment cannot accidentally enter a car");
Check(!ToolPickPolicy.ToolRemoves("cassette"), "Cassettes stay hand-only");
Check(ToolPickPolicy.ToolRemoves("gearlever") && ToolPickPolicy.ToolRemoves("forcedinduction"), "Gear lever and forced induction remain wrench parts");
Check(ToolSlotPolicy.AttachState(new[] { "partMissing", "checkID", "Attach" }) == "Attach", "Standard socket recipes stay supported");
Check(ToolSlotPolicy.AttachState(new[] { "partMissing", "checkID", "check type", "Attach turbo", "Attach supercharger" }) == "check type", "Turbo and supercharger sockets run the native type dispatch");
Check(ToolSlotPolicy.AttachState(new[] { "checkID", "Attach turbo" }) == null, "Incomplete type recipe cannot offer a dead mount");
var transitions = new System.Collections.Generic.List<string>();
var adjustment = new ExclusiveAdjustment<string>((target, active) => transitions.Add(target + (active ? ":on" : ":off")));
adjustment.Begin("engine");
adjustment.Begin("radiator");
Check(string.Join(",", transitions) == "engine:on,engine:off,radiator:on" && adjustment.Active == "radiator",
    "Starting a second mount stops the first before activating the second");
adjustment.Stop();
Check(adjustment.Active == null && transitions[3] == "radiator:off", "Finish stops the retained active mount independently of hover");
adjustment.Stop();
Check(transitions.Count == 4, "Repeated finish cannot deactivate a different mount");
adjustment.Begin("engine");
adjustment.Begin(null);
Check(adjustment.Active == null && transitions[5] == "engine:off", "Starting with a missing target still stops the previous mount");
Console.WriteLine($"PASS: {checks} picking, attachment and exclusive-adjustment checks.");
