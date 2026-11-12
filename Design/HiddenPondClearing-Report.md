# Hidden Forest Pond Clearing — completed in SampleScene

Saved November 7, 2026 (Asia/Shanghai). Unity 6000.6.4f1, project `My project`.

## Placement and reuse

- Scene: `Assets/Scenes/SampleScene.unity`.
- Hierarchy: `Grid/HiddenPondClearing`, with `Terrain`, `Water`, `Structures`, `Props`, and `Vegetation`.
- Prefab: `Assets/Prefabs/Environment/HiddenPondClearing.prefab`, connected to the scene instance.
- Prefab origin / clearing center: **(-19, 14, 0)**. Organic footprint approximately **29 × 21 world tiles**, with a south-east exclusion and an approach extending to the west road. Existing ground cell size remains 1 world unit.
- Cabin doorstep: **(-25.5, 17)**. Yard approach: **(-25.5, 16)**.
- Road connection: **(-27, 1)** on the west arm of the existing east–west road, then a winding path northwest to the yard and around the pond's west side.
- West bridge crossing: **x = -24.5, y = 12.5–15.5**. South bridge crossing: **y = 6.5, x = -20.5–-15.5**.
- All added tile cells lie on existing island ground. The nearest new tile center is 11.34 units from the camp center, beyond the current 5-unit safezone. No central roads or central water-ring tiles were edited.
- To reuse, drag the prefab onto another location covered by the project's navigation ground tilemap. Its origin is at the clearing center; its children preserve their relative grid alignment.

## Imported assets

Only these eight PNG sheets were imported into `Assets/PixelCrawler/Environment/HiddenPondClearing/`. No whole packs, character assets, animations, or package scenes were imported.

| Pack | Source file within pack | Project filename / use |
| --- | --- | --- |
| Cute Fantasy Free | `Outdoor decoration/House_1_Wood_Base_Blue.png` | `Cabin.png` — timber cabin, door, chimney |
| Cute Fantasy Free | `Outdoor decoration/Bridge_Wood.png` | `Bridges.png` — planks and rail/post accents |
| Cute Fantasy Free | `Outdoor decoration/Outdoor_Decor_Free.png` | `Decor.png` — flowers, mushrooms, grass and leaf pads |
| Cute Fantasy Free | `Tiles/Water_Tile.png` | `Water.png` — pond, stream banks and underwater stones |
| Pixel Crawler Free Pack 2.11 | `Environment/Props/Static/Vegetation.png` | `Vegetation.png` — bushes, ferns and reeds |
| Pixel Crawler Free Pack 2.11 | `Environment/Props/Static/Resources.png` | `Resources.png` — hay and fallen timber |
| Pixel Crawler Free Pack 2.11 | `Environment/Props/Static/Tools.png` | `Tools.png` — axe |
| Pixel Crawler Free Pack 2.11 | `Environment/Structures/Buildings/Props.png` | `BuildingProps.png` — timber hay-sled bed |

The existing Tiny Swords `Terrain/Tileset/Water Foam.png` was duplicated locally as `CascadeFoam.png` solely to define new cropped ribbon/ripple sprites without changing its original importer. Trees, stumps, boulders, woodpile logs, grass, dirt, cliff artwork and the shared `ForestDayNightLit` material are reused from existing project assets.

New pack imports use **32 PPU**, matching the existing PixelCrawler BodyA character artwork; existing environment sprites retain their **64 PPU** imports, as does the copied foam. Every texture uses Point filtering, no compression and no mipmaps. All scene object scales are (1,1,1). Ground/path/cliff tilemaps retain 1-unit cells; water and bridge detail use smaller nested grids for native-size sprites. Existing import settings were not modified.

The downloaded Cute Fantasy Free readme specifies **non-commercial use**. Pixel Crawler's bundled terms permit commercial projects. Copies of both supplied license texts are retained in `Design/HiddenPond-Licenses/`; no license or ownership assumptions were made beyond those supplied files.

## Added and modified objects

- **286 new GameObjects**, including 268 sprite objects and six tilemaps with their grid/group parents.
- Tilemaps: `Clearing Ground`, `Winding Dirt Paths`, `Mossy Cliff Ledges`, `Pond and Stream`, `Water Obstacles - Bridge Openings`, and `Walkable Plank Bridges`.
- Cabin, woodpile, chopping stump/axe, timber hay sled, old stumps, fallen timber, boulders, pines, broadleaf trees, bushes, ferns, reeds, flower patches, leafy water pads and two static cascade effects.
- **101 existing trees and 1 existing rock were deactivated**, not destroyed. They overlap the clearing or approach; their transforms, components and serialized references are retained. Every affected hierarchy path and coordinate is listed in `Design/HiddenPond-OverlapChanges.txt`.
- All **1,276 original scene objects** were checked against the saved live-scene snapshot. Only the listed activation flags and the Grid's new child reference changed.
- Existing harvesting and nearby-stone scripts already ignore inactive objects, so retaining the deactivated resource objects does not create invisible harvesting targets or require modifying player references.
- Hash comparison of every pre-existing file under Assets, ProjectSettings and Packages found **SampleScene.unity was the only changed existing file**. No existing scripts, prefabs, other scenes, material assets, importers or project settings changed.

## Gameplay and verification

- Pond/stream water and cliffs have TilemapCollider2D + CompositeCollider2D, with static Rigidbody2D. The water obstacle map has deliberate bridge openings; its renderer is disabled.
- Cabin, boulders and large trees use solid BoxCollider2D components. Tree colliders cover trunks rather than canopies.
- Existing Objects-layer pivot Y-sorting and the existing shared sprite material are reused. Ground decorations and bridge decks render on Ground at appropriate orders.
- ForestNavigation is collider-based, not a baked NavMesh. Its cache was invalidated after edits. No navigation script modification or separate NavMesh bake was needed.
- Navigation finds valid routes from the main road to the cabin, west bank, south crossing and east bank. Full bridge crossings were sampled at 0.25-unit intervals using the navigation system's 0.27-radius collision clearance.
- Pond, stream, cabin and cliff obstacle probes pass. Both bridges and the cabin route also pass in Play Mode. The player's existing capsule is 0.38 × 0.30 units.
- Zero missing sprites/scripts/materials; supported shared material on every renderer; all new tile cells on existing ground; no new objects in the safezone.
- Unity's actual Console counts after Play Mode and after returning to Edit Mode: **0 errors, 0 warnings**, with no compilation failure. Historical Pipeline buffer counters are older-session totals and were not erased.
- Final scene is saved, selected in Scene view, and left outside Play Mode.

Evidence: `HiddenPond-Validation.txt`, `HiddenPond-PlayMode.txt`, `HiddenPond-Preservation.txt`, and `HiddenPond-FilePreservation.json` in this folder. Preview: `HiddenPondClearing-Preview.png`.

## Reference differences and substitutions

- The available cabin is a blue-roofed timber house with a chimney and wooden door, rather than the reference's wide log cabin.
- A timber **hay sled** substitutes for the wheeled hay cart; the available cropped hay bundles are small.
- A **weathered stump** substitutes for the hollow log. Fallen timber is simpler and smaller than the reference's large fallen trunks.
- Small leafy aquatic sprites substitute for round lily pads. Existing broadleaf trees have smaller canopies than the reference's large spreading trees.
- Existing grass-topped cliff tiles form staggered ledges rather than the reference's moss-covered masonry walls and arches.
- Cascades are static compositions of existing foam sprites. There is no new animated waterfall, smoke effect, cabin interior or gameplay interaction script.
- The existing project's mixed art palette is preserved; this is an asset-constrained interpretation, not a pixel-for-pixel reproduction of the reference.

## Backups and editing tool

`Design/Backups/HiddenPondClearing/SampleScene.unity` preserves the original disk scene; `LiveScene.unity` preserves the editor's live scene immediately before mutations. The folder also contains the original file hashes and scene-object snapshot.

`AgentScripts/HiddenPondClearing.cs` is an editor-only, ephemeral Unity Pipeline builder and validator outside Assets. It is not compiled into or attached to the game. Its mutation stages are one-shot construction steps; use the prefab for normal reuse, not a rerun of those stages over an existing instance.
