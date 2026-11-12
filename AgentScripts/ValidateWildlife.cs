using System;
using System.Linq;
using System.Text;
using UnityEngine;

public static class ValidateWildlife
{
    public static string Main()
    {
        var log=new StringBuilder();
        void Check(bool ok,string text){log.AppendLine((ok?"PASS ":"FAIL ")+text);if(!ok)throw new Exception(log.ToString());}
        var spawner=UnityEngine.Object.FindAnyObjectByType<WildlifeSpawner>();
        Check(Application.isPlaying && spawner,"Play mode wildlife spawner exists");
        spawner.cycle.Paused=true;
        if(spawner.cycle.IsNighttime)spawner.cycle.SkipPhase();
        spawner.Spawn();
        Check(spawner.LivingCount==spawner.maximumAnimals,"Population reaches its cap");
        Check(PassiveAnimal.ActiveAnimals.All(a=>a.data && !a.name.Contains("Chicken")),"All animals have data; no chickens");
        var animal=PassiveAnimal.ActiveAnimals.First(a=>a.data.displayName=="Sheep");
        var health=animal.GetComponent<EnemyHealth>();
        Check(!animal.GetComponent<EnemyController>(),"Wildlife has no hostile enemy AI");
        health.TakeDamage(1,animal.transform.position+Vector3.right);
        Check(animal.Fleeing && animal.GetComponent<EnemyHitFeedback>(),"Hit triggers fleeing and shared health-bar/flash feedback");
        int before=UnityEngine.Object.FindObjectsByType<ResourcePickup>().Length;
        health.TakeDamage(9999,animal.transform.position+Vector3.right);
        var drops=UnityEngine.Object.FindObjectsByType<ResourcePickup>();
        Check(drops.Length==before+2,"Sheep death drops meat and wool");
        Check(!health.TakeDamage(9999,Vector2.zero) && UnityEngine.Object.FindObjectsByType<ResourcePickup>().Length==drops.Length,"Repeated hits on a corpse cannot duplicate loot");
        var inventory=UnityEngine.Object.FindAnyObjectByType<InventoryController>();
        foreach(string id in new[]{"feathers","leather","wool","egg","milk","bone"})Check(inventory.FindItem(id),"Inventory registers "+id);
        var meat=drops.First(p=>p.item && p.item.Id=="raw_meat");
        var actor=inventory.GetComponent<PlayerInteractionController>();
        meat.transform.position=actor.transform.position;
        int owned=inventory.Count(meat.item), quantity=meat.quantity;
        meat.Use(actor);
        var meatItem=inventory.FindItem("raw_meat");
        Check(inventory.Count(meatItem)==owned+quantity,"Meat collected through existing ResourcePickup.Use");
        var hunger=inventory.GetComponent<PlayerHunger>();hunger.SetHungerForTesting(40);
        int index=Enumerable.Range(0,InventoryController.SlotCount).First(i=>inventory.GetSlot(i).Item==meatItem);
        Check(inventory.GetComponent<FoodConsumption>().Consume(index),"Existing food consumption accepts meat");
        Check(Mathf.Abs(hunger.CurrentHunger-52)<.01f,"Raw meat restores 12 hunger");
        Check(inventory.FindItem("cooked_meat").HungerRestored==35,"Cooked meat retains its larger food bonus");
        int lootCount=UnityEngine.Object.FindObjectsByType<ResourcePickup>().Length;
        spawner.cycle.SkipPhase();
        Check(spawner.LivingCount==0 && PassiveAnimal.ActiveAnimals.Count==0,"Night despawns passive animals");
        spawner.Spawn();
        Check(spawner.LivingCount==0,"Cannot spawn at night");
        Check(UnityEngine.Object.FindObjectsByType<ResourcePickup>().Length==lootCount,"Night despawn produces no loot");
        spawner.cycle.SkipPhase();spawner.Spawn();spawner.Spawn();
        Check(spawner.LivingCount==spawner.maximumAnimals,"Dawn repopulates without exceeding the cap");
        // Leave a vacancy and shorten the live-only timer for the follow-up respawn check.
        spawner.respawnSeconds=1;
        PassiveAnimal.ActiveAnimals[0].GetComponent<EnemyHealth>().TakeDamage(9999,Vector2.zero);
        var field=typeof(WildlifeSpawner).GetField("nextSpawn",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        field.SetValue(spawner,Time.time+1);
        Check(spawner.LivingCount==spawner.maximumAnimals-1,"Hunt creates a respawn vacancy");
        return log.ToString();
    }
}
