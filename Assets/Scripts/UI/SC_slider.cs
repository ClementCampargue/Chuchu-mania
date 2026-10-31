
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

    private float contentStartY;

    private void Start()
    {
        menu = SC_sticker_menu.instance;

        if (cam == null)
            cam = Camera.main;

        sliderCollider = GetComponent<Collider2D>();

        if (handle != null)
            handleCollider = handle.GetComponent<Collider2D>();

        if (content != null)
            contentStartY = content.position.y;

        SyncHandle();
    }

    private void Update()
    {
        if (menu != null && menu.editing)
        {
            if (dragging)
            {
                dragging = false;
                SC_scursorManager.instance.SetNormalCursor();
            }

            return;
        }

        HandleMouseWheel();
        HandleMouseDrag();
        CheckSelectedButton();
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
    // MOUSE
    // --------------------------------------------------

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;

        mousePosition.z = Mathf.Abs(
            cam.transform.position.z - transform.position.z
        );

        return cam.ScreenToWorldPoint(mousePosition);
    }

    private void HandleMouseDrag()
    {
        if (cam == null || handle == null)
            return;

        Vector3 mouseWorld = GetMouseWorldPosition();

        if (Input.GetMouseButtonDown(0))
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);

            if (hit != null && sliderCollider != null &&
                hit == sliderCollider)
            {
                dragging = true;

                if (SC_scursorManager.instance != null)
                    SC_scursorManager.instance.SetGrabCursor();

                if (handleCollider != null &&
                    handleCollider.OverlapPoint(mouseWorld))
                {
                    dragOffsetY = handle.position.y - mouseWorld.y;
                }
                else
                {
                    dragOffsetY = 0f;
                    SetHandlePosition(mouseWorld.y);
                }
            }
        }

        if (Input.GetMouseButton(0) && dragging)
        {
            float targetY = mouseWorld.y + dragOffsetY;

            SetHandlePosition(
                Mathf.Lerp(
                    handle.position.y,
                    targetY,
                    dragSpeed
                )
            );
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;

            if (SC_scursorManager.instance != null)
                SC_scursorManager.instance.SetNormalCursor();
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

        float screenMinY = 0f;
        float screenMaxY = Screen.height;

        float boxMinY = visibleBoxCenter.y -
                        visibleBoxSize.y * 0.5f;

        float boxMaxY = visibleBoxCenter.y +
                        visibleBoxSize.y * 0.5f;

        // Les limites sont converties depuis le monde vers l'écran.
        Vector3 minWorld = cam.WorldToScreenPoint(
            new Vector3(
                visibleBoxCenter.x,
                boxMinY,
                transform.position.z
            )
        );

        Vector3 maxWorld = cam.WorldToScreenPoint(
            new Vector3(
                visibleBoxCenter.x,
                boxMaxY,
                transform.position.z
            )
        );

        screenMinY = Mathf.Min(minWorld.y, maxWorld.y);
        screenMaxY = Mathf.Max(minWorld.y, maxWorld.y);

        if (screenPosition.y < screenMinY)
        {
            float difference = screenMinY - screenPosition.y;

            MoveContent(
                ScreenToWorldY(difference)
            );
        }
        else if (screenPosition.y > screenMaxY)
        {
            float difference = screenMaxY - screenPosition.y;

            MoveContent(
                ScreenToWorldY(difference)
            );
        }
    }

    private float ScreenToWorldY(float pixels)
    {
        if (cam == null)
            return pixels;

        Vector3 a = cam.ScreenToWorldPoint(
            new Vector3(0f, 0f,
                Mathf.Abs(cam.transform.position.z - transform.position.z))
        );

        Vector3 b = cam.ScreenToWorldPoint(
            new Vector3(0f, pixels,
                Mathf.Abs(cam.transform.position.z - transform.position.z))
        );

        return b.y - a.y;
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
