Ultimate Tools 2.3.7 fixes held items disappearing when putting the tool away and adds progressive damping for raised suspension to address shaking at high ride heights.

Pressing X now drops a held item at its current position before putting away the wrench or screwdriver. The native drop sequence restores collisions, gravity and inherited vehicle velocity. Rotation and scroll movement are covered, and empty hands leave nearby items alone. Using a toolbox to put the tool away follows the same drop behavior.

Each axle's bump and rebound damping decrease linearly from 100% of the native lift-kit rates at zero added height to 25% at +15 cm. Intermediate examples: +5 cm retains 75%, +10 cm retains 50%. Front/rear balance is included separately for each axle. Lowering retains native damping. Width alone does not change damping.

Rates are calculated from the original per-wheel values, so repeated adjustments do not compound the reduction. Reset, kit removal, global disable and component shutdown restore values still controlled by Ultimate Tools. Native stock/lifted transitions establish fresh baselines; external damping edits are respected. Existing saved height and balance require no migration. Spring strength, travel and center of mass remain native.

Installation: extract UltimateTools-2.3.7.zip into BepInEx/plugins, replacing the existing UltimateTools folder, then restart. Disable DukeDamperTrial when testing to avoid compounding damping reductions.

Validation: Release build passed with zero warnings and errors. All five offline suites passed, including 179 progressive damping checks. All 20 focused native Unity checks passed against actual NWH components, covering the curve, axle balance, repeated updates, reset, kit removal/refit, disable/re-enable, external edits, replacement dampers, stock transition and unchanged spring/curve settings. Verification ran in an isolated local game runtime without opening a player save. Actual driving and high-lift shake reduction still need player testing.

Put-away validation: 110 focused native Unity checks passed for both toolboxes, held/rotate/scroll states, stale controller caches, native release events, restored physics and world pose, release ordering, empty hands and hovered items. The full native regression suite passed all 2,330 checks.
