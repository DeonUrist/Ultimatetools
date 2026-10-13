# Ultimate Tools

Ultimate Tools 2.3.3 is a BepInEx mod for Apocalypter. It combines the utility toolbox and Part Adjustment toolbox into one wrench/screwdriver toolset, with native-looking HUD text and the game's existing bindings.

## Install

Install BepInEx first. Extract `UltimateTools-2.3.3.zip` into the game's `BepInEx/plugins` directory; it contains an `UltimateTools` folder. Restart the game.

Remove older `PartAdjustment.dll` installations before installing. Ultimate Tools retains the same plugin GUID, assembly identity and save compatibility; installing both creates a duplicate plugin.

## Settings

Open **Mods → Ultimate Tools** in Apocasetter. The settings appear in this order:

| General | Default |
| --- | --- |
| Enabled | On |
| VisibleGizmosWithoutTool | On |
| PutAwayWithKey | On |
| PutAwayKey | X |
| MovementRotationSpeed | 100% |
| FastMovementRotationSpeed | 1000% |
| FastModifierKey | Left Alt |

| Headlights | Default |
| --- | --- |
| AttachAnywhere | On |
| TailLightChance | 8% |

While carrying an attachable car part near a car, compatible empty slots are highlighted even without the tool. Turn `VisibleGizmosWithoutTool` off to require the tool. No highlights are added for empty hands. Slot discovery runs at most once per second while a car part is held; only cached nearby markers update each frame. Passive markers have no active selection collider, so they do not intercept normal interactions. Occupied fixed sockets are excluded; multi-part sockets retain the native attachment recipe.

Normal 100% speed is 1 cm / 1 degree per item-adjustment press. Fast 1000% speed is 10 cm / 10 degrees. Old settings are migrated to the compact General/Headlights layout; put-away, modifier and headlight preferences are retained. ShowGuide and the former extra settings are removed. A hidden `Apocasetter` registration flag is retained for the Mods menu.

## Tool controls

Pick up either toolbox. Both start in wrench mode. Labels follow rebound native controls.

| Default control | Action |
| --- | --- |
| E | Switch wrench/screwdriver |
| F, wrench | Detach the aimed part into a native held pose; attach a held part to a compatible socket or supported surface |
| F, screwdriver | Begin/finish adjustment |
| Native Adjust bindings | Move, rotate, change submode and reset |
| Left Alt | Fast item adjustment |
| X | Put the tool away |

Part selection follows the camera-centre ray. Mounting and screwdriver socket selection can look through vehicle bodywork; scenery and foreground loose objects still block selection. Free lights and plates retain native contact-based attachment. Attached items use car axes; held items use global axes. Mounted wheel removal keeps the rolling hub's displayed pose. Tool-only vehicle entry/exit uses native controls; a real held part retains the native hand restriction.

Screwdriver selection stops at the first eligible part or mount on that ray. Only one mount receives adjustment input. Finishing, switching tools or starting another adjustment deactivates the previous mount independently of the current cursor target.

Mounted steering wheels, turbos and superchargers remain selectable through their native trigger colliders. Parts such as gear levers that disable their pickup collider when mounted receive a temporary copy of that native pickup shape only while the tool is out and hands are empty. These selection shapes never enable the original physics collider and are excluded from saves. Cassettes are removed by hand, not by the wrench. A removable part under the cursor takes F over an overlapping Drive trigger; otherwise native entry/exit remains available.

Holding a turbo highlights compatible empty engine turbo mounts at their native mounting position, including the nested hinge. Superchargers use their own mounting pose. Native type-specific attachment recipes handle the actual mount and engine effects; occupied forced-induction hinges are not highlighted.

## Suspension

Fit a suspension lift kit, select the displayed axle with the screwdriver, and press F.

| Default control | Action |
| --- | --- |
| Num 4 / 6 | Narrow / widen wheel spacing (1.0–1.5) |
| Num 8 / 2 | Raise / lower, 1 cm per press |
| Num 7 / 1 | Tilt back / forward, 1 cm per end per press |
| Num 0 | Reset width, height and balance |
| F | Finish adjustment |

Each end is limited to 15 cm from the native lifted stance. Front/rear balance allows up to 7.5 cm of opposite movement per end; combined limits prevent excessive lift. Axle meshes follow the physical wheel mounts and fixed underbody pieces retain their body pose. Width, height and balance save per vehicle. Removing the kit resets stance once and returns stock wheels to native control. Wrench mode can detach the fitted kit by aiming at the displayed axle.

Accepted alignment is built in: the lifted family uses X −0.10 m per wheel side and lowers the axle model by 0.20 m; the buggy family uses X 0 and lowers the axle model by 0.10 m. Vertical alignment changes only the visible assembly, while fixed underbody pieces retain their body pose. A freshly fitted lift kit leaves physical wheel-mount Y at the game's native values, avoiding the former downward shift of the car body, particularly on Rustallion. Reset restores native ride height and preserves model alignment. No DEBUG calibration menu, rear mesh swapping or custom centre-of-mass change is enabled.

Supported cars: Duke, Junker, PigPen, Rustallion, Vulture, Outrider, PipeRat, Poloska and TinyTyrant. Disabled native truck suspension systems remain unsupported.

## Build and verification

Build against your installed game's assemblies:

```powershell
dotnet build UltimateTools.csproj -c Release "-p:GameDir=C:\Path\To\Apocalypter"
```

Output: `bin/Release/UltimateTools.dll`. The project requires .NET SDK and the .NET Framework 4.7.2 targeting pack.

Offline checks do not launch the game:

```powershell
dotnet run --project verification/settings/SettingsChecks.csproj -c Release
dotnet run --project verification/stance/StanceChecks.csproj -c Release
dotnet run --project verification/stock/StockChecks.csproj -c Release
dotnet run --project verification/picking/PickingChecks.csproj -c Release
```

2.3.1: clean Release build; settings, stance and stock-suspension lifecycle checks passed. Regression checks cover the original Rustallion front/rear lifted mount heights and manual height offsets. No game session was launched; the reported Rustallion behavior needs player confirmation after updating.

2.3.2: clean Release build; 13 focused picking/input/attachment-policy checks passed. Offline audit covered collider layouts of 206 native removable-part prefabs and 361 native socket FSMs, including all 16 forced-induction dispatch sockets. No game session was launched or interrupted; live pickup and mounting require player confirmation after restarting.
