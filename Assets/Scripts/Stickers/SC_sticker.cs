using UnityEngine;

public class SC_sticker : MonoBehaviour
{
    private bool dragging;

    private Vector3 offset;

    private SpriteRenderer sr;

    public bool spawnedSticker;
    private PolygonCollider2D poly;
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        poly = GetComponent<PolygonCollider2D>();
        // material unique
        sr.material = new Material(sr.material);
    }

    void OnMouseEnter()
    {
        if (!dragging)
            SC_scursorManager.instance.SetHoverCursor();
    }
    public void RefreshCollider()
    {
        poly.pathCount = 0;

        poly.pathCount = sr.sprite.GetPhysicsShapeCount();

        var points = new System.Collections.Generic.List<Vector2>();

        for (int i = 0; i < poly.pathCount; i++)
        {
            points.Clear();

            sr.sprite.GetPhysicsShape(i, points);

            poly.SetPath(i, points.ToArray());
        }
    }

    void OnMouseExit()
    {
        if (!dragging)
            SC_scursorManager.instance.SetNormalCursor();
    }

    void OnMouseDown()
    {
        // gauche
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;

            SC_scursorManager.instance.SetGrabCursor();

            Vector3 mousePos =
                Camera.main.ScreenToWorldPoint(Input.mousePosition);

            mousePos.z = 0;

            offset = transform.position - mousePos;

            sr.sortingOrder =
                SC_stickerManager.instance.GetTopSortingOrder();

            sr.maskInteraction = SpriteMaskInteraction.None;
        }

        // droit
        if (Input.GetMouseButtonDown(1)
            && !SC_scursorManager.instance.grabbing)
        {
            SC_stickerManager.instance.SaveStickers();

            Destroy(gameObject);
        }
    }

    void OnMouseUp()
    {
        dragging = false;

        SC_scursorManager.instance.SetNormalCursor();

        sr.maskInteraction =
            SpriteMaskInteraction.VisibleInsideMask;

        SC_stickerManager.instance.SaveStickers();
    }

    void Update()
    {
        if (!dragging)
            return;

        Vector3 mousePos =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        mousePos.z = 0;

        transform.position = mousePos + offset;
    }
}