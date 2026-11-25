
using UnityEngine;
using System.Collections.Generic;

public class SC_guard_chaser : MonoBehaviour
{
    [Header("Déplacement")]
    public float moveSpeed = 3f;
    public float pointReachDistance = 0.15f;

    [Header("Détection des étages")]
    [Tooltip("Distance verticale maximale pour considérer un étage.")]
    public float floorDetectionTolerance = 0.4f;

    [Tooltip("Priorité donnée au changement d'étage.")]
    public float floorPriority = 100f;

    [Header("Traque")]
    public float repathInterval = 0.25f;
    public float segmentSearchDistance = 100f;

    [Header("Debug")]
    public bool drawPath = true;

    private SC_player player;

    private List<SC_guard_point> path = new();
    private SC_guard_point currentNode;

    private Vector3 targetOnGraph;
    private bool hasTarget;

    private float repathTimer;

    private int playerFloor = -1;

    private class Segment
    {
        public SC_guard_point a;
        public SC_guard_point b;
        public Vector3 closest;
        public float distance;
    }

    private void Start()
    {
        player = SC_player.instance;

        FindClosestStartingNode();
        CalculatePath();
    }

    private void Update()
    {
        if (player == null)
        {
            player = SC_player.instance;
            if (player == null)
                return;
        }

        repathTimer += Time.deltaTime;

        if (repathTimer >= repathInterval)
        {
            repathTimer = 0f;
            CalculatePath();
        }

        MoveAlongPath();
    }

    // --------------------------------------------------
    // ETAGES
    // --------------------------------------------------

    private List<float> GetFloors()
    {
        HashSet<float> heights = new();

        SC_guard_point[] points =
            FindObjectsByType<SC_guard_point>(
                FindObjectsSortMode.None
            );

        foreach (var p in points)
        {
            heights.Add(p.transform.position.y);
        }

        List<float> floors = new(heights);
        floors.Sort();

        return floors;
    }

    private int GetFloorIndex(float y)
    {
        List<float> floors = GetFloors();

        int bestFloor = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < floors.Count; i++)
        {
            float distance = Mathf.Abs(y - floors[i]);

            if (distance < bestDistance &&
                distance <= floorDetectionTolerance)
            {
                bestDistance = distance;
                bestFloor = i;
            }
        }

        return bestFloor;
    }

    // --------------------------------------------------
    // RECHERCHE DE SEGMENT
    // --------------------------------------------------

    private Segment FindClosestSegment(Vector3 position)
    {
        Segment best = null;
        float bestDistance = segmentSearchDistance;

        SC_guard_point[] points =
            FindObjectsByType<SC_guard_point>(
                FindObjectsSortMode.None
            );

        foreach (var node in points)
        {
            foreach (var connected in node.connections)
            {
                if (connected == null)
                    continue;

                Vector3 closest = ClosestPointOnSegment(
                    position,
                    node.transform.position,
                    connected.transform.position
                );

                float distance = Vector3.Distance(
                    position,
                    closest
                );

                if (distance < bestDistance)
                {
                    bestDistance = distance;

                    best = new Segment
                    {
                        a = node,
                        b = connected,
                        closest = closest,
                        distance = distance
                    };
                }
            }
        }

        return best;
    }

    private Vector3 ClosestPointOnSegment(
        Vector3 point,
        Vector3 a,
        Vector3 b)
    {
        Vector3 ab = b - a;

        float t = Vector3.Dot(point - a, ab) /
                  Mathf.Max(ab.sqrMagnitude, 0.0001f);

        t = Mathf.Clamp01(t);

        return a + ab * t;
    }

    // --------------------------------------------------
    // CALCUL DU CHEMIN A*
    // --------------------------------------------------

    private void CalculatePath()
    {
        if (player == null)
            return;

        Segment playerSegment = FindClosestSegment(
            player.transform.position
        );

        if (playerSegment == null)
            return;

        Segment enemySegment = FindClosestSegment(
            transform.position
        );

        if (enemySegment == null)
            return;

        playerFloor = GetFloorIndex(
            player.transform.position.y
        );

        SC_guard_point start = enemySegment.a;
        SC_guard_point endA = playerSegment.a;
        SC_guard_point endB = playerSegment.b;

        List<SC_guard_point> pathA = FindPath(start, endA);
        List<SC_guard_point> pathB = FindPath(start, endB);

        float costA = CalculatePathCost(pathA);
        float costB = CalculatePathCost(pathB);

        // On prend le meilleur des deux itinéraires.
        if (costA <= costB)
        {
            path = pathA;
            currentNode = start;
            targetOnGraph = playerSegment.closest;
        }
        else
        {
            path = pathB;
            currentNode = start;
            targetOnGraph = playerSegment.closest;
        }

        hasTarget = path.Count > 0;
        pathIndex = 0;
        // On ne force pas l'ennemi à rejoindre le premier point :
        // il rejoint directement le chemin depuis sa position.
    }

    private float CalculatePathCost(List<SC_guard_point> p)
    {
        if (p == null || p.Count == 0)
            return float.MaxValue;

        float cost = 0f;

        for (int i = 0; i < p.Count - 1; i++)
        {
            cost += Vector3.Distance(
                p[i].transform.position,
                p[i + 1].transform.position
            );
        }

        return cost;
    }

    private List<SC_guard_point> FindPath(
        SC_guard_point start,
        SC_guard_point target)
    {
        List<SC_guard_point> open = new() { start };

        Dictionary<SC_guard_point, float> gScore = new();
        Dictionary<SC_guard_point, float> fScore = new();

        Dictionary<SC_guard_point, SC_guard_point> cameFrom = new();

        gScore[start] = 0f;

        fScore[start] = Heuristic(start, target);

        while (open.Count > 0)
        {
            SC_guard_point current = open[0];

            foreach (var node in open)
            {
                if (fScore[node] < fScore[current])
                    current = node;
            }

            if (current == target)
                return ReconstructPath(cameFrom, current);

            open.Remove(current);

            foreach (var neighbor in current.connections)
            {
                if (neighbor == null)
                    continue;

                float tentativeG =
                    gScore[current] +
                    Vector3.Distance(
                        current.transform.position,
                        neighbor.transform.position
                    );

                if (!gScore.ContainsKey(neighbor) ||
                    tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;

                    fScore[neighbor] =
                        tentativeG + Heuristic(neighbor, target);

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }

        return new List<SC_guard_point>();
    }

    private float Heuristic(
        SC_guard_point a,
        SC_guard_point b)
    {
        return Vector3.Distance(
            a.transform.position,
            b.transform.position
        );
    }

    private List<SC_guard_point> ReconstructPath(
        Dictionary<SC_guard_point, SC_guard_point> cameFrom,
        SC_guard_point current)
    {
        List<SC_guard_point> result = new() { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            result.Insert(0, current);
        }

        return result;
    }

    // --------------------------------------------------
    // DEPLACEMENT
    // --------------------------------------------------


    private int pathIndex = 0;

    private void MoveAlongPath()
    {
        if (!hasTarget || path == null || path.Count == 0)
            return;

        // Cas où le chemin ne contient qu'un point.
        if (path.Count == 1)
        {
            MoveOnSegment(
                path[0].transform.position,
                targetOnGraph
            );

            return;
        }

        // Segment actuel du chemin.
        Vector3 a = path[pathIndex].transform.position;
        Vector3 b;

        if (pathIndex < path.Count - 1)
        {
            b = path[pathIndex + 1].transform.position;
        }
        else
        {
            b = targetOnGraph;
        }

        MoveOnSegment(a, b);

        // Vérification de l'arrivée au prochain point.
        Vector3 destination = b;

        if (Vector3.Distance(transform.position, destination)
            <= pointReachDistance)
        {
            if (pathIndex < path.Count - 1)
            {
                pathIndex++;
            }
            else
            {
                hasTarget = false;
            }
        }
    }

    private void MoveOnSegment(Vector3 a, Vector3 b)
    {
        Vector3 segment = b - a;

        float length = segment.magnitude;

        if (length < 0.001f)
            return;

        Vector3 direction = segment / length;

        // Projection de l'ennemi sur le segment.
        float progress = Vector3.Dot(
            transform.position - a,
            direction
        );

        // On avance uniquement dans la direction du segment.
        progress += moveSpeed * Time.deltaTime;

        progress = Mathf.Clamp(progress, 0f, length);

        // Position strictement sur le tracé.
        transform.position = a + direction * progress;

        // Orientation.
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    // --------------------------------------------------
    // DEPART
    // --------------------------------------------------

    private void FindClosestStartingNode()
    {
        SC_guard_point[] points =
            FindObjectsByType<SC_guard_point>(
                FindObjectsSortMode.None
            );

        float best = float.MaxValue;

        foreach (var p in points)
        {
            float d = Vector3.Distance(
                transform.position,
                p.transform.position
            );

            if (d < best)
            {
                best = d;
                currentNode = p;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawPath || path == null)
            return;

        Gizmos.color = Color.red;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(
                path[i].transform.position,
                path[i + 1].transform.position
            );
        }

        if (hasTarget)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawSphere(targetOnGraph, 0.2f);
        }
    }
}
