using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Awake()
    {
        sliderCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (cam == null)
            cam = Camera.main;

        if (sliderCollider == null)
            sliderCollider = GetComponent<Collider2D>();

        if (handle != null)
        {
            handleCollider = handle.GetComponent<Collider2D>();

            if (handleCollider == null)
                handleCollider = handle.GetComponentInChildren<Collider2D>();
        }

        menu = SC_sticker_menu.instance;

        if (cam == null)
            Debug.LogError("SC_slider : Camera introuvable !");

        if (sliderCollider == null)
            Debug.LogError("SC_slider : Collider2D du slider manquant !");

        SyncHandle();
    }

    private void OnDisable()
    {
        StopDragging();
    }

    private void Update()
    {
        if (menu != null && menu.editing)
        {
            StopDragging();
            return;
        }

        HandleMouseWheel();
        HandleMouseDrag();
        CheckSelectedButton();
    }

    // --------------------------------------------------
    // POSITION DU CURSEUR : même conversion que SC_Button
    // --------------------------------------------------

    private bool GetCursorWorldPosition(out Vector2 screenPosition, out Vector3 worldPosition)
    {
        screenPosition = Vector2.zero;
        worldPosition = Vector3.zero;

        if (SC_scursorManager.instance == null ||
            cam == null ||
            sliderCollider == null)
            return false;

        Vector3 cursorPosition = SC_scursorManager.instance.transform.position;
        screenPosition = new Vector2(cursorPosition.x, cursorPosition.y);

        if (!cam.pixelRect.Contains(screenPosition))
            return false;

        float depth = cam.WorldToScreenPoint(sliderCollider.bounds.center).z;

        worldPosition = cam.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, depth)
        );

        return true;
    }

    private bool IsPointerOverSlider(Vector3 cursorWorld)
    {
        bool overSlider =
            sliderCollider != null &&
            sliderCollider.enabled &&
            sliderCollider.OverlapPoint(cursorWorld);

        bool overHandle =
            handleCollider != null &&
            handleCollider.enabled &&
            handleCollider.OverlapPoint(cursorWorld);

        return overSlider || overHandle;
    }

    // --------------------------------------------------
    // SOURIS : DRAG
    // --------------------------------------------------

    private void HandleMouseDrag()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || cam == null || handle == null)
        {
            StopDragging();
            return;
        }

        bool usingController =
            SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller;

        if (usingController)
        {
            StopDragging();
            return;
        }

        bool validPosition = GetCursorWorldPosition(
            out _,
            out Vector3 cursorWorld
        );

        if (mouse.leftButton.wasPressedThisFrame &&
            validPosition &&
            IsPointerOverSlider(cursorWorld))
        {
            dragging = true;

            if (SC_scursorManager.instance != null)
                SC_scursorManager.instance.SetGrabCursor();

            if (handleCollider != null &&
                handleCollider.enabled &&
                handleCollider.OverlapPoint(cursorWorld))
            {
                dragOffsetY = handle.position.y - cursorWorld.y;
            }
            else
            {
                dragOffsetY = 0f;
                SetHandlePosition(cursorWorld.y);
            }
        }

        // Le drag continue même lorsque le curseur sort du collider.
        if (dragging && mouse.leftButton.isPressed && validPosition)
        {
            float targetY = cursorWorld.y + dragOffsetY;

            float newY = Mathf.Lerp(
                handle.position.y,
                targetY,
                Mathf.Clamp01(dragSpeed)
            );

            SetHandlePosition(newY);
        }

        if (mouse.leftButton.wasReleasedThisFrame)
            StopDragging();
    }

    private void StopDragging()
    {
        if (!dragging)
            return;

        dragging = false;

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.SetNormalCursor();
    }

    // --------------------------------------------------
    // MOLETTE
    // --------------------------------------------------

    private void HandleMouseWheel()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        float wheel = mouse.scroll.ReadValue().y;

        // Selon la configuration du périphérique, le delta peut être
        // exprimé en unités de 120 plutôt qu'en petits incréments.
        if (Mathf.Abs(wheel) > 1f)
            wheel /= 120f;

        if (Mathf.Abs(wheel) > 0.001f)
            MoveContent(wheel * wheelSpeed);
    }

    // --------------------------------------------------
    // SUIVI DE LA SÉLECTION MANETTE
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

        Vector3 screenPosition = cam.WorldToScreenPoint(selected.transform.position);

        float boxMinY = visibleBoxCenter.y - visibleBoxSize.y * 0.5f;
        float boxMaxY = visibleBoxCenter.y + visibleBoxSize.y * 0.5f;

        Vector3 minScreen = cam.WorldToScreenPoint(
            new Vector3(visibleBoxCenter.x, boxMinY, transform.position.z)
        );

        Vector3 maxScreen = cam.WorldToScreenPoint(
            new Vector3(visibleBoxCenter.x, boxMaxY, transform.position.z)
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

        Ray rayA = cam.ScreenPointToRay(new Vector3(centerX, centerY, 0f));
        Ray rayB = cam.ScreenPointToRay(new Vector3(centerX, centerY + pixels, 0f));

        Plane plane = new Plane(
            Vector3.forward,
            new Vector3(0f, 0f, transform.position.z)
        );

        if (plane.Raycast(rayA, out float distanceA) &&
            plane.Raycast(rayB, out float distanceB))
        {
            return rayB.GetPoint(distanceB).y - rayA.GetPoint(distanceA).y;
        }

        return 0f;
    }

    // --------------------------------------------------
    // DÉPLACEMENT DU CONTENU
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
    // DÉPLACEMENT DU HANDLE
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

        float t = Mathf.InverseLerp(handleMinY, handleMaxY, y);
        float contentY = Mathf.Lerp(contentMinY, contentMaxY, t);

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

        float handleY = Mathf.Lerp(handleMinY, handleMaxY, t);

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
