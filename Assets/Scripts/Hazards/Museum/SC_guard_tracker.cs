
using UnityEngine;
using System.Collections.Generic;

public class SC_guard_ai : MonoBehaviour
{
    [Header("Validation du changement d'étage")]
    public float floorDetectionDelay = 0.25f;
    public float floorConfirmationTolerance = 0.2f;

    private float groundedTimer;
    private int confirmedPlayerFloor = -1;
    private float confirmedPlayerFloorY;

    [Header("Déplacement")]
    public float moveSpeed = 3f;
    public float moveSpeedLadder = 3f;
    public float reachDistance = 0.08f;

    [Header("Anti changement de direction")]
    public float directionLockDuration = 1f;
    public float directionChangeThreshold = 1.5f;
    public float directionDetectionDistance = 0.15f;

    private float directionLockTimer;
    private Vector3 lastMovementDirection;
    private bool hasMovementDirection;

    [Header("Détection des segments")]
    public float maxSegmentDistance = 3f;
    public float repathInterval = 0.35f;

    [Header("Gestion des étages")]
    public float floorHeightTolerance = 0.6f;
    public float verticalTolerance = 0.08f;

    [Header("Debug")]
    public bool drawPath = true;
    public bool showLogs = true;

    private List<SC_guard_point> allPoints =
        new List<SC_guard_point>();

    private List<Vector3> currentPath =
        new List<Vector3>();

    private int pathIndex;
    private float repathTimer;
    private bool chaseActive;

    // Mémorise le dernier étage connu lorsque le joueur saute.
    private int lastGroundedPlayerFloor = -1;
    private float lastGroundedPlayerFloorY;

    // Position mémorisée uniquement lorsque le joueur est réellement au sol.
    // Pendant un saut, les gardes continuent de viser cette position.
    private Vector3 lastGroundedPlayerPosition;
    private bool hasLastGroundedPlayerPosition;

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

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        if (GetComponent<SC_ScreenWarp>() == null)
            gameObject.AddComponent<SC_ScreenWarp>();

        RefreshPoints();

        if (allPoints.Count == 0)
        {
            Debug.LogError(
                "SC_guard_ai : aucun SC_guard_point trouvé."
            );
        }
    }

    // ============================================================
    // UPDATE
    // ============================================================


    private void Update()
    {
        if (!chaseActive)
            return;

        if (SC_player.instance == null)
            return;

        if (directionLockTimer > 0f)
        {
            directionLockTimer -= Time.deltaTime;
        }

        // La position cible n'est actualisée que lorsque le joueur est au sol.
        // Un saut ne peut donc jamais faire croire aux gardes qu'il faut monter.
        UpdateLastGroundedPlayerPosition();

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            repathTimer = Mathf.Max(0.01f, repathInterval);
            CalculatePath();
        }

        MoveAlongPath();
    }

    // ============================================================
    // ACTIVATION DE LA TRAQUE
    // ============================================================

    public void SetChaseActive(bool active)
    {
        chaseActive = active;

        if (active)
        {
            RefreshPoints();

            currentPath.Clear();
            pathIndex = 0;
            repathTimer = 0f;
            groundedTimer = 0f;

            directionLockTimer = 0f;
            hasMovementDirection = false;
            lastMovementDirection = Vector3.zero;

            lastGroundedPlayerFloor = -1;
            lastGroundedPlayerFloorY = 0f;
            hasLastGroundedPlayerPosition = false;

            // Ne mémorise une position de départ que si le joueur est au sol.
            if (SC_player.instance != null && SC_player.instance.isGrounded)
            {
                lastGroundedPlayerPosition = SC_player.instance.transform.position;
                hasLastGroundedPlayerPosition = true;
            }

            CalculatePath();
        }
        else
        {
            currentPath.Clear();
            pathIndex = 0;
            groundedTimer = 0f;
            hasLastGroundedPlayerPosition = false;

            directionLockTimer = 0f;
            hasMovementDirection = false;
            lastMovementDirection = Vector3.zero;
        }
    }

    public bool IsChaseActive()
    {
        return chaseActive;
    }

    public bool TryGetMovementTarget(out Vector3 target)
    {
        target = transform.position;

        if (!chaseActive ||
            currentPath == null ||
            currentPath.Count == 0 ||
            pathIndex >= currentPath.Count)
        {
            return false;
        }

        target = SC_ScreenWarp.NearestImage(transform.position, currentPath[pathIndex]);
        return true;
    }

    // ============================================================
    // POINTS
    // ============================================================

    private void RefreshPoints()
    {
        allPoints.Clear();

        SC_guard_point[] found =
            FindObjectsByType<SC_guard_point>(
                FindObjectsSortMode.None
            );

        allPoints.AddRange(found);
    }

    // ============================================================
    // MEMORISATION DE LA DERNIERE POSITION AU SOL
    // ============================================================

    private void UpdateLastGroundedPlayerPosition()
    {
        if (SC_player.instance == null || !SC_player.instance.isGrounded)
            return;

        lastGroundedPlayerPosition = SC_player.instance.transform.position;
        hasLastGroundedPlayerPosition = true;
    }

    // ============================================================
    // ETAGES
    // ============================================================

    private bool TryGetFloor(
        Vector3 position,
        out int floorId,
        out float floorY)
    {
        floorId = -1;
        floorY = 0f;

        float bestDistance = float.MaxValue;

        foreach (SC_guard_point point in allPoints)
        {
            if (point == null || !point.isFloorReference)
                continue;

            Vector3 pointPosition = point.transform.position;

            float verticalDistance = Mathf.Abs(
                position.y - pointPosition.y
            );

            if (verticalDistance > 0.1f)
                continue;

            float horizontalDistance = SC_ScreenWarp.WrappedDistance(position, pointPosition);

            if (horizontalDistance < bestDistance)
            {
                bestDistance = horizontalDistance;
                floorId = point.floorId;
                floorY = pointPosition.y;
            }
        }

        return floorId != -1;
    }

    // ============================================================
    // DETECTION D'UN TRACE VERTICAL
    // ============================================================

    private bool IsNearVerticalSegment(Vector3 position)
    {
        foreach (SC_guard_point point in allPoints)
        {
            if (point == null || point.connections == null)
                continue;

            foreach (SC_guard_point connected in point.connections)
            {
                if (connected == null)
                    continue;

                Vector3 a = point.transform.position;
                Vector3 b = SC_ScreenWarp.NearestImage(a, connected.transform.position);
                Vector3 localPosition = SC_ScreenWarp.NearestImage(a, position);

                if (Mathf.Abs(b.y - a.y) <= floorHeightTolerance)
                    continue;

                Vector3 closest = ClosestPointOnSegment(
                    localPosition, a, b
                );

                float horizontalDistance = Vector2.Distance(
                    new Vector2(localPosition.x, localPosition.z), new Vector2(closest.x, closest.z));

                if (horizontalDistance <= maxSegmentDistance &&
                    Vector3.Distance(localPosition, closest) <= maxSegmentDistance)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // ============================================================
    // DIRECTION VERTICALE
    // ============================================================

    private int GetVerticalDirection(
        int enemyFloor,
        int playerFloor,
        float enemyFloorY,
        float playerFloorY)
    {
        if (SC_player.instance == null)
            return 0;

        // IMPORTANT :
        // Tant que le joueur n'est pas au sol,
        // aucune décision de changement d'étage.
        if (!SC_player.instance.isGrounded)
        {
            groundedTimer = 0f;
            return 0;
        }

        groundedTimer += Time.deltaTime;

        if (groundedTimer < floorDetectionDelay)
            return 0;

        if (enemyFloor == -1 || playerFloor == -1)
            return 0;

        if (!IsNearVerticalSegment(
            SC_player.instance.transform.position))
        {
            return 0;
        }

        float heightDifference = playerFloorY - enemyFloorY;

        if (Mathf.Abs(heightDifference) <= floorHeightTolerance)
            return 0;

        if (Mathf.Abs(
            SC_player.instance.transform.position.y - playerFloorY
        ) > floorConfirmationTolerance)
        {
            return 0;
        }

        confirmedPlayerFloor = playerFloor;
        confirmedPlayerFloorY = playerFloorY;

        lastGroundedPlayerFloor = playerFloor;
        lastGroundedPlayerFloorY = playerFloorY;

        if (heightDifference < 0f)
            return -1;

        return 1;
    }

    // ============================================================
    // VALIDATION DU SENS DE DEPLACEMENT
    // ============================================================

    private bool CanMoveInDirection(
        Vector3 from,
        Vector3 to,
        int verticalDirection)
    {
        if (verticalDirection == 0)
            return true;

        float deltaY = to.y - from.y;

        if (verticalDirection == -1 &&
            deltaY > verticalTolerance)
            return false;

        if (verticalDirection == 1 &&
            deltaY < -verticalTolerance)
            return false;

        return true;
    }

    // ============================================================
    // ANTI CHANGEMENT DE DIRECTION
    // ============================================================

    private bool ShouldAcceptNewPath(List<Vector3> newPath)
    {
        if (newPath == null || newPath.Count < 2)
            return true;

        if (!hasMovementDirection)
            return true;

        Vector3 newDirection = Vector3.zero;

        for (int i = 1; i < newPath.Count; i++)
        {
            Vector3 difference =
                SC_ScreenWarp.WrappedDelta(transform.position, newPath[i]);

            if (difference.magnitude > directionDetectionDistance)
            {
                newDirection = difference.normalized;
                break;
            }
        }

        if (newDirection == Vector3.zero)
            return true;

        Vector3 oldDirection =
            lastMovementDirection.normalized;

        float directionDot = Vector3.Dot(
            oldDirection,
            newDirection
        );

        if (directionDot < -0.5f)
        {
            if (directionLockTimer > 0f)
                return false;
        }

        return true;
    }

    // ============================================================
    // SEGMENT LE PLUS PROCHE
    // ============================================================

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
            if (point == null || point.connections == null)
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
                Vector3 b = SC_ScreenWarp.NearestImage(a, connected.transform.position);
                Vector3 localPosition = SC_ScreenWarp.NearestImage(a, position);

                Vector3 closest = ClosestPointOnSegment(localPosition, a, b);
                float distance = Vector3.Distance(localPosition, closest);

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

    // ============================================================
    // DIJKSTRA
    // ============================================================

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
            if (point == null)
                continue;

            distances[point] = float.MaxValue;
            unvisited.Add(point);
        }

        if (!distances.ContainsKey(start) ||
            !distances.ContainsKey(target))
            return false;

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

            if (current.connections == null)
                continue;

            foreach (SC_guard_point neighbor in current.connections)
            {
                if (neighbor == null ||
                    !unvisited.Contains(neighbor))
                    continue;

                Vector3 from = current.transform.position;
                Vector3 to = neighbor.transform.position;

                if (!CanMoveInDirection(
                    from,
                    to,
                    verticalDirection))
                    continue;

                float edgeCost = SC_ScreenWarp.WrappedDistance(from, to);
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

    // ============================================================
    // CONSTRUCTION DU TRAJET
    // ============================================================

    private void CalculatePath()
    {
        if (!chaseActive || SC_player.instance == null)
            return;

        // Si le joueur est en l'air, on conserve sa dernière position au sol.
        // Si aucune position au sol n'a encore été enregistrée, on attend l'atterrissage.
        if (SC_player.instance.isGrounded)
        {
            UpdateLastGroundedPlayerPosition();
        }
        else if (!hasLastGroundedPlayerPosition)
        {
            return;
        }

        if (!hasLastGroundedPlayerPosition)
            return;

        if (allPoints.Count == 0)
            RefreshPoints();

        if (!FindClosestSegment(
            transform.position,
            out Segment enemySegment))
        {
            if (showLogs)
                Debug.LogWarning(
                    "SC_guard_ai : ennemi trop éloigné du réseau."
                );

            currentPath.Clear();
            pathIndex = 0;
            return;
        }

        // Important : ne jamais utiliser la hauteur actuelle du joueur pendant un saut.
        Vector3 playerPosition = lastGroundedPlayerPosition;

        if (!FindClosestSegment(
            playerPosition,
            out Segment playerSegment))
        {
            if (showLogs)
                Debug.LogWarning(
                    "SC_guard_ai : joueur trop éloigné du réseau."
                );

            currentPath.Clear();
            pathIndex = 0;
            return;
        }

        int enemyFloor;
        int playerFloor;

        float enemyFloorY;
        float playerFloorY;

        TryGetFloor(
            enemySegment.closest,
            out enemyFloor,
            out enemyFloorY
        );

        TryGetFloor(
            playerSegment.closest,
            out playerFloor,
            out playerFloorY
        );

        int verticalDirection = GetVerticalDirection(
            enemyFloor,
            playerFloor,
            enemyFloorY,
            playerFloorY
        );

        List<Vector3> bestPath = null;
        float bestCost = float.MaxValue;

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
                    SC_ScreenWarp.WrappedDistance(enemySegment.closest, startNode.transform.position)
                    + graphCost
                    + SC_ScreenWarp.WrappedDistance(targetNode.transform.position, playerSegment.closest);

                if (totalCost >= bestCost)
                    continue;

                List<Vector3> candidate = new List<Vector3>();

                candidate.Add(enemySegment.closest);

                foreach (SC_guard_point node in nodes)
                {
                    if (SC_ScreenWarp.WrappedDistance(
                        candidate[candidate.Count - 1], node.transform.position
                    ) > 0.01f)
                    {
                        candidate.Add(node.transform.position);
                    }
                }

                if (SC_ScreenWarp.WrappedDistance(
                    candidate[candidate.Count - 1], playerSegment.closest
                ) > 0.01f)
                {
                    candidate.Add(playerSegment.closest);
                }

                bestCost = totalCost;
                bestPath = candidate;
            }
        }

        // Même segment : trajet direct.
        bool sameSegment =
            (enemySegment.a == playerSegment.a &&
             enemySegment.b == playerSegment.b) ||
            (enemySegment.a == playerSegment.b &&
             enemySegment.b == playerSegment.a);

        if (sameSegment &&
            CanMoveInDirection(
                enemySegment.closest,
                playerSegment.closest,
                verticalDirection))
        {
            float directCost = SC_ScreenWarp.WrappedDistance(enemySegment.closest, playerSegment.closest);

            if (directCost < bestCost)
            {
                bestPath = new List<Vector3>
                {
                    enemySegment.closest,
                    playerSegment.closest
                };

                bestCost = directCost;
            }
        }

        if (bestPath == null || bestPath.Count == 0)
        {
            if (showLogs)
                Debug.LogWarning(
                    "SC_guard_ai : aucun chemin valide vers le joueur."
                );

            currentPath.Clear();
            pathIndex = 0;
            return;
        }

        // Anti retournement.
        if (!ShouldAcceptNewPath(bestPath))
            return;

        currentPath = bestPath;
        pathIndex = 0;
    }

    // ============================================================
    // DEPLACEMENT
    // ============================================================


    private void MoveAlongPath()
    {
        if (currentPath == null ||
            currentPath.Count == 0 ||
            pathIndex >= currentPath.Count)
            return;

        Vector3 target = currentPath[pathIndex];

        target = SC_ScreenWarp.NearestImage(transform.position, target);
        Vector3 movement = target - transform.position;

        if (movement.magnitude > directionDetectionDistance)
        {
            Vector3 newDirection = movement.normalized;

            if (!hasMovementDirection)
            {
                lastMovementDirection = newDirection;
                hasMovementDirection = true;
                directionLockTimer = directionLockDuration;
            }
            else
            {
                float dot = Vector3.Dot(
                    lastMovementDirection.normalized,
                    newDirection
                );

                if (dot < -0.5f)
                {
                    if (directionLockTimer > 0f)
                        return;

                    lastMovementDirection = newDirection;
                    directionLockTimer = directionLockDuration;
                }
                else
                {
                    lastMovementDirection = newDirection;
                }
            }
        }

        // Déplacement horizontal : X et Z
        Vector3 currentPosition = transform.position;

        Vector3 currentHorizontal = new Vector3(
            currentPosition.x, 0f, currentPosition.z
        );

        target = SC_ScreenWarp.NearestImage(currentPosition, target);
        Vector3 targetHorizontal = new Vector3(
            target.x, 0f, target.z
        );

        Vector3 nextHorizontal = Vector3.MoveTowards(
            currentHorizontal,
            targetHorizontal,
            moveSpeed * Time.deltaTime
        );

        // Déplacement vertical : Y
        float nextY = Mathf.MoveTowards(
            currentPosition.y,
            target.y,
            moveSpeedLadder * Time.deltaTime
        );

        Vector3 nextPosition = new Vector3(
            nextHorizontal.x,
            nextY,
            nextHorizontal.z
        );

        SC_guard_movement movementController =
            GetComponent<SC_guard_movement>();

        // La traque utilise exactement le même contrôle de collision que
        // la patrouille et le retour à la base.
        if (movementController == null ||
            !SC_guard_movement.TryMoveWithGuardCollision(
                movementController, nextPosition))
        {
            return;
        }

        // Validation du point atteint
        if (SC_ScreenWarp.WrappedDistance(transform.position, target) <= reachDistance)
        {
            pathIndex++;

            if (pathIndex >= currentPath.Count)
            {
                currentPath.Clear();
                pathIndex = 0;
            }
        }
    }

    // ============================================================
    // GIZMOS
    // ============================================================

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

    // ============================================================
    // CALCUL DU CHEMIN DE RETOUR VERS LA BASE
    // ============================================================

    public bool TryCalculateReturnPath(
        Vector3 startPosition,
        SC_guard_point baseNode,
        out List<Vector3> returnPath)
    {
        returnPath = new List<Vector3>();

        if (baseNode == null)
            return false;

        RefreshPoints();

        if (!FindClosestSegment(
            startPosition,
            out Segment startSegment))
        {
            Debug.LogWarning(
                "SC_guard_ai : impossible de trouver le segment actuel du garde."
            );

            return false;
        }

        if (!allPoints.Contains(baseNode))
            return false;

        SC_guard_point[] startNodes =
        {
            startSegment.a,
            startSegment.b
        };

        float bestCost = float.MaxValue;
        List<Vector3> bestPath = null;

        foreach (SC_guard_point startNode in startNodes)
        {
            if (startNode == null)
                continue;

            if (!FindPath(
                startNode,
                baseNode,
                0,
                out List<SC_guard_point> nodes,
                out float graphCost))
                continue;

            float totalCost =
                SC_ScreenWarp.WrappedDistance(startSegment.closest, startNode.transform.position) + graphCost;

            if (totalCost >= bestCost)
                continue;

            List<Vector3> candidate = new List<Vector3>();

            candidate.Add(startSegment.closest);

            foreach (SC_guard_point node in nodes)
            {
                Vector3 nodePosition = node.transform.position;

                if (SC_ScreenWarp.WrappedDistance(
                    candidate[candidate.Count - 1], nodePosition) > 0.001f)
                {
                    candidate.Add(nodePosition);
                }
            }

            bestCost = totalCost;
            bestPath = candidate;
        }

        if (bestPath == null || bestPath.Count == 0)
        {
            Debug.LogWarning(
                "SC_guard_ai : aucun chemin de retour vers la base."
            );

            return false;
        }

        returnPath = bestPath;

        if (showLogs)
        {
            Debug.Log(
                "SC_guard_ai : chemin de retour calculé. " +
                returnPath.Count + " points."
            );
        }

        return true;
    }
}
