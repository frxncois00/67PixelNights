# Ashen Frontier border refinement

Saved in `Assets/Scenes/SampleScene.unity` using Unity 6000.6.4f1.

The north and east borders now use irregular, staggered inlets and outward curves, with the existing ochre edge and corner tiles. Dry-soil patches end inside the revised outline. The central terrain, entrance trail, roads, shoreline, and environmental objects are preserved.

This requested outline refinement intentionally supersedes the earlier rectangular expansion footprint: 39 outer cells were trimmed and 2 land cells added. The resulting 721-cell footprint is connected, has no uncovered cells, and has no tiles over water.

Validation compared 6,405 non-tilemap components against the live pre-edit snapshot: zero changes. Existing Ashen Trails and Ashen Details tiles are unchanged. Missing scripts and missing serialized references: zero. Unity reports compilation completed without errors and zero current Console errors.

The disk backup predates an existing unsaved `Ground (1)` tilemap in the Editor. That object was already present in the live validation baseline and was preserved when saving; it was not created by the border refinement.

See `scene-overview.png` for the final Scene view and `validation.json` for the checks. The earlier coverage and Outpost gameplay Play Mode results remain in their respective Design folders. No gameplay code or colliders were changed for this visual refinement.
