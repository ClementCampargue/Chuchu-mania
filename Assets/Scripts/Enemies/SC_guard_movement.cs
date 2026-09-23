using System.Collections.Generic;
using UnityEngine;

public class SC_guard_movement : MonoBehaviour
{
    public enum MovementMode
    {
        Chase,
        Patrol
    }
    [Header("Accélération / Décélération - Traque")]
    [Tooltip("Vitesse maximale du garde pendant la traque.")]
    [SerializeField] private float chaseMaxSpeed = 4.5f;

    [Tooltip("Temps nécessaire pour atteindre la vitesse maximale.")]
    [SerializeField] private float chaseAccelerationTime = 0.8f;

    [Tooltip("Temps nécessaire pour ralentir jusqu'à l'arrêt.")]
    [SerializeField] private float chaseDecelerationTime = 0.5f;

    private float currentChaseSpeed = 0f;
    [Header("Sommeil après une traque")]
    [SerializeField] private float sleepDuration = 3f;
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Références")]
    [SerializeField] private GameObject flashlight;
    [SerializeField] private Animator animator;

    // ============================================================
    // DEPLACEMENT
    // ============================================================

    [Header("Déplacement")]
    [SerializeField] private float moveSpeed = 3f;

    [SerializeField]
    private MovementMode movementMode = MovementMode.Patrol;

    // ============================================================
    // NODE ACTUEL
    // ============================================================

    [Header("Point actuel")]
    [SerializeField] private SC_guard_point currentNode;

    // ============================================================
    // BASE
    // ============================================================

    [Header("Zone de base")]
    [Tooltip("Conservé pour compatibilité avec le reste du projet.")]
    [SerializeField] private SC_guard_point baseNode;

    // ============================================================
    // PATROUILLE
    // ============================================================

    [Header("Patrouille")]
    [Tooltip("Points entre lesquels le garde se balade.")]
    [SerializeField]
    private List<SC_guard_point> patrolPoints =
        new List<SC_guard_point>();

    [Tooltip("Temps d'attente sur un point.")]
    [SerializeField] private float patrolWaitTime = 1.5f;

    [Tooltip("Choisit aléatoirement le prochain point.")]
    [SerializeField] private bool randomPatrol = true;

    // ============================================================
    // SOMMEIL ALEATOIRE
    // ============================================================

    [Header("Sommeil aléatoire")]

    [Tooltip("Le garde peut s'endormir aléatoirement pendant sa patrouille.")]
    [SerializeField] private bool allowRandomSleep = true;

    [Tooltip("Temps minimum avant qu'un nouveau sommeil puisse commencer.")]
    [SerializeField] private float minSleepInterval = 8f;

    [Tooltip("Temps maximum avant qu'un nouveau sommeil puisse commencer.")]
    [SerializeField] private float maxSleepInterval = 20f;

    [Tooltip("Durée minimale d'un sommeil.")]
    [SerializeField] private float minSleepDuration = 2f;

    [Tooltip("Durée maximale d'un sommeil.")]
    [SerializeField] private float maxSleepDuration = 5f;

    [Tooltip("Le garde peut s'endormir même lorsqu'il marche entre deux nodes.")]
    [SerializeField] private bool canSleepWhileMoving = true;

    private float randomSleepTimer;

    // ============================================================
    // TRAQUE
    // ============================================================

    [Header("Traque")]
    [Tooltip("Fréquence de recalcul du chemin vers le joueur.")]
    [SerializeField] private float chaseRefreshRate = 0.25f;

    [Tooltip("Tolérance verticale pour considérer deux positions comme étant au même niveau.")]
    [SerializeField] private float verticalTolerance = 0.5f;

    [Tooltip("Influence de la hauteur dans la recherche du node joueur.")]
    [SerializeField] private float verticalWeight = 0.15f;

    [Tooltip("Permet au garde de chercher une échelle lorsqu'il doit changer de niveau.")]
    [SerializeField] private bool useNearestLadder = true;

    [Tooltip("Distance maximale à laquelle le garde peut abandonner son segment horizontal pour rejoindre une échelle.")]
    [SerializeField] private float ladderDecisionDistance = 8f;

    [Tooltip("Le garde peut changer de direction même au milieu d'une liaison horizontale.")]
    [SerializeField] private bool allowMidSegmentTurn = true;

    // ============================================================
    // BONDS GOOFY DE TRAQUE
    // ============================================================

    [Header("Bonds goofy - Traque uniquement")]

    [Tooltip("Hauteur des petits bonds pendant la traque.")]
    [SerializeField] private float chaseHopHeight = 0.28f;

    [Tooltip("Durée d'un bond.")]
    [SerializeField] private float chaseHopDuration = 0.32f;

    [Tooltip("Écrasement du personnage à l'atterrissage.")]
    [SerializeField] private float hopSquash = 0.12f;

    [Tooltip("Étirement vertical pendant le bond.")]
    [SerializeField] private float hopStretch = 0.08f;

    [Tooltip("Petit mouvement latéral pendant le bond.")]
    [SerializeField] private float hopWobble = 0.025f;

    private float hopTimer;

    // ============================================================
    // ANIMATION SOMMEIL
    // ============================================================

    [Header("Sommeil")]
    [SerializeField] private string sleepParameter = "Sleep";

    // ============================================================
    // ANIMATION
    // ============================================================

    [Header("Animation")]
    [SerializeField] private string walkParameter = "Walk";

    [SerializeField] private string ladderUpParameter = "LadderUp";

    [SerializeField] private string ladderDownParameter = "LadderDown";

    // ============================================================
    // FLIP
    // ============================================================

    [Header("Flip")]
    [SerializeField] private bool flipCharacter = true;

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

    // ============================================================
    // TIMERS
    // ============================================================

    private float chaseTimer;

    private float patrolWaitTimer;

    private float sleepTimer;

    // ============================================================
    // ETATS
    // ============================================================

    private bool isMovingBetweenNodes;

    private bool isChasing;

    private bool isWaitingAtPatrolPoint;

    private bool isSleeping;

    private int currentPatrolIndex = -1;

    // ============================================================
    // TYPE ANIMATION
    // ============================================================

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
        {
            animator =
                GetComponentInChildren<Animator>();
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

        /*
         * Le sommeil aléatoire est uniquement possible
         * lorsqu'il n'est PAS en train de poursuivre le joueur.
         */
        UpdateRandomSleep();
    }
    private float UpdateChaseSpeed(bool hasTarget)
    {
        float accelerationTime = Mathf.Max(0.01f, chaseAccelerationTime);
        float decelerationTime = Mathf.Max(0.01f, chaseDecelerationTime);

        if (hasTarget)
        {
            currentChaseSpeed = Mathf.MoveTowards(
                currentChaseSpeed,
                chaseMaxSpeed,
                (chaseMaxSpeed / accelerationTime) * Time.deltaTime
            );
        }
        else
        {
            currentChaseSpeed = Mathf.MoveTowards(
                currentChaseSpeed,
                0f,
                (chaseMaxSpeed / decelerationTime) * Time.deltaTime
            );
        }

        return currentChaseSpeed;
    }
    // ============================================================
    // SOMMEIL ALEATOIRE
    // ============================================================

    private void ResetRandomSleepTimer()
    {
        if (!allowRandomSleep)
        {
            randomSleepTimer =
                Mathf.Infinity;

            return;
        }

        float min =
            Mathf.Max(
                0f,
                minSleepInterval
            );

        float max =
            Mathf.Max(
                min,
                maxSleepInterval
            );

        randomSleepTimer =
            Random.Range(
                min,
                max
            );
    }

    private void UpdateRandomSleep()
    {
        if (!allowRandomSleep)
        {
            return;
        }

        if (isChasing ||
            isSleeping)
        {
            return;
        }

        /*
         * Le garde peut être configuré pour ne dormir
         * que lorsqu'il est en train de marcher.
         */

        if (canSleepWhileMoving &&
            !isMovingBetweenNodes)
        {
            return;
        }

        randomSleepTimer -=
            Time.deltaTime;

        if (randomSleepTimer <= 0f)
        {
            StartRandomSleep();
        }
    }

    private void StartRandomSleep()
    {
        if (isChasing)
        {
            return;
        }

        /*
         * On ne change PAS le currentNode ici.
         *
         * Le garde peut donc réellement s'endormir
         * au milieu d'un déplacement.
         */

        isSleeping = true;

        float min =
            Mathf.Max(
                0f,
                minSleepDuration
            );

        float max =
            Mathf.Max(
                min,
                maxSleepDuration
            );

        sleepTimer =
            Random.Range(
                min,
                max
            );

        SetFlashlight(false);

        StopMovementAnimation();

        ResetGoofyScale();

        if (animator != null)
        {
            animator.SetBool(
                sleepParameter,
                true
            );
        }
    }

    // ============================================================
    // SOMMEIL
    // ============================================================

    private void UpdateSleep()
    {
        /*
         * Si la traque commence pendant le sommeil,
         * SetChaseMode(true) sortira immédiatement de cet état.
         */

        sleepTimer -=
            Time.deltaTime;

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
         * Le garde peut s'être endormi au milieu
         * d'un segment.
         *
         * On le rattache au node le plus proche.
         */

        SC_guard_point nearestNode =
            FindClosestNode(
                transform.position
            );

        if (nearestNode != null)
        {
            currentNode =
                nearestNode;

            transform.position =
                nearestNode.transform.position;
        }

        FinishPath();

        SetFlashlight(true);

        ResetRandomSleepTimer();

        StartPatrol();
    }

    // ============================================================
    // TRAQUE
    // ============================================================

    private void UpdateChase()
    {
        if (SC_player.instance == null)
        {
            return;
        }

        if (isMovingBetweenNodes)
        {
            if (allowMidSegmentTurn &&
                useNearestLadder)
            {
                CheckForNearestLadderDecision();
            }

            FollowCurrentPath();

            return;
        }

        chaseTimer -=
            Time.deltaTime;

        if (chaseTimer <= 0f)
        {
            chaseTimer =
                Mathf.Max(
                    0.01f,
                    chaseRefreshRate
                );

            RecalculateChasePath();
        }

        if (isMovingBetweenNodes)
        {
            FollowCurrentPath();
        }
    }

    // ============================================================
    // DECISION ECHELLE
    // ============================================================

    private void CheckForNearestLadderDecision()
    {
        if (!isChasing)
        {
            return;
        }

        if (!isMovingBetweenNodes)
        {
            return;
        }

        if (currentNode == null ||
            segmentStartNode == null ||
            segmentTargetNode == null)
        {
            return;
        }

        if (SC_player.instance == null)
        {
            return;
        }

        Vector3 playerPosition =
            SC_player.instance.transform.position;

        float verticalDifference =
            Mathf.Abs(
                playerPosition.y -
                transform.position.y
            );

        if (verticalDifference <=
            verticalTolerance)
        {
            return;
        }

        float segmentVerticalDifference =
            Mathf.Abs(
                segmentTargetNode.transform.position.y -
                segmentStartNode.transform.position.y
            );

        if (segmentVerticalDifference > 0.05f)
        {
            return;
        }

        SC_guard_point nearestLadderNode =
            FindNearestUsefulLadderNode(
                playerPosition
            );

        if (nearestLadderNode == null)
        {
            return;
        }

        float distanceToLadder =
            Vector3.Distance(
                transform.position,
                nearestLadderNode.transform.position
            );

        if (distanceToLadder >
            ladderDecisionDistance)
        {
            return;
        }

        float distanceToStart =
            Vector3.Distance(
                transform.position,
                segmentStartNode.transform.position
            );

        float distanceToTarget =
            Vector3.Distance(
                transform.position,
                segmentTargetNode.transform.position
            );

        SC_guard_point bestEntryNode =
            FindBestLadderEntryNode(
                segmentStartNode,
                segmentTargetNode,
                nearestLadderNode
            );

        if (bestEntryNode == null)
        {
            return;
        }

        if (bestEntryNode == currentNode)
        {
            return;
        }

        if (bestEntryNode == segmentStartNode)
        {
            if (distanceToStart <
                distanceToTarget)
            {
                ReverseCurrentSegmentToward(
                    segmentStartNode
                );
            }
        }
    }

    // ============================================================
    // DEMI-TOUR
    // ============================================================

    private void ReverseCurrentSegmentToward(
        SC_guard_point destinationNode)
    {
        if (destinationNode == null)
        {
            return;
        }

        currentPath.Clear();

        pathIndex = 0;

        isMovingBetweenNodes = false;

        segmentStartNode = null;
        segmentTargetNode = null;

        ResetGoofyScale();

        currentPath.Add(
            destinationNode
        );

        pathIndex = 0;

        StartNextNodeMovement();
    }

    // ============================================================
    // RECHERCHE ECHELLE UTILE
    // ============================================================

    private SC_guard_point FindNearestUsefulLadderNode(
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
            {
                continue;
            }

            if (!HasVerticalConnection(node))
            {
                continue;
            }

            float guardDistance =
                Vector3.Distance(
                    transform.position,
                    node.transform.position
                );

            float playerDistance =
                Vector3.Distance(
                    node.transform.position,
                    playerPosition
                );

            float score =
                guardDistance +
                playerDistance * 0.35f;

            if (score < bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        return bestNode;
    }

    // ============================================================
    // DETECTION ECHELLE
    // ============================================================

    private bool HasVerticalConnection(
        SC_guard_point node)
    {
        if (node == null ||
            node.connections == null)
        {
            return false;
        }

        foreach (
            SC_guard_point connection
            in node.connections)
        {
            if (connection == null)
            {
                continue;
            }

            float verticalDifference =
                Mathf.Abs(
                    connection.transform.position.y -
                    node.transform.position.y
                );

            if (verticalDifference > 0.05f)
            {
                return true;
            }
        }

        return false;
    }

    // ============================================================
    // CHOIX ENTREE ECHELLE
    // ============================================================

    private SC_guard_point FindBestLadderEntryNode(
        SC_guard_point start,
        SC_guard_point target,
        SC_guard_point ladderNode)
    {
        if (start == null ||
            target == null ||
            ladderNode == null)
        {
            return null;
        }

        float startDistance =
            EstimatePathDistance(
                start,
                ladderNode
            );

        float targetDistance =
            EstimatePathDistance(
                target,
                ladderNode
            );

        if (startDistance <= targetDistance)
        {
            return start;
        }

        return target;
    }

    // ============================================================
    // ESTIMATION DISTANCE
    // ============================================================

    private float EstimatePathDistance(
        SC_guard_point start,
        SC_guard_point target)
    {
        if (start == null ||
            target == null)
        {
            return Mathf.Infinity;
        }

        if (start == target)
        {
            return 0f;
        }

        List<SC_guard_point> path =
            FindShortestPath(
                start,
                target
            );

        if (path == null)
        {
            return Mathf.Infinity;
        }

        float totalDistance = 0f;

        SC_guard_point previous =
            start;

        foreach (
            SC_guard_point node
            in path)
        {
            if (node == null)
            {
                return Mathf.Infinity;
            }

            totalDistance +=
                Vector3.Distance(
                    previous.transform.position,
                    node.transform.position
                );

            previous = node;
        }

        return totalDistance;
    }

    // ============================================================
    // RECALCUL TRAQUE
    // ============================================================

    private void RecalculateChasePath()
    {
        if (!isChasing)
        {
            return;
        }

        if (SC_player.instance == null)
        {
            return;
        }

        if (currentNode == null)
        {
            currentNode =
                FindClosestNode(
                    transform.position
                );

            if (currentNode == null)
            {
                return;
            }
        }

        SC_guard_point targetNode =
            FindClosestNodeForChase(
                SC_player.instance.transform.position
            );

        if (targetNode == null)
        {
            return;
        }

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

            StartNextNodeMovement();
        }
        else
        {
            FinishPath();
        }
    }

    // ============================================================
    // DEMARRER SEGMENT
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

        if (pathIndex >=
            currentPath.Count)
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

        /*
         * Nouveau segment :
         * on recommence le cycle de bond.
         */

        hopTimer = 0f;

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
    // SUIVI CHEMIN
    // ============================================================

    private void FollowCurrentPath()
    {
        if (currentPath == null ||
            currentPath.Count == 0)
        {
            FinishPath();
            return;
        }

        if (pathIndex >=
            currentPath.Count)
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

        Vector3 targetPosition =
            target.transform.position;

        AnimationMovement movement =
            GetMovementAnimation(
                currentNode,
                target
            );

        /*
         * ========================================================
         * SOL
         * ========================================================
         *
         * PATROUILLE :
         * déplacement NORMAL.
         *
         * TRAQUE :
         * petits bonds goofy.
         */

        if (movement ==
            AnimationMovement.Walk)
        {
            if (isChasing)
            {
                FollowGroundWithGoofyHops(
                    targetPosition
                );
            }
            else
            {
                FollowGroundNormally(
                    targetPosition
                );
            }
        }
        else
        {
            /*
             * ECHELLE :
             * déplacement normal.
             */

            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed *
                    Time.deltaTime
                );

            ResetGoofyScale();
        }

        /*
         * ========================================================
         * ARRIVEE
         * ========================================================
         */

        if (movement ==
            AnimationMovement.Walk)
        {
            Vector3 flatCurrent =
                new Vector3(
                    transform.position.x,
                    0f,
                    transform.position.z
                );

            Vector3 flatTarget =
                new Vector3(
                    targetPosition.x,
                    0f,
                    targetPosition.z
                );

            if (Vector3.Distance(
                    flatCurrent,
                    flatTarget
                ) <= 0.001f)
            {
                transform.position =
                    targetPosition;

                ResetGoofyScale();

                ArriveAtCurrentPathNode();
            }
        }
        else
        {
            if (Vector3.Distance(
                    transform.position,
                    targetPosition
                ) <= 0.001f)
            {
                transform.position =
                    targetPosition;

                ResetGoofyScale();

                ArriveAtCurrentPathNode();
            }
        }
    }

    // ============================================================
    // DEPLACEMENT NORMAL
    // ============================================================

    private void FollowGroundNormally(
        Vector3 targetPosition)
    {
        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed *
                Time.deltaTime
            );

        /*
         * Très important :
         * aucun squash/stretch en patrouille.
         */

        ResetGoofyScale();
    }

    // ============================================================
    // DEPLACEMENT GOOFY DE TRAQUE
    // ============================================================

    private void FollowGroundWithGoofyHops(
        Vector3 targetPosition)
    {
        Vector3 currentPosition = transform.position;

        /*
         * ========================================================
         * ACCELERATION
         * ========================================================
         */

        float currentSpeed = UpdateChaseSpeed(true);

        /*
         * On retire le bond actuel pour calculer correctement
         * le déplacement horizontal.
         */

        float groundY = targetPosition.y;

        Vector3 horizontalCurrent = new Vector3(
            currentPosition.x,
            groundY,
            currentPosition.z
        );

        Vector3 horizontalTarget = new Vector3(
            targetPosition.x,
            groundY,
            targetPosition.z
        );

        Vector3 nextPosition = Vector3.MoveTowards(
            horizontalCurrent,
            horizontalTarget,
            currentSpeed * Time.deltaTime
        );

        /*
         * ========================================================
         * BOND GOOFY
         * ========================================================
         */

        float hopDuration = Mathf.Max(
            0.05f,
            chaseHopDuration
        );

        float hopHeight = chaseHopHeight;

        hopTimer += Time.deltaTime;

        float normalizedTime =
            (hopTimer % hopDuration) / hopDuration;

        float hopOffset =
            Mathf.Sin(
                normalizedTime * Mathf.PI
            ) * hopHeight;

        /*
         * ========================================================
         * PETIT WOBBLE
         * ========================================================
         */

        float wobble =
            Mathf.Sin(
                normalizedTime * Mathf.PI * 2f
            ) * hopWobble;

        Vector3 direction =
            targetPosition -
            segmentStartNode.transform.position;

        direction.y = 0f;

        if (Mathf.Abs(direction.x) > 0.001f)
        {
            float wobbleDirection =
                Mathf.Sign(direction.x);

            nextPosition.x +=
                wobble *
                wobbleDirection;
        }

        /*
         * ========================================================
         * POSITION FINALE
         * ========================================================
         */

        nextPosition.y =
            groundY +
            hopOffset;

        transform.position =
            nextPosition;

        /*
         * ========================================================
         * SQUASH / STRETCH
         * ========================================================
         */

        UpdateHopScale(
            normalizedTime
        );
    }
    // ============================================================
    // SQUASH / STRETCH
    // ============================================================

    private void UpdateHopScale(
        float normalizedTime)
    {
        float scaleY;
        float scaleX;

        if (normalizedTime < 0.5f)
        {
            float t =
                normalizedTime /
                0.5f;

            scaleY =
                Mathf.Lerp(
                    1f,
                    1f + hopStretch,
                    t
                );

            scaleX =
                Mathf.Lerp(
                    1f,
                    1f - hopSquash * 0.5f,
                    t
                );
        }
        else
        {
            float t =
                (normalizedTime - 0.5f) /
                0.5f;

            scaleY =
                Mathf.Lerp(
                    1f + hopStretch,
                    1f - hopSquash,
                    t
                );

            scaleX =
                Mathf.Lerp(
                    1f - hopSquash * 0.5f,
                    1f + hopSquash,
                    t
                );
        }

        Vector3 scale =
            originalScale;

        /*
         * On conserve le signe X correspondant au flip.
         */

        float currentSignX =
            Mathf.Sign(
                transform.localScale.x
            );

        scale.x =
            Mathf.Abs(
                originalScale.x
            ) *
            currentSignX *
            scaleX;

        scale.y =
            originalScale.y *
            scaleY;

        transform.localScale =
            scale;
    }

    // ============================================================
    // ARRIVEE NODE
    // ============================================================

    private void ArriveAtCurrentPathNode()
    {
        currentNode =
            currentPath[pathIndex];

        pathIndex++;

        if (pathIndex >=
            currentPath.Count)
        {
            FinishPath();

            if (isChasing)
            {
                chaseTimer = 0f;
            }

            return;
        }

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

        hopTimer = 0f;

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
    // CHOIX PATROUILLE
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
            currentNode =
                FindClosestNode(
                    transform.position
                );

            if (currentNode == null)
            {
                return;
            }
        }

        List<SC_guard_point> validPoints =
            new List<SC_guard_point>();

        foreach (
            SC_guard_point point
            in patrolPoints)
        {
            if (point == null)
            {
                continue;
            }

            if (point == currentNode &&
                patrolPoints.Count > 1)
            {
                continue;
            }

            validPoints.Add(point);
        }

        if (validPoints.Count == 0)
        {
            return;
        }

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
        {
            return;
        }

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
        else
        {
            FinishPath();
        }
    }

    // ============================================================
    // ATTENTE PATROUILLE
    // ============================================================

    private void BeginPatrolWait()
    {
        FinishPath();

        isWaitingAtPatrolPoint = true;

        patrolWaitTimer =
            patrolWaitTime;
    }

    // ============================================================
    // START PATROUILLE
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

        SetFlashlight(true);

        /*
         * On ne reset pas forcément le timer ici :
         * FinishSleep() le fait déjà.
         *
         * Au lancement du garde, Start() l'a déjà initialisé.
         */

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
        {
            return;
        }

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

                ResetGoofyScale();

                break;

            case AnimationMovement.LadderDown:

                animator.SetBool(
                    ladderDownParameter,
                    true
                );

                SetFlashlight(false);

                ResetGoofyScale();

                break;
        }
    }

    private void StopMovementAnimation()
    {
        if (animator == null)
        {
            return;
        }

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
    // RESET SCALE
    // ============================================================

    private void ResetGoofyScale()
    {
        /*
         * Attention :
         * originalScale contient l'échelle originale,
         * mais on doit conserver le flip horizontal.
         */

        Vector3 scale =
            originalScale;

        if (flipCharacter)
        {
            float currentSign =
                Mathf.Sign(
                    transform.localScale.x
                );

            if (currentSign == 0f)
            {
                currentSign =
                    facingRightByDefault
                        ? 1f
                        : -1f;
            }

            scale.x =
                Mathf.Abs(
                    originalScale.x
                ) *
                currentSign;
        }

        transform.localScale =
            scale;
    }

    // ============================================================
    // LAMPE
    // ============================================================

    private void SetFlashlight(
        bool enabled)
    {
        if (flashlight == null)
        {
            return;
        }

        flashlight.SetActive(
            enabled
        );
    }

    // ============================================================
    // FLIP NODE -> NODE
    // ============================================================

    private void UpdateFlip(
        SC_guard_point from,
        SC_guard_point to)
    {
        if (!flipCharacter)
        {
            return;
        }

        if (from == null ||
            to == null)
        {
            return;
        }

        UpdateFlip(
            from.transform.position,
            to.transform.position
        );
    }

    // ============================================================
    // FLIP POSITION -> POSITION
    // ============================================================

    private void UpdateFlip(
        Vector3 from,
        Vector3 to)
    {
        if (!flipCharacter)
        {
            return;
        }

        Vector3 direction =
            to - from;

        if (Mathf.Abs(
                direction.x
            ) < 0.01f)
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
            transform.localScale;

        scale.x =
            Mathf.Abs(
                originalScale.x
            ) *
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
        {
            return false;
        }

        return from.connections.Contains(
            to
        );
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
                i++)
            {
                SC_guard_point candidate =
                    openSet[i];

                if (!fCost.ContainsKey(
                        candidate))
                {
                    continue;
                }

                if (!fCost.ContainsKey(
                        current
                    ) ||
                    fCost[candidate] <
                    fCost[current])
                {
                    current =
                        candidate;
                }
            }

            if (current == target)
            {
                return ReconstructPath(
                    cameFrom,
                    current
                );
            }

            openSet.Remove(
                current
            );

            closedSet.Add(
                current
            );

            if (current.connections == null)
            {
                continue;
            }

            foreach (
                SC_guard_point neighbour
                in current.connections)
            {
                if (neighbour == null)
                {
                    continue;
                }

                if (closedSet.Contains(
                        neighbour))
                {
                    continue;
                }

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

                if (!gCost.ContainsKey(
                        neighbour))
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

                    if (!openSet.Contains(
                            neighbour))
                    {
                        openSet.Add(
                            neighbour
                        );
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
    // RECONSTRUCTION A*
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

        path.Add(
            current
        );

        while (
            cameFrom.ContainsKey(
                current
            ))
        {
            current =
                cameFrom[current];

            path.Add(
                current
            );
        }

        path.Reverse();

        /*
         * Le premier node est celui où se trouve
         * déjà le garde.
         */

        if (path.Count > 0)
        {
            path.RemoveAt(0);
        }

        return path;
    }

    // ============================================================
    // NODE LE PLUS PROCHE
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
            {
                continue;
            }

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
    // NODE JOUEUR
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
         * PRIORITE :
         *
         * 1. Même niveau
         * 2. Distance horizontale
         * 3. Distance verticale
         */

        foreach (
            SC_guard_point node
            in nodes)
        {
            if (node == null)
            {
                continue;
            }

            Vector3 nodePosition =
                node.transform.position;

            float verticalDifference =
                Mathf.Abs(
                    playerPosition.y -
                    nodePosition.y
                );

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
                verticalDifference *
                verticalWeight;

            if (score <
                bestScore)
            {
                bestScore =
                    score;

                bestNode =
                    node;
            }
        }

        /*
         * Aucun node au même niveau.
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
                {
                    continue;
                }

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

                    bestNode =
                        node;
                }
            }
        }

        return bestNode;
    }

    // ============================================================
    // API : CHASE
    // ============================================================

    public void SetChaseMode(
        bool enabled)
    {
        // ========================================================
        // DEBUT TRAQUE
        // ========================================================

        if (enabled)
        {
            /*
             * ====================================================
             * REVEIL IMMEDIAT
             * ====================================================
             *
             * Même si le garde dormait au moment où le joueur
             * est détecté, il se réveille instantanément.
             */

            if (isSleeping)
            {
                isSleeping = false;

                sleepTimer = 0f;

                if (animator != null)
                {
                    animator.SetBool(
                        sleepParameter,
                        false
                    );
                }

                /*
                 * On conserve sa position actuelle.
                 * Il ne faut surtout pas le téléporter ici.
                 */

                ResetGoofyScale();
            }

            isChasing = true;

            isWaitingAtPatrolPoint =
                false;

            movementMode =
                MovementMode.Chase;

            SetFlashlight(true);

            chaseTimer = 0f;
            currentChaseSpeed = 0f;
            /*
             * On force un nouveau bond dès le début
             * de la traque.
             */

            hopTimer = 0f;

            /*
             * Le timer de sommeil est suspendu pendant
             * toute la traque.
             */

            if (!isMovingBetweenNodes)
            {
                RecalculateChasePath();
            }

            return;
        }

        // ========================================================
        // FIN TRAQUE
        // ========================================================

        isChasing = false;

        movementMode =
            MovementMode.Patrol;

        /*
         * Le garde s'arrête immédiatement et dort.
         *
         * Ce sommeil n'est PAS le sommeil aléatoire :
         * c'est le sommeil déclenché par la fin de la traque.
         */

        StartSleep();
    }
    private void StartSleep()
    {
        isSleeping = true;
        sleepTimer = sleepDuration;

        FinishPath();

        animator.SetBool("Sleep", true);
        animator.SetBool("Walk", false);
        animator.SetBool("LadderUp", false);
        animator.SetBool("LadderDown", false);

        SetFlashlight(false);
    }
    // ============================================================
    // CHANGER NODE
    // ============================================================

    public void SetCurrentNode(
        SC_guard_point node)
    {
        currentNode =
            node;

        if (node != null)
        {
            transform.position =
                node.transform.position;
        }

        FinishPath();
    }

    // ============================================================
    // DEFINIR BASE
    // ============================================================

    public void SetBaseNode(
        SC_guard_point node)
    {
        baseNode =
            node;
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