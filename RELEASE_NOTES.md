Ultimate Tools 2.3.4 fixes suspension-kit bases sitting too low on Duke, Junker, PigPen, Rustallion, Vulture and Outrider.

These cars now keep the suspension base and shields in their vanilla body pose. Screwdriver height and tilt move only the suspension axles, springs and physical wheel mounts. Bases that contain axles as children are handled correctly, and widening preserves the base's original dimensions. The three buggy suspension families retain their existing behavior.

Original prefab geometry overrides displaced base and axle baselines from older saves. Width, height and balance remain save-compatible. Physical wheel-mount Y remains native until the player adjusts height.

Validation: Release builds passed with no warnings or errors. All four offline check suites passed. Native Unity geometry checks exercised 89 original prefab transforms from all nine supported cars across widening, height, balance and reset, including stale saved geometry. The headless fixture run did not load a player save; driving and visual clearance still need player confirmation.

Installation: extract UltimateTools-2.3.4.zip into BepInEx/plugins, replacing the existing UltimateTools folder, then restart.
