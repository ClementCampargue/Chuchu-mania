using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public SC_stick_to_mouse stick;
    public GameObject selected;
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        img = GetComponent<Image>();

        canvas = GetComponentInParent<Canvas>();

        img.material = new Material(img.material);

        // Active le raycast par alpha
        img.alphaHitTestMinimumThreshold = 0.1f;
    }
    void Start()
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;

        selected.SetActive(true);

        //deleteZone = GameObject.Find("TrashZone").GetComponent<RectTransform>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;

        if (!dragging)
            SC_scursorManager.instance.SetHoverCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            selected.SetActive(true);
            dragging = true;
            img.maskable = false;


            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint
            );

            offset = rectTransform.anchoredPosition - localPoint;

            img.transform.SetAsLastSibling();

            stick.enabled = true;
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

    public void OnPointerUp(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name != "Stickers") return;
        selected.SetActive(false);

        dragging = false;
        img.maskable = true;

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
        stick.enabled = false;
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