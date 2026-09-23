using UnityEngine;
using System.Collections.Generic;

public class SC_guard_tracker : MonoBehaviour
{
    [Header("Cible")]
    public Transform target;

    [Header("Graphe")]
    public List<SC_guard_point> allPoints = new List<SC_guard_point>();

    [Header("Détection d'étage")]
    [Tooltip("Différence verticale maximale pour considérer que le joueur est sur le même étage.")]
    public float verticalTolerance = 1.5f;

    [Tooltip("Distance maximale entre le joueur et le graphe pour être considéré comme connecté au graphe.")]
    public float maxPlayerDistanceFromGraph = 5f;

    [Header("Recalcul")]
    public float repathInterval = 0.25f;

    [Tooltip("Recalcule immédiatement le chemin lorsque le joueur change de segment.")]
    public bool repathWhenSegmentChanges = true;

    [Header("Debug")]
    public bool drawPath = true;
    public Color pathColor = Color.red;

    private List<SC_guard_point> currentPath = new List<SC_guard_point>();

    private int currentPathIndex = 0;

    private SC_guard_point currentPlayerPoint;
    private SC_guard_point lastPlayerPoint;

    private float repathTimer;

    public IReadOnlyList<SC_guard_point> CurrentPath => currentPath;

    public SC_guard_point CurrentDestination
    {
        get
        {
            if (currentPath == null || currentPath.Count == 0)
                return null;

            if (currentPathIndex >= currentPath.Count)
                return null;

            return currentPath[currentPathIndex];
        }
    }

    private void Start()
    {
        if (target == null && SC_player.instance != null)
            target = SC_player.instance.transform;

        if (allPoints.Count == 0)
            FindAllPoints();

        RecalculatePath();
    }

    private void Update()
    {
        if (target == null)
        {
            if (SC_player.instance != null)
                target = SC_player.instance.transform;

            return;
        }

        repathTimer -= Time.deltaTime;

        SC_guard_point newPlayerPoint = FindClosestValidPointToPlayer();

        bool playerChangedPoint = newPlayerPoint != currentPlayerPoint;

        currentPlayerPoint = newPlayerPoint;

        if (playerChangedPoint && repathWhenSegmentChanges)
        {
            RecalculatePath();
        }
        else if (repathTimer <= 0f)
        {
            RecalculatePath();
        }
    }

    private void FindAllPoints()
    {
        SC_guard_point[] points =
            FindObjectsByType<SC_guard_point>(FindObjectsSortMode.None);

        allPoints.Clear();
        allPoints.AddRange(points);
    }

    // =========================================================
    // CALCUL DU POINT DU GRAPHE LE PLUS PERTINENT POUR LE JOUEUR
    // =========================================================

    private SC_guard_point FindClosestValidPointToPlayer()
    {
        if (target == null)
            return null;

        SC_guard_point closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (SC_guard_point point in allPoints)
        {
            if (point == null)
                continue;

            float verticalDifference =
                Mathf.Abs(target.position.y - point.transform.position.y);

            // Le joueur est considéré comme appartenant à cet étage
            // uniquement si sa différence verticale reste dans la tolérance.
            if (verticalDifference > verticalTolerance)
                continue;

            float distance =
                Vector3.Distance(target.position, point.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = point;
            }
        }

        return closest;
    }

    // =========================================================
    // A*
    // =========================================================

    private void RecalculatePath()
    {
        repathTimer = repathInterval;

        if (currentPlayerPoint == null)
            return;

        SC_guard_point start = FindClosestPointToGuard();

        if (start == null)
            return;

        List<SC_guard_point> newPath =
            FindShortestPath(start, currentPlayerPoint);

        if (newPath == null)
            return;

        currentPath = newPath;

        // Le premier node est généralement celui sur lequel
        // le garde se trouve déjà.
        if (currentPath.Count > 1)
            currentPathIndex = 1;
        else
            currentPathIndex = 0;

        lastPlayerPoint = currentPlayerPoint;
    }

    private SC_guard_point FindClosestPointToGuard()
    {
        SC_guard_point closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (SC_guard_point point in allPoints)
        {
            if (point == null)
                continue;

            float distance =
                Vector3.Distance(transform.position, point.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = point;
            }
        }

        return closest;
    }

    private List<SC_guard_point> FindShortestPath(
        SC_guard_point start,
        SC_guard_point goal)
    {
        if (start == null || goal == null)
            return null;

        List<SC_guard_point> openSet =
            new List<SC_guard_point>();

        HashSet<SC_guard_point> closedSet =
            new HashSet<SC_guard_point>();

        Dictionary<SC_guard_point, SC_guard_point> cameFrom =
            new Dictionary<SC_guard_point, SC_guard_point>();

        Dictionary<SC_guard_point, float> gScore =
            new Dictionary<SC_guard_point, float>();

        Dictionary<SC_guard_point, float> fScore =
            new Dictionary<SC_guard_point, float>();

        foreach (SC_guard_point point in allPoints)
        {
            gScore[point] = Mathf.Infinity;
            fScore[point] = Mathf.Infinity;
        }

        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);

        openSet.Add(start);

        while (openSet.Count > 0)
        {
            SC_guard_point current = GetLowestFScore(openSet, fScore);

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (SC_guard_point neighbour in current.connections)
            {
                if (neighbour == null)
                    continue;

                if (closedSet.Contains(neighbour))
                    continue;

                float distance =
                    Vector3.Distance(
                        current.transform.position,
                        neighbour.transform.position
                    );

                float tentativeG =
                    gScore[current] + distance;

                if (!openSet.Contains(neighbour))
                    openSet.Add(neighbour);

                if (tentativeG < gScore[neighbour])
                {
                    cameFrom[neighbour] = current;

                    gScore[neighbour] = tentativeG;

                    fScore[neighbour] =
                        tentativeG +
                        Heuristic(neighbour, goal);
                }
            }
        }

        return null;
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

    private SC_guard_point GetLowestFScore(
        List<SC_guard_point> list,
        Dictionary<SC_guard_point, float> scores)
    {
        SC_guard_point best = list[0];

        for (int i = 1; i < list.Count; i++)
        {
            if (scores[list[i]] < scores[best])
                best = list[i];
        }

        return best;
    }

    private List<SC_guard_point> ReconstructPath(
        Dictionary<SC_guard_point, SC_guard_point> cameFrom,
        SC_guard_point current)
    {
        List<SC_guard_point> path =
            new List<SC_guard_point>();

        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();

        return path;
    }

    // =========================================================
    // API POUR LE MOVEMENT
    // =========================================================

    public Vector3 GetCurrentDestination()
    {
        SC_guard_point point = CurrentDestination;

        if (point == null)
            return transform.position;

        return point.transform.position;
    }

    public bool HasPath()
    {
        return currentPath != null &&
               currentPath.Count > 0 &&
               currentPathIndex < currentPath.Count;
    }

    public void AdvanceToNextPoint()
    {
        currentPathIndex++;

        if (currentPathIndex >= currentPath.Count)
        {
            currentPathIndex = currentPath.Count - 1;
        }
    }

    public float DistanceToCurrentDestination()
    {
        if (!HasPath())
            return Mathf.Infinity;

        return Vector3.Distance(
            transform.position,
            CurrentDestination.transform.position
        );
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // Zone verticale autour du garde.
        Vector3 center = transform.position;

        Gizmos.color = new Color(
            0f,
            1f,
            1f,
            0.15f
        );

        Vector3 size = new Vector3(
            2f,
            verticalTolerance * 2f,
            2f
        );

        Gizmos.DrawWireCube(center, size);

        // Chemin actuel
        if (!drawPath || currentPath == null)
            return;

        Gizmos.color = pathColor;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            if (currentPath[i] == null ||
                currentPath[i + 1] == null)
                continue;

            Gizmos.DrawLine(
                currentPath[i].transform.position,
                currentPath[i + 1].transform.position
            );
        }

        // Destination
        if (CurrentDestination != null)
        {
            Gizmos.DrawSphere(
                CurrentDestination.transform.position,
                0.25f
            );
        }
    }
}