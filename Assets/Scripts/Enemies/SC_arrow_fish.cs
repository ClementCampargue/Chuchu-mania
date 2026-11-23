
using System.Collections.Generic;
using UnityEngine;

public class SC_arrow_fish : MonoBehaviour
{
    [Header("Points")]
    public Transform startPoint;
    public Transform endPoint;

    public EdgeCollider2D edgeCollider;

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
    public float arcHeight = 3f;

    [Header("Délai entre les cycles")]
    public float cycleDelay = 2f;

    [Header("Orientation")]
    public bool rotateTowardsDirection = true;
    public float rotationSpeed = 12f;
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
    private Vector3 arcControl;

    private bool startedFromStart;

    // Buffer pour récupérer les positions réelles du TrailRenderer.
    private readonly List<Vector2> colliderPoints = new List<Vector2>();

    private void Start()
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogError("SC_arrow_fish : startPoint ou endPoint non assigné.");
            enabled = false;
            return;
        }

        if (edgeCollider != null)
            edgeCollider.enabled = false;

        RestartPattern();
    }

    private void Update()
    {
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
    // COLLIDER : SUIVRE UNIQUEMENT LE TRAIL REEL
    // =========================================================

    private void LateUpdate()
    {
        if (state != State.Jumping)
        {
            DisableCollider();
            return;
        }

        UpdateTrailCollider();
    }

    private void UpdateTrailCollider()
    {
        if (edgeCollider == null || trail_ == null)
            return;

        int count = trail_.positionCount;

        // Pas assez de points pour créer une collision.
        if (count < 2)
        {
            edgeCollider.enabled = false;
            return;
        }

        Vector3[] positions = new Vector3[count];

        int received = trail_.GetPositions(positions);

        if (received < 2)
        {
            edgeCollider.enabled = false;
            return;
        }

        colliderPoints.Clear();

        // Conversion des positions du Trail en coordonnées locales
        // du EdgeCollider2D.
        for (int i = 0; i < received; i++)
        {
            Vector3 localPosition =
                edgeCollider.transform.InverseTransformPoint(positions[i]);

            colliderPoints.Add(
                new Vector2(localPosition.x, localPosition.y)
            );
        }

        // Le collider épouse uniquement le trail déjà dessiné.
        edgeCollider.SetPoints(colliderPoints);

        edgeCollider.enabled = true;
    }

    private void DisableCollider()
    {
        if (edgeCollider == null)
            return;

        edgeCollider.enabled = false;

        colliderPoints.Clear();
        edgeCollider.SetPoints(colliderPoints);
    }

    // =========================================================
    // NOUVEAU CYCLE
    // =========================================================

    private void RestartPattern()
    {

        startedFromStart = Random.value > 0.5f;

        if (startedFromStart)
        {
            transform.position =
                startPoint.position +
                Vector3.left * distanceFromPoint;

            SetLookDirection(Vector3.right);
        }
        else
        {
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
            RestartPattern();
    }

    // =========================================================
    // ARRIVEE AU POINT
    // =========================================================

    private void MoveToStartingPoint()
    {

        Vector3 target = startedFromStart
            ? startPoint.position
            : endPoint.position;

        Vector3 direction = target - transform.position;

        SetLookDirection(direction);

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target) <= 0.01f)
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

        if (aimBeforeJump)
            LookAtPlayer();

        waitTimer -= Time.deltaTime;

        if (waitTimer <= 0f)
            StartJump();
    }

    // =========================================================
    // REGARDER LE JOUEUR
    // =========================================================

    private void LookAtPlayer()
    {
        if (SC_player.instance == null)
            return;

        Vector3 direction =
            SC_player.instance.transform.position - transform.position;

        SetLookDirection(direction);
    }

    // =========================================================
    // DEBUT DU SAUT
    // =========================================================

    private void StartJump()
    {
        // Sécurité : aucun ancien collider ne doit subsister.

        jumpStart = transform.position;

        jumpTarget = startedFromStart
            ? endPoint.position
            : startPoint.position;

        jumpTimer = 0f;

        CreateArc();

        // Activation du Trail après son nettoyage.
        EnableTrail();

        Vector3 tangent = GetArcTangent(0f);

        SetLookDirection(tangent);

        state = State.Jumping;
    }

    // =========================================================
    // CREATION DE L'ARC
    // =========================================================

    private void CreateArc()
    {
        // La hauteur est calculée UNE SEULE FOIS par saut.
        if (SC_player.instance != null)
        {
            arcHeight =
                (SC_player.instance.transform.position.y + 5f) * 2.5f;
        }

        Vector3 middle = (jumpStart + jumpTarget) * 0.5f;

        arcControl = middle + Vector3.up * arcHeight;
    }

    // =========================================================
    // SAUT
    // =========================================================

    private void Jump()
    {
        jumpTimer += Time.deltaTime;

        float duration = Mathf.Max(jumpDuration, 0.01f);

        float t = Mathf.Clamp01(jumpTimer / duration);

        transform.position = CalculateArcPosition(t);

        if (rotateTowardsDirection)
        {
            Vector3 tangent = GetArcTangent(t);
            SetLookDirection(tangent);
        }

        if (t >= 1f)
        {
            transform.position = jumpTarget;
            StartLeaving();
        }
    }

    // =========================================================
    // POSITION BEZIER
    // =========================================================

    private Vector3 CalculateArcPosition(float t)
    {
        float inverse = 1f - t;

        return
            inverse * inverse * jumpStart +
            2f * inverse * t * arcControl +
            t * t * jumpTarget;
    }

    // =========================================================
    // TANGENTE
    // =========================================================

    private Vector3 GetArcTangent(float t)
    {
        return
            2f * (1f - t) * (arcControl - jumpStart) +
            2f * t * (jumpTarget - arcControl);
    }

    // =========================================================
    // SORTIE
    // =========================================================

    private void StartLeaving()
    {
        if (startedFromStart)
        {
            leaveTarget =
                endPoint.position +
                Vector3.right * distanceFromPoint;
        }
        else
        {
            leaveTarget =
                startPoint.position +
                Vector3.left * distanceFromPoint;
        }

        SetLookDirection(leaveTarget - transform.position);

        state = State.Leaving;
    }

    private void Leave()
    {
        Vector3 direction = leaveTarget - transform.position;

        SetLookDirection(direction);

        transform.position = Vector3.MoveTowards(
            transform.position,
            leaveTarget,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, leaveTarget) <= 0.01f)
        {
            transform.position = leaveTarget;

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
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        angle += rotationOffset;

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, angle);

        transform.rotation = Quaternion.Lerp(
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
        DisableCollider();

        if (trail_ != null)
        {
            trail_.Clear();
            trail_.enabled = true;
        }

        if (trail != null)
            trail.SetActive(true);
    }

    private void DisableTrail()
    {
        DisableCollider();

        if (trail_ != null)
        {
            trail_.Clear();
            trail_.enabled = false;
        }

        if (trail != null)
            trail.SetActive(false);
    }

    // =========================================================
    // FIN DU CYCLE
    // =========================================================

    private void StartCycleDelay()
    {
        Invoke("DisableTrail", 3);

        cycleTimer = cycleDelay;

        state = State.CycleDelay;
    }
}
