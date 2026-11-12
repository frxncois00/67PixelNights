using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class VerifyNeutralSheep
{
    static List<string> log = new List<string>();
    static PlayerMeleeCombat combat;
    static PassiveAnimal sheep;
    static void Check(bool ok, string text) { log.Add((ok ? "PASS " : "FAIL ") + text); File.WriteAllLines("Design/Reviews/NeutralSheep.txt", log); }
    public static string Main()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        combat = UnityEngine.Object.FindAnyObjectByType<PlayerMeleeCombat>();
        sheep = PassiveAnimal.ActiveAnimals.First();
        combat.StartCoroutine(Run());
        return "Checking neutral sheep combat lifecycle";
    }
    static IEnumerator Run()
    {
        var hp = sheep.GetComponent<EnemyHealth>();
        Check(hp && hp.CurrentHealth == 30 && !sheep.Fleeing, "sheep starts peaceful with 30 HP");
        Check(!sheep.GetComponent<InteractionTarget>(), "no harvest interaction or E prompt");
        Check(sheep.GetComponent<EnemyHitFeedback>(), "shared combat feedback installed");
        sheep.transform.position = new Vector3(2,-3,0);
        sheep.GetComponent<Rigidbody2D>().position = sheep.transform.position;
        typeof(PassiveAnimal).GetField("home", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(sheep, new Vector2(2,-3));
        combat.transform.position = sheep.transform.position + Vector3.left;
        combat.GetComponent<Rigidbody2D>().position = combat.transform.position;
        combat.movement.enabled = false;
        typeof(PlayerMovement).GetField("<Facing>k__BackingField", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(combat.movement, Vector2.right);
        var inv = combat.GetComponent<InventoryController>();
        var sword = inv.FindItem("basic_sword");
        if (inv.Count(sword)==0) inv.Add(sword,1);
        int slot = Enumerable.Range(0,InventoryController.SlotCount).First(i=>inv.GetSlot(i).Item==sword);
        if(slot!=20) inv.Move(slot,20);
        combat.equipment.Select(0);
        Physics2D.SyncTransforms();
        int drops = UnityEngine.Object.FindObjectsByType<ResourcePickup>().Count(p=>p.name=="Raw Meat Drop");
        Check(combat.Attack(), "actual sword attack accepted");
        float deadline=Time.time+2;
        while(hp.CurrentHealth==30 && Time.time<deadline) yield return null;
        Check(hp.CurrentHealth==15 && sheep.Fleeing, "sword damages sheep and triggers fleeing");
        Vector3 start=sheep.transform.position;
        yield return new WaitForSeconds(.3f);
        Check(Vector3.Distance(start,sheep.transform.position)>.1f,"sheep moves away after attack");
        Check(UnityEngine.Object.FindObjectsByType<ResourcePickup>().Count(p=>p.name=="Raw Meat Drop")==drops,"nonlethal hit produces no meat");
        var bar=sheep.transform.Find("EnemyHealthBar");
        Check(bar && bar.localPosition.y<1.5f,"health bar remains above sheep at world offset");
        yield return new WaitForSeconds(3.1f);
        Check(!sheep.Fleeing && sheep.enabled,"sheep calms and resumes wandering");
        combat.transform.position=sheep.transform.position+Vector3.left*1.1f;
        combat.GetComponent<Rigidbody2D>().position=combat.transform.position;
        Physics2D.SyncTransforms();
        WoodArrow.Fire(combat,Vector2.right,5,7,inv.FindItem("wood_bow"));
        yield return new WaitForSeconds(.3f);
        Check(hp.CurrentHealth==10 && hp.LastHitKind==WeaponHitKind.Arrow && sheep.Fleeing,"arrow damages and frightens sheep");
        hp.TakeDamage(1000,combat.transform.position,0);
        Check(!hp.TakeDamage(1000,combat.transform.position,0),"dead sheep rejects duplicate hits");
        Check(!sheep.GetComponent<Collider2D>().enabled && !sheep.enabled,"defeated sheep stops moving and colliding");
        var loot=UnityEngine.Object.FindObjectsByType<ResourcePickup>().Where(p=>p.name=="Raw Meat Drop").ToArray();
        Check(loot.Length==drops+1 && loot.Last().quantity>=1 && loot.Last().quantity<=2,"death creates exactly one drop containing 1-2 meat");
        yield return new WaitForSeconds(1);
        Check(!sheep,"defeated sheep cleans up");
        Check(log.All(x=>x.StartsWith("PASS")),"ALL NEUTRAL SHEEP CHECKS");
    }
}
