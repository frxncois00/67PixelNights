using UnityEngine;
using System.Collections.Generic;
using Unity.Profiling;

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Search,
    Retreat,
    Dead
}

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyHealth))]
public class EnemyController : MonoBehaviour
{
    public EnemySpawner world;
    public SpriteRenderer visual;
    public EnemyArchetype archetype;
    [Tooltip("Opt-in flag allowing stronghold guards to act during daytime without altering default night-wave enemies.")]
    public bool activeDuringDaytime;
    [Tooltip("Optional tether radius from spawn origin; 0 means untethered.")]
    [Min(0)]
    public float maxChaseDistanceFromOrigin;
    [Min(.1f)]
    public float patrolSpeed = 1.2f, chaseSpeed = 2, detectionRadius = 5, attackRange = 1, damage = 10, cooldown = 2, windup = .35f, recovery = .3f;
    public EnemyState State { get; private set; }
    public bool Alive => health && health.IsAlive;
    public string EnemyName => archetype ? archetype.displayName : "Forest Stalker";
    public bool BurstActive => burstUntil > Time.time;
    public bool LungeActive => lunging;
    public PlaceableStructure StructureTarget => structure;
    public Vector2 SpawnOrigin => origin;
    public bool IsHitPending => hitPending;
    public int PatrolIndex => patrolIndex;
    public bool HasAssignedPatrol => patrolWaypoints != null && patrolWaypoints.Length > 0;
    Transform[] patrolWaypoints;
    int patrolIndex;
    float patrolPauseUntil, patrolProgressAt;
    Vector2 patrolProgressPosition;
    bool restrictedEncounter;
    Rect encounterBounds;

    public void ConfigureEncounter(Transform[] points, Vector2 anchor, bool restricted, Rect bounds)
    {
        // Unconfigured encounter slots retain their original random patrol and spawn origin.
        if ((points == null || points.Length == 0) && !restricted) return;
        patrolWaypoints = points;
        restrictedEncounter = restricted;
        encounterBounds = bounds;
        origin = anchor;
        ResumePatrol();
    }

    public bool AllowsPosition(Vector2 point) => !EncounterSafeArea.Contains(point, .3f) &&
        (!restrictedEncounter || (encounterBounds.Contains(point) &&
        (maxChaseDistanceFromOrigin <= 0 || Vector2.Distance(point, origin) <= maxChaseDistanceFromOrigin)));

    void ResumePatrol()
    {
        CancelAttack();
        route.Clear();
        replanAt = 0;
        State = EnemyState.Idle;
        until = Time.time + .3f;
        patrolPauseUntil = until;
        patrolProgressAt = Time.time;
        patrolProgressPosition = transform.position;
        float nearest = float.PositiveInfinity;
        if (HasAssignedPatrol) for (int i = 0; i < patrolWaypoints.Length; i++)
        {
            if (!patrolWaypoints[i] || !AllowsPosition(patrolWaypoints[i].position)) continue;
            float d = Vector2.SqrMagnitude((Vector2)patrolWaypoints[i].position - (Vector2)transform.position);
            if (d < nearest) { nearest = d; patrolIndex = i; }
        }
    }

    void FollowPatrol(Vector2 pos)
    {
        if (Time.time < patrolPauseUntil) { State = EnemyState.Idle; return; }
        var point = patrolWaypoints[patrolIndex];
        if (!point || !AllowsPosition(point.position))
        { patrolIndex = (patrolIndex + 1) % patrolWaypoints.Length; patrolPauseUntil = Time.time + .5f; return; }
        if (Vector2.Distance(pos, point.position) <= .6f)
        {
            patrolIndex = (patrolIndex + 1) % patrolWaypoints.Length;
            patrolPauseUntil = Time.time + .65f;
            patrolProgressAt = Time.time;
            route.Clear(); replanAt = 0; State = EnemyState.Idle;
            return;
        }
        if (Vector2.Distance(pos, patrolProgressPosition) > .15f)
        { patrolProgressPosition = pos; patrolProgressAt = Time.time; }
        // Retry another waypoint if a dynamic obstruction temporarily closes the route.
        if (Time.time - patrolProgressAt > 8)
        { patrolIndex = (patrolIndex + 1) % patrolWaypoints.Length; patrolProgressAt = Time.time; route.Clear(); replanAt = 0; }
        State = EnemyState.Patrol;
        Navigate(point.position, patrolSpeed);
    }

    // Optional hooks used only by Sunken Waterworks lake creatures; null on all standard/Ashen Frontier enemies.
    public System.Action<Vector2> customBeginAttack;
    public System.Func<Vector2, bool> customExecuteAttack;
    public System.Action customCancelAttack;
    public System.Action customLateUpdate;

    Rigidbody2D body;
    EnemyHealth health;
    Vector2 origin, goal, lastSeen, push, lungeEnd;
    float until, nextAttack, hitAt, stunUntil;
    bool hitPending, lunging, attackingStructure, routeEvaluated;
    Color tint;
    EnemyAttackTelegraph telegraph;
    PlaceableStructure structure;
    float burstUntil, burstCueUntil, nextBurst, lungeUntil;
    int navRevision;
    bool routeFound;
    readonly List<Vector2> route = new List<Vector2>(256);
    float replanAt, retreatStarted;
    int waypoint;
    static readonly ProfilerMarker aiMarker = new ProfilerMarker("Forest.EnemyAI"), animationMarker = new ProfilerMarker("Forest.EnemyAnimation");
    public static readonly List<EnemyController> ActiveControllers = new List<EnemyController>();
    EnemyBehaviorProfile Behavior => archetype ? archetype.behavior : EnemyBehaviorProfile.Stalker;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        origin = transform.position;
        goal = origin;
        until = Time.time + 1;
        if (archetype)
        {
            archetype.Validate();
            patrolSpeed = archetype.patrolSpeed;
            chaseSpeed = archetype.chaseSpeed;
            detectionRadius = archetype.detectionRange;
            attackRange = archetype.attackRange;
            damage = archetype.damage;
            cooldown = archetype.cooldown;
            windup = archetype.windup;
            recovery = archetype.recovery;
            health.Initialize(archetype.health);
            if (visual)
            {
                if (archetype.sprite)
                    visual.sprite = archetype.sprite;
                visual.color = archetype.tint;
            }
        }

        if (visual)
        {
            tint = visual.color;
            telegraph = gameObject.AddComponent<EnemyAttackTelegraph>();
            telegraph.Initialize(visual.sharedMaterial);
        }

        if (!GetComponent<EnemyHitFeedback>())
            gameObject.AddComponent<EnemyHitFeedback>();
    }

    void OnEnable()
    {
        if (!ActiveControllers.Contains(this))
            ActiveControllers.Add(this);
    }

    bool CanAct => isActiveAndEnabled && world && world.navigation && world.safe && world.player && world.player.IsAlive && (activeDuringDaytime || (world.cycle && world.cycle.IsNighttime)) && Alive;

    float HitRange => Behavior == EnemyBehaviorProfile.Ancient && !structure ? archetype.slamRadius : attackRange;

    public bool ValidAttack() => CanAct && AllowsPosition(world.player.transform.position) && !world.safe.IsPlayerSafe && !world.safe.IsPositionSafe(body.position) && Vector2.Distance(body.position, world.player.transform.position) <= HitRange && world.Visible(body.position, world.player.transform.position, transform, world.player.transform);
    bool ValidStructure()
    {
        if (!CanAct || !structure || structure.IsDestroyed || !structure.definition || world.safe.IsPositionSafe(body.position))
            return false;
        var collider = structure.GetComponent<Collider2D>();
        return collider && collider.enabled && Vector2.Distance(body.position, collider.ClosestPoint(body.position)) <= attackRange && world.BlockingStructure(body.position, world.player.transform.position, transform) == structure && world.Visible(body.position, collider.ClosestPoint(body.position), transform, structure.transform);
    }

    void CancelAttack()
    {
        hitPending = lunging = attackingStructure = false;
        structure = null;
        if (telegraph)
            telegraph.Hide();
        customCancelAttack?.Invoke();
    }

    void BeginAttack(PlaceableStructure target = null)
    {
        structure = target;
        attackingStructure = target != null;
        State = EnemyState.Attack;
        hitPending = true;
        hitAt = Time.time + windup;
        until = hitAt + recovery;
        nextAttack = Time.time + Mathf.Max(cooldown, windup + recovery);
        if (telegraph)
            telegraph.Show(HitRange, new Color(1, .3f, .1f, .8f));
        if (!attackingStructure && world && world.player)
            customBeginAttack?.Invoke(world.player.transform.position);
    }

    public void HideRingTelegraph()
    {
        if (telegraph)
            telegraph.Hide();
    }

    void FixedUpdate()
    {
        using (aiMarker.Auto())
        {
            TickAI();
        }
    }

    void TickAI()
    {
        if (!CanAct)
        {
            CancelAttack();
            if (Alive)
                State = EnemyState.Idle;
            return;
        }

        Vector2 pos = body.position, target = world.player.transform.position;
        if ((!AllowsPosition(target) || (restrictedEncounter && !AllowsPosition(pos))) &&
            (State == EnemyState.Chase || State == EnemyState.Attack || State == EnemyState.Search))
            ResumePatrol();
        if (world.safe.IsPositionSafe(pos) || (world.safe.IsPlayerSafe && (State == EnemyState.Chase || State == EnemyState.Attack || State == EnemyState.Search)))
        {
            CancelAttack();
            burstUntil = burstCueUntil = 0;
            State = EnemyState.Retreat;
            if (retreatStarted == 0)
                retreatStarted = Time.time;
            Navigate(pos, chaseSpeed, true);
            if (!activeDuringDaytime && Time.time - retreatStarted > 12 && world.safe.IsPositionSafe(pos))
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }

            return;
        }

        retreatStarted = 0;
        if (Time.time < stunUntil)
        {
            Move(push, 1);
            return;
        }

        if (State == EnemyState.Attack)
        {
            if (attackingStructure && (!structure || structure.IsDestroyed || navRevision != world.navigation.Revision))
            {
                CancelAttack();
                State = EnemyState.Chase;
                return;
            }

            if (lunging)
            {
                float distance = Vector2.Distance(body.position, lungeEnd);
                if (Time.time < lungeUntil && distance > .05f)
                {
                    Move((lungeEnd - body.position).normalized, Mathf.Min(5, distance / Time.fixedDeltaTime));
                    return;
                }

                lunging = false;
                if (ValidAttack())
                    world.player.TakeDamage(damage, EnemyName);
                until = Time.time + recovery;
                if (telegraph)
                    telegraph.Hide();
            }

            if (hitPending && Time.time >= hitAt)
            {
                hitPending = false;
                if (attackingStructure)
                {
                    if (ValidStructure())
                        structure.TakeDamage(archetype ? archetype.structureDamage : 8);
                    if (telegraph)
                        telegraph.Hide();
                }
                else if (customExecuteAttack != null && customExecuteAttack(target))
                {
                    if (telegraph)
                        telegraph.Hide();
                }
                else if (Behavior == EnemyBehaviorProfile.Crawler)
                {
                    Vector2 direction = (target - pos).normalized;
                    lungeEnd = pos + direction * Mathf.Min(archetype.lungeDistance, Vector2.Distance(pos, target));
                    if (world.Walkable(lungeEnd, transform) && world.Visible(pos, lungeEnd, transform, world.player.transform) && !world.safe.IsPositionSafe(lungeEnd))
                    {
                        lunging = true;
                        lungeUntil = Time.time + .35f;
                        return;
                    }

                    if (ValidAttack())
                        world.player.TakeDamage(damage, EnemyName);
                    if (telegraph)
                        telegraph.Hide();
                }
                else
                {
                    if (ValidAttack())
                        world.player.TakeDamage(damage, EnemyName);
                    if (telegraph)
                        telegraph.Hide();
                }
            }

            if (Time.time < until)
                return;
            // Keep a valid blocking defense as the target throughout the cooldown.
            // Walking away between swings can lose the player before a fence breaks.
            if (attackingStructure && ValidStructure())
            {
                if (Time.time >= nextAttack)
                    BeginAttack(structure);
                return;
            }

            CancelAttack();
            State = EnemyState.Chase;
        }

        // Detection remembers the player through short occlusions; route planning handles obstacles.
        bool withinTether = maxChaseDistanceFromOrigin <= 0f || (Vector2.Distance(pos, origin) <= maxChaseDistanceFromOrigin && Vector2.Distance(target, origin) <= maxChaseDistanceFromOrigin + 2f);
        bool detected = !world.safe.IsPlayerSafe && AllowsPosition(target) && withinTether && Vector2.Distance(pos, target) <= detectionRadius;
        if (detected)
        {
            lastSeen = target;
            float startRange = Behavior == EnemyBehaviorProfile.Crawler ? attackRange + archetype.lungeDistance : HitRange;
            if (Time.time >= nextAttack && Vector2.Distance(pos, target) <= startRange && world.Visible(pos, target, transform, world.player.transform))
            {
                BeginAttack();
                return;
            }

            State = EnemyState.Chase;
            Navigate(target, ChaseSpeed());
            // Only the first solid obstruction on the pursuit ray is eligible. Prefer a
            // short detour; attack only failed or >1.8x direct distance + 2-unit routes.
            if (routeEvaluated && Time.time >= nextAttack && (!routeFound || (route.Count - waypoint) * world.navigation.cellSize > Vector2.Distance(pos, target) * 1.8f + 2))
            {
                structure = world.BlockingStructure(pos, target, transform);
                if (ValidStructure())
                {
                    BeginAttack(structure);
                    return;
                }

                structure = null;
            }

            return;
        }

        if (State == EnemyState.Chase)
        {
            State = EnemyState.Search;
            until = Time.time + 3;
        }

        if (State == EnemyState.Search)
        {
            if (Time.time < until)
            {
                Navigate(lastSeen, patrolSpeed);
                return;
            }

            State = EnemyState.Idle;
            until = Time.time + 1;
        }

        if (State == EnemyState.Retreat)
        {
            State = EnemyState.Idle;
            until = Time.time + 1;
        }

        if (HasAssignedPatrol) { FollowPatrol(pos); return; }

        if (State == EnemyState.Idle && Time.time >= until)
        {
            goal = origin + Random.insideUnitCircle * 3;
            State = EnemyState.Patrol;
            until = Time.time + 3;
        }

        if (State == EnemyState.Patrol)
        {
            if (Time.time >= until || Vector2.Distance(pos, goal) < .25f)
            {
                State = EnemyState.Idle;
                until = Time.time + 1;
            }
            else
                Navigate(goal, patrolSpeed);
        }
    }

    float ChaseSpeed()
    {
        if (Behavior == EnemyBehaviorProfile.Crawler)
            return Mathf.Repeat(Time.time + Mathf.Abs(GetEntityId().GetHashCode() % 100) * .01f, 2.4f) < .35f ? 0 : chaseSpeed;
        if (Behavior != EnemyBehaviorProfile.Wolf)
            return chaseSpeed;
        if (Time.time >= nextBurst)
        {
            nextBurst = Time.time + archetype.burstCooldown;
            burstCueUntil = Time.time + .25f;
            burstUntil = burstCueUntil + archetype.burstDuration;
        }

        if (Time.time < burstCueUntil)
            return 0;
        if (Time.time < burstUntil)
            return chaseSpeed * archetype.burstMultiplier;
        if (Time.time < burstUntil + .25f)
            return 0;
        return chaseSpeed;
    }

    void Navigate(Vector2 target, float speed, bool escape = false)
    {
        if (!world.navigation)
            return;
        if (navRevision != world.navigation.Revision)
        {
            replanAt = 0;
            route.Clear();
            routeEvaluated = false;
            navRevision = world.navigation.Revision;
        }

        if (Time.time >= replanAt && world.navigation.TryReserveSearch())
        {
            replanAt = Time.time + .8f + Mathf.Abs(GetEntityId().GetHashCode() % 7) * .03f;
            waypoint = 0;
            routeFound = world.navigation.Find(body.position, target, route, false, escape, AllowsPosition);
            routeEvaluated = true;
        }

        if (waypoint < route.Count)
        {
            if (Vector2.Distance(body.position, route[waypoint]) < .12f)
                waypoint++;
            if (waypoint < route.Count)
                Move((route[waypoint] - body.position).normalized, speed, escape);
        }
    }

    void Move(Vector2 direction, float speed, bool escape = false)
    {
        Vector2 step = direction * Mathf.Clamp(speed, 0, 6) * Time.fixedDeltaTime;
        for (int i = 0; i < 5; i++)
        {
            float a = i == 0 ? 0 : (i % 2 == 0 ? -1 : 1) * ((i + 1) / 2) * 45;
            Vector2 delta = Quaternion.Euler(0, 0, a) * step;
            Vector2 next = body.position + delta;
            if (!AllowsPosition(next)) continue;
            bool retreat = escape && world.safe.IsPositionSafe(body.position);
            if (world.Walkable(next, transform, retreat) && world.Visible(body.position, next, transform, null))
            {
                body.MovePosition(next);
                if (visual && Mathf.Abs(delta.x) > .001f)
                    visual.flipX = delta.x < 0;
                return;
            }
        }
    }

    void LateUpdate()
    {
        using (animationMarker.Auto())
        {
            if (!visual)
                return;
            if (!Alive)
            {
                var c = visual.color;
                c.a = Mathf.MoveTowards(c.a, 0, Time.deltaTime * 3);
                visual.color = c;
                return;
            }

            visual.color = Time.time < stunUntil ? Color.white : State == EnemyState.Attack ? new Color(1, .35f, .2f) : Time.time < burstCueUntil ? Color.yellow : tint;
            if (archetype)
            {
                var frames = State == EnemyState.Chase || State == EnemyState.Patrol ? archetype.walkFrames : archetype.idleFrames;
                if (frames != null && frames.Length > 0)
                    visual.sprite = frames[(int)(Time.time * archetype.animationFPS) % frames.Length];
            }
            customLateUpdate?.Invoke();
        }
    }

    public void React(Vector2 source, float force)
    {
        CancelAttack();
        State = EnemyState.Search;
        until = Time.time + 3;
        push = (body.position - source).normalized * force * (1 - (archetype ? archetype.knockbackResistance : 0));
        stunUntil = Time.time + .2f;
        if (activeDuringDaytime)
            lastSeen = maxChaseDistanceFromOrigin > 0f && Vector2.Distance(source, origin) > maxChaseDistanceFromOrigin + 2f ? origin : source;
    }

    public void Defeat()
    {
        CancelAttack();
        State = EnemyState.Dead;
        body.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        if (visual)
            visual.color = new Color(.3f, .3f, .3f, .7f);
        Destroy(gameObject, .35f);
    }

    void OnDisable()
    {
        ActiveControllers.Remove(this);
        CancelAttack();
        burstUntil = burstCueUntil = 0;
        route.Clear();
        routeEvaluated = false;
        replanAt = 0;
        if (body)
            body.linearVelocity = Vector2.zero;
    }
}
