using UnityEngine;
using UnityEngine.EventSystems;

public class SC_slider : MonoBehaviour
{
  [Header("Références")]
    public RectTransform handle;
    public RectTransform content;

    [Header("Limites Handle")]
    public float handleMinY = -150f;
    public float handleMaxY = 150f;

    [Header("Limites Content")]
    public float contentMinY = 0f;
    public float contentMaxY = -2500f;

    [Header("Vitesses")]
    public float dragSpeed = 1f;
    public float wheelSpeed = 25f;

    private Vector2 startMouse;
    private float startHandleY;
    private SC_sticker_menu menu;

    private void Start()
    {
        menu = SC_sticker_menu.instance;
    }
    void Update()
    {if (menu.editing) return;
        float wheel = Input.mouseScrollDelta.y;

        if (wheel != 0)
        {
            SetHandlePosition(handle.anchoredPosition.y + wheel * wheelSpeed);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        startMouse = eventData.position;
        startHandleY = handle.anchoredPosition.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float delta = (eventData.position.y - startMouse.y) * dragSpeed;
        SetHandlePosition(startHandleY + delta);
    }

    void SetHandlePosition(float y)
    {
        // Clamp du handle
        y = Mathf.Clamp(y, handleMinY, handleMaxY);

        handle.anchoredPosition = new Vector2(
            handle.anchoredPosition.x,
            y
        );

        // Pourcentage du déplacement
        float t = Mathf.InverseLerp(handleMinY, handleMaxY, y);

        // Position correspondante du contenu
        float contentY = Mathf.Lerp(contentMinY, contentMaxY, t);

        content.anchoredPosition = new Vector2(
            content.anchoredPosition.x,
            contentY
        );
    }
}