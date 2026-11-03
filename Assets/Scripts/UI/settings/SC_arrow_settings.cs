using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class SC_arrow_settings : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SC_Button button;
    [SerializeField] private SC_list_text optionList;

    [Header("Settings")]
    [SerializeField] private bool next = true;

    private Collider2D collider_;
    private bool mouseInside;

    private void Awake()
    {
        collider_ = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (button == null || optionList == null || collider_ == null)
            return;

        // Pas de détection souris avec une manette
        if (SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller)
        {
            mouseInside = false;
            return;
        }

        if (SC_scursorManager.instance == null)
            return;

        if (!collider_.enabled || !button.clickable || !button.accept_input)
        {
            mouseInside = false;
            return;
        }

        // Même détection que SC_Button
        Vector2 cursorPosition =
            SC_scursorManager.instance.transform.position;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector3 colliderScreenPosition =
            cam.WorldToScreenPoint(collider_.bounds.center);

        Vector3 cursorScreenPosition = new Vector3(
            cursorPosition.x,
            cursorPosition.y,
            colliderScreenPosition.z
        );

        Vector3 cursorWorldPosition =
            cam.ScreenToWorldPoint(cursorScreenPosition);

        bool inside = collider_.OverlapPoint(cursorWorldPosition);

        // Entrée souris
        if (inside && !mouseInside)
        {
            mouseInside = true;

            button.isHovered = true;
            button.isSelected = false;

            if (button.indicator != null)
                button.indicator.SetActive(true);

            button.PlayAnimation(button.hover);
            button.Hover();
        }

        // Sortie souris
        else if (!inside && mouseInside)
        {
            mouseInside = false;

            button.isHovered = false;

            if (!button.isSelected)
            {
                if (button.indicator != null)
                    button.indicator.SetActive(false);

                button.OnUnhover();
                button.PlayAnimation(button.unhover);
            }
        }

        // Clic souris
        if (inside && Mouse.current != null &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (!button.gameObject.activeInHierarchy)
                return;

            button.Press();

            int direction = next ? 1 : -1;

            if (optionList.juice != null)
                optionList.juice.PlayJuice();

            optionList.SetIndex(optionList.index + direction);
        }
    }

    private void OnDisable()
    {
        mouseInside = false;

        if (button == null)
            return;

        button.isHovered = false;

        if (button.indicator != null)
            button.indicator.SetActive(false);
    }
}