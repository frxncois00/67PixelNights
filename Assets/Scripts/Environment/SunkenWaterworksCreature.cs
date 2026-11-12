using UnityEngine;

[DisallowMultipleComponent]
public class SunkenWaterworksCreature : MonoBehaviour
{
    public enum CreatureKind
    {
        ReedStalker,
        SluiceCrab,
        LanternEel,
        FloodgateKeeper
    }

    public CreatureKind kind = CreatureKind.ReedStalker;
    public SpriteRenderer nightTellRenderer;
    public SpriteRenderer reedCamouflageRenderer;
    public SpriteRenderer rippleRenderer;
    public Color nightTellColor = new Color(0.35f, 0.95f, 0.85f, 0.85f);

    [Header("Reed Stalker")]
    public float reedLungeWarningRadius = 1.35f;

    [Header("Sluice Crab")]
    [Range(0.1f, 1f)]
    public float frontalGuardDamageMultiplier = 0.35f;
    public float exposedDamageMultiplier = 1.35f;
    public float exposedDuration = 1.65f;

    [Header("Lantern Eel & Floodgate Keeper Surge")]
    public float surgeLength = 2.75f;
    public float surgeWidth = 0.75f;
    public float surfaceOrWeakPointDuration = 2.2f;
    public float weakPointDamageMultiplier = 1.5f;

    EnemyController controller;
    EnemyHealth health;
    LineRenderer laneTelegraph;
    LineRenderer statusRing;
    Vector2 facingDirection = Vector2.down;
    Vector2 lastPosition;
    Vector2 lockedSurgeOrigin;
    Vector2 lockedSurgeDirection = Vector2.left;
    bool currentAttackIsLaneSurge;
    int attackCount;
    float exposedUntil;
    float lastDeflectFlashUntil;

    public bool IsExposedWindowActive => Time.time < exposedUntil;
    public bool IsShellGuardActive => kind == CreatureKind.SluiceCrab && Time.time >= exposedUntil && controller != null && controller.Alive;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        health = GetComponent<EnemyHealth>();
        lastPosition = transform.position;

        if (controller != null)
        {
            controller.customBeginAttack = OnCustomBeginAttack;
            controller.customExecuteAttack = OnCustomExecuteAttack;
            controller.customCancelAttack = OnCustomCancelAttack;
            controller.customLateUpdate = OnCustomLateUpdate;
        }

        if (health != null)
        {
            health.damageModifier = ModifyIncomingDamage;
        }

        BuildTelegraphRenderers();
    }

    void BuildTelegraphRenderers()
    {
        Material sharedMat = controller != null && controller.visual != null
            ? controller.visual.sharedMaterial
            : new Material(Shader.Find("Sprites/Default"));

        var laneGo = new GameObject("SurgeLaneTelegraph");
        laneGo.transform.SetParent(transform, false);
        laneTelegraph = laneGo.AddComponent<LineRenderer>();
        laneTelegraph.sharedMaterial = sharedMat;
        laneTelegraph.useWorldSpace = true;
        laneTelegraph.loop = true;
        laneTelegraph.positionCount = 4;
        laneTelegraph.widthMultiplier = 0.055f;
        laneTelegraph.sortingLayerName = "Foreground";
        laneTelegraph.sortingOrder = 6;
        laneTelegraph.enabled = false;

        var ringGo = new GameObject("CreatureStatusRing");
        ringGo.transform.SetParent(transform, false);
        statusRing = ringGo.AddComponent<LineRenderer>();
        statusRing.sharedMaterial = sharedMat;
        statusRing.useWorldSpace = false;
        statusRing.loop = true;
        statusRing.positionCount = 24;
        statusRing.widthMultiplier = 0.04f;
        statusRing.sortingLayerName = "Foreground";
        statusRing.sortingOrder = 6;
        statusRing.enabled = false;
    }

    float ModifyIncomingDamage(float amount, Vector2 source)
    {
        if (controller == null || !controller.Alive)
            return amount;

        if (kind == CreatureKind.SluiceCrab)
        {
            if (Time.time < exposedUntil)
            {
                return amount * exposedDamageMultiplier;
            }

            Vector2 fromAttackerToCrab = ((Vector2)transform.position - source).normalized;
            // If player strikes against the front shell (attacker is in front of facingDirection)
            float frontalAlignment = Vector2.Dot(-fromAttackerToCrab, facingDirection);
            if (frontalAlignment >= -0.05f)
            {
                lastDeflectFlashUntil = Time.time + 0.18f;
                return Mathf.Max(1f, amount * frontalGuardDamageMultiplier);
            }

            return amount * 1.15f;
        }

        if (kind == CreatureKind.LanternEel)
        {
            if (Time.time < exposedUntil)
                return amount * 1.35f;
            if (controller.State == EnemyState.Attack && controller.IsHitPending)
                return amount;
            return Mathf.Max(1f, amount * 0.55f);
        }

        if (kind == CreatureKind.FloodgateKeeper)
        {
            if (Time.time < exposedUntil)
                return amount * weakPointDamageMultiplier;
        }

        return amount;
    }

    void OnCustomBeginAttack(Vector2 playerPos)
    {
        Vector2 myPos = transform.position;
        Vector2 toPlayer = playerPos - myPos;
        if (toPlayer.sqrMagnitude < 0.001f)
            toPlayer = Vector2.down;
        facingDirection = toPlayer.normalized;

        if (kind == CreatureKind.ReedStalker)
        {
            ShowStatusRing(reedLungeWarningRadius, new Color(0.35f, 0.95f, 0.78f, 0.85f), 0f, 360f);
        }
        else if (kind == CreatureKind.SluiceCrab)
        {
            exposedUntil = 0f;
            ShowStatusRing(0.62f, new Color(1f, 0.45f, 0.18f, 0.85f), 0f, 360f);
        }
        else if (kind == CreatureKind.LanternEel)
        {
            currentAttackIsLaneSurge = true;
            lockedSurgeOrigin = myPos;
            lockedSurgeDirection = facingDirection;
            controller.HideRingTelegraph();
            ShowSurgeLane(lockedSurgeOrigin, lockedSurgeDirection, surgeLength, surgeWidth, new Color(0.25f, 0.95f, 1f, 0.9f));
        }
        else if (kind == CreatureKind.FloodgateKeeper)
        {
            attackCount++;
            float dist = Vector2.Distance(myPos, playerPos);
            currentAttackIsLaneSurge = (attackCount % 2 == 1) || dist > 1.55f;
            if (currentAttackIsLaneSurge)
            {
                lockedSurgeOrigin = myPos;
                lockedSurgeDirection = SnapToPlatformLane(facingDirection);
                controller.HideRingTelegraph();
                ShowSurgeLane(lockedSurgeOrigin, lockedSurgeDirection, surgeLength, surgeWidth, new Color(0.3f, 0.92f, 1f, 0.92f));
            }
            else
            {
                if (laneTelegraph != null)
                    laneTelegraph.enabled = false;
            }
        }
    }

    Vector2 SnapToPlatformLane(Vector2 rawDir)
    {
        // Bias toward clean shoreline/platform lane directions while still tracking the player's corridor
        if (Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.y) * 1.35f)
            return new Vector2(Mathf.Sign(rawDir.x), Mathf.Clamp(rawDir.y, -0.35f, 0.35f)).normalized;
        if (Mathf.Abs(rawDir.y) > Mathf.Abs(rawDir.x) * 1.35f)
            return new Vector2(Mathf.Clamp(rawDir.x, -0.35f, 0.35f), Mathf.Sign(rawDir.y)).normalized;
        return rawDir.normalized;
    }

    bool OnCustomExecuteAttack(Vector2 playerPos)
    {
        if (laneTelegraph != null)
            laneTelegraph.enabled = false;

        if (kind == CreatureKind.ReedStalker)
        {
            if (statusRing != null)
                statusRing.enabled = false;
            // Return false so EnemyController executes its built-in Crawler short lunge onto dry ground
            return false;
        }

        if (kind == CreatureKind.SluiceCrab)
        {
            if (controller.ValidAttack())
                controller.world.player.TakeDamage(controller.damage, controller.EnemyName);
            exposedUntil = Time.time + exposedDuration;
            ShowStatusRing(0.55f, new Color(1f, 0.78f, 0.2f, 0.92f), 0f, 360f);
            return true;
        }

        if (kind == CreatureKind.LanternEel)
        {
            DeliverSurgeDamage(lockedSurgeOrigin, lockedSurgeDirection, surgeLength, surgeWidth);
            exposedUntil = Time.time + surfaceOrWeakPointDuration;
            ShowStatusRing(0.52f, new Color(0.35f, 1f, 0.88f, 0.92f), 0f, 360f);
            return true;
        }

        if (kind == CreatureKind.FloodgateKeeper)
        {
            if (currentAttackIsLaneSurge)
            {
                DeliverSurgeDamage(lockedSurgeOrigin, lockedSurgeDirection, surgeLength, surgeWidth);
                exposedUntil = Time.time + surfaceOrWeakPointDuration;
                ShowStatusRing(0.72f, new Color(1f, 0.85f, 0.25f, 0.95f), 0f, 360f);
                return true;
            }

            if (controller.ValidAttack())
                controller.world.player.TakeDamage(controller.damage, controller.EnemyName);
            return true;
        }

        return false;
    }

    void DeliverSurgeDamage(Vector2 origin, Vector2 dir, float length, float width)
    {
        if (controller == null || controller.world == null || controller.world.player == null || controller.world.safe == null)
            return;
        if (controller.world.safe.IsPlayerSafe || controller.world.safe.IsPositionSafe(origin))
            return;

        Vector2 playerPos = controller.world.player.transform.position;
        Vector2 rel = playerPos - origin;
        float along = Vector2.Dot(rel, dir);
        Vector2 perpVec = rel - dir * along;
        float halfWidth = width * 0.5f + 0.18f;
        if (along >= -0.25f && along <= length + 0.2f && perpVec.magnitude <= halfWidth)
        {
            controller.world.player.TakeDamage(controller.damage, controller.EnemyName);
        }
    }

    void OnCustomCancelAttack()
    {
        if (laneTelegraph != null)
            laneTelegraph.enabled = false;
        if (statusRing != null && Time.time >= exposedUntil)
            statusRing.enabled = false;
    }

    void OnCustomLateUpdate()
    {
        if (controller == null || controller.visual == null || !controller.Alive)
        {
            if (laneTelegraph != null) laneTelegraph.enabled = false;
            if (statusRing != null) statusRing.enabled = false;
            return;
        }

        Vector2 curPos = transform.position;
        Vector2 delta = curPos - lastPosition;
        if (delta.sqrMagnitude > 0.0001f)
            facingDirection = delta.normalized;
        else if (controller.world != null && controller.world.player != null &&
                 (controller.State == EnemyState.Chase || controller.State == EnemyState.Attack))
        {
            Vector2 toP = (Vector2)controller.world.player.transform.position - curPos;
            if (toP.sqrMagnitude > 0.001f)
                facingDirection = toP.normalized;
        }
        lastPosition = curPos;

        // Night tell pulse
        if (nightTellRenderer != null)
        {
            float pulse = 0.72f + 0.28f * Mathf.Sin(Time.time * 4.2f + (GetEntityId().GetHashCode() & 0xFF) * 0.17f);
            Color c = nightTellColor;
            c.a *= pulse;
            nightTellRenderer.color = c;
            nightTellRenderer.flipX = controller.visual.flipX;
        }

        // Ripple animation
        if (rippleRenderer != null)
        {
            float fast = (controller.State == EnemyState.Attack || Time.time < exposedUntil) ? 6.5f : 2.6f;
            float s = 0.92f + 0.16f * Mathf.Sin(Time.time * fast);
            rippleRenderer.transform.localScale = new Vector3(s, s, 1f);
            Color rc = rippleRenderer.color;
            rc.a = (controller.State == EnemyState.Attack) ? 0.88f : 0.55f;
            rippleRenderer.color = rc;
        }

        // Reed Stalker camouflage & rustling reeds tell
        if (kind == CreatureKind.ReedStalker)
        {
            bool hiding = controller.State == EnemyState.Idle || controller.State == EnemyState.Patrol;
            if (hiding)
            {
                Color vc = controller.visual.color;
                vc.a = 0.68f;
                controller.visual.color = vc;
            }
            if (reedCamouflageRenderer != null)
            {
                float rustle = controller.State == EnemyState.Attack
                    ? Mathf.Sin(Time.time * 28f) * 11f
                    : Mathf.Sin(Time.time * 3.2f) * 2.5f;
                reedCamouflageRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, rustle);
            }
        }
        else if (kind == CreatureKind.SluiceCrab)
        {
            if (Time.time < lastDeflectFlashUntil)
            {
                controller.visual.color = new Color(0.55f, 0.92f, 1f, 1f);
            }
            else if (Time.time < exposedUntil)
            {
                float flash = 0.5f + 0.5f * Mathf.Sin(Time.time * 16f);
                controller.visual.color = Color.Lerp(new Color(1f, 0.85f, 0.35f, 1f), Color.white, flash * 0.45f);
                ShowStatusRing(0.55f, new Color(1f, 0.78f, 0.2f, 0.9f), 0f, 360f);
            }
            else if (controller.State != EnemyState.Attack)
            {
                // Show frontal shell guard arc
                float centerAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
                ShowStatusRing(0.46f, new Color(0.35f, 0.85f, 0.95f, 0.55f), centerAngle - 70f, centerAngle + 70f);
            }
        }
        else if (kind == CreatureKind.LanternEel)
        {
            if (Time.time < exposedUntil)
            {
                // Surfaced window: bright, elevated slightly for clear melee/bow punish
                controller.visual.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                controller.visual.color = new Color(0.75f, 1f, 0.95f, 1f);
                ShowStatusRing(0.52f, new Color(0.35f, 1f, 0.88f, 0.9f), 0f, 360f);
            }
            else
            {
                controller.visual.transform.localPosition = new Vector3(0f, -0.04f, 0f);
                if (statusRing != null && controller.State != EnemyState.Attack)
                    statusRing.enabled = false;
            }
        }
        else if (kind == CreatureKind.FloodgateKeeper)
        {
            if (Time.time < exposedUntil)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
                controller.visual.color = Color.Lerp(new Color(1f, 0.86f, 0.32f, 1f), new Color(0.45f, 1f, 0.95f, 1f), pulse);
                ShowStatusRing(0.72f, new Color(1f, 0.85f, 0.25f, 0.95f), 0f, 360f);
            }
            else if (statusRing != null && controller.State != EnemyState.Attack)
            {
                statusRing.enabled = false;
            }
        }
    }

    void ShowSurgeLane(Vector2 origin, Vector2 dir, float length, float width, Color color)
    {
        if (laneTelegraph == null)
            return;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (width * 0.5f);
        Vector2 end = origin + dir * length;
        laneTelegraph.SetPosition(0, origin + perp);
        laneTelegraph.SetPosition(1, end + perp);
        laneTelegraph.SetPosition(2, end - perp);
        laneTelegraph.SetPosition(3, origin - perp);
        laneTelegraph.startColor = laneTelegraph.endColor = color;
        laneTelegraph.enabled = true;
    }

    void ShowStatusRing(float radius, Color color, float startDeg, float endDeg)
    {
        // Circle outlines are disabled; creature states read through sprite tint and EnemyHitFeedback instead.
        if (statusRing != null) statusRing.enabled = false;
        return;
#pragma warning disable CS0162
        if (statusRing == null)
            return;
        bool fullLoop = Mathf.Abs((endDeg - startDeg) - 360f) < 1f;
        statusRing.loop = fullLoop;
        int count = statusRing.positionCount;
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (fullLoop ? count : count - 1) : 0f;
            float rad = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;
            statusRing.SetPosition(i, new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f));
        }
        statusRing.startColor = statusRing.endColor = color;
        statusRing.enabled = true;
#pragma warning restore CS0162
    }
}
