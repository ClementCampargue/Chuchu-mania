using System.Collections.Generic;
using UnityEngine;

public class SC_guard_movement : MonoBehaviour
{
    public enum MovementMode
    {
        Chase,
        Patrol
    }

    [Header("Références")]
    public GameObject flashlight;

    // ============================================================
    // DEPLACEMENT
    // ============================================================

    [Header("Déplacement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private MovementMode movementMode = MovementMode.Patrol;

    // ============================================================
    // POINT ACTUEL
    // ============================================================

    [Header("Point actuel")]
    [SerializeField] private SC_guard_point currentNode;

    // ============================================================
    // ZONE DE BASE
    // ============================================================

    [Header("Zone de base")]
    [Tooltip("Point vers lequel le personnage revient lorsqu'il arrête de traquer.")]
    [SerializeField] private SC_guard_point baseNode;

    // ============================================================
    // PATROUILLE
    // ============================================================

    [Header("Points de balade")]
    [Tooltip("Liste des points entre lesquels le personnage se balade lorsqu'il ne traque pas.")]
    [SerializeField]
    private List<SC_guard_point> patrolPoints =
        new List<SC_guard_point>();

    [Tooltip("Temps d'attente sur un point avant de repartir.")]
    [SerializeField] private float patrolWaitTime = 1.5f;

    [Tooltip("Choisit un nouveau point aléatoire parmi les points de balade.")]
    [SerializeField] private bool randomPatrol = true;

    // ============================================================
    // TRAQUE
    // ============================================================

    [Header("Traque")]
    [Tooltip("Fréquence de recalcul du chemin vers le joueur.")]
    [SerializeField] private float chaseRefreshRate = 0.25f;

    [Tooltip("Le X du joueur est pris en compte uniquement si son Y est à cette distance du garde/node.")]
    [SerializeField] private float verticalTolerance = 0.5f;

    [Tooltip("Influence de la différence de hauteur lors du choix du node du joueur.")]
    [SerializeField] private float verticalWeight = 0.15f;

    [Tooltip("Autorise le garde à faire demi-tour immédiatement sur une liaison horizontale pendant la traque.")]
    [SerializeField] private bool allowChaseHorizontalTurn = true;

    // ============================================================
    // RETOUR A LA BASE
    // ============================================================

    [Header("Retour à la base")]
    [SerializeField] private float baseReturnRefreshRate = 0.5f;

    // ============================================================
    // SOMMEIL
    // ============================================================

    [Header("Sommeil après la traque")]
    [Tooltip("Durée pendant laquelle le garde dort avant de reprendre sa patrouille.")]
    [SerializeField] private float sleepDuration = 3f;

    [Tooltip("Nom du Bool Animator pour l'animation Sleep.")]
    [SerializeField] private string sleepParameter = "Sleep";

    // ============================================================
    // ANIMATION
    // ============================================================

    [Header("Animation")]
    [Tooltip("Animator du garde. Si vide, le script cherchera un Animator dans les enfants.")]
    [SerializeField] private Animator animator;

    [Tooltip("Nom du Bool Animator pour la marche.")]
    [SerializeField] private string walkParameter = "Walk";

    [Tooltip("Nom du Bool Animator pour monter une échelle.")]
    [SerializeField] private string ladderUpParameter = "LadderUp";

    [Tooltip("Nom du Bool Animator pour descendre une échelle.")]
    [SerializeField] private string ladderDownParameter = "LadderDown";

    // ============================================================
    // FLIP
    // ============================================================

    [Header("Flip")]
    [Tooltip("Retourne automatiquement le personnage selon sa direction horizontale.")]
    [SerializeField] private bool flipCharacter = true;

    [Tooltip("Si vrai, le personnage regarde vers la droite avec un scale X positif.")]
    [SerializeField] private bool facingRightByDefault = true;

    private Vector3 originalScale;

    // ============================================================
    // CHEMIN
    // ============================================================

    private List<SC_guard_point> currentPath =
        new List<SC_guard_point>();

    private int pathIndex = 0;

    // ============================================================
    // SEGMENT ACTUEL
    // ============================================================

    private SC_guard_point segmentStartNode;
    private SC_guard_point segmentTargetNode;

    private bool movingBackwardsOnChase = false;

    // ============================================================
    // TIMERS
    // ============================================================

    private float chaseTimer = 0f;
    private float patrolWaitTimer = 0f;
    private float baseReturnTimer = 0f;
    private float sleepTimer = 0f;

    // ============================================================
    // ETAT
    // ============================================================

    private bool isMovingBetweenNodes = false;
    private bool isChasing = false;
    private bool isReturningToBase = false;
    private bool isWaitingAtPatrolPoint = false;
    private bool isSleeping = false;

    private int currentPatrolIndex = -1;

    // ============================================================
    // TYPE D'ANIMATION
    // ============================================================

    private enum AnimationMovement
    {
        Walk,
        LadderUp,
        LadderDown
    }

    // ============================================================
    // INITIALISATION
    // ============================================================

    private void Start()
    {
        originalScale = transform.localScale;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (currentNode == null)
        {
            currentNode = FindClosestNode(transform.position);
        }

        if (baseNode == null)
        {
            baseNode = currentNode;
        }

        if (currentNode != null)
        {
            transform.position =
                currentNode.transform.position;
        }

        movementMode =
            MovementMode.Patrol;

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

        if (isReturningToBase)
        {
            UpdateReturnToBase();
            return;
        }

        UpdatePatrol();
    }

    // ============================================================
    // SOMMEIL
    // ============================================================

    private void StartSleep()
    {
        FinishPath();

        isMovingBetweenNodes = false;
        isSleeping = true;

        sleepTimer =
            Mathf.Max(
                0f,
                sleepDuration
            );

        SetFlashlight(false);

        StopMovementAnimation();

        if (animator != null)
        {
            animator.SetBool(
                sleepParameter,
                true
            );
        }

        if (sleepTimer <= 0f)
        {
            FinishSleep();
        }
    }

    private void UpdateSleep()
    {
        sleepTimer -= Time.deltaTime;

        if (sleepTimer <= 0f)
        {
            FinishSleep();
        }
    }

    private void FinishSleep()
    {
        isSleeping = false;

        if (animator != null)
        {
            animator.SetBool(
                sleepParameter,
                false
            );
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

        if (isMovingBetweenNodes)
        {
            if (allowChaseHorizontalTurn)
            {
                CheckForChaseHorizontalTurn();
            }

            FollowCurrentPath();
            return;
        }

        chaseTimer -= Time.deltaTime;

        if (chaseTimer <= 0f)
        {
            chaseTimer =
                chaseRefreshRate;

            RecalculateChasePath();
        }

        if (isMovingBetweenNodes)
        {
            FollowCurrentPath();
        }
    }

    // ============================================================
    // CHANGEMENT DE SENS PENDANT LA TRAQUE
    // ============================================================

    private void CheckForChaseHorizontalTurn()
    {
        if (!isChasing)
            return;

        if (!isMovingBetweenNodes)
            return;

        if (SC_player.instance == null)
            return;

        if (segmentStartNode == null ||
            segmentTargetNode == null)
        {
            return;
        }

        /*
         * ========================================================
         * CONDITION Y DU JOUEUR
         * ========================================================
         *
         * Le garde ne regarde le X du joueur pour décider
         * d'un demi-tour QUE si le joueur est quasiment à
         * la même hauteur que lui.
         */

        Vector3 playerPosition =
            SC_player.instance.transform.position;

        float playerVerticalDifference =
            Mathf.Abs(
                playerPosition.y -
                transform.position.y
            );

        if (playerVerticalDifference >
            verticalTolerance)
        {
            return;
        }

        /*
         * ========================================================
         * VERIFICATION DU SEGMENT
         * ========================================================
         *
         * Le demi-tour immédiat fonctionne uniquement sur
         * une liaison horizontale.
         */

        Vector3 startPosition =
            segmentStartNode.transform.position;

        Vector3 targetPosition =
            segmentTargetNode.transform.position;

        float segmentVerticalDifference =
            Mathf.Abs(
                targetPosition.y -
                startPosition.y
            );

        const float verticalThreshold = 0.05f;

        if (segmentVerticalDifference >
            verticalThreshold)
        {
            return;
        }

        float segmentDirection =
            targetPosition.x -
            startPosition.x;

        if (Mathf.Abs(segmentDirection) < 0.01f)
            return;

        /*
         * Direction du joueur par rapport au garde.
         */

        float playerDirection =
            playerPosition.x -
            transform.position.x;

        if (Mathf.Abs(playerDirection) < 0.05f)
            return;

        bool playerIsRight =
            playerDirection > 0f;

        bool segmentGoesRight =
            segmentDirection > 0f;

        /*
         * Le joueur se trouve-t-il dans la direction opposée
         * au déplacement actuel ?
         */

        bool playerIsOpposite =
            playerIsRight != segmentGoesRight;

        if (!playerIsOpposite)
            return;

        /*
         * Le garde doit revenir immédiatement vers le node
         * qu'il vient de quitter.
         */

        movingBackwardsOnChase = true;

        UpdateFlip(
            transform.position,
            segmentStartNode.transform.position
        );

        SetMovementAnimation(
            AnimationMovement.Walk
        );
    }

    // ============================================================
    // CALCUL DU CHEMIN DE TRAQUE
    // ============================================================

    private void RecalculateChasePath()
    {
        if (SC_player.instance == null)
            return;

        if (currentNode == null)
            return;

        if (isMovingBetweenNodes)
            return;

        Transform player =
            SC_player.instance.transform;

        SC_guard_point targetNode =
            FindClosestNodeForChase(
                player.position
            );

        if (targetNode == null)
            return;

        if (targetNode == currentNode)
        {
            FinishPath();
            return;
        }

        List<SC_guard_point> newPath =
            FindShortestPath(
                currentNode,
                targetNode
            );

        if (newPath != null &&
            newPath.Count > 0)
        {
            currentPath =
                newPath;

            pathIndex = 0;

            movingBackwardsOnChase = false;

            StartNextNodeMovement();
        }
        else
        {
            FinishPath();
        }
    }

    // ============================================================
    // DEMARRER LE DEPLACEMENT VERS LE PROCHAIN NODE
    // ============================================================

    private void StartNextNodeMovement()
    {
        if (currentNode == null)
        {
            FinishPath();
            return;
        }

        if (currentPath == null ||
            currentPath.Count == 0)
        {
            FinishPath();
            return;
        }

        if (pathIndex >= currentPath.Count)
        {
            FinishPath();
            return;
        }

        SC_guard_point nextNode =
            currentPath[pathIndex];

        if (nextNode == null)
        {
            FinishPath();
            return;
        }

        if (!IsConnected(
                currentNode,
                nextNode))
        {
            FinishPath();
            return;
        }

        segmentStartNode =
            currentNode;

        segmentTargetNode =
            nextNode;

        movingBackwardsOnChase = false;

        UpdateFlip(
            currentNode,
            nextNode
        );

        AnimationMovement movement =
            GetMovementAnimation(
                currentNode,
                nextNode
            );

        SetMovementAnimation(
            movement
        );

        isMovingBetweenNodes = true;
    }

    // ============================================================
    // SUIVI DU CHEMIN
    // ============================================================

    private void FollowCurrentPath()
    {
        if (currentPath == null ||
            currentPath.Count == 0)
        {
            /*
             * Si le garde doit retourner à sa base et que
             * l'ancien chemin n'existe plus, on recalcule.
             */

            if (isReturningToBase &&
                !isChasing)
            {
                RecalculateReturnPath();
            }
            else
            {
                FinishPath();
            }

            return;
        }

        if (pathIndex >= currentPath.Count)
        {
            /*
             * IMPORTANT :
             *
             * Si la traque vient de se terminer, l'ancien chemin
             * de chase ne doit PAS déclencher le sommeil.
             *
             * On passe d'abord au retour vers la base.
             */

            if (isReturningToBase &&
                !isChasing)
            {
                FinishPath();
                RecalculateReturnPath();
                return;
            }

            FinishPath();
            return;
        }

        if (currentNode == null)
        {
            FinishPath();
            return;
        }

        SC_guard_point target =
            currentPath[pathIndex];

        if (target == null)
        {
            FinishPath();
            return;
        }

        if (!IsConnected(
                currentNode,
                target))
        {
            FinishPath();
            return;
        }

        Vector3 targetPosition;

        // ========================================================
        // DEMI-TOUR PENDANT LA TRAQUE
        // ========================================================

        if (isChasing &&
            movingBackwardsOnChase &&
            segmentStartNode != null)
        {
            targetPosition =
                segmentStartNode.transform.position;
        }
        else
        {
            targetPosition =
                target.transform.position;
        }

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

        // ========================================================
        // ARRIVEE
        // ========================================================

        if (Vector3.Distance(
                transform.position,
                targetPosition
            ) <= 0.001f)
        {
            transform.position =
                targetPosition;

            // ====================================================
            // DEMI-TOUR PENDANT LA TRAQUE
            // ====================================================

            if (isChasing &&
                movingBackwardsOnChase)
            {
                currentNode =
                    segmentStartNode;

                movingBackwardsOnChase = false;

                isMovingBetweenNodes = false;

                currentPath.Clear();

                pathIndex = 0;

                segmentStartNode = null;
                segmentTargetNode = null;

                StopMovementAnimation();

                chaseTimer = 0f;

                RecalculateChasePath();

                return;
            }

            // ====================================================
            // ARRIVEE NORMALE SUR LE NODE
            // ====================================================

            currentNode =
                target;

            pathIndex++;

            // ====================================================
            // FIN DU SEGMENT / FIN DU CHEMIN
            // ====================================================

            if (pathIndex >= currentPath.Count)
            {
                /*
                 * Si le chase vient de se terminer et que le garde
                 * doit rentrer à la base, on NE DORT PAS ici.
                 *
                 * On efface l'ancien chemin de chase puis on calcule
                 * le chemin vers la base.
                 */

                if (isReturningToBase &&
                    !isChasing)
                {
                    FinishPath();

                    /*
                     * S'il est déjà à la base, sommeil immédiat.
                     */

                    if (currentNode == baseNode)
                    {
                        isReturningToBase = false;

                        StartSleep();

                        return;
                    }

                    /*
                     * Sinon on repart vers la base.
                     */

                    isReturningToBase = true;

                    baseReturnTimer = 0f;

                    RecalculateReturnPath();

                    return;
                }

                /*
                 * Cas normal : fin du chemin de patrouille.
                 */

                FinishPath();

                return;
            }

            // ====================================================
            // PROCHAIN SEGMENT
            // ====================================================

            SC_guard_point nextNode =
                currentPath[pathIndex];

            if (nextNode == null ||
                !IsConnected(
                    currentNode,
                    nextNode))
            {
                FinishPath();
                return;
            }

            segmentStartNode =
                currentNode;

            segmentTargetNode =
                nextNode;

            movingBackwardsOnChase = false;

            UpdateFlip(
                currentNode,
                nextNode
            );

            SetMovementAnimation(
                GetMovementAnimation(
                    currentNode,
                    nextNode
                )
            );

            isMovingBetweenNodes = true;
        }
    }

    // ============================================================
    // PATROUILLE
    // ============================================================

    private void UpdatePatrol()
    {
        if (patrolPoints == null ||
            patrolPoints.Count == 0)
        {
            return;
        }

        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer -=
                Time.deltaTime;

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

    // ============================================================
    // CHOIX DU POINT DE PATROUILLE
    // ============================================================

    private void ChooseNextPatrolPoint()
    {
        if (patrolPoints == null ||
            patrolPoints.Count == 0)
        {
            return;
        }

        if (currentNode == null)
        {
            return;
        }

        List<SC_guard_point> validPoints =
            new List<SC_guard_point>();

        foreach (SC_guard_point point
                 in patrolPoints)
        {
            if (point == null)
                continue;

            if (point == currentNode &&
                patrolPoints.Count > 1)
            {
                continue;
            }

            validPoints.Add(point);
        }

        if (validPoints.Count == 0)
            return;

        SC_guard_point targetPoint;

        if (randomPatrol)
        {
            targetPoint =
                validPoints[
                    Random.Range(
                        0,
                        validPoints.Count
                    )
                ];
        }
        else
        {
            currentPatrolIndex++;

            if (currentPatrolIndex >=
                validPoints.Count)
            {
                currentPatrolIndex = 0;
            }

            targetPoint =
                validPoints[
                    currentPatrolIndex
                ];
        }

        if (targetPoint == null)
            return;

        if (targetPoint == currentNode)
        {
            BeginPatrolWait();
            return;
        }

        List<SC_guard_point> newPath =
            FindShortestPath(
                currentNode,
                targetPoint
            );

        if (newPath != null &&
            newPath.Count > 0)
        {
            currentPath =
                newPath;

            pathIndex = 0;

            StartNextNodeMovement();
        }
    }

    // ============================================================
    // ATTENTE SUR UN POINT
    // ============================================================

    private void BeginPatrolWait()
    {
        FinishPath();

        isWaitingAtPatrolPoint =
            true;

        patrolWaitTimer =
            patrolWaitTime;
    }

    // ============================================================
    // RETOUR A LA BASE
    // ============================================================

    private void UpdateReturnToBase()
    {
        if (baseNode == null)
        {
            isReturningToBase = false;

            StartSleep();

            return;
        }

        /*
         * Le garde termine le segment actuel avant de recalculer
         * son chemin vers la base.
         */

        if (isMovingBetweenNodes)
        {
            FollowCurrentPath();
            return;
        }

        /*
         * Déjà arrivé à la base.
         */

        if (currentNode == baseNode)
        {
            isReturningToBase = false;

            StartSleep();

            return;
        }

        baseReturnTimer -=
            Time.deltaTime;

        if (baseReturnTimer <= 0f)
        {
            baseReturnTimer =
                baseReturnRefreshRate;

            RecalculateReturnPath();
        }

        if (isMovingBetweenNodes)
        {
            FollowCurrentPath();
        }
    }

    // ============================================================
    // CALCUL DU RETOUR
    // ============================================================

    private void RecalculateReturnPath()
    {
        if (baseNode == null ||
            currentNode == null)
        {
            return;
        }

        if (isMovingBetweenNodes)
            return;

        if (currentNode == baseNode)
        {
            FinishPath();

            isReturningToBase = false;

            StartSleep();

            return;
        }

        List<SC_guard_point> newPath =
            FindShortestPath(
                currentNode,
                baseNode
            );

        if (newPath != null &&
            newPath.Count > 0)
        {
            currentPath =
                newPath;

            pathIndex = 0;

            StartNextNodeMovement();
        }
        else
        {
            FinishPath();

            isReturningToBase = false;

            StartSleep();
        }
    }

    // ============================================================
    // DEMARRER LA PATROUILLE
    // ============================================================

    private void StartPatrol()
    {
        movementMode =
            MovementMode.Patrol;

        isChasing = false;
        isReturningToBase = false;

        FinishPath();

        isWaitingAtPatrolPoint = false;

        patrolWaitTimer = 0f;

        currentPatrolIndex = -1;

        SetFlashlight(true);

        ChooseNextPatrolPoint();
    }

    // ============================================================
    // FIN DU CHEMIN
    // ============================================================

    private void FinishPath()
    {
        currentPath.Clear();

        pathIndex = 0;

        isMovingBetweenNodes = false;

        movingBackwardsOnChase = false;

        segmentStartNode = null;
        segmentTargetNode = null;

        StopMovementAnimation();
    }

    // ============================================================
    // ANIMATION
    // ============================================================

    private AnimationMovement GetMovementAnimation(
        SC_guard_point from,
        SC_guard_point to)
    {
        if (from == null ||
            to == null)
        {
            return AnimationMovement.Walk;
        }

        float verticalDifference =
            to.transform.position.y -
            from.transform.position.y;

        const float verticalThreshold = 0.05f;

        if (verticalDifference >
            verticalThreshold)
        {
            return AnimationMovement.LadderUp;
        }

        if (verticalDifference <
            -verticalThreshold)
        {
            return AnimationMovement.LadderDown;
        }

        return AnimationMovement.Walk;
    }

    private void SetMovementAnimation(
        AnimationMovement movement)
    {
        if (animator == null)
            return;

        animator.SetBool(
            walkParameter,
            false
        );

        animator.SetBool(
            ladderUpParameter,
            false
        );

        animator.SetBool(
            ladderDownParameter,
            false
        );

        animator.SetBool(
            sleepParameter,
            false
        );

        switch (movement)
        {
            case AnimationMovement.Walk:

                animator.SetBool(
                    walkParameter,
                    true
                );

                SetFlashlight(true);

                break;

            case AnimationMovement.LadderUp:

                animator.SetBool(
                    ladderUpParameter,
                    true
                );

                SetFlashlight(false);

                break;

            case AnimationMovement.LadderDown:

                animator.SetBool(
                    ladderDownParameter,
                    true
                );

                SetFlashlight(false);

                break;
        }
    }

    private void StopMovementAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(
            walkParameter,
            false
        );

        animator.SetBool(
            ladderUpParameter,
            false
        );

        animator.SetBool(
            ladderDownParameter,
            false
        );
    }

    // ============================================================
    // FLASHLIGHT
    // ============================================================

    private void SetFlashlight(bool enabled)
    {
        if (flashlight == null)
            return;

        flashlight.SetActive(enabled);
    }

    // ============================================================
    // FLIP
    // ============================================================

    private void UpdateFlip(
        SC_guard_point from,
        SC_guard_point to)
    {
        if (!flipCharacter)
            return;

        if (from == null ||
            to == null)
        {
            return;
        }

        Vector3 direction =
            to.transform.position -
            from.transform.position;

        if (Mathf.Abs(direction.x) < 0.01f)
        {
            return;
        }

        bool movingRight =
            direction.x > 0f;

        bool shouldFaceRight =
            facingRightByDefault
                ? movingRight
                : !movingRight;

        Vector3 scale =
            originalScale;

        scale.x =
            Mathf.Abs(originalScale.x) *
            (
                shouldFaceRight
                    ? 1f
                    : -1f
            );

        transform.localScale =
            scale;
    }

    // ============================================================
    // FLIP VERS UNE POSITION
    // ============================================================

    private void UpdateFlip(
        Vector3 from,
        Vector3 to)
    {
        if (!flipCharacter)
            return;

        Vector3 direction =
            to - from;

        if (Mathf.Abs(direction.x) < 0.01f)
            return;

        bool movingRight =
            direction.x > 0f;

        bool shouldFaceRight =
            facingRightByDefault
                ? movingRight
                : !movingRight;

        Vector3 scale =
            originalScale;

        scale.x =
            Mathf.Abs(originalScale.x) *
            (
                shouldFaceRight
                    ? 1f
                    : -1f
            );

        transform.localScale =
            scale;
    }

    // ============================================================
    // CONNEXION
    // ============================================================

    private bool IsConnected(
        SC_guard_point from,
        SC_guard_point to)
    {
        if (from == null ||
            to == null)
        {
            return false;
        }

        if (from.connections == null)
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
        if (start == null ||
            target == null)
        {
            return null;
        }

        if (start == target)
        {
            return new List<SC_guard_point>();
        }

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
        > gCost =
            new Dictionary<
                SC_guard_point,
                float
            >();

        Dictionary<
            SC_guard_point,
            float
        > fCost =
            new Dictionary<
                SC_guard_point,
                float
            >();

        openSet.Add(start);

        gCost[start] = 0f;

        fCost[start] =
            Heuristic(
                start,
                target
            );

        while (openSet.Count > 0)
        {
            SC_guard_point current =
                openSet[0];

            for (
                int i = 1;
                i < openSet.Count;
                i++
            )
            {
                SC_guard_point candidate =
                    openSet[i];

                if (!fCost.ContainsKey(candidate))
                    continue;

                if (!fCost.ContainsKey(current) ||
                    fCost[candidate] <
                    fCost[current])
                {
                    current = candidate;
                }
            }

            if (current == target)
            {
                return ReconstructPath(
                    cameFrom,
                    current
                );
            }

            openSet.Remove(current);

            closedSet.Add(current);

            if (current.connections == null)
                continue;

            foreach (
                SC_guard_point neighbour
                in current.connections)
            {
                if (neighbour == null)
                    continue;

                if (closedSet.Contains(neighbour))
                    continue;

                if (!IsConnected(
                        current,
                        neighbour))
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        current.transform.position,
                        neighbour.transform.position
                    );

                float tentativeGCost =
                    gCost[current] +
                    distance;

                if (!gCost.ContainsKey(neighbour))
                {
                    gCost[neighbour] =
                        Mathf.Infinity;
                }

                if (tentativeGCost <
                    gCost[neighbour])
                {
                    cameFrom[neighbour] =
                        current;

                    gCost[neighbour] =
                        tentativeGCost;

                    fCost[neighbour] =
                        tentativeGCost +
                        Heuristic(
                            neighbour,
                            target
                        );

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }

        return null;
    }

    // ============================================================
    // HEURISTIQUE
    // ============================================================

    private float Heuristic(
        SC_guard_point a,
        SC_guard_point b)
    {
        return Vector3.Distance(
            a.transform.position,
            b.transform.position
        );
    }

    // ============================================================
    // RECONSTRUCTION DU CHEMIN
    // ============================================================

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

        if (path.Count > 0)
        {
            path.RemoveAt(0);
        }

        return path;
    }

    // ============================================================
    // RECHERCHE DU POINT LE PLUS PROCHE
    // ============================================================

    private SC_guard_point FindClosestNode(
        Vector3 position)
    {
        SC_guard_point[] nodes =
            FindObjectsByType<SC_guard_point>(
                FindObjectsSortMode.None
            );

        SC_guard_point closest = null;

        float closestDistance =
            Mathf.Infinity;

        foreach (
            SC_guard_point node
            in nodes)
        {
            if (node == null)
                continue;

            float distance =
                Vector3.Distance(
                    position,
                    node.transform.position
                );

            if (distance <
                closestDistance)
            {
                closestDistance =
                    distance;

                closest = node;
            }
        }

        return closest;
    }

    // ============================================================
    // RECHERCHE DU NODE POUR LA TRAQUE
    // ============================================================

    private SC_guard_point FindClosestNodeForChase(
        Vector3 playerPosition)
    {
        SC_guard_point[] nodes =
            FindObjectsByType<SC_guard_point>(
                FindObjectsSortMode.None
            );

        SC_guard_point bestNode = null;

        float bestScore =
            Mathf.Infinity;

        /*
         * ========================================================
         * ETAPE 1
         * ========================================================
         *
         * On cherche UNIQUEMENT parmi les nodes qui sont
         * suffisamment proches du joueur sur Y.
         *
         * Dans cette zone, le X devient le critère principal.
         */

        foreach (
            SC_guard_point node
            in nodes)
        {
            if (node == null)
                continue;

            Vector3 nodePosition =
                node.transform.position;

            float verticalDifference =
                Mathf.Abs(
                    playerPosition.y -
                    nodePosition.y
                );

            /*
             * Le node est sur le même niveau que le joueur.
             */

            if (verticalDifference >
                verticalTolerance)
            {
                continue;
            }

            float horizontalDistance =
                Mathf.Abs(
                    playerPosition.x -
                    nodePosition.x
                );

            float score =
                horizontalDistance +
                (
                    verticalDifference *
                    verticalWeight
                );

            if (score < bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        /*
         * ========================================================
         * ETAPE 2 : FALLBACK
         * ========================================================
         *
         * Si aucun node n'est à la hauteur du joueur, on NE
         * choisit surtout pas un node uniquement parce qu'il
         * possède un X proche.
         *
         * On cherche simplement le niveau vertical le plus proche.
         */

        if (bestNode == null)
        {
            float bestVerticalDifference =
                Mathf.Infinity;

            foreach (
                SC_guard_point node
                in nodes)
            {
                if (node == null)
                    continue;

                float verticalDifference =
                    Mathf.Abs(
                        playerPosition.y -
                        node.transform.position.y
                    );

                if (verticalDifference <
                    bestVerticalDifference)
                {
                    bestVerticalDifference =
                        verticalDifference;

                    bestNode = node;
                }
            }
        }

        return bestNode;
    }

    // ============================================================
    // API
    // ============================================================

    public void SetChaseMode(
        bool enabled)
    {
        // ========================================================
        // DEBUT DE TRAQUE
        // ========================================================

        if (enabled)
        {
            /*
             * Réveil immédiat si le garde dormait.
             */

            if (isSleeping)
            {
                isSleeping = false;

                if (animator != null)
                {
                    animator.SetBool(
                        sleepParameter,
                        false
                    );
                }
            }

            isChasing = true;

            isReturningToBase = false;

            isWaitingAtPatrolPoint = false;

            movementMode =
                MovementMode.Chase;

            SetFlashlight(true);

            /*
             * Si aucun segment n'est actuellement parcouru,
             * on peut repartir immédiatement vers le joueur.
             */

            if (!isMovingBetweenNodes)
            {
                FinishPath();

                chaseTimer = 0f;

                RecalculateChasePath();
            }

            return;
        }

        // ========================================================
        // FIN DE TRAQUE
        // ========================================================

        isChasing = false;

        isWaitingAtPatrolPoint = false;

        /*
         * Le garde doit toujours rentrer à la base.
         */

        if (baseNode == null)
        {
            isReturningToBase = false;

            StartSleep();

            return;
        }

        /*
         * Si le garde est déjà à la base, sommeil immédiat.
         */

        if (currentNode == baseNode &&
            !isMovingBetweenNodes)
        {
            isReturningToBase = false;

            StartSleep();

            return;
        }

        /*
         * On active le retour à la base.
         *
         * Si un segment de chase est actuellement parcouru,
         * FollowCurrentPath() va d'abord le terminer.
         *
         * Ensuite, l'ancien chemin de chase est supprimé et
         * un nouveau chemin vers baseNode est calculé.
         */

        isReturningToBase = true;

        movementMode =
            MovementMode.Patrol;

        baseReturnTimer = 0f;

        /*
         * Si aucun segment n'est en cours, on peut commencer
         * le retour immédiatement.
         */

        if (!isMovingBetweenNodes)
        {
            FinishPath();

            RecalculateReturnPath();
        }
    }

    // ============================================================
    // CHANGEMENT MANUEL DU NODE
    // ============================================================

    public void SetCurrentNode(
        SC_guard_point node)
    {
        currentNode = node;

        if (node != null)
        {
            transform.position =
                node.transform.position;
        }

        FinishPath();
    }

    // ============================================================
    // DEFINIR LA BASE
    // ============================================================

    public void SetBaseNode(
        SC_guard_point node)
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
