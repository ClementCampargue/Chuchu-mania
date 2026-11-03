
using UnityEngine;

public class SC_MouseDebug : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("Debug Square")]
    public float squareSize = 0.15f;
    public Color squareColor = Color.red;

    [Header("Debug")]
    public bool showCoordinates = true;
    public bool showCollider = true;

    private Vector3 mouseWorldPosition;
    private Collider2D hoveredCollider;
    private bool validPosition;

    private Texture2D squareTexture;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        squareTexture = new Texture2D(1, 1);
        squareTexture.SetPixel(0, 0, squareColor);
        squareTexture.Apply();
    }

    private void Update()
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(
            Input.mousePosition
        );

        Plane plane = new Plane(
            Vector3.forward,
            Vector3.zero
        );

        validPosition = plane.Raycast(ray, out float distance);

        if (!validPosition)
            return;

        mouseWorldPosition = ray.GetPoint(distance);

        hoveredCollider = Physics2D.OverlapPoint(
            new Vector2(
                mouseWorldPosition.x,
                mouseWorldPosition.y
            )
        );
    }

    private void OnGUI()
    {
        if (!validPosition || mainCamera == null)
            return;

        // Conversion World Space vers Screen Space
        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(mouseWorldPosition);

        // Carré rouge centré sur la position détectée
        float size = squareSize * mainCamera.pixelHeight /
                     (mainCamera.orthographicSize * 2f);

        Rect rect = new Rect(
            screenPosition.x - size / 2f,
            Screen.height - screenPosition.y - size / 2f,
            size,
            size
        );

        GUI.color = squareColor;
        GUI.DrawTexture(rect, squareTexture);
        GUI.color = Color.white;

        // Informations de debug
        GUILayout.BeginArea(new Rect(10, 10, 350, 120), GUI.skin.box);

        GUILayout.Label("MOUSE DEBUG");

        if (showCoordinates)
            GUILayout.Label("World: " + mouseWorldPosition);

        if (showCollider)
        {
            GUILayout.Label(
                "Collider: " +
                (hoveredCollider != null
                    ? hoveredCollider.name
                    : "None")
            );
        }

        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (squareTexture != null)
            Destroy(squareTexture);
    }
}
