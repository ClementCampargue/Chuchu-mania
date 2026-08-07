using UnityEngine;

public class SC_ui_mouse_effect : MonoBehaviour
{
    [Header("Tilt")]
    public float maxTilt = 8f;
    public float smoothSpeed = 10f;

    [Header("Range")]
    public float influenceRadius = 200f; // Distance en pixels UI

    private RectTransform rectTransform;
    private Canvas canvas;
    private Quaternion targetRotation;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // Position du centre de la carte à l'écran
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);

        Vector2 mouse = Input.mousePosition;
        Vector2 delta = mouse - screenCenter;

        float distance = delta.magnitude;

        // Si la souris est trop loin, on revient à la rotation normale
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

        rectTransform.localRotation = Quaternion.Slerp(
            rectTransform.localRotation,
            targetRotation,
            Time.deltaTime * smoothSpeed);
    }
}