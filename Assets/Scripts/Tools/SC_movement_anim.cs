using UnityEngine;

public class SC_movement_anim : MonoBehaviour
{
    [Header("Vertical Movement")]
    public float height = 2f;
    public float speed = 2f;

    [Header("Horizontal Movement")]
    public float horizontalDistance = 0f;
    public float horizontalSpeed = 0f;
    public Vector3 horizontalDirection = Vector3.right;

    [Header("Smoothing")]
    public bool useSmooth = true;

    [Header("Rotation")]
    public Vector3 rotationAxis = new Vector3(0, 0, 1);
    public float rotationSpeed = 50f;

    private Vector3 startLocalPos;

    void Start()
    {
        startLocalPos = transform.localPosition;

        // Légère variation aléatoire
        height *= Random.Range(0.8f, 1.2f);
        speed *= Random.Range(0.8f, 1.2f);

        horizontalDistance *= Random.Range(0.8f, 1.2f);
        horizontalSpeed *= Random.Range(0.8f, 1.2f);

        rotationSpeed *= Random.Range(0.8f, 1.2f);

        horizontalDirection.Normalize();
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        float offsetY = Mathf.Sin(Time.time * speed) * height;
        float offsetX = Mathf.Sin(Time.time * horizontalSpeed) * horizontalDistance;

        Vector3 targetPosition =
            startLocalPos +
            Vector3.up * offsetY +
            horizontalDirection * offsetX;

        if (useSmooth)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                Time.unscaledDeltaTime * 5f
            );
        }
        else
        {
            transform.localPosition = targetPosition;
        }
    }

    void HandleRotation()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.unscaledDeltaTime);
    }
}