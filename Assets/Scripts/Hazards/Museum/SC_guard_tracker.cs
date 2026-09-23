using UnityEngine;
using System.Collections.Generic;

public class SC_guard_tracker : MonoBehaviour
{
    [Header("Cible")]
    public Transform target;

    [Header("Graphe")]
    public List<SC_guard_point> allPoints =
        new List<SC_guard_point>();

    [Header("Détection d'étage")]
    public float verticalTolerance = 1.5f;

    [Header("Connexions")]
    [Tooltip("Tolérance utilisée pour reconnaître une ligne horizontale/verticale.")]
    public float axisTolerance = 0.05f;

    [Header("Recalcul")]
    public float repathInterval = 0.25f;

    public bool repathWhenSegmentChanges = true;

    [Header("Debug")]
    public bool drawPath = true;
    public Color pathColor = Color.red;

    private List<SC_guard_point> currentPath =
        new List<SC_guard_point>();

    private int currentPathIndex;

    private SC_guard_point currentPlayerPoint;

    private float repathTimer;

    public IReadOnlyList<SC_guard_point> CurrentPath
    {
        get { return currentPath; }
    }

    public SC_guard_point CurrentDestination
    {
        get
        {
            if (currentPath == null ||
                currentPath.Count == 0)
            {
                return null;
            }

            if (currentPathIndex < 0 ||
                currentPathIndex >= currentPath.Count)
            {
                return null;
            }

            return currentPath[currentPathIndex];
        }
    }

    public SC_guard_point CurrentSegmentStart
    {
        get
        {
            if (!HasPath())
                return null;

            if (currentPathIndex == 0)
                return currentPath[0];

            return currentPath[currentPathIndex - 1];
        }
    }

    public SC_guard_point CurrentSegmentEnd
    {
        get
        {
            return CurrentDestination;
        }
    }

    private void Start()
    {
        if (target == null &&
            SC_player.instance != null)
        {
            target =
                SC_player.instance.transform;
        }

        if (allPoints.Count == 0)
            FindAllPoints();

        currentPlayerPoint =
            FindPlayerPoint();

        RecalculatePath();
    }

    private void Update()
    {
        if (target == null)
            return;

        repathTimer -= Time.deltaTime;

        SC_guard_point newPoint =
            FindPlayerPoint();

        bool changed =
            newPoint != currentPlayerPoint;

        currentPlayerPoint =
            newPoint;

        if (changed &&
            repathWhenSegmentChanges)
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
            FindObjectsByType<SC_guard_point>(
                FindObjectsSortMode.None
            );

        allPoints.Clear();
        allPoints.AddRange(points);
    }

    /*
     * ============================================================
     * POINT DU JOUEUR
     * ============================================================
     *
     * Même étage :
     * on choisit le point selon le X du joueur.
     */
    private SC_guard_point FindPlayerPoint()
    {
        if (target == null)
            return null;

        SC_guard_point best = null;
        float bestX = Mathf.Infinity;

        foreach (SC_guard_point point in allPoints)
        {
            if (point == null)
                continue;

            float yDifference =
                Mathf.Abs(
                    target.position.y -
                    point.transform.position.y
                );

            if (yDifference > verticalTolerance)
                continue;

            float xDifference =
                Mathf.Abs(
                    target.position.x -
                    point.transform.position.x
                );

            if (xDifference < bestX)
            {
                bestX = xDifference;
                best = point;
            }
        }

        return best;
    }

    /*
     * ============================================================
     * RECALCUL
     * ============================================================
     */
    private void RecalculatePath()
    {
        repathTimer = repathInterval;

        if (currentPlayerPoint == null)
            return;

        SC_guard_point start =
            FindClosestPointToGuard();

        if (start == null)
            return;

        List<SC_guard_point> newPath =
            FindShortestPath(
                start,
                currentPlayerPoint
            );

        if (newPath == null ||
            newPath.Count == 0)
        {
            return;
        }

        /*
         * On conserve le segment actuel si possible.
         * Cela évite que le garde reparte constamment
         * du début du nouveau chemin.
         */
        SC_guard_point oldDestination =
            CurrentDestination;

        currentPath = newPath;

        int preservedIndex = -1;

        if (oldDestination != null)
        {
            for (int i = 0; i < currentPath.Count; i++)
            {
                if (currentPath[i] == oldDestination)
                {
                    preservedIndex = i;
                    break;
                }
            }
        }

        if (preservedIndex > 0)
        {
            currentPathIndex =
                preservedIndex;
        }
        else if (currentPath.Count > 1)
        {
            currentPathIndex = 1;
        }
        else
        {
            currentPathIndex = 0;
        }
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
                Vector2.Distance(
                    transform.position,
                    point.transform.position
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = point;
            }
        }

        return closest;
    }

    /*
     * ============================================================
     * A*
     * ============================================================
     *
     * UNIQUEMENT les connexions horizontales
     * et verticales.
     */
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

        Dictionary<
            SC_guard_point,
            SC_guard_point
        > cameFrom =
            new Dictionary<
                SC_guard_point,
                SC_guard_point
            >();

        Dictionary<
            SC_guard_point,
            float
        > gScore =
            new Dictionary<
                SC_guard_point,
                float
            >();

        Dictionary<
            SC_guard_point,
            float
        > fScore =
            new Dictionary<
                SC_guard_point,
                float
            >();

        foreach (SC_guard_point point in allPoints)
        {
            if (point == null)
                continue;

            gScore[point] = Mathf.Infinity;
            fScore[point] = Mathf.Infinity;
        }

        gScore[start] = 0f;
        fScore[start] =
            Heuristic(start, goal);

        openSet.Add(start);

        while (openSet.Count > 0)
        {
            SC_guard_point current =
                GetLowestFScore(
                    openSet,
                    fScore
                );

            if (current == goal)
            {
                return ReconstructPath(
                    cameFrom,
                    current
                );
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (SC_guard_point neighbour
                     in current.connections)
            {
                if (neighbour == null)
                    continue;

                if (closedSet.Contains(neighbour))
                    continue;

                Vector2 delta =
                    neighbour.transform.position -
                    current.transform.position;

                bool horizontal =
                    Mathf.Abs(delta.y) <= axisTolerance &&
                    Mathf.Abs(delta.x) > axisTolerance;

                bool vertical =
                    Mathf.Abs(delta.x) <= axisTolerance &&
                    Mathf.Abs(delta.y) > axisTolerance;

                /*
                 * Toute connexion qui n'est pas
                 * strictement H ou V est ignorée.
                 */
                if (!horizontal && !vertical)
                    continue;

                float distance =
                    Mathf.Abs(delta.x) +
                    Mathf.Abs(delta.y);

                float tentativeG =
                    gScore[current] +
                    distance;

                if (!openSet.Contains(neighbour))
                    openSet.Add(neighbour);

                if (tentativeG <
                    gScore[neighbour])
                {
                    cameFrom[neighbour] =
                        current;

                    gScore[neighbour] =
                        tentativeG;

                    fScore[neighbour] =
                        tentativeG +
                        Heuristic(
                            neighbour,
                            goal
                        );
                }
            }
        }

        return null;
    }

    private float Heuristic(
        SC_guard_point a,
        SC_guard_point b)
    {
        return
            Mathf.Abs(
                a.transform.position.x -
                b.transform.position.x
            )
            +
            Mathf.Abs(
                a.transform.position.y -
                b.transform.position.y
            );
    }

    private SC_guard_point GetLowestFScore(
        List<SC_guard_point> list,
        Dictionary<
            SC_guard_point,
            float
        > scores)
    {
        SC_guard_point best = list[0];

        for (int i = 1; i < list.Count; i++)
        {
            if (scores[list[i]] <
                scores[best])
            {
                best = list[i];
            }
        }

        return best;
    }

    private List<SC_guard_point> ReconstructPath(
        Dictionary<
            SC_guard_point,
            SC_guard_point
        > cameFrom,
        SC_guard_point current)
    {
        List<SC_guard_point> path =
            new List<SC_guard_point>();

        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current =
                cameFrom[current];

            path.Add(current);
        }

        path.Reverse();

        return path;
    }

    /*
     * ============================================================
     * API POUR LE MOVEMENT
     * ============================================================
     */

    public bool HasPath()
    {
        return
            currentPath != null &&
            currentPath.Count > 0 &&
            currentPathIndex <
            currentPath.Count;
    }

    public void AdvanceToNextPoint()
    {
        if (!HasPath())
            return;

        currentPathIndex++;

        if (currentPathIndex >=
            currentPath.Count)
        {
            currentPathIndex =
                currentPath.Count - 1;
        }
    }

    public Vector3 GetCurrentDestination()
    {
        if (CurrentDestination == null)
            return transform.position;

        return CurrentDestination.transform.position;
    }

    public float DistanceToCurrentDestination()
    {
        if (!HasPath())
            return Mathf.Infinity;

        return Vector2.Distance(
            transform.position,
            CurrentDestination.transform.position
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPath ||
            currentPath == null)
        {
            return;
        }

        Gizmos.color = pathColor;

        for (int i = 0;
             i < currentPath.Count - 1;
             i++)
        {
            if (currentPath[i] == null ||
                currentPath[i + 1] == null)
            {
                continue;
            }

            Gizmos.DrawLine(
                currentPath[i].transform.position,
                currentPath[i + 1].transform.position
            );
        }

        if (CurrentDestination != null)
        {
            Gizmos.DrawSphere(
                CurrentDestination.transform.position,
                0.25f
            );
        }
    }
}
