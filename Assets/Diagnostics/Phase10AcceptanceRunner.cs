using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Profiling;

// Opt-in development-player acceptance run. Never added to the saved scene.
// Start the development build with --phase10-acceptance <absolute-report-path>.
public class Phase10AcceptanceRunner : MonoBehaviour
{
    string output;
    EnemySpawner spawner;
    readonly List<string> results = new List<string>();
    readonly List<double> frames = new List<double>(12000), cpu = new List<double>(12000), gc = new List<double>(12000);
    readonly string[] markerNames =
    {
        "Forest.Navigation",
        "Forest.EnemyAI",
        "Forest.EnemyPhysics",
        "Forest.SpawnValidation",
        "Forest.EnemyAnimation"
    };
    ProfilerRecorder mainRecorder, gcRecorder;
    ProfilerRecorder[] markers;
    double[] markerTotals, markerMax;
    int samples, maxEnemies, highestWave, runtimeErrors;
    float maxSpawn;
    bool sampling;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!Debug.isDebugBuild)
            return;
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "--phase10-acceptance");
        if (index < 0 || index + 1 >= args.Length)
            return;
        var runner = new GameObject("Phase10 Development Acceptance").AddComponent<Phase10AcceptanceRunner>();
        runner.output = args[index + 1];
    }

    IEnumerator Start()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Application.logMessageReceived += Log;
        yield return new WaitForSecondsRealtime(3);
        spawner = FindAnyObjectByType<EnemySpawner>();
        if (!spawner)
        {
            Finish("FAIL missing spawner");
            yield break;
        }

        // Stop external movement input from changing a reproducible sample.
        spawner.player.GetComponent<PlayerMovement>().enabled = false;
        spawner.player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        spawner.player.transform.position = new Vector3(-1.8f, -1.3f, 0);
        spawner.player.GetComponent<Rigidbody2D>().position = spawner.player.transform.position;
        Physics2D.SyncTransforms();
        while (spawner.cycle.CurrentDay < 10 || spawner.cycle.IsNighttime)
            spawner.cycle.SkipPhase();
        mainRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        markers = markerNames.Select(n => ProfilerRecorder.StartNew(ProfilerCategory.Scripts, n, 1)).ToArray();
        markerTotals = new double[markers.Length];
        markerMax = new double[markers.Length];
        spawner.cycle.SkipPhase(); // Night10, natural 120-second progression starts here.
        sampling = true;
        float deadline = Time.realtimeSinceStartup + 130;
        while (spawner.cycle.IsNighttime && Time.realtimeSinceStartup < deadline)
        {
            if (spawner.safe.fire.CurrentFuel < 50)
                spawner.safe.fire.TryAddFuel(new TestWood());
            yield return null;
        }

        sampling = false;
        results.Add((spawner.cycle.IsDaytime && spawner.cycle.CurrentDay == 11 ? "PASS " : "FAIL ") + "Natural Night10 -> Day11 at normal night duration");
        results.Add((maxEnemies == 10 && highestWave == 3 ? "PASS " : "FAIL ") + "Three waves reach ten live enemies; max=" + maxEnemies + " wave=" + highestWave);
        results.Add((spawner.ActiveCount == 0 && spawner.PendingCount == 0 ? "PASS " : "FAIL ") + "Sunrise removes active and queued enemies");
        results.Add((spawner.player.CurrentHealth == 100 ? "PASS " : "FAIL ") + "Protected player health remains100");
        ReportSample("Natural night (safe camp, test refueling, no health restoration)");
        // Repeat with ten mixed mobs near an exposed player, for close-combat cost.
        frames.Clear();
        cpu.Clear();
        gc.Clear();
        samples = 0;
        maxEnemies = 0;
        highestWave = 0;
        maxSpawn = 0;
        Array.Clear(markerTotals, 0, markerTotals.Length);
        Array.Clear(markerMax, 0, markerMax.Length);
        spawner.safe.fire.Extinguish();
        spawner.player.transform.position = new Vector3(0, 20, 0);
        spawner.player.GetComponent<Rigidbody2D>().position = spawner.player.transform.position;
        spawner.cycle.SkipPhase();
        spawner.cycle.Advance(81);
        spawner.cycle.Paused = true;
        float fillDeadline = Time.realtimeSinceStartup + 10;
        while (spawner.PendingCount > 0 && Time.realtimeSinceStartup < fillDeadline)
            yield return null;
        int moved = 0;
        foreach (var enemy in spawner.Snapshot())
        {
            bool placed = false;
            for (int i = 0; i < 48 && !placed; i++)
            {
                float angle = (i + moved * 7) * Mathf.PI * 2 / 48;
                Vector2 pos = (Vector2)spawner.player.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3.5f;
                if (!spawner.Walkable(pos, enemy.transform))
                    continue;
                enemy.transform.position = pos;
                enemy.GetComponent<Rigidbody2D>().position = pos;
                Physics2D.SyncTransforms();
                placed = true;
                moved++;
            }
        }

        sampling = true;
        float combatUntil = Time.realtimeSinceStartup + 30;
        int damageEvents = 0;
        Action<float> damage = amount => damageEvents++;
        spawner.player.Damaged += damage;
        while (Time.realtimeSinceStartup < combatUntil)
        {
            spawner.player.Heal(100);
            yield return null;
        }

        spawner.player.Damaged -= damage;
        sampling = false;
        results.Add((damageEvents > 0 ? "PASS " : "FAIL ") + "Live crowd combat hit events=" + damageEvents + " relocated mobs=" + moved);
        ReportSample("30s mixed crowd combat (health restored by harness, clock held)");
        results.Add((runtimeErrors == 0 ? "PASS " : "FAIL ") + "Runtime errors=" + runtimeErrors);
        Finish("Standalone Windows development build;1280x720;target60fps;VSync off;no Editor/CLI sampling calls during run.");
    }

    void LateUpdate()
    {
        if (!sampling || !spawner)
            return;
        frames.Add(Time.unscaledDeltaTime * 1000.0);
        if (mainRecorder.Valid)
            cpu.Add(mainRecorder.LastValue / 1e6);
        if (gcRecorder.Valid)
            gc.Add(gcRecorder.LastValue);
        maxEnemies = Mathf.Max(maxEnemies, spawner.ActiveCount);
        highestWave = Mathf.Max(highestWave, spawner.CurrentWave);
        maxSpawn = Mathf.Max(maxSpawn, (float)spawner.LastSpawnMilliseconds);
        for (int i = 0; i < markers.Length; i++)
            if (markers[i].Valid)
            {
                double v = markers[i].LastValue / 1e6;
                markerTotals[i] += v;
                markerMax[i] = Math.Max(markerMax[i], v);
            }

        samples++;
    }

    void ReportSample(string title)
    {
        results.Add(title);
        results.Add("Frame ms " + Stats(frames));
        results.Add("Main thread ms " + Stats(cpu));
        results.Add("GC bytes/frame " + Stats(gc));
        results.Add("Max spawn validation call ms=" + maxSpawn);
        for (int i = 0; i < markerNames.Length; i++)
            results.Add(markerNames[i] + " mean/ms=" + (markerTotals[i] / Math.Max(1, samples)) + " max/ms=" + markerMax[i]);
    }

    static string Stats(List<double> values)
    {
        if (values.Count == 0)
            return "UNVERIFIED recorder unavailable";
        var a = values.OrderBy(x => x).ToArray();
        return "n=" + a.Length + " mean=" + a.Average() + " p95=" + a[(int)((a.Length - 1) * .95)] + " p99=" + a[(int)((a.Length - 1) * .99)] + " max=" + a.Last();
    }

    void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            runtimeErrors++;
    }

    void Finish(string text)
    {
        results.Add(text);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllLines(output, results);
        Application.logMessageReceived -= Log;
        mainRecorder.Dispose();
        gcRecorder.Dispose();
        if (markers != null)
            foreach (var r in markers)
                r.Dispose();
        Application.Quit();
    }

    class TestWood : IWoodWallet
    {
        public int Wood => 100;

        public bool TrySpend(int amount) => true;
    }
}
