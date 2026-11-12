APPROVED AND APPLIED
Add optional fixed-point registration to EnemySpawner.cs.
The new outpost reference component would register its six ground-level markers on enable and unregister on disable.
Existing wave counts, archetypes, ten-enemy cap, player-distance checks, safe zone checks, collision checks, route validation, and sunrise cleanup remain unchanged.
Each existing spawn attempt has a 25% chance to try a registered marker; otherwise it uses the current random-cell logic.
No second spawner or serialized change to GameSystems is needed.
The elevated tower marker is excluded because the current ground enemy AI cannot remain on a tower or fire arrows.
The user explicitly approved this exception: "Allow the small spawner update".
EnemySpawner.cs and GoblinOutpostReferences.cs now implement this registration.
All 12 Play Mode spawner checks passed; see Design/Outpost-SpawnerTests.txt.
EnemySpawner.cs.proposed is retained as the original review copy, not an additional runtime script.
