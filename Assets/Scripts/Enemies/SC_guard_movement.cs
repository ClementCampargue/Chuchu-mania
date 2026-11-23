
using System.Collections.Generic;
using UnityEngine;

public class SC_guard_movement : MonoBehaviour
{
    public enum MovementMode { Chase, Patrol }

    public GameObject exclamation;
    public Transform exclamation_point;
    public SC_damage_player damage;

    [Header("Traque - Vitesse")]
    [SerializeField] private float chaseMaxSpeed = 4.5f;
    [SerializeField] private float chaseAccelerationTime = 0.8f;
    [SerializeField] private float chaseDecelerationTime = 0.5f;

    private float currentChaseSpeed;

    [Header("Sommeil après traque")]
    [SerializeField] private float sleepDuration = 3f;

    [Header("Références")]
    [SerializeField] private Animator flashlight;
    [SerializeField] private Animator animator;

    [Header("Déplacement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private MovementMode movementMode = MovementMode.Patrol;

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

    [Header("Traque")]
    [SerializeField] private float chaseRefreshRate = 0.25f;
    [SerializeField] private float verticalTolerance = 0.5f;
    [SerializeField] private float verticalWeight = 0.15f;
    [SerializeField] private bool useNearestLadder = true;
    [SerializeField] private float ladderDecisionDistance = 8f;
    [SerializeField] private bool allowMidSegmentTurn = true;

    [Header("Déplacement échelles")]
    [Tooltip("Vitesse de déplacement sur les échelles pendant la patrouille.")]
    [SerializeField] private float ladderSpeed = 3f;

    [Tooltip("Tolérance pour considérer que le garde est aligné avec l'échelle.")]
    [SerializeField] private float ladderAlignmentTolerance = 0.025f;

    [Header("Bonds goofy - Traque uniquement")]
    [SerializeField] private float chaseHopHeight = 0.28f;
    [SerializeField] private float chaseHopDuration = 0.32f;
    [SerializeField] private float hopSquash = 0.12f;
    [SerializeField] private float hopStretch = 0.08f;
    [SerializeField] private float hopWobble = 0.025f;

    private float hopTimer;

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

    private float chaseTimer;
    private float patrolWaitTimer;
    private float sleepTimer;

    private bool isMovingBetweenNodes;
    private bool isChasing;
    private bool isSleeping;
    private bool isWaitingAtPatrolPoint;

    // Le chemin sera recalculé dès que le segment actuel sera terminé.
    private bool chasePathDirty;

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
            UpdateChase();
            return;
        }

        UpdatePatrol();
        UpdateRandomSleep();
    }

    // ============================================================
    // VITESSE DE CHASSE
    // ============================================================

    private float UpdateChaseSpeed(bool hasTarget)
    {
        float acceleration = Mathf.Max(0.01f, chaseAccelerationTime);
        float deceleration = Mathf.Max(0.01f, chaseDecelerationTime);

        if (hasTarget)
        {
            currentChaseSpeed = Mathf.MoveTowards(
                currentChaseSpeed,
                chaseMaxSpeed,
                (chaseMaxSpeed / acceleration) * Time.deltaTime
            );
        }
        else
        {
            currentChaseSpeed = Mathf.MoveTowards(
                currentChaseSpeed,
                0f,
                (chaseMaxSpeed / deceleration) * Time.deltaTime
            );
        }

        return currentChaseSpeed;
    }

    private float GetCurrentMovementSpeed()
    {
        if (isChasing)
            return UpdateChaseSpeed(true);

        return moveSpeed;
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
        ResetGoofyScale();

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

            // IMPORTANT :
            // Aucun changement de currentNode.
            // Aucune téléportation.
            // Aucun FinishPath().
            // Le segment reprend depuis la position exacte.

            if (isMovingBetweenNodes)
            {
                if (segmentStartNode != null &&
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
            }

            return;
        }

        StartPatrol();
    }

    // ============================================================
    // TRAQUE
    // ============================================================

    private void UpdateChase()
    {
        if (SC_player.instance == null)
            return;

        // Le timer continue de tourner même pendant le déplacement.
        chaseTimer -= Time.deltaTime;

        if (chaseTimer <= 0f)
        {
            chaseTimer = Mathf.Max(0.01f, chaseRefreshRate);

            chasePathDirty = true;
        }

        // On laisse le segment actuel se terminer.
        // Le nouveau chemin sera pris au prochain node.
        if (isMovingBetweenNodes)
        {
            if (allowMidSegmentTurn && useNearestLadder)
                CheckForNearestLadderDecision();

            FollowCurrentPath();
            return;
        }

        if (chasePathDirty)
        {
            chasePathDirty = false;
            RecalculateChasePath();
        }

        if (isMovingBetweenNodes)
            FollowCurrentPath();
    }

    private void RecalculateChasePath()
    {
        if (!isChasing || SC_player.instance == null)
            return;

        if (currentNode == null)
        {
            currentNode = FindClosestNode(transform.position);

            if (currentNode == null)
                return;
        }

        SC_guard_point targetNode =
            FindClosestNodeForChase(
                SC_player.instance.transform.position
            );

        if (targetNode == null)
            return;

        if (targetNode == currentNode)
        {
            FinishPath();
            return;
        }

        List<SC_guard_point> newPath =
            FindShortestPath(currentNode, targetNode);

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

    // ============================================================
    // DECISION ECHELLE
    // ============================================================

    private void CheckForNearestLadderDecision()
    {
        if (!isChasing || !isMovingBetweenNodes)
            return;

        if (segmentStartNode == null ||
            segmentTargetNode == null ||
            SC_player.instance == null)
            return;

        float verticalDifference = Mathf.Abs(
            SC_player.instance.transform.position.y -
            transform.position.y
        );

        if (verticalDifference <= verticalTolerance)
            return;

        float segmentHeight = Mathf.Abs(
            segmentTargetNode.transform.position.y -
            segmentStartNode.transform.position.y
        );

        if (segmentHeight > 0.05f)
            return;

        SC_guard_point ladder =
            FindNearestUsefulLadderNode(
                SC_player.instance.transform.position
            );

        if (ladder == null)
            return;

        if (Vector3.Distance(
            transform.position,
            ladder.transform.position
        ) > ladderDecisionDistance)
            return;

        float startDistance = Vector3.Distance(
            transform.position,
            segmentStartNode.transform.position
        );

        float targetDistance = Vector3.Distance(
            transform.position,
            segmentTargetNode.transform.position
        );

        SC_guard_point bestEntry = FindBestLadderEntryNode(
            segmentStartNode,
            segmentTargetNode,
            ladder
        );

        if (bestEntry == null || bestEntry != segmentStartNode)
            return;

        if (startDistance < targetDistance)
        {
            ReverseCurrentSegmentToward(segmentStartNode);
            chasePathDirty = true;
        }
    }

    private void ReverseCurrentSegmentToward(
        SC_guard_point destination)
    {
        if (destination == null)
            return;

        currentPath.Clear();
        pathIndex = 0;

        isMovingBetweenNodes = false;

        segmentStartNode = null;
        segmentTargetNode = null;

        ResetGoofyScale();

        currentPath.Add(destination);

        StartNextNodeMovement();
    }

    // ============================================================
    // RECHERCHE ECHELLE
    // ============================================================

    private SC_guard_point FindNearestUsefulLadderNode(
        Vector3 playerPosition)
    {
        SC_guard_point[] nodes =
            FindObjectsByType<SC_guard_point>(
                FindObjectsSortMode.None
            );

        SC_guard_point bestNode = null;
        float bestScore = Mathf.Infinity;

        foreach (SC_guard_point node in nodes)
        {
            if (node == null || !HasVerticalConnection(node))
                continue;

            float guardDistance = Vector3.Distance(
                transform.position,
                node.transform.position
            );

            float playerDistance = Vector3.Distance(
                node.transform.position,
                playerPosition
            );

            float score = guardDistance + playerDistance * 0.35f;

            if (score < bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        return bestNode;
    }

    private bool HasVerticalConnection(SC_guard_point node)
    {
        if (node == null || node.connections == null)
            return false;

        foreach (SC_guard_point connection in node.connections)
        {
            if (connection == null)
                continue;

            if (Mathf.Abs(
                connection.transform.position.y -
                node.transform.position.y
            ) > 0.05f)
                return true;
        }

        return false;
    }

    private SC_guard_point FindBestLadderEntryNode(
        SC_guard_point start,
        SC_guard_point target,
        SC_guard_point ladder)
    {
        if (start == null || target == null || ladder == null)
            return null;

        float startDistance = EstimatePathDistance(start, ladder);
        float targetDistance = EstimatePathDistance(target, ladder);

        return startDistance <= targetDistance ? start : target;
    }

    private float EstimatePathDistance(
        SC_guard_point start,
        SC_guard_point target)
    {
        if (start == null || target == null)
            return Mathf.Infinity;

        if (start == target)
            return 0f;

        List<SC_guard_point> path = FindShortestPath(start, target);

        if (path == null)
            return Mathf.Infinity;

        float distance = 0f;
        SC_guard_point previous = start;

        foreach (SC_guard_point node in path)
        {
            if (node == null)
                return Mathf.Infinity;

            distance += Vector3.Distance(
                previous.transform.position,
                node.transform.position
            );

            previous = node;
        }

        return distance;
    }

    // ============================================================
    // DEMARRER DEPLACEMENT
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

        hopTimer = 0f;

        UpdateFlip(currentNode, nextNode);

        SetMovementAnimation(
            GetMovementAnimation(currentNode, nextNode)
        );

        isMovingBetweenNodes = true;
    }

    // ============================================================
    // SUIVI DU CHEMIN
    // ============================================================

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
            if (isChasing)
                FollowGroundWithGoofyHops(targetPosition);
            else
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
                ResetGoofyScale();
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
                ResetGoofyScale();
                ArriveAtCurrentPathNode();
            }
        }
    }

    // ============================================================
    // DEPLACEMENT ECHELLE SANS DIAGONALE
    // ============================================================

    private void FollowLadder(Vector3 targetPosition)
    {
        float speed = GetCurrentMovementSpeed();

        Vector3 position = transform.position;

        // ETAPE 1 :
        // Alignement horizontal avec l'échelle.
        // On ne monte PAS en diagonale.

        float horizontalDifference =
            targetPosition.x - position.x;

        if (Mathf.Abs(horizontalDifference) >
            ladderAlignmentTolerance)
        {
            position.x = Mathf.MoveTowards(
                position.x,
                targetPosition.x,
                speed * Time.deltaTime
            );

            transform.position = position;

            ResetGoofyScale();
            return;
        }

        // ETAPE 2 :
        // Le garde est aligné.
        // Il peut maintenant monter ou descendre verticalement.

        position.x = targetPosition.x;

        position.y = Mathf.MoveTowards(
            position.y,
            targetPosition.y,
            speed * Time.deltaTime
        );

        position.z = Mathf.MoveTowards(
            position.z,
            targetPosition.z,
            speed * Time.deltaTime
        );

        transform.position = position;

        ResetGoofyScale();
    }

    // ============================================================
    // DEPLACEMENT SOL NORMAL
    // ============================================================

    private void FollowGroundNormally(Vector3 targetPosition)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        ResetGoofyScale();
    }

    // ============================================================
    // BONDS GOOFY - CHASSE
    // ============================================================

    private void FollowGroundWithGoofyHops(Vector3 targetPosition)
    {
        // La vitesse de chasse continue de monter jusqu'au maximum.
        // Aucun reset lors du changement de segment ou d'étage.

        float speed = GetCurrentMovementSpeed();

        Vector3 current = transform.position;

        Vector3 horizontalCurrent = new Vector3(
            current.x,
            targetPosition.y,
            current.z
        );

        Vector3 horizontalTarget = new Vector3(
            targetPosition.x,
            targetPosition.y,
            targetPosition.z
        );

        Vector3 nextPosition = Vector3.MoveTowards(
            horizontalCurrent,
            horizontalTarget,
            speed * Time.deltaTime
        );

        float duration = Mathf.Max(0.05f, chaseHopDuration);

        hopTimer += Time.deltaTime;

        float normalizedTime =
            (hopTimer % duration) / duration;

        float hopOffset = Mathf.Sin(
            normalizedTime * Mathf.PI
        ) * chaseHopHeight;

        float wobble = Mathf.Sin(
            normalizedTime * Mathf.PI * 2f
        ) * hopWobble;

        if (segmentStartNode != null)
        {
            Vector3 direction =
                targetPosition -
                segmentStartNode.transform.position;

            direction.y = 0f;

            if (Mathf.Abs(direction.x) > 0.001f)
                nextPosition.x += wobble * Mathf.Sign(direction.x);
        }

        nextPosition.y = targetPosition.y + hopOffset;

        transform.position = nextPosition;

        UpdateHopScale(normalizedTime);
    }

    // ============================================================
    // SQUASH / STRETCH
    // ============================================================

    private void UpdateHopScale(float normalizedTime)
    {
        float scaleY;
        float scaleX;

        if (normalizedTime < 0.5f)
        {
            float t = normalizedTime / 0.5f;

            scaleY = Mathf.Lerp(1f, 1f + hopStretch, t);
            scaleX = Mathf.Lerp(1f, 1f - hopSquash * 0.5f, t);
        }
        else
        {
            float t = (normalizedTime - 0.5f) / 0.5f;

            scaleY = Mathf.Lerp(1f + hopStretch, 1f - hopSquash, t);
            scaleX = Mathf.Lerp(1f - hopSquash * 0.5f, 1f + hopSquash, t);
        }

        Vector3 scale = originalScale;

        float signX = Mathf.Sign(transform.localScale.x);

        scale.x = Mathf.Abs(originalScale.x) * signX * scaleX;
        scale.y = originalScale.y * scaleY;

        transform.localScale = scale;
    }

    // ============================================================
    // ARRIVEE NODE
    // ============================================================

    private void ArriveAtCurrentPathNode()
    {
        currentNode = currentPath[pathIndex];

        pathIndex++;

        // Si on est en chasse et que le timer est arrivé à zéro,
        // on termine le segment et on recalcule immédiatement.
        if (isChasing && chasePathDirty)
        {
            FinishPath();

            chasePathDirty = false;

            RecalculateChasePath();

            return;
        }

        if (pathIndex >= currentPath.Count)
        {
            FinishPath();

            if (isChasing)
                chasePathDirty = true;

            return;
        }

        SC_guard_point nextNode = currentPath[pathIndex];

        if (nextNode == null || !IsConnected(currentNode, nextNode))
        {
            FinishPath();

            if (isChasing)
                chasePathDirty = true;

            return;
        }

        segmentStartNode = currentNode;
        segmentTargetNode = nextNode;

        hopTimer = 0f;

        UpdateFlip(currentNode, nextNode);

        SetMovementAnimation(
            GetMovementAnimation(currentNode, nextNode)
        );

        isMovingBetweenNodes = true;
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

        // PATROUILLE ALEATOIRE

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

        // PATROUILLE ALLER-RETOUR

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

        chasePathDirty = false;

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
    // FIN CHEMIN
    // ============================================================

    private void FinishPath()
    {
        currentPath.Clear();

        pathIndex = 0;

        isMovingBetweenNodes = false;

        segmentStartNode = null;
        segmentTargetNode = null;

        hopTimer = 0f;

        ResetGoofyScale();

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

        // Ne jamais modifier currentChaseSpeed ici.
        // La vitesse doit rester continue entre les segments.

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
                ResetGoofyScale();

                break;

            case AnimationMovement.LadderDown:

                animator.SetBool(ladderDownParameter, true);

                SetFlashlight(false);
                ResetGoofyScale();

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
    // RESET SCALE
    // ============================================================

    private void ResetGoofyScale()
    {
        Vector3 scale = originalScale;

        if (flipCharacter)
        {
            float sign = Mathf.Sign(transform.localScale.x);

            if (sign == 0f)
                sign = facingRightByDefault ? 1f : -1f;

            scale.x = Mathf.Abs(originalScale.x) * sign;
        }

        transform.localScale = scale;
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

                if (!IsConnected(current, neighbour))
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
    // NODE JOUEUR
    // ============================================================

    private SC_guard_point FindClosestNodeForChase(Vector3 playerPosition)
    {
        SC_guard_point[] nodes =
            FindObjectsByType<SC_guard_point>(FindObjectsSortMode.None);

        SC_guard_point bestNode = null;
        float bestScore = Mathf.Infinity;

        // PRIORITE : nodes au même étage.

        foreach (SC_guard_point node in nodes)
        {
            if (node == null)
                continue;

            float verticalDifference = Mathf.Abs(
                playerPosition.y - node.transform.position.y
            );

            if (verticalDifference > verticalTolerance)
                continue;

            float horizontalDistance = Mathf.Abs(
                playerPosition.x - node.transform.position.x
            );

            float score =
                horizontalDistance +
                verticalDifference * verticalWeight;

            if (score < bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        // Si aucun node n'est au même étage,
        // on cherche le niveau vertical le plus proche.
        // On ne ralentit PAS le garde pour autant.

        if (bestNode == null)
        {
            float bestVerticalDifference = Mathf.Infinity;

            foreach (SC_guard_point node in nodes)
            {
                if (node == null)
                    continue;

                float difference = Mathf.Abs(
                    playerPosition.y - node.transform.position.y
                );

                if (difference < bestVerticalDifference)
                {
                    bestVerticalDifference = difference;
                    bestNode = node;
                }
            }
        }

        return bestNode;
    }

    // ============================================================
    // API CHASE
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

                ResetGoofyScale();
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

            SetFlashlight(false);

            isWaitingAtPatrolPoint = false;

            // On conserve la vitesse actuelle si une chasse
            // reprend pendant un déplacement.
            if (!isMovingBetweenNodes)
                currentChaseSpeed = 0f;

            chaseTimer = 0f;
            chasePathDirty = true;
            hopTimer = 0f;

            if (!isMovingBetweenNodes)
            {
                chasePathDirty = false;
                RecalculateChasePath();
            }

            return;
        }

        if (!isChasing)
            return;

        isChasing = false;
        movementMode = MovementMode.Patrol;

        StartSleep();
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
