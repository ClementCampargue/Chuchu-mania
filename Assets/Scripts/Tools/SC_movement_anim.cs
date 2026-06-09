using UnityEngine;

public class SC_movement_anim : MonoBehaviour
{
    [Header("Vertical Movement")]
    public float height = 2f;
    public float speed = 2f;
    public bool useSmooth = true;

    [Header("Rotation")]
    public Vector3 rotationAxis = new Vector3(0, 0, 1);
    public float rotationSpeed = 50f;

    private Vector3 startLocalPos;

    void Start()
    {
        // On stocke la position LOCALE
        startLocalPos = transform.localPosition;

        height = Random.Range(height *0.8f, height *1.2f);
        speed = Random.Range(speed * 0.8f, speed * 1.2f);
        rotationSpeed = Random.Range(rotationSpeed * 0.8f, rotationSpeed * 1.2f);
    }

    void Update()
    {
        HandleVerticalMovement();
        HandleRotation();
    }

    void HandleVerticalMovement()
    {
        float newY = Mathf.Sin(Time.time * speed) * height;

        // Utilisation de localPosition au lieu de position
        Vector3 targetPosition = startLocalPos + Vector3.up * newY;

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