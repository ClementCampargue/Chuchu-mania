
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SC_guard_ai))]
public class SC_guard_movement : MonoBehaviour
{
    public enum MovementMode { Chase, Patrol }

    public GameObject exclamation;
    public Transform exclamation_point;
    public SC_damage_player damage;

    [Header("Sommeil après traque")]
    [SerializeField] private float sleepDuration = 3f;

    [Header("Références")]
    [SerializeField] private Animator flashlight;
    [SerializeField] private Animator animator;
    [SerializeField] private SC_guard_ai guardAI;

    [Header("Déplacement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float ladderSpeed = 3f;
    [SerializeField] private float ladderAlignmentTolerance = 0.025f;
    [SerializeField] private MovementMode movementMode = MovementMode.Patrol;

    [Header("Animation de début de traque")]
    [SerializeField] private string chaseStartTrigger = "ChaseStart";

    private bool chaseMovementAuthorized;

    [Header("Point actuel")]
    [SerializeField] private SC_guard_point currentNode;

    [Header("Zone de base")]
    [SerializeField] private SC_guard_point baseNode;

    [Header("Patrouille")]
    [SerializeField]
    private List<SC_guard_point> patrolPoints =
        new List<SC_guard_point>();

    [SerializeField] private float patrolWaitTime = 1.5f;
    [SerializeField] private bool randomPatrol = false;

    private int currentPatrolIndex = -1;
    private int patrolDirection = 1;

    [Header("Sommeil aléatoire")]
    [SerializeField] private bool allowRandomSleep = true;
    [SerializeField] private float minSleepInterval = 8f;
    [SerializeField] private float maxSleepInterval = 20f;
    [SerializeField] private float minSleepDuration = 2f;
    [SerializeField] private float maxSleepDuration = 5f;
    [SerializeField] private bool canSleepWhileMoving = true;

    private float randomSleepTimer;
    private bool randomSleepInProgress;

    [Header("Sommeil")]
    [SerializeField] private string sleepParameter = "Sleep";

    [Header("Animation")]
    [SerializeField] private string walkParameter = "Walk";
    [SerializeField] private string ladderUpParameter = "LadderUp";
    [SerializeField] private string ladderDownParameter = "LadderDown";

    [Header("Flip")]
    [SerializeField] private bool flipCharacter = true;
    [SerializeField] private bool facingRightByDefault = true;

    private Vector3 originalScale;

    private List<SC_guard_point> currentPath =
        new List<SC_guard_point>();

    private int pathIndex;

    private SC_guard_point segmentStartNode;
    private SC_guard_point segmentTargetNode;

    private float patrolWaitTimer;
    private float sleepTimer;

    private bool isMovingBetweenNodes;
    private bool isChasing;
    private bool isSleeping;
    private bool isWaitingAtPatrolPoint;

    private enum AnimationMovement
    {
        Walk,
        LadderUp,
        LadderDown
    }

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        originalScale = transform.localScale;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (guardAI == null)
            guardAI = GetComponent<SC_guard_ai>();

        if (guardAI == null)
        {
            Debug.LogError(
                "SC_guard_movement : SC_guard_ai est introuvable.",
                this
            );

            enabled = false;
            return;
        }

        guardAI.SetChaseActive(false);

        if (currentNode == null)
            currentNode = FindClosestNode(transform.position);

        if (baseNode == null)
            baseNode = currentNode;

        if (currentNode != null)
            transform.position = currentNode.transform.position;

        movementMode = MovementMode.Patrol;

        ResetRandomSleepTimer();
        StartPatrol();
    }

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (isSleeping)
        {
            UpdateSleep();
            return;
        }

        if (isChasing)
        {
            UpdateChaseAnimation();
            return;
        }

        UpdatePatrol();
        UpdateRandomSleep();
    }

    // ============================================================
    // ANIMATION PENDANT LA TRAQUE
    // ============================================================

    private void UpdateChaseAnimation()
    {
        // Tant que l'Animation Event n'a pas été déclenché,
        // le garde reste immobile.

        if (!chaseMovementAuthorized || guardAI == null)
        {
            StopMovementAnimation();
            return;
        }

        if (!guardAI.TryGetMovementTarget(out Vector3 target))
        {
            StopMovementAnimation();
            return;
        }

        UpdateFlip(transform.position, target);

        float verticalDifference = target.y - transform.position.y;

        if (verticalDifference > 0.05f)
        {
            SetMovementAnimation(AnimationMovement.LadderUp);
        }
        else if (verticalDifference < -0.05f)
        {
            SetMovementAnimation(AnimationMovement.LadderDown);
        }
        else
        {
            SetMovementAnimation(AnimationMovement.Walk);
        }
    }

    // ============================================================
    // ANIMATION EVENT : DEBUT REEL DE LA TRAQUE
    // ============================================================

    public void StartChaseMovement()
    {
        if (!isChasing || chaseMovementAuthorized)
            return;

        chaseMovementAuthorized = true;

        if (guardAI != null)
            guardAI.SetChaseActive(true);
    }

    // ============================================================
    // SOMMEIL ALEATOIRE
    // ============================================================

    private void ResetRandomSleepTimer()
    {
        if (!allowRandomSleep)
        {
            randomSleepTimer = Mathf.Infinity;
            return;
        }

        randomSleepTimer = Random.Range(
            Mathf.Max(0f, minSleepInterval),
            Mathf.Max(minSleepInterval, maxSleepInterval)
        );
    }

    private void UpdateRandomSleep()
    {
        if (!allowRandomSleep || isChasing || isSleeping)
            return;

        if (!canSleepWhileMoving && isMovingBetweenNodes)
            return;

        randomSleepTimer -= Time.deltaTime;

        if (randomSleepTimer <= 0f)
            StartRandomSleep();
    }

    private void StartRandomSleep()
    {
        if (isChasing || isSleeping)
            return;

        randomSleepInProgress = true;
        isSleeping = true;

        sleepTimer = Random.Range(
            Mathf.Max(0f, minSleepDuration),
            Mathf.Max(minSleepDuration, maxSleepDuration)
        );

        if (damage != null)
            damage.enabled = false;

        SetFlashlight(false);
        StopMovementAnimation();

        if (animator != null)
            animator.SetBool(sleepParameter, true);
    }

    // ============================================================
    // SOMMEIL
    // ============================================================

    private void UpdateSleep()
    {
        sleepTimer -= Time.deltaTime;

        if (sleepTimer <= 0f)
            FinishSleep();
    }

    private void FinishSleep()
    {
        isSleeping = false;

        if (damage != null)
            damage.enabled = true;

        if (animator != null)
            animator.SetBool(sleepParameter, false);

        SetFlashlight(true);

        if (randomSleepInProgress)
        {
            randomSleepInProgress = false;
            ResetRandomSleepTimer();

            if (isMovingBetweenNodes &&
                segmentStartNode != null &&
                segmentTargetNode != null)
            {
                UpdateFlip(
                    segmentStartNode.transform.position,
                    segmentTargetNode.transform.position
                );

                SetMovementAnimation(
                    GetMovementAnimation(
                        segmentStartNode,
                        segmentTargetNode
                    )
                );
            }

            return;
        }

        currentNode = FindClosestNode(transform.position);

        StartPatrol();
    }

    private void StartSleep()
    {
        isSleeping = true;
        randomSleepInProgress = false;

        sleepTimer = sleepDuration;

        FinishPath();

        if (damage != null)
            damage.enabled = false;

        if (animator != null)
        {
            animator.SetBool(walkParameter, false);
            animator.SetBool(ladderUpParameter, false);
            animator.SetBool(ladderDownParameter, false);
            animator.SetBool(sleepParameter, true);
        }

        SetFlashlight(false);
    }

    // ============================================================
    // TRAQUE
    // ============================================================

    public void SetChaseMode(bool enabled)
    {
        if (enabled)
        {
            if (isChasing)
                return;

            if (isSleeping)
            {
                isSleeping = false;
                randomSleepInProgress = false;

                if (animator != null)
                    animator.SetBool(sleepParameter, false);

                if (damage != null)
                    damage.enabled = true;
            }

            if (exclamation != null && exclamation_point != null)
            {
                Instantiate(
                    exclamation,
                    exclamation_point.position,
                    Quaternion.identity
                );
            }

            isChasing = true;
            movementMode = MovementMode.Chase;

            isWaitingAtPatrolPoint = false;

            // Interrompt immédiatement la patrouille.
            FinishPath();

            // La traque commence sans autoriser le déplacement.
            chaseMovementAuthorized = false;

            SetFlashlight(false);

            // Le système de poursuite reste désactivé.
            if (guardAI != null)
                guardAI.SetChaseActive(false);

            // Déclenche l'animation de début de traque.
            if (animator != null)
                animator.SetTrigger(chaseStartTrigger);

            return;
        }

        if (!isChasing)
            return;

        isChasing = false;
        chaseMovementAuthorized = false;

        movementMode = MovementMode.Patrol;

        if (guardAI != null)
            guardAI.SetChaseActive(false);

        currentNode = FindClosestNode(transform.position);

        StartSleep();
    }

    // ============================================================
    // PATROUILLE
    // ============================================================

    private void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
            return;

        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer -= Time.deltaTime;

            if (patrolWaitTimer <= 0f)
            {
                isWaitingAtPatrolPoint = false;
                ChooseNextPatrolPoint();
            }

            return;
        }

        if (isMovingBetweenNodes)
        {
            FollowCurrentPath();
            return;
        }

        ChooseNextPatrolPoint();
    }

    private void ChooseNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
            return;

        if (currentNode == null)
        {
            currentNode = FindClosestNode(transform.position);

            if (currentNode == null)
                return;
        }

        if (randomPatrol)
        {
            List<SC_guard_point> validPoints =
                new List<SC_guard_point>();

            foreach (SC_guard_point point in patrolPoints)
            {
                if (point != null && point != currentNode)
                    validPoints.Add(point);
            }

            if (validPoints.Count == 0)
            {
                BeginPatrolWait();
                return;
            }

            SC_guard_point randomTarget =
                validPoints[Random.Range(0, validPoints.Count)];

            SetPatrolDestination(randomTarget);
            return;
        }

        if (currentPatrolIndex < 0)
        {
            currentPatrolIndex = patrolPoints.IndexOf(currentNode);

            if (currentPatrolIndex < 0)
                currentPatrolIndex = FindClosestPatrolIndex();
        }

        if (patrolPoints.Count <= 1)
        {
            BeginPatrolWait();
            return;
        }

        int nextIndex = currentPatrolIndex + patrolDirection;

        if (nextIndex >= patrolPoints.Count || nextIndex < 0)
        {
            patrolDirection *= -1;
            nextIndex = currentPatrolIndex + patrolDirection;
        }

        SC_guard_point target = patrolPoints[nextIndex];

        if (target == null)
        {
            patrolDirection *= -1;
            return;
        }

        currentPatrolIndex = nextIndex;

        SetPatrolDestination(target);
    }

    private int FindClosestPatrolIndex()
    {
        int bestIndex = 0;
        float bestDistance = Mathf.Infinity;

        for (int i = 0; i < patrolPoints.Count; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                patrolPoints[i].transform.position
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void SetPatrolDestination(SC_guard_point target)
    {
        if (target == null || currentNode == null)
            return;

        if (target == currentNode)
        {
            BeginPatrolWait();
            return;
        }

        List<SC_guard_point> newPath =
            FindShortestPath(currentNode, target);

        if (newPath != null && newPath.Count > 0)
        {
            currentPath = newPath;
            pathIndex = 0;
            StartNextNodeMovement();
        }
        else
        {
            FinishPath();
        }
    }

    private void BeginPatrolWait()
    {
        FinishPath();

        isWaitingAtPatrolPoint = true;
        patrolWaitTimer = patrolWaitTime;
    }

    private void StartPatrol()
    {
        movementMode = MovementMode.Patrol;
        isChasing = false;
        chaseMovementAuthorized = false;

        isWaitingAtPatrolPoint = false;

        FinishPath();

        patrolWaitTimer = 0f;

        currentPatrolIndex = patrolPoints.IndexOf(currentNode);

        if (currentPatrolIndex < 0)
            currentPatrolIndex = FindClosestPatrolIndex();

        patrolDirection = 1;

        ChooseNextPatrolPoint();
    }

    // ============================================================
    // DEPLACEMENT SUR LE CHEMIN DE PATROUILLE
    // ============================================================

    private void StartNextNodeMovement()
    {
        if (currentNode == null ||
            currentPath == null ||
            pathIndex >= currentPath.Count)
        {
            FinishPath();
            return;
        }

        SC_guard_point nextNode = currentPath[pathIndex];

        if (nextNode == null || !IsConnected(currentNode, nextNode))
        {
            FinishPath();
            return;
        }

        segmentStartNode = currentNode;
        segmentTargetNode = nextNode;

        UpdateFlip(currentNode, nextNode);

        SetMovementAnimation(
            GetMovementAnimation(currentNode, nextNode)
        );

        isMovingBetweenNodes = true;
    }

    private void FollowCurrentPath()
    {
        if (currentPath == null ||
            pathIndex >= currentPath.Count ||
            currentNode == null)
        {
            FinishPath();
            return;
        }

        SC_guard_point target = currentPath[pathIndex];

        if (target == null || !IsConnected(currentNode, target))
        {
            FinishPath();
            return;
        }

        Vector3 targetPosition = target.transform.position;

        AnimationMovement movement =
            GetMovementAnimation(currentNode, target);

        if (movement == AnimationMovement.Walk)
        {
            FollowGroundNormally(targetPosition);

            Vector3 currentFlat = new Vector3(
                transform.position.x, 0f, transform.position.z
            );

            Vector3 targetFlat = new Vector3(
                targetPosition.x, 0f, targetPosition.z
            );

            if (Vector3.Distance(currentFlat, targetFlat) <= 0.001f)
            {
                transform.position = targetPosition;
                ArriveAtCurrentPathNode();
            }
        }
        else
        {
            FollowLadder(targetPosition);

            if (Vector3.Distance(
                transform.position,
                targetPosition
            ) <= 0.001f)
            {
                transform.position = targetPosition;
                ArriveAtCurrentPathNode();
            }
        }
    }

    private void FollowGroundNormally(Vector3 targetPosition)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }

    private void FollowLadder(Vector3 targetPosition)
    {
        Vector3 position = transform.position;

        float horizontalDifference =
            targetPosition.x - position.x;

        if (Mathf.Abs(horizontalDifference) >
            ladderAlignmentTolerance)
        {
            position.x = Mathf.MoveTowards(
                position.x,
                targetPosition.x,
                ladderSpeed * Time.deltaTime
            );

            transform.position = position;
            return;
        }

        position.x = targetPosition.x;

        position.y = Mathf.MoveTowards(
            position.y,
            targetPosition.y,
            ladderSpeed * Time.deltaTime
        );

        position.z = Mathf.MoveTowards(
            position.z,
            targetPosition.z,
            ladderSpeed * Time.deltaTime
        );

        transform.position = position;
    }

    private void ArriveAtCurrentPathNode()
    {
        currentNode = currentPath[pathIndex];

        pathIndex++;

        if (pathIndex >= currentPath.Count)
        {
            FinishPath();
            return;
        }

        SC_guard_point nextNode = currentPath[pathIndex];

        if (nextNode == null || !IsConnected(currentNode, nextNode))
        {
            FinishPath();
            return;
        }

        segmentStartNode = currentNode;
        segmentTargetNode = nextNode;

        UpdateFlip(currentNode, nextNode);

        SetMovementAnimation(
            GetMovementAnimation(currentNode, nextNode)
        );

        isMovingBetweenNodes = true;
    }

    private void FinishPath()
    {
        currentPath.Clear();

        pathIndex = 0;

        isMovingBetweenNodes = false;

        segmentStartNode = null;
        segmentTargetNode = null;

        StopMovementAnimation();
    }

    // ============================================================
    // ANIMATIONS
    // ============================================================

    private AnimationMovement GetMovementAnimation(
        SC_guard_point from,
        SC_guard_point to)
    {
        if (from == null || to == null)
            return AnimationMovement.Walk;

        float difference =
            to.transform.position.y -
            from.transform.position.y;

        if (difference > 0.05f)
            return AnimationMovement.LadderUp;

        if (difference < -0.05f)
            return AnimationMovement.LadderDown;

        return AnimationMovement.Walk;
    }

    private void SetMovementAnimation(AnimationMovement movement)
    {
        if (animator == null)
            return;

        animator.SetBool(walkParameter, false);
        animator.SetBool(ladderUpParameter, false);
        animator.SetBool(ladderDownParameter, false);

        switch (movement)
        {
            case AnimationMovement.Walk:
                animator.SetBool(walkParameter, true);

                if (!isChasing)
                    SetFlashlight(true);

                break;

            case AnimationMovement.LadderUp:
                animator.SetBool(ladderUpParameter, true);
                SetFlashlight(false);
                break;

            case AnimationMovement.LadderDown:
                animator.SetBool(ladderDownParameter, true);
                SetFlashlight(false);
                break;
        }
    }

    private void StopMovementAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(walkParameter, false);
        animator.SetBool(ladderUpParameter, false);
        animator.SetBool(ladderDownParameter, false);
    }

    // ============================================================
    // LAMPE
    // ============================================================

    private void SetFlashlight(bool enabled)
    {
        if (flashlight != null)
            flashlight.SetBool("on", enabled);
    }

    // ============================================================
    // FLIP
    // ============================================================

    private void UpdateFlip(SC_guard_point from, SC_guard_point to)
    {
        if (from == null || to == null)
            return;

        UpdateFlip(
            from.transform.position,
            to.transform.position
        );
    }

    private void UpdateFlip(Vector3 from, Vector3 to)
    {
        if (!flipCharacter)
            return;

        Vector3 direction = to - from;

        if (Mathf.Abs(direction.x) < 0.01f)
            return;

        bool movingRight = direction.x > 0f;

        bool shouldFaceRight =
            facingRightByDefault ? movingRight : !movingRight;

        Vector3 scale = transform.localScale;

        scale.x = Mathf.Abs(originalScale.x) *
                  (shouldFaceRight ? 1f : -1f);

        transform.localScale = scale;
    }

    // ============================================================
    // CONNEXIONS
    // ============================================================

    private bool IsConnected(SC_guard_point from, SC_guard_point to)
    {
        if (from == null || to == null || from.connections == null)
            return false;

        return from.connections.Contains(to);
    }

    // ============================================================
    // A*
    // ============================================================

    private List<SC_guard_point> FindShortestPath(
        SC_guard_point start,
        SC_guard_point target)
    {
        if (start == null || target == null)
            return null;

        if (start == target)
            return new List<SC_guard_point>();

        List<SC_guard_point> openSet =
            new List<SC_guard_point>();

        HashSet<SC_guard_point> closedSet =
            new HashSet<SC_guard_point>();

        Dictionary<SC_guard_point, SC_guard_point> cameFrom =
            new Dictionary<SC_guard_point, SC_guard_point>();

        Dictionary<SC_guard_point, float> gCost =
            new Dictionary<SC_guard_point, float>();

        Dictionary<SC_guard_point, float> fCost =
            new Dictionary<SC_guard_point, float>();

        openSet.Add(start);

        gCost[start] = 0f;
        fCost[start] = Heuristic(start, target);

        while (openSet.Count > 0)
        {
            SC_guard_point current = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                SC_guard_point candidate = openSet[i];

                if (fCost[candidate] < fCost[current])
                    current = candidate;
            }

            if (current == target)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            if (current.connections == null)
                continue;

            foreach (SC_guard_point neighbour in current.connections)
            {
                if (neighbour == null || closedSet.Contains(neighbour))
                    continue;

                float distance = Vector3.Distance(
                    current.transform.position,
                    neighbour.transform.position
                );

                float tentativeGCost = gCost[current] + distance;

                if (!gCost.ContainsKey(neighbour) ||
                    tentativeGCost < gCost[neighbour])
                {
                    cameFrom[neighbour] = current;
                    gCost[neighbour] = tentativeGCost;

                    fCost[neighbour] =
                        tentativeGCost + Heuristic(neighbour, target);

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }

        return null;
    }

    private float Heuristic(SC_guard_point a, SC_guard_point b)
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
        List<SC_guard_point> path = new List<SC_guard_point>();

        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();

        if (path.Count > 0)
            path.RemoveAt(0);

        return path;
    }

    // ============================================================
    // NODE LE PLUS PROCHE
    // ============================================================

    private SC_guard_point FindClosestNode(Vector3 position)
    {
        SC_guard_point[] nodes =
            FindObjectsByType<SC_guard_point>(FindObjectsSortMode.None);

        SC_guard_point closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (SC_guard_point node in nodes)
        {
            if (node == null)
                continue;

            float distance = Vector3.Distance(
                position,
                node.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = node;
            }
        }

        return closest;
    }

    // ============================================================
    // CHANGER NODE
    // ============================================================

    public void SetCurrentNode(SC_guard_point node)
    {
        currentNode = node;

        if (node != null)
            transform.position = node.transform.position;

        FinishPath();
    }

    public void SetBaseNode(SC_guard_point node)
    {
        baseNode = node;
    }

    // ============================================================
    // GETTERS
    // ============================================================

    public SC_guard_point GetCurrentNode()
    {
        return currentNode;
    }

    public SC_guard_point GetBaseNode()
    {
        return baseNode;
    }
}
