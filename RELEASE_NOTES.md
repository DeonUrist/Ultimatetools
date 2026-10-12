Ultimate Tools 2.3.2 fixes wrench removal of mounted steering wheels, turbos and superchargers. Native attached trigger colliders are accepted. Parts such as gear levers that disable their pickup collider while mounted receive temporary tool-only selection shapes using the original collider dimensions. Cassettes remain hand-only.

Aimed removable parts now take priority over an overlapping Drive (F) trigger and display the correct tool hint. Normal vehicle entry and exit still use native controls when no part is targeted.

Holding a turbo highlights compatible empty mounts on nearby engines at the turbo's native mounting pose. Superchargers use their own pose. Both use the game's type-specific attachment recipes; occupied nested hinges are excluded. Native forced-induction cleanup still runs when removing a turbo or supercharger.

Validation: clean Release build and 13 focused picking/input/attachment-policy checks passed. The offline audit inspected 206 native removable-part collider layouts and 361 native attachment FSMs, including 16 forced-induction sockets. No game launch or interruption was performed; live behavior requires player confirmation after updating.

Installation: extract UltimateTools-2.3.2.zip into BepInEx/plugins, replacing the existing UltimateTools folder, then restart. Remove older PartAdjustment.dll installations to avoid a duplicate plugin. Existing settings and saves remain compatible.
