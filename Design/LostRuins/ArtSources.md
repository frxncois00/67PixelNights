# Lost Ruins art

The scene uses the connected prefab at `Assets/LostRuins/LostRuinsBiome.prefab`.

**Original landmark artwork.** Two original transparent sprite sheets were generated for this region. The first prompt requested a front-facing three-quarter pixel-art gatehouse with twin watchtowers and crimson banners, ruined temple, collapsed house, watchtower, subterranean tomb entrance and red crystal dais. The second requested matching horizontal and north–south defensive walls, a breached round tower, a hooded warden statue, a roofless forge and an ivy arch. The direction specified dark blue-grey masonry, olive moss, clear silhouettes, a restrained palette and distinct shadow clusters. The sheets were cut to transparent sprites, reduced to the game's pixel scale and imported at 32 PPU with Point filtering and no compression. Rubble, the gate stair, the chipped column and the brazier plinth are smaller cutouts from the same art.

**Downloaded art.** BlightedAsh and OldOliveAsh use two individual frames of `Pixel Crawler – Free Pack 2.11/Environment/Props/Static/Trees/Model_03/Size_04.png` from the local Unity Assets collection. They use 64 PPU. Only the two selected frames were imported.

**Existing project art.** The regional ground, stone roads and pools reuse artwork from ForestTiles, DrownedGrove and AshenFrontier. Additional vegetation and debris use Tiny Swords and AshenFrontier. The grave markers and hanging standards use GoblinOutpost/DungeonProps. The braziers animate with the existing PixelCrawler Bonfire sheet. All biome renderers share the scene's ForestDayNightLit material.

**Navigation.** Enter from the east road near (20, −2) or the south road near (2, −18). The causeway and central fortress arch are passable. The three dark pools, wall sections, buildings and selected large props use static 2D obstacles recognized by ForestNavigation. The far-eastern crypt is an exploration landmark with accessible steps and a sealed inner passage.
