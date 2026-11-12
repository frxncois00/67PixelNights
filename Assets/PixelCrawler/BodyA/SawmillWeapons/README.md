# Body_A sawmill weapon animations

Six equipped weapons: wood knife, spear, shovel, long sword, long hammer and bow.
The fence and the original basic axe/pickaxe/sword are not part of this set.

Each weapon folder contains Idle, Walk and Attack sequences for Down, Up and Side.
Side faces right; the game mirrors it for left. Each sequence has an editable
five-layer `.aseprite` source and a transparent `-Sheet.png` export. Unity creates
the associated `.meta` files. PNG sheets are the authoritative in-game sprites.

Cells are 96 x 96, at 32 pixels per unit, Point filtering and no compression.
Feet pivot: (0.5, 1/3). The larger canvas adds space for the weapons while keeping
the original Body_A character size and feet position. Idle has 4 frames, Walk 6,
and Attack 8. Source attack durations match the game's existing weapon timing.

Unity clips: `Assets/Animations/Player/SawmillWeapons/<weapon>/`.
The Player's `SawmillWeaponAnimations` component references the sliced frames.
`EquippedToolVisual` selects idle/walk frames, and `PlayerGatherAnimation` plays
attack frames and dispatches the original impact callback. Damage, projectiles,
cooldowns and inventory ownership remain controlled by the existing systems.
Clips are provided for animation preview/editing; runtime playback uses the
serialized sprite arrays so it can integrate with the existing action coroutine.

Art sources: existing Pixel Crawler Body_A layers and the existing sawmill weapon
icons. Body poses and legs are reused. Weapon grips are aligned to the leading
hand in each frame. Bow draw/release arm pixels use the character's skin palette.
Original pack files have not been overwritten. Existing source-asset license
terms still apply to these derivatives.

Rebuild from project root:
1. `python AgentScripts/build_sawmill_sheets.py` (requires Python and Pillow).
2. Refresh Unity, then in Edit Mode run `AgentScripts/SetupSawmillAnimations.cs`,
   entry `SetupSawmillAnimations.Main`, through the existing Unity CLI.
   This assigns the Player component and saves SampleScene.

If editing Aseprite by hand, export horizontal sheets with all frames on the same
96-pixel canvas. Do not rerun the Python generator afterward unless you intend to
replace those manual edits. Reimport sheets and rerun setup when frame counts change.

The source reader/writer follows the official Aseprite file specification:
https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md
