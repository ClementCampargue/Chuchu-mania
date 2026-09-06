using UnityEngine;

public class SC_mouse_tilt_transform : MonoBehaviour
{
    [Header("Tilt")]
    public float maxTilt = 8f;
    public float smoothSpeed = 10f;

    [Header("Range")]
    public float influenceRadius = 2f; // Distance en unités monde

    private Transform targetTransform;
    private Quaternion targetRotation;

    void Awake()
    {
        targetTransform = transform;
    }

    void Update()
    {
        // Position de la souris dans le monde
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                -Camera.main.transform.position.z
            )
        );

        // Position du centre du sprite
        Vector2 spriteCenter = targetTransform.position;

        Vector2 delta = (Vector2)mouseWorld - spriteCenter;

        float distance = delta.magnitude;

        // Si la souris est trop loin, retour à la rotation normale
        if (distance > influenceRadius)
        {
            targetRotation = Quaternion.identity;
        }
        else
        {
            // Intensité de 0 à 1 selon la proximité
            float strength = 1f - (distance / influenceRadius);

            Vector2 dir = delta.normalized;

            float tiltX = -dir.y * maxTilt * strength;
            float tiltY = dir.x * maxTilt * strength;
            float tiltZ = -dir.x * maxTilt * 0.5f * strength;

            targetRotation = Quaternion.Euler(tiltX, tiltY, tiltZ);
        }

        // Rotation fluide
        targetTransform.localRotation = Quaternion.Slerp(
            targetTransform.localRotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );
    }
}