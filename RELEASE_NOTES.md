Ultimate Tools 2.3.3 prevents a previously adjusted mount from moving alongside the next selected mount.

The active native adjustment mount is retained independently of the cursor target. Finishing, switching tools, putting the tool away or starting another adjustment explicitly deactivates it. Other native mount FSMs cannot consume adjustment input while the tool is active. Suspension and held-part adjustment also release the previous native mount.

The cursor ray already stops at the first eligible part or mount. Selection through vehicle bodywork remains available; overlapping engine and radiator hits do not select multiple targets.

Validation: Release build passed with no warnings or errors. All 17 focused picking, attachment and exclusive-adjustment checks passed, including engine-to-radiator switching, finish and missing-target cleanup. Live behavior needs confirmation in game after restarting.

Installation: extract UltimateTools-2.3.3.zip into BepInEx/plugins, replacing the existing UltimateTools folder, then restart. Existing settings and saves remain compatible.
