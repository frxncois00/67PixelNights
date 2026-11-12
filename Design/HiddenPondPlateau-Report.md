# HiddenPondClearing — Tiny Swords plateau redesign

Completed November 7, 2026, in SampleScene, project `My project`.

The existing `Grid/HiddenPondClearing` instance now uses a stepped lower plateau, a cabin terrace to the north-west, and a connected L-shaped raised terrace to the east. The prefab at `Assets/Prefabs/Environment/HiddenPondClearing.prefab` is updated. The lake, cabin, both plank bridges, all 134 existing flower sprites and the leafy water plants were retained. Flowers increased to 147 sprites.

## Assets used

All new terrain and supporting artwork comes from the existing `Assets/Tiny Swords/Terrain`:

- `Tileset/Tilemap_color3.png`: softer green grass, outlined edges, grey-blue cliff faces and native grass slope pieces.
- `Tileset/Tilemap_color4.png`: yard and approach path.
- `Tileset/Water Background color.png` and `Tileset/Water Foam.png`: water, shore fringe and static cascade accents.
- `Decorations/Bushes/Bushe1–4.png`, `Decorations/Rocks/Rock1.png` and `Rock3.png`, and `Decorations/Rocks in the Water/Water Rocks_01–04.png`.
- Existing Tiny Swords trees and cabin woodpile sprites remain where they fit.

**No files were imported from Downloads.** The downloaded terrain sheets were hash-identical to the existing project copies. Three unchanged local copies of grass, path and foam sheets were placed in `Assets/ForestTiles/HiddenPondPlateau/Source` for dedicated slicing, so the original importers were not modified. The copies use **64 PPU, Point filtering, uncompressed textures, no mipmaps, full rectangular sprite meshes**, and unit object scales.

The retained cabin, flowers, pads and bridge planks retain their existing artwork as explicitly requested. No new assets from other packs were imported. The cascade ribbons reuse the previous clearing's cropped Tiny Swords foam sprites.

## Scene changes

- Kept the existing parent and its Terrain, Water, Structures, Props and Vegetation groups.
- Replaced the old grass, lake, path and isolated cliff layout with dedicated **Water, Foam, Ground, Ground - Upper Tiers, Cliffs (upper tier), Grass Ramps, Path, Decor** tilemaps, plus hidden cliff-side collision strips and the retained plank bridge tilemap.
- Built **83 raised ground cells** and two ramps, each two cells wide and two cells long. Added a detached lake islet, stepped inlets, native bushes and water stones.
- Moved the cabin and its woodpile onto the north-west terrace. Kept and rearranged all existing flower sprites, adding 13 additional flower sprites in small clusters.
- Retained six clearing trees. Removed 21 trees belonging to HiddenPondClearing that overlapped new terrain or crowded the tier layout.
- Removed the previous clearing's non-Tiny-Swords hay sled, axe, fallen timber, auxiliary grasses, mushrooms, reeds, ferns and bushes. Removed obstructing stumps/boulders and replaced supporting details with Tiny Swords artwork.
- Rebuilt both plank decks while retaining their original plank assets; removed the two redundant rail/post overlays.

The construction change log is `Design/Plateau-Changes.txt`; the final cabin coordinate below supersedes its intermediate construction coordinate. No source sprite files were deleted.

## Coordinates and connections

| Feature | World coordinates |
| --- | --- |
| Existing prefab origin | **(-19, 14)** |
| Main road entry | **(-27, 1)** |
| Cabin anchor | **(-25.5, 20.75)** |
| Clear cabin approach | **(-25.5, 19.5)** |
| North-west ramp | **x -23 to -21, y 17 to 19**; centerline x = -22 |
| Eastern ramp | **x -15 to -13, y 16 to 18**; centerline x = -14 |
| Western bridge | Centerline **x = -24.5**, crossing y 12–15.5 |
| Southern bridge | Centerline **y = 6.5**, crossing x -21–-16 |
| Northern stream / cascade | Approximately **x = -18.5**, falling into the lake near y 18 |
| Southern outflow | Approximately **x = -18.5**, continuing below the south bridge |

The entry path runs north from the existing west road, follows the west lake bank, and reaches the broad landing below the cabin ramp. The south bridge connects the western bank to the eastern lower plateau. The east ramp reaches the upper terrace and its L-shaped south extension.

## Verification and preservation

- Water and cliffs use TilemapCollider2D plus CompositeCollider2D and static Rigidbody2D components. Quarter-cell hidden collision strips prevent climbing the back and sides of upper tiers. Ramp and bridge openings remain clear.
- Existing ForestNavigation uses these colliders; its cache was invalidated. No NavMesh package, navigation script or gameplay system was changed.
- Routes from the main road to the cabin, both raised tiers and both bridges pass. Both ramps and bridges pass continuous clearance sampling at 0.125-unit increments. Closing either ramp with a temporary test obstacle makes its tier unreachable, confirming there is no cliff-side bypass.
- The same destinations and water/cliff obstacle checks pass in Play Mode. Tests restore the temporary simulated obstacle immediately.
- All new terrain cells have existing navigation ground beneath them. All new props and tiles are outside the safezone.
- Cliff faces render individually on the existing Objects sorting layer. Tree and cabin pivots use the scene's existing Y-sorting. Ground details stay on Ground. Every renderer uses the existing supported shared sprite material.
- No missing sprites, materials or scripts; no stretched object scales.
- **All 1,276 objects outside HiddenPondClearing match the original serialized snapshot.** Existing deactivated forest objects from the previous task were not changed.
- File hashes show **HiddenPondClearing.prefab is the only changed pre-existing project file**. SampleScene.unity is byte-identical to its backup because its existing connected prefab instance picks up the updated asset. Other prefabs, scripts, scenes, settings, materials and importers remain unchanged.
- Final Unity Console: **0 errors, 0 warnings**. One temporary documentation-camera capture command initially failed; the capture was corrected, its diagnostic retained in `Plateau-CaptureDiagnostic.txt`, and the resolved tool message cleared. No gameplay errors were observed.

Evidence: `Plateau-Validation.txt`, `Plateau-PlayMode.json`, `Plateau-FilePreservation.json`, `Plateau-Ramps.csv`, `Plateau-Faces.csv`, `Plateau-Land.csv` and `Plateau-Water.csv`. Preview: `HiddenPondPlateau-Preview.png`.

## Reference differences

- The available Tiny Swords sheets all contain painted grass texture. The softer color3 variant replaces the previous high-contrast grass, but it is **not completely texture-free**. No external or generated replacement art was introduced.
- To preserve the surrounding scene, the redesign stays within the existing clearing footprint: approximately **28 × 22 tiles** for the terrain/water envelope, plus the road approach. It was not enlarged to 34 × 26 into neighboring forest.
- Height is conveyed with 2D cliff art and collision boundaries, consistent with this scene's existing top-down navigation. There is no new vertical physics or elevation system.
- Waterfalls and shore foam are static native-art compositions. The existing timber cabin and leafy water-pad substitutes remain as requested.

Backups: `Design/Backups/HiddenPondPlateau/`. Authoring/validation code: `AgentScripts/HiddenPondPlateau.cs`, outside Assets and never attached to a game object. Its construction stages are not intended to be rerun over the finished prefab; use the prefab for reuse.
