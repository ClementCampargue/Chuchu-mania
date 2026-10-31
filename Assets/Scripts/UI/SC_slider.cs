
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
    public Vector2 visibleBoxCenter = new Vector2(0f, 0f);
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
            handleCollider = handle.GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (menu != null && menu.editing)
        {
            dragging = false;
            return;
        }

        HandleMouseWheel();
        HandleMouseDrag();
        CheckSelectedButton();
    }

    // -------------------------
    // MOUSE WHEEL
    // -------------------------

    private void HandleMouseWheel()
    {
        float wheel = Input.mouseScrollDelta.y;

        if (wheel != 0f)
        {
            MoveContent(wheel * wheelSpeed);
        }
    }

    // -------------------------
    // MOUSE CLICK AND DRAG
    // -------------------------

    private void HandleMouseDrag()
    {
        if (cam == null || handle == null)
            return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(
            new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                Mathf.Abs(cam.transform.position.z - transform.position.z)
            )
        );

        if (Input.GetMouseButtonDown(0))
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);

            // Vérifie si on clique sur le slider.
            if (hit != null && sliderCollider != null &&
                hit == sliderCollider)
            {
                dragging = true;
                SC_scursorManager.instance.SetGrabCursor();
                // Clic sur le handle : conserve sa position relative.
                if (handleCollider != null &&
                    handleCollider.OverlapPoint(mouseWorld))
                {
                    dragOffsetY = handle.position.y - mouseWorld.y;
                }
                else
                {
                    // Clic ailleurs sur la barre :
                    // déplacement immédiat à la position cliquée.
                    dragOffsetY = 0f;

                    SetHandlePosition(mouseWorld.y);
                }
            }
        }

        // Déplacement pendant le maintien du clic.
        if (Input.GetMouseButton(0) && dragging)
        {
            float targetY = mouseWorld.y + dragOffsetY;

            float delta = (targetY - handle.position.y) * dragSpeed;

            SetHandlePosition(handle.position.y + delta);
        }

        if (Input.GetMouseButtonUp(0))
        {
            SC_scursorManager.instance.SetNormalCursor();

            dragging = false;
        }
    }

    // -------------------------
    // SELECTION SCROLL
    // -------------------------

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

        float selectedY = selected.transform.position.y;

        float boxMinY = visibleBoxCenter.y - visibleBoxSize.y * 0.5f;
        float boxMaxY = visibleBoxCenter.y + visibleBoxSize.y * 0.5f;

        if (selectedY < boxMinY)
        {
            MoveContent(boxMinY - selectedY);
        }
        else if (selectedY > boxMaxY)
        {
            MoveContent(boxMaxY - selectedY);
        }
    }

    // -------------------------
    // CONTENT MOVEMENT
    // -------------------------

    private void MoveContent(float deltaY)
    {
        if (content == null)
            return;

        float newY = content.position.y + deltaY;

        newY = Mathf.Clamp(
            newY,
            contentMaxY,
            contentMinY
        );

        content.position = new Vector3(
            content.position.x,
            newY,
            content.position.z
        );

        SyncHandle();
    }

    // -------------------------
    // HANDLE MOVEMENT
    // -------------------------

    private void SetHandlePosition(float y)
    {
        if (handle == null || content == null)
            return;

        y = Mathf.Clamp(y, handleMinY, handleMaxY);

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

    // -------------------------
    // EDITOR GIZMOS
    // -------------------------

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
