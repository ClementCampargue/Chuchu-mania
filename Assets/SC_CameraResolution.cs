
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SC_CameraResolution : MonoBehaviour
{
    [Header("Résolution de référence")]
    public float referenceWidth = 1920f;
    public float referenceHeight = 1080f;

    [Header("Caméra")]
    public float referenceOrthographicSize = 5f;

    [Header("Limites")]
    public float minOrthographicSize = 3f;
    public float maxOrthographicSize = 10f;

    private Camera cam;

    private float previousAspect;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        UpdateCameraSize();
    }

    private void Update()
    {
        if (previousAspect != (float)Screen.width / Screen.height)
        {
            UpdateCameraSize();
        }
    }

    private void UpdateCameraSize()
    {
        float referenceAspect = referenceWidth / referenceHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        // Ajustement selon le ratio de l'écran.
        float ratio = referenceAspect / currentAspect;

        cam.orthographicSize = Mathf.Clamp(
            referenceOrthographicSize * ratio,
            minOrthographicSize,
            maxOrthographicSize
        );

        previousAspect = currentAspect;
    }
}
