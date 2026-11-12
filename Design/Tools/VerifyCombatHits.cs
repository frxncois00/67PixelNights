using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

// Run in a disposable Play session via: unity command script run --file Design/Tools/VerifyCombatHits.cs
public static class VerifyCombatHits
{
    const string Output = "Design/Reviews/CombatHits/";
    static readonly List<string> results = new List<string>();
    static readonly BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;
    static PlayerMeleeCombat combat;
    static InventoryController inventory;
    static Camera camera;
    static Vector3 cameraPosition;
    static float cameraMovement;
    static int renderSamples;
    static EnemyController target;

    public static string Main()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Requires Play mode.");
        Directory.CreateDirectory(Output);
        combat = UnityEngine.Object.FindAnyObjectByType<PlayerMeleeCombat>();
        inventory = combat.GetComponent<InventoryController>();
        foreach (var e in EnemyController.ActiveControllers.ToArray()) if (e) e.enabled = false;
        foreach (var s in UnityEngine.Object.FindObjectsByType<EnemySpawner>()) s.enabled = false;
        foreach (var cycle in UnityEngine.Object.FindObjectsByType<DayNightCycle>()) cycle.enabled = false;
        combat.GetComponent<PlayerHunger>().enabled = false;
        combat.transform.position = new Vector3(0, -2.2f, 0);
        combat.GetComponent<Rigidbody2D>().position = combat.transform.position;
        combat.movement.enabled = false;
        typeof(PlayerMovement).GetField("<Facing>k__BackingField", Fields).SetValue(combat.movement, Vector2.right);
        camera = Camera.main;
        camera.GetComponent<CameraFollow>().enabled = false;
        camera.orthographicSize = 2.6f;
        cameraPosition = new Vector3(.45f, -1.9f, camera.transform.position.z);
        camera.transform.position = cameraPosition;
        RenderPipelineManager.beginCameraRendering += ObserveCamera;
        RenderPipelineManager.endCameraRendering += ObserveCamera;
        combat.StartCoroutine(Run());
        return "Running real attack callbacks for seven weapons plus rapid-hit, lifecycle and camera checks.";
    }

    static void ObserveCamera(ScriptableRenderContext context, Camera current)
    {
        if (current != camera) return;
        cameraMovement = Mathf.Max(cameraMovement, Vector3.Distance(current.transform.position, cameraPosition));
        renderSamples++;
    }

    static void Check(bool condition, string message)
    {
        results.Add((condition ? "PASS " : "FAIL ") + message);
        File.WriteAllLines(Output + "verification.txt", results);
    }

    static EnemyController Spawn(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go = UnityEngine.Object.Instantiate(prefab, combat.transform.position + Vector3.right * .95f, Quaternion.identity);
        go.name = "Combat review target";
        var e = go.GetComponent<EnemyController>();
        e.world = null;
        e.GetComponent<EnemyHealth>().Initialize(80);
        Physics2D.SyncTransforms();
        return e;
    }

    static bool Equip(string id)
    {
        var item = inventory.FindItem(id);
        if (!item) return false;
        if (inventory.Count(item) == 0) inventory.Add(item, 1);
        int slot = Enumerable.Range(0, InventoryController.SlotCount).First(i => inventory.GetSlot(i).Item == item);
        if (slot != InventoryController.MainSlots) inventory.Move(slot, InventoryController.MainSlots);
        return combat.equipment.Select(0) && combat.equipment.Equipped == item;
    }

    static void Capture(string name)
    {
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Output + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);
    }

    static IEnumerator Run()
    {
        try
        {
            int firstFrame = Time.frameCount;
            yield return new WaitForSeconds(.15f);
            // A compilation stall can consume the first wait in a single (long) frame.
            yield return null;
            Check(Time.frameCount > firstFrame, "Play mode advances frames");
            var ids = new[] { "basic_sword", "wood_longsword", "wood_knife", "wood_spear", "wood_hammer", "wood_shovel", "wood_bow" };
            var kinds = new[] { WeaponHitKind.Slash, WeaponHitKind.Cleave, WeaponHitKind.Stab, WeaponHitKind.Thrust, WeaponHitKind.Slam, WeaponHitKind.Scoop, WeaponHitKind.Arrow };
            for (int i = 0; i < ids.Length; i++)
            {
                target = Spawn("Assets/DrownedGrove/Prefabs/WaterSlime.prefab");
                yield return null;
                var hp = target.GetComponent<EnemyHealth>();
                Vector3 root = target.transform.position;
                Vector3 original = target.visual.transform.localScale;
                Vector3 position = target.visual.transform.localPosition;
                Quaternion rotation = target.visual.transform.localRotation;
                Check(Equip(ids[i]), ids[i] + " equipped");
                Check(combat.Attack(), ids[i] + " actual Attack accepted");
                float deadline = Time.time + 2f;
                while (hp.CurrentHealth >= 80 && Time.time < deadline) yield return null;
                Check(hp.CurrentHealth < 80 && hp.LastHitKind == kinds[i], ids[i] + " damage uses expected weapon kind " + hp.LastHitKind);
                yield return new WaitForSeconds(.025f);
                yield return new WaitForEndOfFrame();
                Check(target.visual.transform.localScale != original, ids[i] + " mob visibly reacts");
                Capture(ids[i] + "-impact");
                Check(target.transform.position == root && target.GetComponent<Collider2D>().enabled, ids[i] + " visual effect leaves root/collider intact");
                yield return new WaitForSeconds(.80f);
                Check(target.visual.transform.localScale == original && target.visual.transform.localPosition == position &&
                    target.visual.transform.localRotation == rotation, ids[i] + " original mob pose restored");
                Check(UnityEngine.Object.FindObjectsByType<HitFxParticle>().Length == 0 && UnityEngine.Object.FindObjectsByType<DamageNumber>().Length == 0,
                    ids[i] + " effects cleaned up");
                if (ids[i] == "wood_hammer")
                {
                    var fb = target.GetComponent<EnemyHitFeedback>();
                    float filled = (float)typeof(EnemyHitFeedback).GetField("displayedFraction", Fields).GetValue(fb);
                    float chipped = (float)typeof(EnemyHitFeedback).GetField("chipFraction", Fields).GetValue(fb);
                    Check(Mathf.Abs(filled - hp.CurrentHealth / hp.MaxHealth) < .001f && Mathf.Abs(chipped - filled) < .001f,
                        "health fill and delayed damage chip settle to actual HP");
                }
                UnityEngine.Object.Destroy(target.gameObject);
                yield return new WaitForSeconds(.1f);
            }

            target = Spawn("Assets/DrownedGrove/Prefabs/WaterSlime.prefab");
            yield return null;
            var health = target.GetComponent<EnemyHealth>();
            var feedback = target.GetComponent<EnemyHitFeedback>();
            Vector3 baseScale = target.visual.transform.localScale;
            Vector3 basePosition = target.visual.transform.localPosition;
            for (int i = 0; i < 8; i++)
            {
                health.TakeDamage(1, (Vector2)target.transform.position + (i % 2 == 0 ? Vector2.left : Vector2.down), 0, kinds[i % kinds.Length]);
                yield return new WaitForSeconds(.055f);
            }
            yield return new WaitForSeconds(.8f);
            Check(target.visual.transform.localScale == baseScale && target.visual.transform.localPosition == basePosition,
                "rapid overlapping hits do not accumulate scale/position drift");
            health.TakeDamage(1, combat.transform.position, 0, WeaponHitKind.Slam);
            yield return new WaitForSeconds(.045f);
            feedback.enabled = false;
            Check(target.visual.transform.localScale == baseScale && target.visual.transform.localPosition == basePosition,
                "disabling feedback mid-hit restores the mob");
            Check(!target.visual.transform.Find("HitFlashOverlay").GetComponent<SpriteRenderer>().enabled,
                "disabling feedback removes flash");
            feedback.enabled = true;
            yield return new WaitForSeconds(.8f);
            health.TakeDamage(1000, combat.transform.position, 0, WeaponHitKind.Cleave);
            yield return new WaitForSeconds(.05f);
            Check(health.IsDead && !target.GetComponent<Collider2D>().enabled, "lethal hit keeps defeat/collider behavior");
            yield return new WaitForEndOfFrame();
            Capture("finishing-hit");
            yield return new WaitForSeconds(.85f);
            Check(!target && UnityEngine.Object.FindObjectsByType<HitFxParticle>().Length == 0 && UnityEngine.Object.FindObjectsByType<DamageNumber>().Length == 0,
                "lethal-hit effects outlive target safely and clean up");

            target = Spawn("Assets/DrownedGrove/Prefabs/LanternEel.prefab");
            yield return null;
            health = target.GetComponent<EnemyHealth>();
            // A moving custom pose must remain authoritative beneath the feedback layer.
            Vector3 eelPose = target.visual.transform.localPosition;
            health.TakeDamage(5, combat.transform.position, 0, WeaponHitKind.Thrust);
            yield return new WaitForSeconds(.5f);
            Check(target.visual.transform.localPosition == eelPose, "custom eel submerge pose survives recoil");
            UnityEngine.Object.Destroy(target.gameObject);
            yield return new WaitForSeconds(.4f);

            // Launch, then swap immediately before the projectile can arrive.
            target = Spawn("Assets/DrownedGrove/Prefabs/WaterSlime.prefab");
            yield return null;
            Equip("wood_bow");
            WoodArrow.Fire(combat, Vector2.right, 5, 7, inventory.FindItem("wood_bow"));
            Equip("wood_hammer");
            yield return new WaitForSeconds(.35f);
            Check(target.GetComponent<EnemyHealth>().LastHitKind == WeaponHitKind.Arrow && target.GetComponent<EnemyHealth>().CurrentHealth < 80,
                "in-flight arrow retains arrow impact after equipping hammer");
            yield return new WaitForSeconds(.8f);
            Check(renderSamples > 50 && cameraMovement < .00001f, "camera stationary across " + renderSamples + " render callbacks; max motion=" + cameraMovement);
            Check(Time.timeScale == 1f, "no global hit stop or player timing change");
            Check(results.All(r => r.StartsWith("PASS")), "ALL COMBAT HIT CHECKS");
        }
        finally
        {
            RenderPipelineManager.beginCameraRendering -= ObserveCamera;
            RenderPipelineManager.endCameraRendering -= ObserveCamera;
            File.WriteAllLines(Output + "verification.txt", results);
        }
    }
}
