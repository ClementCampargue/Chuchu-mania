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
    [Tooltip("Conservé pour compatibilité avec le reste du projet.")]
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

    [Tooltip("Le X du joueur est pris en compte uniquement si son Y est suffisamment proche.")]
    [SerializeField] private float verticalTolerance = 0.5f;

    [Tooltip("Influence de la hauteur lors du choix du node du joueur.")]
    [SerializeField] private float verticalWeight = 0.15f;

    [Tooltip("Autorise le garde à faire demi-tour immédiatement sur une liaison horizontale pendant la traque.")]
    [SerializeField] private bool allowChaseHorizontalTurn = true;

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
    private float sleepTimer = 0f;

    // ============================================================
    // ETAT
    // ============================================================

    private bool isMovingBetweenNodes = false;
    private bool isChasing = false;
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
            currentNode =
                FindClosestNode(
                    transform.position
                );
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

        UpdatePatrol();
    }

    // ============================================================
    // SOMMEIL
    // ============================================================

    private void StartSleep()
    {
        /*
         * STOP IMMEDIAT :
         *
         * On efface complètement le chemin actuel.
         * Le garde reste exactement à l'endroit où il était
         * lorsque la traque s'est terminée.
         */

        FinishPath();

        isMovingBetweenNodes = false;

        isWaitingAtPatrolPoint = false;

        isSleeping = true;

        sleepTimer =
            Mathf.Max(
                0f,
                sleepDuration
            );

        /*
         * Lampe éteinte.
         */

        SetFlashlight(false);

        /*
         * Arrêt des animations de déplacement.
         */

        StopMovementAnimation();

        /*
         * Animation Sleep.
         */

        if (animator != null)
        {
            animator.SetBool(
                sleepParameter,
                true
            );
        }

        /*
         * Si sleepDuration = 0,
         * on reprend immédiatement.
         */

        if (sleepTimer <= 0f)
        {
            FinishSleep();
        }
    }

    private void UpdateSleep()
    {
        /*
         * Aucun mouvement pendant le sommeil.
         */

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

        /*
         * Après le sommeil :
         *
         * - lampe rallumée
         * - retour au comportement de patrouille
         * - choix d'un des points habituels
         */

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

        Vector3 playerPosition =
            SC_player.instance.transform.position;

        /*
         * ========================================================
         * FILTRE Y
         * ========================================================
         *
         * Le X du joueur n'est utilisé pour le demi-tour
         * que si le joueur est quasiment au même niveau.
         */

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
         * Vérifie que le segment actuel est horizontal.
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
         * Position du joueur par rapport au garde.
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
         * Le joueur est-il derrière le garde ?
         */

        bool playerIsOpposite =
            playerIsRight != segmentGoesRight;

        if (!playerIsOpposite)
            return;

        /*
         * Demi-tour immédiat.
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

        /*
         * Le joueur est sur le node actuel.
         */

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
    // DEMARRER LE DEPLACEMENT
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

        SetMovementAnimation(
            GetMovementAnimation(
                currentNode,
                nextNode
            )
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
            FinishPath();
            return;
        }

        if (pathIndex >= currentPath.Count)
        {
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

        /*
         * Demi-tour pendant la traque.
         */

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

        /*
         * Arrivée.
         */

        if (Vector3.Distance(
                transform.position,
                targetPosition
            ) <= 0.001f)
        {
            transform.position =
                targetPosition;

            /*
             * Demi-tour.
             */

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

            /*
             * Arrivée normale sur le node.
             */

            currentNode =
                target;

            pathIndex++;

            /*
             * Fin du chemin.
             */

            if (pathIndex >= currentPath.Count)
            {
                FinishPath();
                return;
            }

            /*
             * Nouveau segment.
             */

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

        /*
         * Attente sur un point.
         */

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

        /*
         * Déplacement entre deux points.
         */

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

        /*
         * Déjà sur le point.
         */

        if (targetPoint == currentNode)
        {
            BeginPatrolWait();
            return;
        }

        /*
         * Calcul A*.
         */

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
    // DEMARRER LA PATROUILLE
    // ============================================================

    private void StartPatrol()
    {
        movementMode =
            MovementMode.Patrol;

        isChasing = false;

        isWaitingAtPatrolPoint = false;

        FinishPath();

        patrolWaitTimer = 0f;

        currentPatrolIndex = -1;

        /*
         * Lampe rallumée.
         */

        SetFlashlight(true);

        /*
         * Reprise des points habituels.
         */

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

        /*
         * Le premier node est celui sur lequel
         * se trouve déjà le garde.
         */

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
         * ETAPE 1 :
         * Y PROCHE -> LE X DU JOUEUR EST UTILISE
         * ========================================================
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
             * Si le node est trop éloigné verticalement,
             * son X est ignoré.
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
         * ETAPE 2 :
         * AUCUN NODE SUR LE MEME NIVEAU
         * ========================================================
         *
         * Dans ce cas, on cherche uniquement le niveau Y le
         * plus proche.
         *
         * Le X du joueur n'intervient PAS.
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
             * Si le garde dort, on le réveille.
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

            isWaitingAtPatrolPoint = false;

            movementMode =
                MovementMode.Chase;

            SetFlashlight(true);

            /*
             * S'il n'est pas déjà en déplacement,
             * on commence immédiatement la traque.
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

        movementMode =
            MovementMode.Patrol;

        /*
         * ========================================================
         * IMPORTANT
         * ========================================================
         *
         * On NE termine PAS le segment de chase.
         *
         * On arrête le garde immédiatement à sa position actuelle.
         */

        FinishPath();

        /*
         * Il s'endort immédiatement sur place.
         */

        StartSleep();
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
