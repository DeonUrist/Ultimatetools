Fixes a suspension calibration error that could put Rustallion near the ground with an unadjusted vanilla lift kit. The old vertical alignment raised the physical wheel mounts by 20 cm on the lifted family (10 cm on the buggy family), lowering the body. Vertical alignment now lowers the visible axle model instead, retaining native physical mount height and the accepted relative wheel/axle alignment. Fixed underbody pieces remain with the body.

Cars without a lift kit stay under native wheel-position control. Kit removal restores stock geometry once; subsequent native wheel callbacks cannot overwrite neighbouring stock wheels. Orphaned saved width/stance values without a kit are reset.

Width, height and front/rear balance controls remain available, with height now measured from the native lifted stance. Existing saved adjustments remain readable and are applied relative to that corrected baseline. No additional settings are introduced.

Validation: clean Release build and offline settings, stance and stock-suspension lifecycle checks passed, including original Rustallion lifted mount heights. No game launch was performed. Live Rustallion behavior awaits player confirmation.

Installation: extract the attached ZIP into BepInEx/plugins and restart. Remove older PartAdjustment.dll installations to avoid a duplicate plugin.
