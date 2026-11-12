using System.Text;
using UnityEngine;

return VerifyWaterworksPlayMode.Main();

public static class VerifyWaterworksPlayMode
{
    public static string Main()
    {
        var sb = new StringBuilder();
        var player = Object.FindAnyObjectByType<PlayerInteractionController>();
        var spawner = Object.FindAnyObjectByType<EnemySpawner>();
        var nav = Object.FindAnyObjectByType<ForestNavigation>();
        var grove = GameObject.Find("EnvironmentExpansion/DrownedGrove")?.GetComponent<AshenFrontierEncounterController>();
        var ashen = GameObject.Find("EnvironmentExpansion/AshenFrontier")?.GetComponent<AshenFrontierEncounterController>();

        sb.AppendLine($"PlayMode={Application.isPlaying}");
        if (!Application.isPlaying || player == null || grove == null)
            return "ERROR: Must be in Play Mode!";

        // 1. Test Waterworks Diagram Clue at Pumpkeeper's Cabin
        var diagram = Object.FindAnyObjectByType<WaterworksDiagramClue>();
        player.transform.position = diagram.transform.position + new Vector3(0f, -0.8f, 0f);
        diagram.Use(player);
        sb.AppendLine($"1. Diagram Clue Notice: '{player.message.text}'");

        // 2. Test Sluice Shaft Entrance interaction
        var shaft = Object.FindAnyObjectByType<SluiceShaftEntrance>();
        player.transform.position = new Vector3(-27.25f, 16.0f, 0f);
        shaft.Use(player);
        sb.AppendLine($"2. Sluice Shaft Grate Opened: {shaft.grateOpened} | Notice: '{player.message.text}'");

        // 3. Test Drainage Wheel interaction & bounded channel payoff
        var wheel = Object.FindAnyObjectByType<SluiceDrainageWheel>();
        bool channelBefore = spawner.Walkable(new Vector2(-18.5f, 16.0f));
        player.transform.position = wheel.transform.position + new Vector3(0f, -0.8f, 0f);
        wheel.Use(player);
        bool channelAfter = spawner.Walkable(new Vector2(-18.5f, 16.0f)) && spawner.Walkable(new Vector2(-17.5f, 16.0f));
        sb.AppendLine($"3. Drainage Wheel Drained: {wheel.isDrained} | Channel Walkable Before={channelBefore} -> After={channelAfter} | Notice: '{player.message.text}'");

        // 4. Test Proximity Spawning & Cap Enforcement at Flooded Pump Station / Gauge Shrine
        player.transform.position = new Vector3(-13.2f, 11.2f, 0f);
        Physics2D.SyncTransforms();
        grove.EvaluateEncounters();
        sb.AppendLine($"4. Drowned Grove Active Local={grove.LocalActiveCount} (Max={grove.maxConcurrentLocalEnemies}) | Global={spawner.TotalActiveCount} (Max={spawner.maximumActive})");

        // 5. Verify Sluice Crab frontal shell guard vs exposed side damage
        var crabPrefab = grove.rogueSlots[5].prefab;
        var testCrab = Object.Instantiate(crabPrefab, new Vector3(-12.5f, 9.5f, 0f), Quaternion.identity);
        testCrab.world = spawner;
        var crabHp = testCrab.GetComponent<EnemyHealth>();
        var crabLake = testCrab.GetComponent<SunkenWaterworksCreature>();
        float hp0 = crabHp.CurrentHealth;
        // Frontal hit while shell guard is up (player in front of default down-facing crab)
        crabHp.TakeDamage(20f, new Vector2(-12.5f, 8.5f), 0f);
        float frontalDmgTaken = hp0 - crabHp.CurrentHealth;
        // Flank/rear hit from behind shell
        float hp1 = crabHp.CurrentHealth;
        crabHp.TakeDamage(20f, new Vector2(-12.5f, 10.8f), 0f);
        float flankDmgTaken = hp1 - crabHp.CurrentHealth;
        Object.Destroy(testCrab.gameObject);
        sb.AppendLine($"5. Sluice Crab Shell Guard: frontalDmg={frontalDmgTaken:F1} (expected 7.0) vs flankDmg={flankDmgTaken:F1} (expected 23.0)");

        // 6. Verify Floodgate Keeper Boss surge & weak-point bonus damage
        var bossSlot = grove.rogueSlots[12];
        var boss = bossSlot.instance != null ? bossSlot.instance : Object.Instantiate(bossSlot.prefab, new Vector3(-12.0f, 10.9f, 0f), Quaternion.identity);
        boss.world = spawner;
        var bossHp = boss.GetComponent<EnemyHealth>();
        var bossLake = boss.GetComponent<SunkenWaterworksCreature>();
        // Trigger attack & execute surge to open weak-point window
        boss.customBeginAttack?.Invoke(new Vector2(-12.0f, 9.0f));
        boss.customExecuteAttack?.Invoke(new Vector2(-12.0f, 9.0f));
        bool weakPointOpen = bossLake.IsExposedWindowActive;
        float bhp0 = bossHp.CurrentHealth;
        bossHp.TakeDamage(20f, new Vector2(-12.0f, 9.5f), 0f);
        float weakPointDmg = bhp0 - bossHp.CurrentHealth;
        sb.AppendLine($"6. Floodgate Keeper Weak-Point Window Open={weakPointOpen} | WeakPointDmg={weakPointDmg:F1} (expected 30.0 for 20 base)");

        // 7. Verify Ashen Frontier Regression
        if (ashen != null)
        {
            player.transform.position = new Vector3(-22.0f, -15.0f, 0f);
            Physics2D.SyncTransforms();
            ashen.EvaluateEncounters();
            sb.AppendLine($"7. Ashen Frontier Regression Check: ActiveLocal={ashen.LocalActiveCount}/{ashen.maxConcurrentLocalEnemies} TotalSlots={ashen.TotalSlots}");
        }

        // Move player back to Flooded Pump Station / Gauge Shrine overlook for screenshot
        player.transform.position = new Vector3(-14.8f, 12.2f, 0f);
        Physics2D.SyncTransforms();
        grove.EvaluateEncounters();

        return sb.ToString();
    }
}
