using System;
using UnityEngine;

public class SC_arrow_fish : MonoBehaviour
{
    [Header("Points")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Joueur")]
    public bool aimBeforeJump = true;

    [Header("Apparition / Sortie")]
    public float distanceFromPoint = 3f;

    [Header("Déplacement vers le point")]
    public float moveSpeed = 5f;

    [Header("Attente avant le saut")]
    public float waitTime = 0.5f;

    [Header("Saut")]
    public float jumpDuration = 1.5f;

    [Tooltip("Hauteur maximale de l'arc au-dessus de la ligne entre les deux points.")]
    public float arcHeight = 3f;

    [Header("Délai entre les cycles")]
    public float cycleDelay = 2f;

    [Header("Orientation")]
    public bool rotateTowardsDirection = true;

    public float rotationSpeed = 12f;

    [Tooltip(
        "Si le sprite regarde naturellement vers la droite : 0. " +
        "S'il regarde naturellement vers la gauche : 180."
    )]
    public float rotationOffset = 0f;

    [Header("Trail")]
    public GameObject trail;

    public TrailRenderer trail_;


    private enum State
    {
        CycleDelay,
        MovingToPoint,
        Waiting,
        Jumping,
        Leaving
    }

    private State state;


    private float waitTimer;
    private float jumpTimer;
    private float cycleTimer;


    private Vector3 jumpStart;
    private Vector3 jumpTarget;

    private Vector3 leaveTarget;


    // Contrôle de la Bezier
    private Vector3 arcControl;


    // true  = arrive au START puis saute vers END
    // false = arrive au END puis saute vers START
    private bool startedFromStart;


    private void Start()
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogError(
                "SC_arrow_fish : startPoint ou endPoint n'est pas assigné."
            );

            enabled = false;
            return;
        }

        RestartPattern();
    }


    private void Update()
    {
        arcHeight = (SC_player.instance.transform.position.y+5)*2.5f;

        switch (state)
        {
            case State.CycleDelay:
                CycleDelay();
                break;

            case State.MovingToPoint:
                MoveToStartingPoint();
                break;

            case State.Waiting:
                Wait();
                break;

            case State.Jumping:
                Jump();
                break;

            case State.Leaving:
                Leave();
                break;
        }
    }


    // =========================================================
    // NOUVEAU CYCLE
    // =========================================================

    private void RestartPattern()
    {
        startedFromStart =
            UnityEngine.Random.value > 0.5f;


        if (startedFromStart)
        {
            // -----------------------------------------------
            // Arrive depuis la GAUCHE
            // -----------------------------------------------

            transform.position =
                startPoint.position +
                Vector3.left * distanceFromPoint;

            SetLookDirection(Vector3.right);
        }
        else
        {
            // -----------------------------------------------
            // Arrive depuis la DROITE
            // -----------------------------------------------

            transform.position =
                endPoint.position +
                Vector3.right * distanceFromPoint;

            SetLookDirection(Vector3.left);
        }


        waitTimer = 0f;
        jumpTimer = 0f;
        cycleTimer = 0f;


        state = State.MovingToPoint;
    }


    // =========================================================
    // DELAI
    // =========================================================

    private void CycleDelay()
    {
        cycleTimer -= Time.deltaTime;

        if (cycleTimer <= 0f)
        {
            RestartPattern();
        }
    }


    // =========================================================
    // ARRIVEE AU POINT
    // =========================================================

    private void MoveToStartingPoint()
    {
        DisableTrail();


        Vector3 target;

        if (startedFromStart)
        {
            target = startPoint.position;
        }
        else
        {
            target = endPoint.position;
        }


        Vector3 direction =
            target - transform.position;


        SetLookDirection(direction);


        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );


        if (Vector3.Distance(
                transform.position,
                target) <= 0.01f)
        {
            transform.position = target;

            waitTimer = waitTime;

            state = State.Waiting;
        }
    }


    // =========================================================
    // ATTENTE
    // =========================================================

    private void Wait()
    {
        // -----------------------------------------------------
        // REGARDE LE JOUEUR
        // -----------------------------------------------------

        if (aimBeforeJump)
        {
            LookAtPlayer();
        }


        waitTimer -= Time.deltaTime;


        if (waitTimer <= 0f)
        {
            StartJump();
        }
    }


    // =========================================================
    // REGARDER LE JOUEUR
    // =========================================================

    private void LookAtPlayer()
    {
        if (SC_player.instance == null)
            return;


        Transform player =
            SC_player.instance.transform;


        if (player == null)
            return;


        Vector3 direction =
            player.position -
            transform.position;


        SetLookDirection(direction);
    }


    // =========================================================
    // DEBUT DU SAUT
    // =========================================================

    private void StartJump()
    {
        EnableTrail();


        jumpStart =
            transform.position;


        if (startedFromStart)
        {
            jumpTarget =
                endPoint.position;
        }
        else
        {
            jumpTarget =
                startPoint.position;
        }


        jumpTimer = 0f;


        // Création de l'arc
        CreateArc();


        // Orientation au début du saut
        Vector3 tangent =
            GetArcTangent(0f);


        SetLookDirection(tangent);


        state = State.Jumping;
    }


    // =========================================================
    // CREATION DE L'ARC
    // =========================================================

    private void CreateArc()
    {
        /*
         * On veut un arc qui monte TOUJOURS vers le haut.
         *
         * Exemple :
         *
         *          X
         *       .     .
         *     .         .
         * START-----------END
         *
         * Peu importe si le poisson part :
         *
         * START -> END
         *
         * ou :
         *
         * END -> START
         *
         * l'arc monte toujours.
         */


        Vector3 middle =
            (jumpStart + jumpTarget) * 0.5f;


        // On force la hauteur vers le HAUT.
        Vector3 upward =
            Vector3.up * arcHeight;


        arcControl =
            middle + upward;
    }


    // =========================================================
    // SAUT
    // =========================================================

    private void Jump()
    {
        jumpTimer += Time.deltaTime;


        float duration =
            Mathf.Max(jumpDuration, 0.01f);


        float t =
            Mathf.Clamp01(
                jumpTimer / duration
            );


        // -----------------------------------------------------
        // POSITION
        // -----------------------------------------------------

        Vector3 position =
            CalculateArcPosition(t);


        transform.position =
            position;


        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

        if (rotateTowardsDirection)
        {
            Vector3 tangent =
                GetArcTangent(t);


            SetLookDirection(tangent);
        }


        // -----------------------------------------------------
        // FIN
        // -----------------------------------------------------

        if (t >= 1f)
        {
            transform.position =
                jumpTarget;


            StartLeaving();
        }
    }


    // =========================================================
    // POSITION BEZIER
    // =========================================================

    private Vector3 CalculateArcPosition(float t)
    {
        float inverse =
            1f - t;


        return
            inverse * inverse * jumpStart
            +
            2f * inverse * t * arcControl
            +
            t * t * jumpTarget;
    }


    // =========================================================
    // TANGENTE
    // =========================================================

    private Vector3 GetArcTangent(float t)
    {
        Vector3 tangent =
            2f * (1f - t) *
            (arcControl - jumpStart)
            +
            2f * t *
            (jumpTarget - arcControl);


        return tangent;
    }


    // =========================================================
    // SORTIE
    // =========================================================

    private void StartLeaving()
    {
        if (startedFromStart)
        {
            // Le poisson vient d'arriver au END.
            // Il continue vers la droite.

            leaveTarget =
                endPoint.position +
                Vector3.right * distanceFromPoint;
        }
        else
        {
            // Le poisson vient d'arriver au START.
            // Il continue vers la gauche.

            leaveTarget =
                startPoint.position +
                Vector3.left * distanceFromPoint;
        }


        SetLookDirection(
            leaveTarget - transform.position
        );


        state = State.Leaving;
    }


    // =========================================================
    // SORTIE
    // =========================================================

    private void Leave()
    {
        Vector3 direction =
            leaveTarget -
            transform.position;


        SetLookDirection(direction);


        transform.position =
            Vector3.MoveTowards(
                transform.position,
                leaveTarget,
                moveSpeed * Time.deltaTime
            );


        if (Vector3.Distance(
                transform.position,
                leaveTarget) <= 0.01f)
        {
            transform.position =
                leaveTarget;


            StartCycleDelay();
        }
    }


    // =========================================================
    // ORIENTATION
    // =========================================================

    private void SetLookDirection(Vector3 direction)
    {
        if (!rotateTowardsDirection)
            return;


        direction.z = 0f;


        if (direction.sqrMagnitude < 0.0001f)
            return;


        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;


        angle += rotationOffset;


        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );


        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }


    // =========================================================
    // TRAIL
    // =========================================================

    private void EnableTrail()
    {
        if (trail != null)
        {
            trail.SetActive(true);
        }
    }


    private void DisableTrail()
    {
        if (trail_ != null)
        {
            trail_.Clear();
        }


        if (trail != null)
        {
            trail.SetActive(false);
        }
    }


    // =========================================================
    // FIN DU CYCLE
    // =========================================================

    private void StartCycleDelay()
    {
        cycleTimer =
            cycleDelay;


        state =
            State.CycleDelay;
    }
}
