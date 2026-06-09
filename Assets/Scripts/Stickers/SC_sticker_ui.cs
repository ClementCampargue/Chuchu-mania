using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SC_sticker_UI : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IDragHandler
{
    private bool dragging;
    private Vector2 offset;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Image img;

    public bool spawnedSticker;

    [Header("Delete Zone")]
    public RectTransform deleteZone;   // Assigne le rectangle dans l'inspecteur
    public bool showDebug = true;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        img = GetComponent<Image>();

        canvas = GetComponentInParent<Canvas>();

        // équivalent "mat unique"
        img.material = new Material(img.material);
    }
    void Start()
    {
        deleteZone = GameObject.Find("TrashZone").GetComponent<RectTransform>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "StickerMenu") return;

        if (!dragging)
            SC_scursorManager.instance.SetHoverCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "StickerMenu") return;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "StickerMenu") return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            dragging = true;
            img.maskable = false;

            SC_scursorManager.instance.SetGrabCursor();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint
            );

            offset = rectTransform.anchoredPosition - localPoint;

            img.transform.SetAsLastSibling();

            img.raycastTarget = false;
        }

        if (eventData.button == PointerEventData.InputButton.Right
            && !SC_scursorManager.instance.grabbing)
        {
            SC_stickerManager.instance.SaveStickers();
            Destroy(gameObject);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "StickerMenu") return;
        if (!dragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        rectTransform.anchoredPosition = localPoint + offset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "StickerMenu") return;

        dragging = false;
        img.maskable = true;

        SC_scursorManager.instance.SetNormalCursor();
        img.raycastTarget = true;

        // Vérifie si le sticker a été lâché dans la zone de suppression
        if (deleteZone != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                deleteZone,
                eventData.position,
                canvas.worldCamera))
        {
            Debug.Log("Sticker supprimé");

            SC_stickerManager.instance.SaveStickers();
            Destroy(gameObject);
            return;
        }

        SC_StickerSaveSystem.instance.AutoSave();
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