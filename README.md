# Ultimate Tools

Ultimate Tools 2.3.6 is a BepInEx mod for Apocalypter. It combines the utility toolbox and Part Adjustment toolbox into one wrench/screwdriver toolset, with native-looking HUD text and the game's existing bindings.

## Install

Install BepInEx first. Extract `UltimateTools-2.3.6.zip` into the game's `BepInEx/plugins` directory; it contains an `UltimateTools` folder. Restart the game.

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
| AbsoluteAdjustmentRotation | On |

| Headlights | Default |
| --- | --- |
| AttachAnywhere | On |
| TailLightChance | 8% |

While carrying an attachable car part near a car, compatible empty slots are highlighted even without the tool. Turn `VisibleGizmosWithoutTool` off to require the tool. No highlights are added for empty hands. Slot discovery runs at most once per second while a car part is held; only cached nearby markers update each frame. Passive markers have no active selection collider, so they do not intercept normal interactions. Occupied fixed sockets are excluded; multi-part sockets retain the native attachment recipe.

Normal 100% speed is 1 cm / 1 degree per item-adjustment press. Fast 1000% speed is 10 cm / 10 degrees. `AbsoluteAdjustmentRotation` on: screwdriver rotate hotkeys turn an attached part around the car's axes (a held part around the world axes); off: around the part's own axes. Old settings are migrated to the compact General/Headlights layout; put-away, modifier and headlight preferences are retained. ShowGuide and the former extra settings are removed. A hidden `Apocasetter` registration flag is retained for the Mods menu.

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

Part selection follows the camera-centre ray. Mounting and screwdriver socket selection can look through vehicle bodywork; scenery and foreground loose objects still block selection. Free lights and plates retain native contact-based attachment. Screwdriver hotkeys move attached items along car axes and held items along global axes; rotation follows `AbsoluteAdjustmentRotation`. Rotating a held item with the middle mouse button is always the native rotation, with or without the tool. Mounted wheel removal keeps the rolling hub's displayed pose. Tool-only vehicle entry/exit uses native controls; a real held part retains the native hand restriction.

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

Accepted alignment is built in: the lifted family uses X −0.10 m per wheel side and lowers only the visible axles by 0.20 m; the buggy family retains X 0 and its existing 0.10 m model alignment. Duke, Junker, PigPen, Rustallion, Vulture and Outrider keep their suspension bases and shields at the original prefab pose, including bases that parent the axles. Screwdriver height and balance move the axles, springs and physical wheel mounts relative to the fixed body. Widening preserves the base's original dimensions. Original prefab geometry takes precedence over displaced base/axle values in older saves. A freshly fitted lift kit leaves physical wheel-mount Y at the game's native values. Reset restores native ride height and preserves axle alignment. No DEBUG calibration menu, rear mesh swapping or custom centre-of-mass change is enabled.

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

Tail light spawn regression checks run native PlayMaker actions in a separate headless game copy under `research/tail-light-runtime`:

```powershell
.\verification\native\RunTailLights.ps1 -GameDir 'C:\Path\To\Apocalypter'
```

The checks cover all three headlight variants at 100%, 0%, shared and separate spawn variables, preservation of the source prefab and unique names, and exclusion of other items and non-spawner actions. No player save is loaded. The spawn fix preserves the prefab before vanilla `CreateObject` overwrites its shared input/output variable; previously every such spawn was rejected before the chance roll. This affects newly spawned world loot; existing saved headlights retain their type.

2.3.6: clean Release build; 26 focused native Unity checks passed for tail light spawning. The same regression fails against the previous spawn code. Existing saved headlights retain their type.

2.3.1: clean Release build; settings, stance and stock-suspension lifecycle checks passed. Regression checks cover the original Rustallion front/rear lifted mount heights and manual height offsets. No game session was launched; the reported Rustallion behavior needs player confirmation after updating.

2.3.2: clean Release build; 13 focused picking/input/attachment-policy checks passed. Offline audit covered collider layouts of 206 native removable-part prefabs and 361 native socket FSMs, including all 16 forced-induction dispatch sockets. No game session was launched or interrupted; live pickup and mounting require player confirmation after restarting.

2.3.5: middle-mouse rotation of a held item no longer changes while the wrench or screwdriver is out; new `AbsoluteAdjustmentRotation` setting for the rotate hotkeys. Settings checks passed; not yet confirmed in game.

2.3.4: clean Release build; all four offline check suites passed. Headless native Unity regression checks used 89 original prefab transforms from all nine supported cars. Checks covered fixed base/shield position, orientation and dimensions; axle/spring height and balance; widening and reset; and ignoring displaced saved geometry when the prefab is available. Buggy-family geometry retained its previous behavior. The fixture run did not load a player save or verify a driven vehicle visually.
