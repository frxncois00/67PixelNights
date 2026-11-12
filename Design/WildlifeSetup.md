# Passive wildlife setup

## Already configured

Open `Assets/Scenes/SampleScene.unity` and press Play. No manual setup is required for this scene.

The existing **WildlifeSpawner** replaces the previous chicken/sheep-only configuration. It uses the existing shared **PassiveAnimal** base behavior, **EnemyHealth**, **EnemyHitFeedback**, **WildlifeHarvest**, and **ResourcePickup**. There is no second combat or inventory system. Chickens are not in the roster; their original imported art remains available in the project.

Installed art: the Farm RPG FREE pack contains male/female cow and chicken sheets. The sheep comes from the existing Tiny Swords pack. The attached promotional picture shows more species than the installed pack contains; it is not an idle/walk animation sheet. Ducks, pigs, horses, highlands/bison, and ostriches have not been fabricated from that picture. No additional asset download is needed for the current three prefabs.

| Prefab in Assets/Wildlife/Prefabs | Health | Guaranteed drops | Chance drop |
| --- | --- | --- | --- |
| Sheep | 30 | Meat 1–2, wool 1–3 | None |
| Cow | 55 | Meat 2–4, leather 1–2 | Milk 1, 25% |
| Bull | 70 | Meat 3–4, leather 2 | Bone 1, 30% |

All three are passive and flee when damaged. Cows/bulls walk slowly and pause longer. AnimalData assets are in `Assets/Wildlife/Data`. Edit health, speed, roam radius, pauses, flee duration/multiplier, animation frames and drop chances there.

The population cap is **36** across the map's grass sectors. At night, living animals are removed without loot. At dawn, the population refills. During the day, a vacant slot is refilled every **30 game seconds**, outside a **6-unit radius** of the player. Blocked cells are skipped, so population can remain below the cap when space is unavailable. Pausing game time pauses respawning. Animal deaths and pickups are not saved across new play sessions, consistent with the existing game.

## Food and inventory

Kill an animal with the existing sword/bow attack. The existing hit flash, health bar and damage numbers appear when damaged. Walk up to a drop and use **E**. Select meat in the hotbar and use **F** to eat for 12 hunger. Near a burning campfire, **C** cooks meat for the existing 35 hunger/5 health benefit.

Six ItemDefinition assets were added and registered in the player's InventoryController: **feathers, leather, wool, egg, milk, bone**. Eggs restore 10 hunger; milk restores 18. The other four are stackable Utility materials (not wood/stone ResourceType aliases). Feathers and eggs are ready for future bird loot tables; the current cow/sheep roster does not drop them. Their simple pixel inventory symbols live in `Assets/Wildlife/Icons`. No new crafting recipes were added.

## Add another species in the Editor

1. Import the species' actual animation PNG from your Farm Animals pack into Assets. Use **Texture Type: Sprite (2D and UI)**, **Sprite Mode: Multiple**, **Filter Mode: Point**, **Compression: None**, **Mip Maps: Off**.
2. In Sprite Editor, slice using the real frame size, not the pack's general pixel-art label. The installed cows use **32 x 32 frames at 16 Pixels Per Unit**; the existing Tiny Swords sheep use **128 x 128 frames at 64 PPU**. Inspect other species before choosing their grid size. Apply.
3. Create **Assets > Create > Survival > Animal Data**. Enter the animal name and stats. Put side-facing idle frames in Idle Frames and side-facing walk frames in Walk Frames, in animation order. Cows currently use the first/top row of four frames; other rows face up/down and should not be mixed into a side-walk loop. Set Artwork Faces Left to match the source art. Flip-X handles movement direction. This implementation uses frame arrays, not an Animator Controller.
4. Add drop entries: drag an ItemDefinition into Item, enter Minimum/Maximum, and Chance from 0 to 1. Zero never drops, one always drops. A drop quantity is rolled once per entry and delivered as a pickup stack. Example pig: meat 2–3 at 1, leather 1 at .3. Example duck: meat 1 at 1, feathers 1–2 at 1, egg 1 at .2. Do not add chickens to the roster unless you intentionally want them back.
5. Duplicate one of the prefabs under Assets/Wildlife/Prefabs and rename it. Assign the new AnimalData to **PassiveAnimal > Data**. Set the SpriteRenderer's preview sprite to the first idle frame and EnemyHealth's preview Maximum Health to the data's health. Runtime values come from AnimalData. Set CircleCollider2D radius to the body's footprint; match AnimalData > Body Radius. Keep local scale at 1 and Rigidbody2D Kinematic, Gravity Scale 0, Freeze Rotation, Interpolate.
6. Keep **EnemyHealth**, **EnemyHitFeedback**, **WildlifeHarvest**, **PassiveAnimal**, SpriteRenderer, CircleCollider2D and Rigidbody2D. Do not add EnemyController: that is hostile wave AI. PassiveAnimal is the shared Animal base script in this project.
7. Add the new prefab to the scene's **WildlifeSpawner > Animal Prefabs** list. The spawner is the project's AnimalSpawner implementation. New items must also be added to **Player > InventoryController > Definitions** or inventory pickups will be rejected.

## Spawner, layers and terrain

The existing scene object named WildlifeSpawner is already wired to Ground, Player, Campfire and DayNightCycle. Its Grass Tiles list contains the nine actual Grass tile assets used on the main ground map. For a new map, assign its ground tilemap and explicit grass tile assets. Position of the spawner GameObject does not limit the spawn region: it samples the ground tilemap's full bounds in 36 sectors.

Prefabs use the existing **Default physics layer** with non-trigger CircleCollider2D and **Objects sorting layer**. This preserves melee, bow raycasts and existing world collision without changing the project's collision matrix. Movement checks the animal footprint against ground tiles and all non-trigger solid colliders; missing ground blocks water crossings, solid tree/rock colliders block props, and a campfire exclusion radius keeps wildlife out of the fire. Keep water absent from the walkable ground map and keep solid-prop colliders enabled. Trigger-only interaction areas do not block wildlife. If using a custom Wildlife physics layer later, include it in player/combat collision queries and permit world collisions in Physics 2D settings.

## Validation and maintenance

`AgentScripts/BuildWildlife.cs` creates/updates the three prefab/data assets, inventory items and scene references through Unity APIs. It also restores the standard roster and settings, so do not rerun it after custom tuning unless that is intended.

`AgentScripts/ValidateWildlife.cs` is a live Play Mode validation script. It modifies only the temporary play session (damage, loot, hunger, phase, respawn timing); exit Play Mode afterward. It checks the cap, no chickens, fleeing, shared feedback, loot exactly once, item registration, pickup, hunger restore, night removal without loot, and dawn refill. A follow-up live check confirmed timed respawn restores the cap. The test scene was stopped afterward, leaving saved gameplay settings at their normal values.

Run with the installed CLI from the project folder:

```
unity command editor.playmode start
unity command script run --file AgentScripts/ValidateWildlife.cs --entry ValidateWildlife.Main
unity command editor.playmode stop
```
