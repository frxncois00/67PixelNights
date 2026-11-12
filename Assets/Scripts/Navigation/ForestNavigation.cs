using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Profiling;

public class ForestNavigation : MonoBehaviour
{
    public Tilemap ground;
    public CampfireSafeZone safe;
    [Min(.25f)]
    public float cellSize = .5f;
    [Min(100)]
    public int maximumSearch = 12000;
    [Min(.25f)]
    public float searchBudgetMilliseconds = 4;
    struct Sample
    {
        public bool open;
        public float expires;
    }

    struct Candidate
    {
        public Vector2Int cell;
        public int score, serial;
    }

    readonly Dictionary<Vector2Int, Sample> cache = new Dictionary<Vector2Int, Sample>(16384);
    readonly Collider2D[] colliders = new Collider2D[32];
    readonly List<Candidate> heap = new List<Candidate>(4096);
    readonly Dictionary<Vector2Int, int> costs = new Dictionary<Vector2Int, int>(16384);
    readonly Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>(16384);
    static readonly Vector2Int[] steps =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };
    static readonly ProfilerMarker searchMarker = new ProfilerMarker("Forest.Navigation"), queryMarker = new ProfilerMarker("Forest.NavigationPhysics");
    public Rect? SimulatedObstacle { get; set; }
    public int LastVisited { get; private set; }
    public double LastMilliseconds { get; private set; }
    public int SearchCount { get; private set; }
    public int Revision { get; private set; }

    int serial, requestFrame = -1, requests;
    double frameSpent;
    bool wasSafe;
    float lastRadius;
    float Size => EnemyArchetype.Safe(cellSize, .5f, .25f, 2);

    public Vector2Int Cell(Vector2 p) => Vector2Int.RoundToInt(p / Size);
    public Vector2 Point(Vector2Int c) => (Vector2)c * Size;
    public void Invalidate()
    {
        cache.Clear();
        Revision++;
    }

    void Update()
    {
        if (safe && (safe.IsSafeZoneActive != wasSafe || safe.CurrentSafeRadius != lastRadius))
        {
            wasSafe = safe.IsSafeZoneActive;
            lastRadius = safe.CurrentSafeRadius;
            Revision++;
        }
    }

    // Shared AI/spawn budget. Explicit validation callers can still use Find directly.
    public bool TryReserveSearch()
    {
        if (requestFrame != Time.frameCount)
        {
            requestFrame = Time.frameCount;
            requests = 0;
            frameSpent = 0;
        }

        if (requests >= 2 || frameSpent >= searchBudgetMilliseconds)
            return false;
        requests++;
        return true;
    }

    bool Before(Candidate a, Candidate b) => a.score < b.score || (a.score == b.score && a.serial < b.serial);
    void Enqueue(Vector2Int c, Vector2Int goal, int cost, bool escape)
    {
        var entry = new Candidate
        {
            cell = c,
            score = cost + (escape ? 0 : Mathf.Abs(c.x - goal.x) + Mathf.Abs(c.y - goal.y)),
            serial = serial++
        };
        int i = heap.Count;
        heap.Add(entry);
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (!Before(entry, heap[p]))
                break;
            heap[i] = heap[p];
            i = p;
        }

        heap[i] = entry;
    }

    Candidate Pop()
    {
        var result = heap[0];
        var last = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        if (heap.Count == 0)
            return result;
        int i = 0;
        while (i * 2 + 1 < heap.Count)
        {
            int c = i * 2 + 1;
            if (c + 1 < heap.Count && Before(heap[c + 1], heap[c]))
                c++;
            if (!Before(heap[c], last))
                break;
            heap[i] = heap[c];
            i = c;
        }

        heap[i] = last;
        return result;
    }

    public bool Terrain(Vector2 p)
    {
        if (!ground)
            return false;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (!ground.HasTile(ground.WorldToCell(p + new Vector2(x, y) * .25f)))
                    return false;
        return true;
    }

    public bool Open(Vector2Int c)
    {
        if (SimulatedObstacle.HasValue && SimulatedObstacle.Value.Contains(Point(c)))
            return false;
        if (cache.TryGetValue(c, out var hit) && hit.expires > Time.time)
            return hit.open;
        Vector2 p = Point(c);
        bool open = Terrain(p);
        if (open)
        {
            using (queryMarker.Auto())
            {
                var filter = ContactFilter2D.noFilter;
                filter.useTriggers = false;
                int n = Physics2D.OverlapCircle(p, .27f, filter, colliders);
                open = n < colliders.Length;
                for (int i = 0; i < n; i++)
                {
                    var col = colliders[i];
                    if (col && !col.isTrigger && (!col.attachedRigidbody || col.attachedRigidbody.bodyType == RigidbodyType2D.Static))
                    {
                        open = false;
                        break;
                    }
                }
            }
        }

        cache[c] = new Sample
        {
            open = open,
            expires = Time.time + 1
        };
        return open;
    }

    public bool Find(Vector2 from, Vector2 to, List<Vector2> output, bool ignoreSafety = false, bool escape = false, System.Predicate<Vector2> permitted = null)
    {
        using (searchMarker.Auto())
        {
            long startTime = System.Diagnostics.Stopwatch.GetTimestamp();
            SearchCount++;
            LastVisited = 0;
            output.Clear();
            if (!ground || !safe)
                return false;
            var start = Cell(from);
            var goal = Cell(to);
            heap.Clear();
            parents.Clear();
            costs.Clear();
            serial = 0;
            Enqueue(start, goal, 0, escape);
            parents[start] = start;
            costs[start] = 0;
            Vector2Int end = start;
            bool found = false;
            int budget = Mathf.Clamp(maximumSearch, 100, 30000);
            while (heap.Count > 0 && budget-- > 0)
            {
                var c = Pop().cell;
                LastVisited++;
                bool outside = !safe.IsPositionSafe(Point(c));
                if (c != start && (escape ? outside && Vector2.Distance(Point(c), safe.transform.position) > safe.CurrentSafeRadius + .4f : (Point(c) - Point(goal)).sqrMagnitude <= Size * Size))
                {
                    end = c;
                    found = true;
                    break;
                }

                if ((LastVisited & 63) == 0 && (System.Diagnostics.Stopwatch.GetTimestamp() - startTime) * 1000.0 / System.Diagnostics.Stopwatch.Frequency > EnemyArchetype.Safe(searchBudgetMilliseconds, 4, .25f, 50))
                    break;
                foreach (var d in steps)
                {
                    var next = c + d;
                    if (parents.ContainsKey(next) || !Open(next))
                        continue;
                    if (permitted != null && !permitted(Point(next)))
                        continue;
                    if (!ignoreSafety && !escape && safe.IsPositionSafe(Point(next)))
                        continue;
                    parents[next] = c;
                    costs[next] = costs[c] + 1;
                    Enqueue(next, goal, costs[next], escape);
                }
            }

            LastMilliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - startTime) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            frameSpent += LastMilliseconds;
            if (!found)
                return false;
            for (var c = end; c != start; c = parents[c])
                output.Add(Point(c));
            output.Reverse();
            return true;
        }
    }
}
