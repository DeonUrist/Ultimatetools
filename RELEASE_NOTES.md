Carrying an attachable car part near a car now highlights compatible slots without requiring a tool. VisibleGizmosWithoutTool is on by default; turning it off restores tool-only highlights. Empty hands and unrelated held items do not trigger discovery. The existing once-per-second scan is reused, with only nearby cached markers updated each frame.

Settings are reduced and reordered into General and Headlights. Normal/fast movement and rotation now share percentage controls: 100% retains 1 cm / 1 degree steps, and 1000% retains 10 cm / 10 degree steps. Put-away and Alt modifier settings are grouped under General; ShowGuide and other extra options are removed. Existing preferences migrate automatically.

The accepted wheel alignment, suspension width/height/front-rear balance, wrench removal and native attachment behavior remain available.

Validation: clean Release build and offline settings, speed, visibility and stance checks passed. No game launch was performed. New highlighting awaits player feedback.

Installation: extract the attached ZIP into BepInEx/plugins and restart. Remove older PartAdjustment.dll installations to avoid a duplicate plugin.
