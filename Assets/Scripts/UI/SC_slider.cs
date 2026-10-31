
using UnityEngine;

public class SC_slider : MonoBehaviour
{
    [Header("References")]
    public Transform handle;
    public Transform content;
    public Camera cam;

    [Header("Handle Limits")]
    public float handleMinY = -3f;
    public float handleMaxY = 3f;

    [Header("Content Limits")]
    public float contentMinY = 0f;
    public float contentMaxY = -25f;

    [Header("Speeds")]
    public float dragSpeed = 1f;
    public float wheelSpeed = 0.5f;

    [Header("Visible Box")]
    public Vector2 visibleBoxCenter = Vector2.zero;
    public Vector2 visibleBoxSize = new Vector2(10f, 4f);

    [Header("Visible Box Gizmos")]
    public Color visibleBoxColor = Color.green;

    private bool dragging;
    private float dragOffsetY;

    private Collider2D sliderCollider;
    private Collider2D handleCollider;

    private GameObject lastSelected;

    private SC_sticker_menu menu;

    private void Start()
    {
        menu = SC_sticker_menu.instance;

        if (cam == null)
            cam = Camera.main;

        sliderCollider = GetComponent<Collider2D>();

        if (handle != null)
        {
            handleCollider = handle.GetComponent<Collider2D>();

            if (handleCollider == null)
                handleCollider = handle.GetComponentInChildren<Collider2D>();
        }

        if (cam == null)
            Debug.LogError("SC_slider : Camera introuvable !");

        if (sliderCollider == null)
            Debug.LogError("SC_slider : Collider2D du slider manquant !");

        SyncHandle();
    }

    private void Update()
    {
        if (menu != null && menu.editing)
        {
            if (dragging)
            {
                dragging = false;

                if (SC_scursorManager.instance != null)
                    SC_scursorManager.instance.SetNormalCursor();
            }

            return;
        }

        HandleMouseWheel();
        HandleMouseDrag();
        CheckSelectedButton();
    }

    // --------------------------------------------------
    // MOUSE TO WORLD
    // --------------------------------------------------

    private bool GetMouseWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (cam == null || handle == null)
            return false;

        Vector3 mousePosition = Input.mousePosition;

        // Vérifie que la souris est dans la zone de la caméra.
        if (!cam.pixelRect.Contains(mousePosition))
            return false;

        // Conversion des pixels réels de l'écran
        // vers un rayon dans le monde.
        Ray ray = cam.ScreenPointToRay(mousePosition);

        // Plan XY correspondant à la position du handle.
        Plane plane = new Plane(
            Vector3.forward,
            handle.position
        );

        if (!plane.Raycast(ray, out float distance))
            return false;

        worldPosition = ray.GetPoint(distance);

        return true;
    }

    // --------------------------------------------------
    // COLLIDER DETECTION
    // --------------------------------------------------

    private bool IsPointerOverSlider(Vector3 mouseWorld)
    {
        bool overSlider =
            sliderCollider != null &&
            sliderCollider.OverlapPoint(mouseWorld);

        bool overHandle =
            handleCollider != null &&
            handleCollider.OverlapPoint(mouseWorld);

        return overSlider || overHandle;
    }

    // --------------------------------------------------
    // MOUSE DRAG
    // --------------------------------------------------

    private void HandleMouseDrag()
    {
        if (cam == null || handle == null)
            return;

        bool validMousePosition =
            GetMouseWorldPosition(out Vector3 mouseWorld);

        if (Input.GetMouseButtonDown(0))
        {
            if (validMousePosition &&
                IsPointerOverSlider(mouseWorld))
            {
                dragging = true;

                if (SC_scursorManager.instance != null)
                    SC_scursorManager.instance.SetGrabCursor();

                // Clic directement sur le handle.
                if (handleCollider != null &&
                    handleCollider.OverlapPoint(mouseWorld))
                {
                    dragOffsetY =
                        handle.position.y - mouseWorld.y;
                }
                else
                {
                    // Clic sur la barre.
                    dragOffsetY = 0f;

                    SetHandlePosition(mouseWorld.y);
                }
            }
        }

        // Pendant le drag, on continue même si la souris
        // sort du collider du slider.
        if (Input.GetMouseButton(0) && dragging)
        {
            if (validMousePosition)
            {
                float targetY = mouseWorld.y + dragOffsetY;

                float newY = Mathf.Lerp(
                    handle.position.y,
                    targetY,
                    Mathf.Clamp01(dragSpeed)
                );

                SetHandlePosition(newY);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;

            if (SC_scursorManager.instance != null)
                SC_scursorManager.instance.SetNormalCursor();
        }
    }

    // --------------------------------------------------
    // MOUSE WHEEL
    // --------------------------------------------------

    private void HandleMouseWheel()
    {
        float wheel = Input.mouseScrollDelta.y;

        if (Mathf.Abs(wheel) > 0.001f)
        {
            MoveContent(wheel * wheelSpeed);
        }
    }

    // --------------------------------------------------
    // CONTROLLER SELECTION
    // --------------------------------------------------

    private void CheckSelectedButton()
    {
        if (!SC_Button.IsControllerNavigation)
            return;

        SC_Button selectedButton = SC_Button.GetActiveButton();

        if (selectedButton == null)
        {
            lastSelected = null;
            return;
        }

        GameObject selected = selectedButton.gameObject;

        if (selected == lastSelected)
            return;

        lastSelected = selected;

        if (cam == null)
            return;

        Vector3 screenPosition = cam.WorldToScreenPoint(
            selected.transform.position
        );

        float boxMinY = visibleBoxCenter.y -
                        visibleBoxSize.y * 0.5f;

        float boxMaxY = visibleBoxCenter.y +
                        visibleBoxSize.y * 0.5f;

        Vector3 minScreen = cam.WorldToScreenPoint(
            new Vector3(
                visibleBoxCenter.x,
                boxMinY,
                transform.position.z
            )
        );

        Vector3 maxScreen = cam.WorldToScreenPoint(
            new Vector3(
                visibleBoxCenter.x,
                boxMaxY,
                transform.position.z
            )
        );

        float screenMinY = Mathf.Min(minScreen.y, maxScreen.y);
        float screenMaxY = Mathf.Max(minScreen.y, maxScreen.y);

        if (screenPosition.y < screenMinY)
        {
            float difference = screenMinY - screenPosition.y;

            MoveContent(ScreenToWorldY(difference));
        }
        else if (screenPosition.y > screenMaxY)
        {
            float difference = screenMaxY - screenPosition.y;

            MoveContent(ScreenToWorldY(difference));
        }
    }

    private float ScreenToWorldY(float pixels)
    {
        if (cam == null)
            return pixels;

        float centerX = cam.pixelRect.center.x;
        float centerY = cam.pixelRect.center.y;

        Ray rayA = cam.ScreenPointToRay(
            new Vector3(centerX, centerY, 0f)
        );

        Ray rayB = cam.ScreenPointToRay(
            new Vector3(centerX, centerY + pixels, 0f)
        );

        Plane plane = new Plane(
            Vector3.forward,
            new Vector3(0f, 0f, transform.position.z)
        );

        if (plane.Raycast(rayA, out float distanceA) &&
            plane.Raycast(rayB, out float distanceB))
        {
            return rayB.GetPoint(distanceB).y -
                   rayA.GetPoint(distanceA).y;
        }

        return 0f;
    }

    // --------------------------------------------------
    // CONTENT MOVEMENT
    // --------------------------------------------------

    private void MoveContent(float deltaY)
    {
        if (content == null)
            return;

        float newY = Mathf.Clamp(
            content.position.y + deltaY,
            Mathf.Min(contentMinY, contentMaxY),
            Mathf.Max(contentMinY, contentMaxY)
        );

        content.position = new Vector3(
            content.position.x,
            newY,
            content.position.z
        );

        SyncHandle();
    }

    // --------------------------------------------------
    // HANDLE MOVEMENT
    // --------------------------------------------------

    private void SetHandlePosition(float y)
    {
        if (handle == null || content == null)
            return;

        y = Mathf.Clamp(
            y,
            Mathf.Min(handleMinY, handleMaxY),
            Mathf.Max(handleMinY, handleMaxY)
        );

        handle.position = new Vector3(
            handle.position.x,
            y,
            handle.position.z
        );

        float t = Mathf.InverseLerp(
            handleMinY,
            handleMaxY,
            y
        );

        float contentY = Mathf.Lerp(
            contentMinY,
            contentMaxY,
            t
        );

        content.position = new Vector3(
            content.position.x,
            contentY,
            content.position.z
        );
    }

    private void SyncHandle()
    {
        if (handle == null || content == null)
            return;

        float t = Mathf.InverseLerp(
            contentMinY,
            contentMaxY,
            content.position.y
        );

        float handleY = Mathf.Lerp(
            handleMinY,
            handleMaxY,
            t
        );

        handle.position = new Vector3(
            handle.position.x,
            handleY,
            handle.position.z
        );
    }

    // --------------------------------------------------
    // GIZMOS
    // --------------------------------------------------

    private void OnDrawGizmos()
    {
        Gizmos.color = visibleBoxColor;

        Vector3 center = new Vector3(
            visibleBoxCenter.x,
            visibleBoxCenter.y,
            transform.position.z
        );

        Vector3 size = new Vector3(
            visibleBoxSize.x,
            visibleBoxSize.y,
            0.1f
        );

        Gizmos.DrawWireCube(center, size);
    }
}
