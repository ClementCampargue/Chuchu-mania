using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SC_sticker_UI : MonoBehaviour,
    IPointerDownHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IDragHandler
{
    [Header("Resize")]
    public float minScale = 0.5f;
    public float defaultScale = 1f;
    public float maxScale = 2.5f;
    public float wheelSpeed = 0.2f;
    private bool dragging;
    private Vector2 offset;
    [Header("Rotation")]
    public float rotationSpeed = 10f;

    private bool overDeleteZone;
    private Color baseColor;
    private RectTransform rectTransform;
    private Canvas canvas;
    public Image img;
    public bool spawnedSticker; 

    [Header("Delete Zone")]
    public RectTransform deleteZone;   // Assigne le rectangle dans l'inspecteur
    public bool showDebug = true;
    public SC_stick_to_mouse stick;
    public GameObject selected;

    public Animator anim;
    void Awake()
    {
        baseColor = img.color;
        rectTransform = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        rectTransform.localScale = new Vector3(defaultScale, defaultScale,defaultScale);
        canvas = GetComponentInParent<Canvas>();

        img.material = new Material(img.material);

        // Active le raycast par alpha
        img.alphaHitTestMinimumThreshold = 0.1f;
    }
    void Start()
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;
        if (!spawnedSticker)
        {
            SC_scursorManager.instance.SetGrabCursor();
            dragging = true;
            stick.enabled = true;
        }
        else
        {
        }

        selected.SetActive(true);
        anim.enabled = true;
        deleteZone = GameObject.Find("TrashZone").GetComponent<RectTransform>();
    }
    void Update()
    {
        if (SceneManager.GetActiveScene().name != "Stickers")
            return;

        // Seulement lorsque le sticker est tenu
        if (!dragging)
            return;

 
        if (deleteZone != null)
        {
            overDeleteZone = RectTransformUtility.RectangleContainsScreenPoint(
                deleteZone,
                Input.mousePosition,
                canvas.worldCamera);

            if (overDeleteZone)
            {
                float a = Mathf.Lerp(0.3f, 1f, (Mathf.Sin(Time.time * 12f) + 1f) * 0.5f);
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            }
            else
            {
                img.color = baseColor;
            }
        }


        float wheel = Input.mouseScrollDelta.y;

        if (Mathf.Abs(wheel) > 0.01f)
        {
            // ALT = Rotation
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                rectTransform.Rotate(0, 0, -wheel * rotationSpeed, Space.Self);
            }
            // Sinon = Scale
            else
            {
                float scale = rectTransform.localScale.x;

                scale += wheel * wheelSpeed;
                scale = Mathf.Clamp(scale, minScale, maxScale);

                rectTransform.localScale = Vector3.one * scale;
            }
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;
        if (Input.GetMouseButtonDown(1))
        {

            SC_sticker_menu.instance.quit_edit_mode();
            SC_StickerSaveSystem.instance.AutoSave();
            Destroy(gameObject);
            return;
        }

        if (dragging)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {

                if (SceneManager.GetActiveScene().name != "Stickers") return;
                selected.SetActive(false);

                img.maskable = true;


                // Vérifie si le sticker a été lâché dans la zone de suppression
                if (deleteZone != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(
                        deleteZone,
                        eventData.position,
                        canvas.worldCamera))
                {
                    Debug.Log("Sticker supprimé");
                    SC_sticker_menu.instance.quit_edit_mode();
                    SC_StickerSaveSystem.instance.AutoSave();
                    Destroy(gameObject);
                    return;
                }
                anim.ResetTrigger("grab");
                anim.SetTrigger("drop");
                stick.enabled = false;
                SC_sticker_menu.instance.quit_edit_mode();
                SC_StickerSaveSystem.instance.AutoSave();
                SC_scursorManager.instance.SetHoverCursor();
            }
        }
        else
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                selected.SetActive(true);
                img.maskable = false;


                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    eventData.position,
                    canvas.worldCamera,
                    out Vector2 localPoint
                );

                offset = rectTransform.anchoredPosition - localPoint;

                img.transform.SetAsLastSibling();
                anim.ResetTrigger("drop");
                anim.SetTrigger("grab");
                stick.enabled = true;
                transform.parent.SetAsLastSibling();
                SC_sticker_menu.instance.start_edit_mode();
                SC_scursorManager.instance.SetGrabCursor();
            }
            else
            {

            }
        }
        dragging = !dragging;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;
        if (!dragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        rectTransform.anchoredPosition = localPoint + offset;
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebug || deleteZone == null)
            return;

        Vector3[] corners = new Vector3[4];
        deleteZone.GetWorldCorners(corners);

        Gizmos.color = Color.red;

        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }
    }
#endif
}