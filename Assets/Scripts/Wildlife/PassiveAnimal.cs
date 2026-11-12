using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemyHealth))]
public class PassiveAnimal : MonoBehaviour
{
    public AnimalData data;
    public Tilemap ground;
    public Transform campfire;
    public Sprite[] idleFrames, moveFrames;
    [Min(.1f)] public float speed = 1.5f, wanderRadius = 4, campfireAvoidance = 2.2f;
    [Header("Temperament")]
    public Vector2 pauseSeconds = new Vector2(1f, 3f);
    [Min(.1f)] public float walkingSeconds = 4f, fleeSeconds = 3f;
    [Min(1f)] public float fleeSpeedMultiplier = 1.8f;
    Rigidbody2D body;
    SpriteRenderer visual;
    Vector2 home, destination;
    float pauseUntil, walkingUntil;
    bool moving;
    public static readonly List<PassiveAnimal> ActiveAnimals = new List<PassiveAnimal>();
    EnemyHealth health;
    Vector2 threat;
    float fleeUntil;
    public bool Fleeing => health && health.IsAlive && Time.time < fleeUntil;
    readonly Collider2D[] hits = new Collider2D[16];
    readonly Vector2[] footprint = { Vector2.zero, Vector2.left * .3f, Vector2.right * .3f, Vector2.up * .3f, Vector2.down * .3f };
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
        if (data)
        {
            idleFrames = data.idleFrames; moveFrames = data.walkFrames;
            speed = data.moveSpeed; wanderRadius = data.roamingRadius;
            pauseSeconds = data.pauseSeconds; walkingSeconds = data.walkingSeconds;
            fleeSeconds = data.fleeSeconds; fleeSpeedMultiplier = data.fleeSpeedMultiplier;
            GetComponent<CircleCollider2D>().radius = Mathf.Max(.1f, data.bodyRadius);
            health.Initialize(data.health);
            if (idleFrames != null && idleFrames.Length > 0) visual.sprite = idleFrames[0];
        }
        home = body.position;
        destination = home;
    }
    void OnEnable()
    {
        if (!ActiveAnimals.Contains(this)) ActiveAnimals.Add(this);
        if (!health) health = GetComponent<EnemyHealth>();
        health.Damaged += React;
    }
    void OnDisable()
    {
        ActiveAnimals.Remove(this);
        if (health) health.Damaged -= React;
    }
    void React(float amount, Vector2 source)
    {
        if (data && !data.fleeWhenHit) return;
        threat = source;
        fleeUntil = Time.time + Mathf.Max(.1f, fleeSeconds);
        moving = true;
    }
    void Start()
    {
        if (!ground || idleFrames == null || idleFrames.Length == 0)
        { Debug.LogError("Passive animal needs Ground and sprite frames.", this); enabled = false; return; }
        Pause();
    }
    public bool ValidPosition(Vector2 position)
    {
        if (!ground || (!Fleeing && Vector2.Distance(position, home) > Mathf.Max(.1f, wanderRadius))) return false;
        float radius = data ? Mathf.Max(.1f, data.bodyRadius) : .3f;
        foreach (var offset in footprint) if (!ground.HasTile(ground.WorldToCell(position + offset * (radius / .3f)))) return false;
        if (campfire && Vector2.Distance(position, campfire.position) < campfireAvoidance) return false;
        var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
        int count = Physics2D.OverlapCircle(position, radius, filter, hits);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++) if (hits[i] && hits[i].gameObject != gameObject) return false;
        return true;
    }
    void Pause()
    {
        moving = false;
        float minimum = Mathf.Max(.1f, pauseSeconds.x);
        pauseUntil = Time.time + Random.Range(minimum, Mathf.Max(minimum, pauseSeconds.y));
    }
    void ChooseDestination()
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            var candidate = home + Random.insideUnitCircle * Mathf.Max(.1f, wanderRadius);
            if (!ValidPosition(candidate)) continue;
            destination = candidate;
            moving = true;
            walkingUntil = Time.time + Mathf.Max(.1f, walkingSeconds);
            return;
        }
        Pause();
    }
    void FixedUpdate()
    {
        if (!health || !health.IsAlive) return;
        if (Fleeing)
        {
            Vector2 away = body.position - threat;
            if (away.sqrMagnitude < .001f) away = Vector2.right;
            // Try safe escape angles, preserving terrain, fire and solid-object avoidance.
            moving = false;
            for (int i = 0; i < 7; i++)
            {
                float angle = i == 0 ? 0f : ((i + 1) / 2) * 30f * (i % 2 == 0 ? -1f : 1f);
                Vector2 direction = Quaternion.Euler(0, 0, angle) * away.normalized;
                Vector2 escapeStep = body.position + direction * Mathf.Max(.1f, speed) * Mathf.Max(1f, fleeSpeedMultiplier) * Time.fixedDeltaTime;
                if (!ValidPosition(escapeStep)) continue;
                Face(direction.x);
                body.MovePosition(escapeStep);
                moving = true;
                break;
            }
            return;
        }
        if (fleeUntil > 0f)
        {
            fleeUntil = 0f;
            home = body.position;
            Pause();
        }
        if (!moving) { if (Time.time >= pauseUntil) ChooseDestination(); return; }
        var next = Vector2.MoveTowards(body.position, destination, Mathf.Max(.1f, speed) * Time.fixedDeltaTime);
        if (!ValidPosition(next) || Time.time >= walkingUntil || Vector2.Distance(body.position, destination) < .08f) { Pause(); return; }
        Face(next.x - body.position.x);
        body.MovePosition(next);
    }
    void Update()
    {
        if (!health || !health.IsAlive) return;
        var frames = moving && moveFrames != null && moveFrames.Length > 0 ? moveFrames : idleFrames;
        float fps = data ? Mathf.Max(1, data.animationFPS) : 8;
        if (frames != null && frames.Length > 0) visual.sprite = frames[(int)(Time.time * fps * (Fleeing ? 1.5f : 1)) % frames.Length];
    }
    void Face(float x)
    {
        if (Mathf.Abs(x) > .001f) visual.flipX = (x < 0) != (data && data.artworkFacesLeft);
    }
}
