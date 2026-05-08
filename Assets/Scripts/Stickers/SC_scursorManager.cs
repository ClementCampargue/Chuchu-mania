using UnityEngine;

public class SC_scursorManager : MonoBehaviour
{
    public static SC_scursorManager instance;

    [Header("Sprites curseur")]
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite grabSprite;

    [Header("Prefab curseur")]
    public GameObject cursorPrefab;

    public SpriteRenderer cursorRenderer;

    private Camera cam;
    public bool grabing;
    public GameObject fader;
    void Awake()
    {
        instance = this;
        cam = Camera.main;

        Cursor.visible = false;


        SetNormalCursor();
    }

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;

        Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);
        worldPos.z = -5f;

        transform.position = worldPos;
    }

    public void SetNormalCursor()
    {
        fader.SetActive(false);
        grabing = false;
        cursorRenderer.sprite = normalSprite;
    }

    public void SetHoverCursor()
    {
        if (!grabing)
        {
            cursorRenderer.sprite = hoverSprite;
        }
    }

    public void SetGrabCursor()
    {
        fader.SetActive(true);

        grabing = true;
        cursorRenderer.sprite = grabSprite;
    }
}