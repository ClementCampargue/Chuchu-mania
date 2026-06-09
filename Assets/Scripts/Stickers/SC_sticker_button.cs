using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class SC_sticker_button : MonoBehaviour
{
    [Header("Scale settings")]
    public float hoverScale = 1.2f;
    public float scaleSpeed = 10f;

    public SpriteRenderer sprite;
    public Transform pivot;
    private Vector3 originalScale;
    private Vector3 targetScale;
    public SO_Sticker sticker;
    public Material locked;
    private Material unlocked;

    public GameObject hover;
    public GameObject Sticker_prefab;
    public bool hovered;
    private void Start()
    {
        originalScale = pivot.transform.localScale;
        targetScale = originalScale;
        sprite.sprite = sticker.sticker_sprite;
  
    }
    private void OnEnable()
    {
        originalScale = pivot.transform.localScale;
        targetScale = originalScale;
        sprite.sprite = sticker.sticker_sprite;
        unlocked = sprite.material;
        if (!sticker.unlocked)
        {
            sprite.material = locked;   
        }
    }

    private void Update()
    {
        pivot.transform.localScale = Vector3.Lerp(pivot.transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        if (!sticker.unlocked)
        {
            sprite.material = locked;
        }
        else
        {
            sprite.material = unlocked;
        }
    }

    public void OnMouseEnter()
    {
        if (sticker.unlocked)
        {
            targetScale = originalScale * hoverScale;
            hover.SetActive(true);
        }
        hovered = true;
    }

    public void OnMouseExit()
    {
        targetScale = originalScale;
        hover.SetActive(false);
        hovered = false;
    }

    public void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!sticker.unlocked) return;

        Canvas canvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        GameObject obj = Instantiate(Sticker_prefab, canvas.transform.GetChild(0).transform);
        Image img = obj.GetComponent<Image>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            null,
            out Vector2 localPoint
        );
        img.maskable = false;

        hovered = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = localPoint;

        img.sprite = sticker.sticker_sprite;
        img.SetNativeSize();

        hover.SetActive(false);
    }
}