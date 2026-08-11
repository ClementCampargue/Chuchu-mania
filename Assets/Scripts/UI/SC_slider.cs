using UnityEngine;
using UnityEngine.EventSystems;

public class SC_slider : MonoBehaviour
{
    [Header("Références")]
    public RectTransform handle;
    public RectTransform content;
    public RectTransform viewport;

    [Header("Limites Handle")]
    public float handleMinY = -150f;
    public float handleMaxY = 150f;

    [Header("Limites Content")]
    public float contentMinY = 0f;
    public float contentMaxY = -2500f;

    [Header("Vitesses")]
    public float dragSpeed = 1f;
    public float wheelSpeed = 25f;

    [Header("Auto Scroll")]
    public float selectionScrollAmount = 150f;

    private Vector2 startMouse;
    private float startHandleY;
    private SC_sticker_menu menu;

    private GameObject lastSelected;

    private void Start()
    {
        menu = SC_sticker_menu.instance;
    }

    private void Update()
    {
        if (menu.editing)
            return;

        // Scroll souris
        float wheel = Input.mouseScrollDelta.y;

        if (wheel != 0)
        {
            SetHandlePosition(
                handle.anchoredPosition.y + wheel * wheelSpeed
            );
        }

        // Vérifie le bouton sélectionné à la manette
        CheckSelectedButton();
    }

    private void CheckSelectedButton()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null)
            return;

        // Aucun changement de bouton
        if (selected == lastSelected)
            return;

        // Si c'est le premier bouton sélectionné,
        // on ne scroll pas.
        if (lastSelected == null)
        {
            lastSelected = selected;
            return;
        }

        RectTransform previousRect = lastSelected.GetComponent<RectTransform>();
        RectTransform selectedRect = selected.GetComponent<RectTransform>();

        if (previousRect == null || selectedRect == null)
        {
            lastSelected = selected;
            return;
        }

        // Compare la position verticale des deux boutons
        float previousY = previousRect.position.y;
        float selectedY = selectedRect.position.y;

        if (selectedY < previousY)
        {
            // On descend dans la liste
            MoveContent(-selectionScrollAmount);
        }
        else if (selectedY > previousY)
        {
            // On remonte dans la liste
            MoveContent(selectionScrollAmount);
        }

        lastSelected = selected;
    }

    private void MoveContent(float deltaY)
    {
        float newContentY = content.anchoredPosition.y + deltaY;

        // Clamp du contenu
        newContentY = Mathf.Clamp(
            newContentY,
            contentMaxY,
            contentMinY
        );

        content.anchoredPosition = new Vector2(
            content.anchoredPosition.x,
            newContentY
        );

        // Synchronise le handle avec le contenu
        float t = Mathf.InverseLerp(
            contentMinY,
            contentMaxY,
            newContentY
        );

        float handleY = Mathf.Lerp(
            handleMinY,
            handleMaxY,
            t
        );

        handle.anchoredPosition = new Vector2(
            handle.anchoredPosition.x,
            handleY
        );
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
        y = Mathf.Clamp(
            y,
            handleMinY,
            handleMaxY
        );

        handle.anchoredPosition = new Vector2(
            handle.anchoredPosition.x,
            y
        );

        // Pourcentage du déplacement
        float t = Mathf.InverseLerp(
            handleMinY,
            handleMaxY,
            y
        );

        // Position correspondante du contenu
        float contentY = Mathf.Lerp(
            contentMinY,
            contentMaxY,
            t
        );

        content.anchoredPosition = new Vector2(
            content.anchoredPosition.x,
            contentY
        );
    }
}
