
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SC_guard_ai))]
public class SC_guard_movement : MonoBehaviour
{
    // Registre global des gardes actifs : aucune réservation de node.
    private static readonly HashSet<SC_guard_movement> activeGuards =
        new HashSet<SC_guard_movement>();

    [Header("Collision entre gardes")]
    [Tooltip("Distance minimale entre les centres de deux gardes.")]
    [SerializeField, Min(0.05f)] private float guardCollisionRadius = 0.65f;

    [Tooltip("Autorise deux gardes à se croiser s'ils sont suffisamment éloignés en hauteur.")]
    [SerializeField, Min(0f)] private float collisionVerticalTolerance = 0.8f;

    private void OnEnable()
    {
        activeGuards.Add(this);
    }

    private void OnDisable()
    {
        activeGuards.Remove(this);
    }

    /// <summary>
    /// Teste le déplacement complet (pas seulement sa position finale) contre
    /// les autres gardes. En cas de collision, le déplacement est bloqué.
    /// Cette méthode ne consulte aucun SC_guard_point.
    /// </summary>
    public static bool TryMoveWithGuardCollision(
        SC_guard_movement mover,
        Vector3 destination)
    {
        if (mover == null)
            return false;

        Vector3 start = mover.transform.position;
        float minDistance = mover.guardCollisionRadius;
        bool blocked = false;

        foreach (SC_guard_movement other in activeGuards)
        {
            if (other == null || other == mover || !other.isActiveAndEnabled)
                continue;

            Vector3 otherPosition = other.transform.position;
            if (Mathf.Abs(start.y - otherPosition.y) >
                mover.collisionVerticalTolerance &&
                Mathf.Abs(destination.y - otherPosition.y) >
                mover.collisionVerticalTolerance)
                continue;

            // Distance minimale entre la position de l'autre garde et le
            // segment parcouru pendant cette frame : évite de traverser un garde.
            Vector3 closest = ClosestPointOnMovementSegment(
                otherPosition, start, destination);

            float requiredDistance = Mathf.Max(
                minDistance,
                other.guardCollisionRadius);

            float closestDistance = Vector3.Distance(closest, otherPosition);
            float startDistance = Vector3.Distance(start, otherPosition);
            float endDistance = Vector3.Distance(destination, otherPosition);

            // Si les gardes sont déjà trop proches, autoriser celui qui
            // s'éloigne : cela évite de les immobiliser définitivement.
            bool movingAway = endDistance > startDistance + 0.001f;

            if (closestDistance < requiredDistance && !movingAway)
            {
                blocked = true;
                break;
            }
        }

        if (blocked)
            return false;

        mover.transform.position = destination;
        return true;
    }

    private static Vector3 ClosestPointOnMovementSegment(
        Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < 0.000001f)
            return start;

        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        return start + segment * t;
    }
    public enum MovementMode { Chase, Patrol }
    private List<Vector3> returnPath = new List<Vector3>();
    private int returnPathIndex;
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

    // Nouveau : indique que le garde retourne à sa base.
    private bool isReturningToBase;

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

        if (animator != null)
            animator.SetBool("Chase", false);

        guardAI.SetChaseActive(false);

        if (currentNode == null)
            currentNode = FindClosestNode(transform.position);

        if (baseNode == null)
            baseNode = currentNode;

        if (currentNode != null)
            TryMoveWithGuardCollision(this, currentNode.transform.position);

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

        if (animator != null)
            animator.SetBool("Chase", true);
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

        if (damage != null)
            damage.enabled = true;

        if (animator != null)
            animator.SetBool(sleepParameter, false);
        animator.SetTrigger("Wake");


        // Sommeil aléatoire : reprendre le déplacement interrompu.
        if (randomSleepInProgress)
        {
            randomSleepInProgress = false;

            return;
        }

    }
    public void wakeup()
    {
        isSleeping = false;

        if (damage != null)
            damage.enabled = true;

        if (animator != null)
            animator.SetBool(sleepParameter, false);

        SetFlashlight(true);

        // Sommeil aléatoire : reprendre le déplacement interrompu.
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

        // Après le sommeil suivant la traque,
        // retourner à la position initiale.
        if (isReturningToBase)
        {
            ReturnToBase();
            return;
        }

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

            // Annule un éventuel retour à la base.
            returnPath.Clear();
            returnPathIndex = 0;
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

            FinishPath();

            chaseMovementAuthorized = false;

            SetFlashlight(false);

            if (guardAI != null)
                guardAI.SetChaseActive(false);

            if (animator != null)
            {
                animator.SetBool("Chase", false);
                animator.SetTrigger(chaseStartTrigger);
            }

            return;
        }

        if (!isChasing)
            return;

        isChasing = false;
        chaseMovementAuthorized = false;

        movementMode = MovementMode.Patrol;

        if (guardAI != null)
            guardAI.SetChaseActive(false);

        if (animator != null)
            animator.SetBool("Chase", false);

        currentNode = FindClosestNode(transform.position);

        // Le retour à la base sera lancé après le sommeil.
        isReturningToBase = true;

        StartSleep();
    }

    // ============================================================
    // RETOUR A LA POSITION INITIALE
    // ============================================================

    private void ReturnToBase()
    {
        if (baseNode == null || guardAI == null)
        {
            Debug.LogWarning(
                "Retour impossible : baseNode ou guardAI manquant.",
                this
            );

            isReturningToBase = false;
            StartPatrol();
            return;
        }

        if (!guardAI.TryCalculateReturnPath(
            transform.position,
            baseNode,
            out returnPath))
        {
            isReturningToBase = false;
            StartPatrol();
            return;
        }

        returnPathIndex = 0;

        // Ignore le premier point s'il correspond
        // à la position actuelle du garde.
        if (returnPath.Count > 0 &&
            Vector3.Distance(
                transform.position,
                returnPath[0]) <= 0.01f)
        {
            returnPathIndex++;
        }

        if (returnPathIndex >= returnPath.Count)
        {
            ArriveAtBase();
            return;
        }

        StartReturnMovement();
    }
    private void StartReturnMovement()
    {
        if (returnPathIndex >= returnPath.Count)
        {
            ArriveAtBase();
            return;
        }

        Vector3 target = returnPath[returnPathIndex];

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

        isMovingBetweenNodes = true;
    }
    private void FollowReturnPath()
    {
        if (returnPath == null ||
            returnPathIndex >= returnPath.Count)
        {
            ArriveAtBase();
            return;
        }

        Vector3 targetPosition = returnPath[returnPathIndex];

        float verticalDifference =
            targetPosition.y - transform.position.y;

        if (Mathf.Abs(verticalDifference) <= 0.05f)
        {
            FollowGroundNormally(targetPosition);
        }
        else
        {
            FollowLadder(targetPosition);
        }

        if (Vector3.Distance(
            transform.position,
            targetPosition) <= 0.001f)
        {
            transform.position = targetPosition;

            returnPathIndex++;

            if (returnPathIndex >= returnPath.Count)
            {
                ArriveAtBase();
                return;
            }

            StartReturnMovement();
        }
    }
    private void ArriveAtBase()
    {
        if (!TryMoveWithGuardCollision(this, baseNode.transform.position))
            return;

        currentNode = baseNode;

        returnPath.Clear();
        returnPathIndex = 0;

        isMovingBetweenNodes = false;
        isReturningToBase = false;

        segmentStartNode = null;
        segmentTargetNode = null;

        StopMovementAnimation();

        // Reprise de la patrouille depuis le point de base.
        StartPatrol();
    }
    // ============================================================
    // PATROUILLE
    // ============================================================

    private void UpdatePatrol()
    {
        // IMPORTANT :
        // Le retour à la base doit fonctionner même si
        // patrolPoints est vide ou non renseigné.

        if (isReturningToBase)
        {
            FollowReturnPath();
            return;
        }

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

        isReturningToBase = false;

        isWaitingAtPatrolPoint = false;

        FinishPath();

        patrolWaitTimer = 0f;

        if (patrolPoints == null || patrolPoints.Count == 0)
            return;

        currentPatrolIndex = patrolPoints.IndexOf(currentNode);

        if (currentPatrolIndex < 0)
            currentPatrolIndex = FindClosestPatrolIndex();

        patrolDirection = 1;

        ChooseNextPatrolPoint();
    }

    // ============================================================
    // DEPLACEMENT SUR LE CHEMIN
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

            if (isReturningToBase)
                ReturnToBase();

            return;
        }

        SC_guard_point target = currentPath[pathIndex];

        if (target == null || !IsConnected(currentNode, target))
        {
            FinishPath();

            if (isReturningToBase)
                ReturnToBase();

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
        Vector3 nextPosition = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        TryMoveWithGuardCollision(this, nextPosition);
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

            TryMoveWithGuardCollision(this, position);
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

        TryMoveWithGuardCollision(this, position);
    }

    private void ArriveAtCurrentPathNode()
    {
        currentNode = currentPath[pathIndex];

        pathIndex++;

        // Le garde vient d'arriver à sa position initiale.
        if (isReturningToBase && currentNode == baseNode)
        {
            FinishPath();

            isReturningToBase = false;

            // Reprend la patrouille depuis la base.
            StartPatrol();
            return;
        }

        if (pathIndex >= currentPath.Count)
        {
            FinishPath();
            return;
        }

        SC_guard_point nextNode = currentPath[pathIndex];

        if (nextNode == null || !IsConnected(currentNode, nextNode))
        {
            FinishPath();

            if (isReturningToBase)
                ReturnToBase();

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
        List<SC_guard_point> path =
            new List<SC_guard_point>();

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
            FindObjectsByType<SC_guard_point>(
                FindObjectsSortMode.None
            );

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
        if (node != null &&
            !TryMoveWithGuardCollision(this, node.transform.position))
            return;

        currentNode = node;

        isReturningToBase = false;

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
