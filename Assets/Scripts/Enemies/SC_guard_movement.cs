using System.Collections.Generic;
using UnityEngine;

public class SC_guard_movement : MonoBehaviour
{
    public enum MovementMode
    {
        Chase,
        Patrol
    }
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

    [Tooltip("Distance maximale pour considérer un node comme étant au même niveau que le joueur.")]
    [SerializeField] private float verticalTolerance = 2f;

    [Tooltip("Influence de la hauteur lors du choix du node du joueur.")]
    [SerializeField] private float verticalWeight = 0.15f;

    // ============================================================
    // RETOUR A LA BASE
    // ============================================================

    [Header("Retour à la base")]
    [SerializeField] private float baseReturnRefreshRate = 0.5f;

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
    // TIMERS
    // ============================================================

    private float chaseTimer = 0f;
    private float patrolWaitTimer = 0f;
    private float baseReturnTimer = 0f;

    // ============================================================
    // ETAT
    // ============================================================

    private bool isMovingBetweenNodes = false;

    private bool isChasing = false;

    private bool isReturningToBase = false;

    private bool isWaitingAtPatrolPoint = false;

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
    // TRAQUE
    // ============================================================

    private void UpdateChase()
    {
        if (SC_player.instance == null)
            return;

        /*
         * Si le garde est actuellement entre deux nodes,
         * il doit impérativement terminer son segment.
         */

        if (isMovingBetweenNodes)
        {
            FollowCurrentPath();
            return;
        }

        /*
         * Le garde est arrivé sur un node.
         * Il peut recalculer son chemin.
         */

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
    // CALCUL DU CHEMIN DE TRAQUE
    // ============================================================

    private void RecalculateChasePath()
    {
        if (SC_player.instance == null)
            return;

        if (currentNode == null)
            return;

        /*
         * Impossible de recalculer au milieu d'un segment.
         */

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

        /*
         * Calcul A*.
         */

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

        /*
         * Vérification absolue de la connexion.
         */

        if (!IsConnected(
                currentNode,
                nextNode))
        {
            FinishPath();
            return;
        }

        /*
         * Flip horizontal.
         */

        UpdateFlip(
            currentNode,
            nextNode
        );

        /*
         * Détermination de l'animation.
         */

        AnimationMovement movement =
            GetMovementAnimation(
                currentNode,
                nextNode
            );

        SetMovementAnimation(
            movement
        );

        /*
         * Maintenant seulement,
         * on autorise le mouvement.
         */

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

        /*
         * Le prochain node doit être connecté
         * au node actuel.
         */

        if (!IsConnected(
                currentNode,
                target))
        {
            FinishPath();
            return;
        }

        /*
         * Direction unique :
         *
         * currentNode -> target
         *
         * Jamais directement vers le joueur.
         */

        Vector3 targetPosition =
            target.transform.position;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

        /*
         * Arrivée sur le node.
         */

        if (Vector3.Distance(
                transform.position,
                targetPosition
            ) <= 0.001f)
        {
            /*
             * Position exacte du node.
             */

            transform.position =
                targetPosition;

            /*
             * Le garde est officiellement
             * sur ce node.
             */

            currentNode =
                target;

            /*
             * Passe au prochain segment.
             */

            pathIndex++;

            /*
             * Fin du chemin.
             */

            if (pathIndex >= currentPath.Count)
            {
                FinishPath();

                /*
                 * Si on était en retour à la base,
                 * on reprend la patrouille.
                 */

                if (isReturningToBase)
                {
                    isReturningToBase = false;

                    StartPatrol();

                    return;
                }

                return;
            }

            /*
             * Vérification du prochain segment.
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

            /*
             * Prépare le prochain segment :
             *
             * - flip
             * - animation
             */

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
         * Si le garde parcourt une liaison,
         * il doit la terminer.
         */

        if (isMovingBetweenNodes)
        {
            FollowCurrentPath();
            return;
        }

        /*
         * Sinon on choisit une nouvelle destination.
         */

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
         * Si on est déjà sur le point.
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
    // RETOUR A LA BASE
    // ============================================================

    private void UpdateReturnToBase()
    {
        if (baseNode == null)
        {
            isReturningToBase = false;

            StartPatrol();

            return;
        }

        /*
         * Si on parcourt un segment,
         * on ne recalcule surtout pas.
         */

        if (isMovingBetweenNodes)
        {
            FollowCurrentPath();
            return;
        }

        /*
         * Arrivé à la base.
         */

        if (currentNode == baseNode)
        {
            isReturningToBase = false;

            StartPatrol();

            return;
        }

        /*
         * Le garde est sur un node.
         */

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

        /*
         * Impossible de recalculer au milieu
         * d'un segment.
         */

        if (isMovingBetweenNodes)
            return;

        if (currentNode == baseNode)
        {
            FinishPath();

            isReturningToBase = false;

            StartPatrol();

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
        }
    }

    // ============================================================
    // DEMARRER LA PATROUILLE
    // ============================================================

    private void StartPatrol()
    {
        movementMode =
            MovementMode.Patrol;

        FinishPath();

        isWaitingAtPatrolPoint =
            false;

        patrolWaitTimer = 0f;

        currentPatrolIndex = -1;

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

        /*
         * Petite tolérance pour éviter qu'une différence
         * minuscule soit considérée comme une échelle.
         */

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

        /*
         * Désactive toutes les animations
         * de déplacement.
         */

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

        /*
         * Active l'animation correspondante.
         */

        switch (movement)
        {
            case AnimationMovement.Walk:

                animator.SetBool(
                    walkParameter,
                    true
                );
                flashlight.SetActive(true);

                break;

            case AnimationMovement.LadderUp:

                animator.SetBool(
                    ladderUpParameter,
                    true
                );
                flashlight.SetActive(false);

                break;

            case AnimationMovement.LadderDown:

                animator.SetBool(
                    ladderDownParameter,
                    true
                );
                flashlight.SetActive(false);

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

        /*
         * Pour un déplacement vertical,
         * on conserve l'orientation actuelle.
         */

        if (Mathf.Abs(direction.x) <
            0.01f)
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
            (shouldFaceRight
                ? 1f
                : -1f);

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

                /*
                 * Le déplacement n'est possible
                 * que si la connexion existe réellement.
                 */

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

        foreach (
            SC_guard_point node
            in nodes)
        {
            if (node == null)
                continue;

            Vector3 nodePosition =
                node.transform.position;

            /*
             * Distance verticale.
             */

            float verticalDifference =
                Mathf.Abs(
                    playerPosition.y -
                    nodePosition.y
                );

            float toleratedVerticalDifference =
                Mathf.Max(
                    0f,
                    verticalDifference -
                    verticalTolerance
                );

            /*
             * Distance horizontale.
             */

            Vector2 playerXZ =
                new Vector2(
                    playerPosition.x,
                    playerPosition.z
                );

            Vector2 nodeXZ =
                new Vector2(
                    nodePosition.x,
                    nodePosition.z
                );

            float horizontalDistance =
                Vector2.Distance(
                    playerXZ,
                    nodeXZ
                );

            float score =
                horizontalDistance +
                (
                    toleratedVerticalDifference *
                    verticalWeight
                );

            if (score < bestScore)
            {
                bestScore = score;

                bestNode = node;
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
        if (enabled)
        {
            isChasing = true;

            isReturningToBase = false;

            isWaitingAtPatrolPoint = false;

            movementMode =
                MovementMode.Chase;

            /*
             * On annule uniquement l'ancien chemin
             * si aucun segment n'est actuellement parcouru.
             */

            if (!isMovingBetweenNodes)
            {
                FinishPath();
            }

            chaseTimer = 0f;

            /*
             * Si aucun segment n'est en cours,
             * on lance immédiatement la poursuite.
             */

            if (!isMovingBetweenNodes)
            {
                RecalculateChasePath();
            }
        }
        else
        {
            isChasing = false;

            isWaitingAtPatrolPoint = false;

            /*
             * Si le garde est actuellement entre deux nodes,
             * il termine d'abord son segment.
             */

            if (baseNode != null &&
                currentNode != baseNode)
            {
                isReturningToBase = true;

                baseReturnTimer = 0f;

                /*
                 * Si aucun segment n'est en cours,
                 * on calcule immédiatement le retour.
                 */

                if (!isMovingBetweenNodes)
                {
                    FinishPath();

                    RecalculateReturnPath();
                }
            }
            else
            {
                isReturningToBase = false;

                StartPatrol();
            }
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
