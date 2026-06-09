using UnityEngine;
using System.Collections;

public class SC_go_to_position : MonoBehaviour
{
    [Header("Cible")]
    public Transform target;

    [Header("Seconde cible")]
    public Transform secondTarget;
    public float delayBeforeSecondTarget = 0.5f;

    [Header("Attraction")]
    public float attractionSpeed = 10f;
    public float steeringForce = 5f;

    [Header("Ralentissement du mouvement actuel")]
    public float initialVelocityDamping = 2f;

    [Header("Mouvement courbé")]
    public bool useCurve = true;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float curveAmplitude = 1f;

    [Header("Noise")]
    public bool useNoise = false;
    public float noiseAmplitude = 0.5f;
    public float noiseFrequency = 5f;

    private Vector3 velocity;
    private bool isMoving = false;

    private Vector3 startPosition;
    private float startDistance;

    private bool goingToSecondTarget = false;

    /// <summary>
    /// Lance le déplacement vers la première cible.
    /// </summary>
    public void GoToTarget()
    {
        goingToSecondTarget = false;
        StartMoveTo(target);
    }

    private void StartMoveTo(Transform destination)
    {
        if (destination == null)
            return;

        target = destination;

        startPosition = transform.position;
        startDistance = Vector3.Distance(transform.position, target.position);

        isMoving = true;
    }

    private IEnumerator GoToSecondTargetRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeSecondTarget);

        goingToSecondTarget = true;
        StartMoveTo(secondTarget);
    }

    void Update()
    {
        if (!isMoving || target == null)
            return;

        Vector3 toTarget = target.position - transform.position;

        // Arrivé
        if (toTarget.magnitude < 0.05f)
        {
            transform.position = target.position;
            isMoving = false;
            velocity = Vector3.zero;

            // Enchaîne vers la seconde cible
            if (!goingToSecondTarget && secondTarget != null)
            {
                StartCoroutine(GoToSecondTargetRoutine());
            }

            return;
        }

        // Ralentit progressivement le mouvement actuel
        velocity = Vector3.Lerp(
            velocity,
            Vector3.zero,
            initialVelocityDamping * Time.unscaledDeltaTime
        );

        // Attraction vers la cible
        Vector3 desiredVelocity = toTarget.normalized * attractionSpeed;

        velocity += (desiredVelocity - velocity)
                    * steeringForce
                    * Time.unscaledDeltaTime;

        Vector3 offset = Vector3.zero;

        // Mouvement courbé
        if (useCurve && startDistance > 0.001f)
        {
            float progress = 1f - (toTarget.magnitude / startDistance);

            Vector3 perpendicular =
                Vector3.Cross(toTarget.normalized, Vector3.forward);

            float curveOffset =
                curve.Evaluate(progress) * curveAmplitude;

            offset += perpendicular * curveOffset;
        }

        // Noise
        if (useNoise)
        {
            float t = Time.unscaledTime * noiseFrequency;

            offset += new Vector3(
                Mathf.PerlinNoise(t, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, t) - 0.5f,
                0f
            ) * noiseAmplitude;
        }

        transform.position +=
            (velocity + offset) * Time.unscaledDeltaTime;
    }
}