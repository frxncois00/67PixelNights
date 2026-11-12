using UnityEngine;
using System.Collections;

// Existing loot component: only combat death rolls AnimalData's drop table.
[RequireComponent(typeof(EnemyHealth))]
public class WildlifeHarvest : MonoBehaviour
{
    EnemyHealth health;
    bool dropped;

    void Awake() => health = GetComponent<EnemyHealth>();
    void OnEnable()
    {
        if (!health) health = GetComponent<EnemyHealth>();
        health.Died += OnDefeated;
    }
    void OnDisable() { if (health) health.Died -= OnDefeated; }

    void OnDefeated()
    {
        if (dropped) return;
        dropped = true;
        var animal = GetComponent<PassiveAnimal>();
        if (animal) animal.enabled = false;
        foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
        var body = GetComponent<Rigidbody2D>();
        if (body) body.linearVelocity = Vector2.zero;
        var source = GetComponent<SpriteRenderer>();
        if (animal && animal.data && animal.data.drops != null)
        {
            int index = 0;
            foreach (var drop in animal.data.drops)
            {
            if (drop == null || !drop.item || !drop.item.Icon || drop.chance <= 0 ||
                (drop.chance < 1 && Random.value >= drop.chance)) continue;
            var go = new GameObject(drop.item.DisplayName + " Drop");
            go.SetActive(false);
            Vector2 position = (Vector2)transform.position + new Vector2(Mathf.Cos(index * 2.4f), Mathf.Sin(index * 2.4f)) * .35f;
            if (!animal.ground || !animal.ground.HasTile(animal.ground.WorldToCell(position)) || Physics2D.OverlapCircle(position, .15f)) position = transform.position;
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = drop.item.Icon;
            renderer.color = drop.item.IconTint;
            if (source) renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerName = "Objects";
            var pickup = go.AddComponent<ResourcePickup>();
            pickup.item = drop.item;
            int min = Mathf.Clamp(drop.minimum, 1, 999);
            pickup.quantity = Random.Range(min, Mathf.Clamp(drop.maximum, min, 999) + 1);
            go.SetActive(true);
            index++;
            }
        }
        StartCoroutine(FadeOut(source));
    }

    IEnumerator FadeOut(SpriteRenderer visual)
    {
        Color color = visual ? visual.color : Color.white;
        for (float age = 0; age < .3f; age += Time.deltaTime)
        {
            if (visual) visual.color = new Color(color.r, color.g, color.b, color.a * (1f - age / .3f));
            yield return null;
        }
        Destroy(gameObject);
    }
}
