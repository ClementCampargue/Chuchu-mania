
using UnityEngine;

public class SC_roate_lean_anim : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 30f;

    [Header("Léger penchement")]
    public float tiltAngle = 5f;
    public float tiltSpeed = 2f;

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        // Rotation continue sur l'axe Y
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.Self);

        // Inclinaison douce sur les axes X et Z
        float tiltX = Mathf.Sin(Time.time * tiltSpeed) * tiltAngle;
        float tiltZ = Mathf.Cos(Time.time * tiltSpeed * 0.7f) * tiltAngle;

        Quaternion tilt = Quaternion.Euler(tiltX, 0f, tiltZ);

        // Applique le léger penchement à la rotation actuelle
        transform.localRotation = transform.localRotation * tilt;
    }
}
