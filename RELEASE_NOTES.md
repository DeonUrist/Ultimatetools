Ultimate Tools 2.3.6 fixes red tail lights never appearing in world loot, even with TailLightChance set to 100%.

Newly spawned headlights now follow the configured chance for all three headlight variants. At 100%, every eligible newly spawned headlight becomes a red tail light; at 0%, they remain white. Existing saved headlights keep their current type.

The game reuses one variable for the source prefab and the spawned item. Ultimate Tools previously mistook them for the same object and skipped conversion before rolling the chance. The fix preserves the source before spawning.

Installation: extract UltimateTools-2.3.6.zip into BepInEx/plugins, replacing the existing UltimateTools folder, then restart.

Validation: Release build passed with zero warnings and errors. All four offline suites passed: settings, stance, stock suspension, and picking/attachment. All 26 focused native Unity checks passed, covering the three light variants, 100% and 0%, shared and separate spawn variables, unique names, source prefab preservation, and unrelated spawn actions. The original spawn code fails the regression. No player save was loaded.
