using UnityEngine;

public class SC_ui_rotate_move_effect : MonoBehaviour
{
    public float rotationAmount = 15f;
    public float smoothSpeed = 8f;

    private RectTransform rect;
    private Vector2 lastPosition;
    private Quaternion targetRotation;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        lastPosition = rect.anchoredPosition;
        targetRotation = rect.localRotation;
    }

    void Update()
    {
        Vector2 currentPosition = rect.anchoredPosition;

        Vector2 movement = currentPosition - lastPosition;

        // Nouvelle inclinaison seulement s'il y a un mouvement
        if (movement.sqrMagnitude > 0.001f)
        {
            float rotZ = -movement.x * rotationAmount;
            float rotX = movement.y * rotationAmount * 0.5f;

            targetRotation = Quaternion.Euler(rotX, 0, rotZ);
        }

        // Suivi fluide
        rect.localRotation = Quaternion.Lerp(
            rect.localRotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );

        lastPosition = currentPosition;
    }
}