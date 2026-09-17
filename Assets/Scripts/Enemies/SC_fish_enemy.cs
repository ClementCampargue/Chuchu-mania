using System.Collections.Generic;
using UnityEngine;

public class SC_fish_enemy : MonoBehaviour
{
    private enum State
    {
        Swimming,

        WaitingBeforeJump,
        JumpingUp,
        WaitingAtTop,

        Falling,
        WaitingAfterJump,

        ChasingChuchu,
        WaitingBeforeChuchuJump,
        JumpingAtChuchu,
        WaitingAtChuchuTop
    }

    public List<Animator> anims;

    public SC_water_physic water;
    public bool big;
    private bool  caughtStarbit;

    [Header("Comportement")]
    [SerializeField] private bool once = false;
    [SerializeField] private int starbits_before_leave = 1;
     private int currentstarbit =0;

    [Header("Patrouille")]
    [SerializeField] private Transform leftPoint;

    [SerializeField] private Transform rightPoint;

    [SerializeField] private float swimSpeed = 2f;

    [SerializeField] private float horizontalTolerance = 0.05f;

    [Header("Recherche Starbit")]
    [SerializeField] private float searchInterval = 0.25f;

    [SerializeField] private float maxStarbitDistance = 15f;

    [Header("Avant le saut Starbit")]
    [SerializeField] private float preJumpDelay = 0.5f;

    [Header("Saut Starbit")]
    [SerializeField] private float jumpDuration = 1.2f;

    [SerializeField] private float apexDelay = 0.15f;

    [Header("Retour")]
    [SerializeField] private float fallDuration = 0.5f;

    [SerializeField] private float postJumpDelay = 0.5f;

    [Header("Starbit")]
    [SerializeField] private float eatDistance = 0.35f;

    [Header("Traque du joueur")]
    [SerializeField] private float chaseDuration = 5f;

    [SerializeField] private float chaseSpeed = 2f;

    [SerializeField] private float chuchuCatchDistance = 0.5f;

    [Header("Préparation du saut sur le joueur")]
    [SerializeField] private float chuchuPreJumpDelay = 0.5f;

    [Header("Saut sur le joueur")]
    [SerializeField] private float chuchuJumpDuration = 1.2f;

    [SerializeField] private float chuchuJumpHeight = 3f;

    [SerializeField] private float chuchuApexDelay = 0.15f;

    [Header("Sortie de l'écran")]
    [SerializeField] private float leaveScreenDistance = 30f;

    private State state = State.Swimming;

    private SC_starbit targetStarbit;

    private float searchTimer;

    private float preJumpTimer;

    private float jumpTimer;
    private float apexTimer;

    private float fallTimer;

    private float postJumpTimer;

    private float chaseTimer;

    private float chuchuPreJumpTimer;
    private float chuchuJumpTimer;
    private float chuchuApexTimer;

    private float originalSwimHeight;

    private float jumpX;
    private float jumpTargetHeight;


    private float chuchuJumpX;
    private float chuchuJumpTargetHeight;

    private int swimDirection = 1;

    private bool isFacingRight = true;
    private bool isChasingChuchu = false;

    private bool shouldChaseChuchuAfterJump = false;

    private bool hasChosenStarbit = false;

    private bool sequenceFinished = false;
    private bool leavingScreen = false;
    private Vector3 leaveStartPosition;


    private void Start()
    {
        water = SC_water_physic.instance;
        originalSwimHeight = transform.position.y;

        searchTimer = 0f;
        leftPoint = GameObject.Find("Fish_L").transform;
        rightPoint = GameObject.Find("Fish_R").transform;
        InitializeSwimDirection();
    }


    private void Update()
    {
        if (leavingScreen)
        {
            UpdateLeavingScreen();
            return;
        }

        switch (state)
        {
            case State.Swimming:
                UpdateSwimming();
                break;

            case State.WaitingBeforeJump:
                UpdateWaitingBeforeJump();
                break;

            case State.JumpingUp:
                UpdateJumpingUp();
                break;

            case State.WaitingAtTop:
                UpdateWaitingAtTop();
                break;

            case State.Falling:
                UpdateFalling();
                break;

            case State.WaitingAfterJump:
                UpdateWaitingAfterJump();
                break;

            case State.ChasingChuchu:
                UpdateChasingChuchu();
                break;

            case State.WaitingBeforeChuchuJump:
                UpdateWaitingBeforeChuchuJump();
                break;

            case State.JumpingAtChuchu:
                UpdateJumpingAtChuchu();
                break;

            case State.WaitingAtChuchuTop:
                UpdateWaitingAtChuchuTop();
                break;
        }
    }


    private void TriggerAnimation(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
            return;

        if (anims == null)
            return;

        foreach (Animator animator in anims)
        {
            if (animator == null)
                continue;

            animator.SetTrigger(triggerName);
        }
    }

    private void BoolAnimation(string triggerName, bool st)
    {
        if (string.IsNullOrEmpty(triggerName))
            return;

        if (anims == null)
            return;

        foreach (Animator animator in anims)
        {
            if (animator == null)
                continue;

            animator.SetBool(triggerName, st);
        }
    }

    private void SetNormalMode()
    {
        TriggerAnimation("change_state");
        BoolAnimation("angry", false);
    }

    private void SetAngryMode()
    {
        TriggerAnimation("change_state");
        BoolAnimation("angry", true);
    }

    private void UpdateSwimming()
    {
        SwimBetweenPoints();

        if(targetStarbit == null)
        {
            hasChosenStarbit = false;
        }
        if (once && sequenceFinished)
            return;

        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            searchTimer = searchInterval;
            if (targetStarbit == null && !hasChosenStarbit)
            {
                SC_starbit starbit = FindStarbit();

                if (starbit != null)
                {
                    targetStarbit = starbit;

                    hasChosenStarbit = true;
                }
            }
        }

        if (targetStarbit != null)
        {
            if (!IsStarbitValid(targetStarbit))
            {
                targetStarbit = null;

                StartChasingChuchu();
                return;
            }

            CheckIfUnderTarget();
        }
    }

    private void SwimBetweenPoints()
    {
        if (leftPoint == null || rightPoint == null)
            return;

        float leftX = leftPoint.position.x;
        float rightX = rightPoint.position.x;

        if (leftX > rightX)
        {
            float temp = leftX;
            leftX = rightX;
            rightX = temp;
        }

        float currentX = transform.position.x;

        if (swimDirection > 0 && currentX >= rightX)
        {
            transform.position = new Vector2(
                rightX,
                originalSwimHeight
            );

            swimDirection = -1;
        }
        else if (swimDirection < 0 && currentX <= leftX)
        {
            transform.position = new Vector2(
                leftX,
                originalSwimHeight
            );

            swimDirection = 1;
        }

        float movement =
            swimDirection *
            swimSpeed *
            Time.deltaTime;

        transform.position += Vector3.right * movement;

        currentX = transform.position.x;

        if (swimDirection > 0 && currentX > rightX)
        {
            transform.position = new Vector2(
                rightX,
                originalSwimHeight
            );

            swimDirection = -1;
        }
        else if (swimDirection < 0 && currentX < leftX)
        {
            transform.position = new Vector2(
                leftX,
                originalSwimHeight
            );

            swimDirection = 1;
        }

        UpdateFacingDirection();
    }


    private SC_starbit FindStarbit()
    {
        SC_starbit[] starbits =
            FindObjectsByType<SC_starbit>(
                FindObjectsSortMode.None
            );

        SC_starbit closest = null;

        float closestDistance =
            maxStarbitDistance;

        foreach (SC_starbit starbit in starbits)
        {
            if (!IsStarbitValid(starbit))
                continue;

            float distance =
                Vector2.Distance(
                    transform.position,
                    starbit.transform.position
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = starbit;
            }
        }

        return closest;
    }

    private bool IsStarbitValid(SC_starbit starbit)
    {
        if (starbit == null)
            return false;

        if (!starbit.gameObject.activeInHierarchy)
            return false;

        return true;
    }

    private void CheckIfUnderTarget()
    {
        if (!IsStarbitValid(targetStarbit))
        {
            targetStarbit = null;

            StartChasingChuchu();
            return;
        }

        float targetX =
            targetStarbit.transform.position.x;

        float currentX =
            transform.position.x;

        float distanceX =
            targetX - currentX;

        if (Mathf.Abs(distanceX) <= horizontalTolerance)
        {
            jumpX = currentX;

            transform.position = new Vector2(
                jumpX,
                originalSwimHeight
            );

            StartWaitingBeforeJump();
        }
    }

    private void StartWaitingBeforeJump()
    {
        preJumpTimer = preJumpDelay;

        TriggerAnimation("wait_jump");

        state = State.WaitingBeforeJump;
    }

    private void UpdateWaitingBeforeJump()
    {
        if (!IsStarbitValid(targetStarbit))
        {
            targetStarbit = null;

            StartChasingChuchu();
            return;
        }

        jumpX =
            targetStarbit.transform.position.x;

        transform.position = new Vector2(
            jumpX,
            originalSwimHeight
        );

        preJumpTimer -= Time.deltaTime;

        if (preJumpTimer <= 0f)
        {
            StartJump();
        }
    }

    private void StartJump()
    {
        if (!IsStarbitValid(targetStarbit))
        {
            targetStarbit = null;

            StartChasingChuchu();
            return;
        }

        TriggerAnimation("jump");

        jumpX =
            targetStarbit.transform.position.x;

        jumpTargetHeight =
            targetStarbit.transform.position.y;

        jumpTimer = 0f;

        transform.position = new Vector2(
            jumpX,
            originalSwimHeight
        );

        isChasingChuchu = false;
        shouldChaseChuchuAfterJump = false;

        state = State.JumpingUp;
    }


    private void UpdateJumpingUp()
    {
        if (!IsStarbitValid(targetStarbit))
        {
            shouldChaseChuchuAfterJump = true;
        }

        jumpTimer += Time.deltaTime;

        float t =
            jumpTimer / jumpDuration;

        if (t >= 1f)
        {
            transform.position = new Vector2(
                jumpX,
                jumpTargetHeight
            );

            apexTimer = apexDelay;

            state = State.WaitingAtTop;

            return;
        }

        t = Mathf.Clamp01(t);

        float movement =
            Mathf.Sin(
                t * Mathf.PI * 0.5f
            );

        float currentY =
            Mathf.Lerp(
                originalSwimHeight,
                jumpTargetHeight,
                movement
            );

        transform.position = new Vector2(
            jumpX,
            currentY
        );
    }

    private void UpdateWaitingAtTop()
    {
        if (!IsStarbitValid(targetStarbit))
        {
            shouldChaseChuchuAfterJump = true;
        }

        transform.position = new Vector2(
            jumpX,
            jumpTargetHeight
        );

        apexTimer -= Time.deltaTime;

        if (apexTimer <= 0f)
        {
            if (shouldChaseChuchuAfterJump)
            {
                targetStarbit = null;

                StartFalling();

                return;
            }

            EatStarbit();
        }
    }

    private void EatStarbit()
    {
         caughtStarbit = false;

        if (IsStarbitValid(targetStarbit))
        {
            float distance =
                Vector2.Distance(
                    transform.position,
                    targetStarbit.transform.position
                );

            if (distance <= eatDistance)
            {
                targetStarbit.Destroy_();

                caughtStarbit = true;
            }
        }

        targetStarbit = null;

        isChasingChuchu = false;
        shouldChaseChuchuAfterJump = false;

        if (!caughtStarbit)
        {
            StartChasingChuchu();
            return;
        }
        hasChosenStarbit = false;

        StartFalling();
    }

    private void StartChasingChuchu()
    {
        targetStarbit = null;

        isChasingChuchu = true;

        chaseTimer = chaseDuration;

        shouldChaseChuchuAfterJump = false;

        transform.position = new Vector2(
            transform.position.x,
            originalSwimHeight
        );

        SetAngryMode();

        state = State.ChasingChuchu;
    }


    private void UpdateChasingChuchu()
    {
        if (SC_player.instance == null)
        {
            StopChasingChuchu();
            return;
        }

        chaseTimer -= Time.deltaTime;

        if (chaseTimer <= 0f)
        {
            StopChasingChuchu();
            return;
        }

        Transform chuchu =
            SC_player.instance.transform;

        float distanceX =
            chuchu.position.x -
            transform.position.x;

        if (Mathf.Abs(distanceX) <= horizontalTolerance)
        {
            StartChuchuPreJump();
            return;
        }

        float direction =
            Mathf.Sign(distanceX);

        transform.position +=
            Vector3.right *
            direction *
            chaseSpeed *
            Time.deltaTime;

        transform.position = new Vector2(
            transform.position.x,
            originalSwimHeight
        );

        UpdateFacingDirection(
            direction > 0f
        );
    }

    private void StartChuchuPreJump()
    {
        TriggerAnimation("wait_jump");

        chuchuPreJumpTimer =
            chuchuPreJumpDelay;

        transform.position = new Vector2(
            transform.position.x,
            originalSwimHeight
        );

        state = State.WaitingBeforeChuchuJump;
    }
    private void UpdateWaitingBeforeChuchuJump()
    {

        transform.position = new Vector2(
            transform.position.x,
            originalSwimHeight
        );

        chuchuPreJumpTimer -= Time.deltaTime;

        if (chuchuPreJumpTimer <= 0f)
        {
            StartChuchuJump();
        }
    }

    private void StartChuchuJump()
    {
        TriggerAnimation("jump");

        if (SC_player.instance == null)
        {
            StopChasingChuchu();
            return;
        }
        chuchuJumpX =
            transform.position.x;

        chuchuJumpTargetHeight =
            chuchuJumpHeight;


        transform.position = new Vector2(
            chuchuJumpX,
            originalSwimHeight
        );

        chuchuJumpTimer = 0f;

        isChasingChuchu = true;

        state = State.JumpingAtChuchu;
    }

    private void UpdateJumpingAtChuchu()
    {

        chuchuJumpTimer += Time.deltaTime;

        float t =
            chuchuJumpTimer /
            chuchuJumpDuration;

        if (t >= 1f)
        {
            transform.position = new Vector2(
                chuchuJumpX,
                chuchuJumpTargetHeight
            );

            chuchuApexTimer =
                chuchuApexDelay;

            state = State.WaitingAtChuchuTop;

            return;
        }

        t = Mathf.Clamp01(t);

        float movement =
            Mathf.Sin(
                t * Mathf.PI * 0.5f
            );

        float currentY =
            Mathf.Lerp(
                originalSwimHeight,
                chuchuJumpTargetHeight,
                movement
            );

        transform.position = new Vector2(
            chuchuJumpX,
            currentY
        );
    }

    private void UpdateWaitingAtChuchuTop()
    {
        transform.position = new Vector2(
            chuchuJumpX,
            chuchuJumpTargetHeight
        );

        chuchuApexTimer -= Time.deltaTime;

        if (chuchuApexTimer <= 0f)
        {
            TryCatchChuchu();
        }
    }
    private void TryCatchChuchu()
    {
        if (SC_player.instance == null)
        {
            StartFallingFromChuchu();
            return;
        }

        float distance =
            Vector2.Distance(
                transform.position,
                SC_player.instance.transform.position
            );

        if (distance <= chuchuCatchDistance)
        {
            CatchChuchu();
            return;
        }

        StartFallingFromChuchu();
    }

    private void CatchChuchu()
    {
        Debug.Log("Le poisson a attrapé Chuchu !");

        StopChasingChuchu();
    }
    private void StartFallingFromChuchu()
    {
        TriggerAnimation("fall");

        fallTimer = 0f;

        jumpX =
            chuchuJumpX;

        jumpTargetHeight =
            chuchuJumpTargetHeight;

        isChasingChuchu = true;

        state = State.Falling;
    }

    private void StartFalling()
    {
        fallTimer = 0f;

        TriggerAnimation("fall");

        state = State.Falling;
    }
    private void UpdateFalling()
    {
        fallTimer += Time.deltaTime;

        float t =
            fallTimer /
            fallDuration;

        if (t >= 1f)
        {
            transform.position = new Vector2(
                jumpX,
                originalSwimHeight
            );

            if (isChasingChuchu)
            {
                if (big && water != null)
                {
                    water.BigSplashAt(transform.position);
                }

                TriggerAnimation("swim");

                if (SC_player.instance == null)
                {
                    StopChasingChuchu();
                    return;
                }

                if (chaseTimer <= 0f)
                {
                    StopChasingChuchu();
                    return;
                }

                state = State.ChasingChuchu;

                return;
            }

            if (shouldChaseChuchuAfterJump)
            {
                shouldChaseChuchuAfterJump = false;

                StartChasingChuchu();

                return;
            }

            StartWaitingAfterJump();

            return;
        }

        t = Mathf.Clamp01(t);

        float currentY =
            Mathf.Lerp(
                jumpTargetHeight,
                originalSwimHeight,
                t
            );

        transform.position = new Vector2(
            jumpX,
            currentY
        );
    }

    private void StartWaitingAfterJump()
    {
        if (big && water != null)
        {
            water.BigSplashAt(transform.position);
        }

        postJumpTimer =
            postJumpDelay;
        if(currentstarbit== starbits_before_leave)
        {
            if (once && caughtStarbit)
            {
                sequenceFinished = true;
                LeaveScreen();

                return;
            }
        }
        currentstarbit++;

        TriggerAnimation("swim");

        state = State.WaitingAfterJump;
    }

    private void UpdateWaitingAfterJump()
    {
        transform.position = new Vector2(
            jumpX,
            originalSwimHeight
        );

        postJumpTimer -= Time.deltaTime;

        if (postJumpTimer <= 0f)
        {
            targetStarbit = null;

            if (currentstarbit == starbits_before_leave)
            {
                if (once)
                {
                    sequenceFinished = true;

                    LeaveScreen();

                    return;
                }
            }
            currentstarbit++;

            hasChosenStarbit = false;

            searchTimer = 0f;

            state = State.Swimming;
        }

        TriggerAnimation("swim");
    }

    private void StopChasingChuchu()
    {
        isChasingChuchu = false;

        chaseTimer = 0f;

        chuchuPreJumpTimer = 0f;
        chuchuJumpTimer = 0f;
        chuchuApexTimer = 0f;

        transform.position = new Vector2(
            transform.position.x,
            originalSwimHeight
        );

        SetNormalMode();
        if (currentstarbit == starbits_before_leave)
        {
            if (once)
            {
                sequenceFinished = true;

                LeaveScreen();

                return;
            }
        }
        currentstarbit++;

        hasChosenStarbit = false;

        searchTimer = 0f;

        state = State.Swimming;
    }

    private void LeaveScreen()
    {
        if (leavingScreen)
            return;

        leavingScreen = true;

        targetStarbit = null;

        isChasingChuchu = false;
        shouldChaseChuchuAfterJump = false;

        chaseTimer = 0f;

        SetNormalMode();

        TriggerAnimation("swim");

        leaveStartPosition = transform.position;
    }

    private void UpdateLeavingScreen()
    {
        transform.position +=
            Vector3.right *
            swimDirection *
            swimSpeed *
            Time.deltaTime;

        UpdateFacingDirection();

        float distanceTravelled =
            Mathf.Abs(
                transform.position.x -
                leaveStartPosition.x
            );

        if (distanceTravelled >= leaveScreenDistance)
        {
            gameObject.SetActive(false);
        }
    }

    private void InitializeSwimDirection()
    {
        if (leftPoint == null || rightPoint == null)
        {
            swimDirection = 1;
            return;
        }

        float middleX =
            (leftPoint.position.x +
             rightPoint.position.x) *
            0.5f;

        if (transform.position.x < middleX)
            swimDirection = 1;
        else
            swimDirection = -1;

        UpdateFacingDirection();
    }
    private void UpdateFacingDirection()
    {
        bool shouldFaceRight =
            swimDirection > 0;

        UpdateFacingDirection(
            shouldFaceRight
        );
    }

    private void UpdateFacingDirection(
        bool shouldFaceRight
    )
    {
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;

            Vector3 scale =
                transform.localScale;

            scale.x =
                Mathf.Abs(scale.x) *
                (isFacingRight ? 1f : -1f);

            transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            maxStarbitDistance
        );
        if (targetStarbit != null)
        {
            Gizmos.DrawLine(
                transform.position,
                targetStarbit.transform.position
            );
        }

        if (SC_player.instance != null &&
            isChasingChuchu)
        {
            Gizmos.DrawLine(
                transform.position,
                SC_player.instance.transform.position
            );

            Gizmos.DrawWireSphere(
                SC_player.instance.transform.position,
                chuchuCatchDistance
            );
        }

        if (leftPoint != null &&
            rightPoint != null)
        {
            Vector3 left =
                new Vector3(
                    leftPoint.position.x,
                    transform.position.y,
                    transform.position.z
                );

            Vector3 right =
                new Vector3(
                    rightPoint.position.x,
                    transform.position.y,
                    transform.position.z
                );

            Gizmos.DrawLine(
                left,
                right
            );

            Gizmos.DrawWireSphere(
                left,
                0.12f
            );

            Gizmos.DrawWireSphere(
                right,
                0.12f
            );
        }
    }
}
