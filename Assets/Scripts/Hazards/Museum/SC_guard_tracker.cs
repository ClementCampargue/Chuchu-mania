
using UnityEngine;
using System.Collections.Generic;

public class SC_guard_ai : MonoBehaviour
{
    [Header("Déplacement")]
    public float moveSpeed = 3f;
    public float reachDistance = 0.08f;

    [Header("Détection des segments")]
    public float maxSegmentDistance = 3f;
    public float repathInterval = 0.35f;

    [Header("Gestion des étages")]
    public float floorHeightTolerance = 0.6f;
    public float verticalTolerance = 0.08f;

    [Header("Debug")]
    public bool drawPath = true;
    public bool showLogs = true;

    private List<SC_guard_point> allPoints = new List<SC_guard_point>();

    private List<Vector3> currentPath = new List<Vector3>();
    private int pathIndex = 0;

    private float repathTimer = 0f;

    private SC_guard_point lastTargetA;
    private SC_guard_point lastTargetB;

    private struct Segment
    {
        public SC_guard_point a;
        public SC_guard_point b;
        public Vector3 closest;
        public float distance;

        public Segment(
            SC_guard_point a,
            SC_guard_point b,
            Vector3 closest,
            float distance)
        {
            this.a = a;
            this.b = b;
            this.closest = closest;
            this.distance = distance;
        }
    }

    private void Start()
    {
        RefreshPoints();

        if (allPoints.Count == 0)
        {
            Debug.LogError("SC_guard_ai : aucun SC_guard_point trouvé.");
        }
    }

    private void Update()
    {
        if (SC_player.instance == null)
            return;

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            CalculatePath();
        }

        MoveAlongPath();
    }

    private void RefreshPoints()
    {
        allPoints.Clear();

        SC_guard_point[] found =
            FindObjectsByType<SC_guard_point>(FindObjectsSortMode.None);

        allPoints.AddRange(found);
    }

    // -------------------------------------------------
    // ÉTAGES
    // -------------------------------------------------

    private bool TryGetFloor(float y, out int floorId, out float floorY)
    {
        floorId = -1;
        floorY = 0f;

        float bestDistance = float.MaxValue;

        foreach (SC_guard_point point in allPoints)
        {
            if (point == null || !point.isFloorReference)
                continue;

            float distance = Mathf.Abs(
                point.transform.position.y - y
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                floorId = point.floorId;
                floorY = point.transform.position.y;
            }
        }

        return bestDistance <= floorHeightTolerance;
    }

    // -1 : descendre
    //  0 : aucune priorité verticale
    //  1 : monter

    private int GetVerticalDirection(float enemyY, float playerY)
    {
        if (playerY < enemyY - floorHeightTolerance)
            return -1;

        if (playerY > enemyY + floorHeightTolerance)
            return 1;

        return 0;
    }

    private bool CanMoveInDirection(
        Vector3 from,
        Vector3 to,
        int verticalDirection)
    {
        if (verticalDirection == 0)
            return true;

        float deltaY = to.y - from.y;

        if (verticalDirection == -1 && deltaY > verticalTolerance)
            return false;

        if (verticalDirection == 1 && deltaY < -verticalTolerance)
            return false;

        return true;
    }

    // -------------------------------------------------
    // RECHERCHE DU SEGMENT LE PLUS PROCHE
    // -------------------------------------------------

    private bool FindClosestSegment(
        Vector3 position,
        out Segment result)
    {
        result = default;

        float bestDistance = float.MaxValue;
        bool found = false;

        HashSet<string> checkedEdges = new HashSet<string>();

        foreach (SC_guard_point point in allPoints)
        {
            if (point == null)
                continue;

            foreach (SC_guard_point connected in point.connections)
            {
                if (connected == null)
                    continue;

                int id1 = point.GetInstanceID();
                int id2 = connected.GetInstanceID();

                string edgeKey =
                    Mathf.Min(id1, id2) + "_" +
                    Mathf.Max(id1, id2);

                if (checkedEdges.Contains(edgeKey))
                    continue;

                checkedEdges.Add(edgeKey);

                Vector3 a = point.transform.position;
                Vector3 b = connected.transform.position;

                Vector3 closest = ClosestPointOnSegment(
                    position,
                    a,
                    b
                );

                float distance = Vector3.Distance(
                    position,
                    closest
                );

                if (distance < bestDistance)
                {
                    bestDistance = distance;

                    result = new Segment(
                        point,
                        connected,
                        closest,
                        distance
                    );

                    found = true;
                }
            }
        }

        return found && bestDistance <= maxSegmentDistance;
    }

    private Vector3 ClosestPointOnSegment(
        Vector3 position,
        Vector3 a,
        Vector3 b)
    {
        Vector3 ab = b - a;

        float length = ab.sqrMagnitude;

        if (length < 0.0001f)
            return a;

        float t = Vector3.Dot(position - a, ab) / length;

        t = Mathf.Clamp01(t);

        return a + ab * t;
    }

    // -------------------------------------------------
    // DIJKSTRA
    // -------------------------------------------------

    private bool FindPath(
        SC_guard_point start,
        SC_guard_point target,
        int verticalDirection,
        out List<SC_guard_point> result,
        out float resultCost)
    {
        result = new List<SC_guard_point>();
        resultCost = float.MaxValue;

        if (start == null || target == null)
            return false;

        Dictionary<SC_guard_point, float> distances =
            new Dictionary<SC_guard_point, float>();

        Dictionary<SC_guard_point, SC_guard_point> previous =
            new Dictionary<SC_guard_point, SC_guard_point>();

        List<SC_guard_point> unvisited =
            new List<SC_guard_point>();

        foreach (SC_guard_point point in allPoints)
        {
            distances[point] = float.MaxValue;
            unvisited.Add(point);
        }

        distances[start] = 0f;

        while (unvisited.Count > 0)
        {
            SC_guard_point current = null;
            float bestDistance = float.MaxValue;

            foreach (SC_guard_point point in unvisited)
            {
                if (distances[point] < bestDistance)
                {
                    bestDistance = distances[point];
                    current = point;
                }
            }

            if (current == null || bestDistance == float.MaxValue)
                break;

            if (current == target)
                break;

            unvisited.Remove(current);

            foreach (SC_guard_point neighbor in current.connections)
            {
                if (neighbor == null || !unvisited.Contains(neighbor))
                    continue;

                Vector3 from = current.transform.position;
                Vector3 to = neighbor.transform.position;

                if (!CanMoveInDirection(
                    from,
                    to,
                    verticalDirection))
                    continue;

                float edgeCost = Vector3.Distance(from, to);
                float newCost = bestDistance + edgeCost;

                if (newCost < distances[neighbor])
                {
                    distances[neighbor] = newCost;
                    previous[neighbor] = current;
                }
            }
        }

        if (distances[target] == float.MaxValue)
            return false;

        resultCost = distances[target];

        SC_guard_point step = target;

        while (step != start)
        {
            result.Insert(0, step);

            if (!previous.ContainsKey(step))
                return false;

            step = previous[step];
        }

        result.Insert(0, start);

        return true;
    }

    // -------------------------------------------------
    // CONSTRUCTION DU TRAJET
    // -------------------------------------------------

    private void CalculatePath()
    {
        if (allPoints.Count == 0)
            RefreshPoints();

        if (!FindClosestSegment(transform.position, out Segment enemySegment))
        {
            if (showLogs)
                Debug.LogWarning("Ennemi trop éloigné du réseau de segments.");

            currentPath.Clear();
            return;
        }

        Vector3 playerPosition = SC_player.instance.transform.position;

        if (!FindClosestSegment(playerPosition, out Segment playerSegment))
        {
            if (showLogs)
                Debug.LogWarning("Joueur trop éloigné du réseau de segments.");

            currentPath.Clear();
            return;
        }

        int verticalDirection = GetVerticalDirection(
            enemySegment.closest.y,
            playerSegment.closest.y
        );

        if (TryGetFloor(playerSegment.closest.y, out int playerFloor, out float playerFloorY))
        {
            if (TryGetFloor(enemySegment.closest.y, out int enemyFloor, out float enemyFloorY))
            {
                if (playerFloor != enemyFloor)
                {
                    verticalDirection = GetVerticalDirection(
                        enemyFloorY,
                        playerFloorY
                    );
                }
                else
                {
                    verticalDirection = 0;
                }
            }
        }

        List<Vector3> bestPath = null;
        float bestCost = float.MaxValue;

        // Les deux sens de sortie du segment ennemi.
        // Les deux sens d'entrée sur le segment du joueur.

        SC_guard_point[] starts =
        {
            enemySegment.a,
            enemySegment.b
        };

        SC_guard_point[] targets =
        {
            playerSegment.a,
            playerSegment.b
        };

        foreach (SC_guard_point startNode in starts)
        {
            if (!CanMoveInDirection(
                enemySegment.closest,
                startNode.transform.position,
                verticalDirection))
                continue;

            foreach (SC_guard_point targetNode in targets)
            {
                if (!CanMoveInDirection(
                    targetNode.transform.position,
                    playerSegment.closest,
                    verticalDirection))
                    continue;

                if (!FindPath(
                    startNode,
                    targetNode,
                    verticalDirection,
                    out List<SC_guard_point> nodes,
                    out float graphCost))
                    continue;

                float totalCost =
                    Vector3.Distance(
                        enemySegment.closest,
                        startNode.transform.position
                    )
                    + graphCost
                    + Vector3.Distance(
                        targetNode.transform.position,
                        playerSegment.closest
                    );

                if (totalCost >= bestCost)
                    continue;

                List<Vector3> candidate = new List<Vector3>();

                candidate.Add(enemySegment.closest);

                foreach (SC_guard_point node in nodes)
                {
                    if (candidate.Count == 0 ||
                        Vector3.Distance(
                            candidate[candidate.Count - 1],
                            node.transform.position
                        ) > 0.01f)
                    {
                        candidate.Add(node.transform.position);
                    }
                }

                if (Vector3.Distance(
                    candidate[candidate.Count - 1],
                    playerSegment.closest
                ) > 0.01f)
                {
                    candidate.Add(playerSegment.closest);
                }

                bestCost = totalCost;
                bestPath = candidate;
            }
        }

        // Cas particulier : ennemi et joueur sur le même segment.
        if (enemySegment.a == playerSegment.a &&
            enemySegment.b == playerSegment.b ||
            enemySegment.a == playerSegment.b &&
            enemySegment.b == playerSegment.a)
        {
            if (CanMoveInDirection(
                enemySegment.closest,
                playerSegment.closest,
                verticalDirection))
            {
                float directCost = Vector3.Distance(
                    enemySegment.closest,
                    playerSegment.closest
                );

                if (directCost < bestCost)
                {
                    bestPath = new List<Vector3>
                    {
                        enemySegment.closest,
                        playerSegment.closest
                    };
                }
            }
        }

        if (bestPath == null || bestPath.Count == 0)
        {
            if (showLogs)
                Debug.LogWarning(
                    "SC_guard_ai : aucun chemin valide vers le joueur."
                );

            currentPath.Clear();
            return;
        }

        currentPath = bestPath;
        pathIndex = 0;

        lastTargetA = playerSegment.a;
        lastTargetB = playerSegment.b;
    }

    // -------------------------------------------------
    // DÉPLACEMENT
    // -------------------------------------------------

    private void MoveAlongPath()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        if (pathIndex >= currentPath.Count)
            return;

        Vector3 target = currentPath[pathIndex];

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target) <= reachDistance)
        {
            pathIndex++;

            if (pathIndex >= currentPath.Count)
            {
                currentPath.Clear();
                pathIndex = 0;
            }
        }
    }

    // -------------------------------------------------
    // GIZMOS
    // -------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!drawPath || currentPath == null)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(
                currentPath[i],
                currentPath[i + 1]
            );
        }

        Gizmos.color = Color.red;

        if (pathIndex < currentPath.Count)
        {
            Gizmos.DrawSphere(
                currentPath[pathIndex],
                0.12f
            );
        }
    }
}
