# Forest Outpost gameplay — 10 November 2026

Scene: `Assets/Scenes/SampleScene.unity`, Unity `6000.6.4f1`.

The pictured red-banner camp is `EnvironmentExpansion/GoblinOutpost/GoblinCamp`. It had no encounter controller, so a new instance of the existing `AshenFrontierEncounterController` was added here. The separate Ashen Frontier and Drowned Grove controller configurations are unchanged.

- Seven guard slots use existing Rogue Scout, Raider and Captain prefabs. Five may be active locally; both night waves and encounters honor the scene's shared `EnemySpawner.maximumActive` (currently 28).
- Ordered patrols are optional. Unassigned slots retain random patrols. The bandit camp uses constrained navigation, slot leashes and the existing search budget. The tower-foot sentry holds one ground waypoint.
- Local safe areas cover the marketplace, upper cabin and pond/dock. They exclude hostile spawning, navigation and attacks without changing `CampfireSafeZone` or blocking player movement.
- The existing neutral trader offers **E: Trade** and **T: Next offer**: 5 wood → 1 cooked meat (stock 6), 6 scrap → 1 pickaxe (stock 2), 4 stone → 3 berries (stock 5). These use registered items and atomic `InventoryController.CraftBatch` transactions.
- Guarded camp/quarry chests can be opened during combat. Existing chest logic retains loot that does not fit. The existing ruins chest was placed behind the upper cabin as a concealed cache.
- The captain retains its existing 115 HP archetype, compared with 38 HP scouts and 55 HP raiders. It drops 6 scrap, 2 cooked meat and 1 sword once per scene session. This camp's captain does not respawn on a new day. No new save system was introduced.
- The upper cabin's rear loose-plank entrance opens with E. Its collider is disabled, navigation is invalidated, and the concealed chest becomes interactable. New fence pieces use the existing timber-fence sprite.

Validation evidence:

- `validation.json`: zero missing scripts/references; existing sprite renderer artwork and other encounter configurations unchanged; original Ashen expansion has zero uncovered target cells and zero tiles over water.
- `setup-validation.json`: every patrol segment has a valid ground route; closed/open barrier navigation; registered chest rewards.
- `playtest-results.json`: 32 passing interaction, inventory, AI, reward, chest and night-wave checks.
- `walking-loop.json`: actual keyboard/Rigidbody2D traversal through 13 route legs, captain defeated through eight equipped-sword attacks and the animation-driven combat code, followed by guarded and concealed loot collection.
- `grove-regression.json`: Drowned Grove enemies still activate and use their original patrol behavior.
- `cap-test.json`: a runtime cap of 2 admits exactly 2 guards; the configured scene value remains 28.
- `gate-cross.json`: player movement through the opened barrier.

The automated movement/combat checks temporarily isolated encounters and restored player health to test mechanics separately from combat balance. These runtime test settings are discarded on leaving Play Mode. No test scripts were placed in Assets; verification helpers live under Temp/OutpostGameplay.

Original scene and edited script backups are under `Design/Backups/OutpostGameplay-20261110`. Earlier Ashen coverage backups and traversal evidence are under `Design/Backups/AshenCoverage-20261110` and `Design/AshenCoverage-20261110`.
