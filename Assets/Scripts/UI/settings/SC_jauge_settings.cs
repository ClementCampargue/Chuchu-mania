
using UnityEngine;

public class SC_jauge_settings : MonoBehaviour
{
    [SerializeField] private SC_Button button;
    [SerializeField] private SC_slider_ slider;

    private Camera mainCamera;
    private bool isDragging;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void OnMouseEnter()
    {
        button.enabled = false;
        button.isPressed = false;
        button.isHovered = true;

        if (button.indicator != null)
            button.indicator.SetActive(true);

        button.PlayAnimation(button.hover);
        button.Hover();
    }

    private void OnMouseExit()
    {
        button.enabled = true;

        if (SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller)
            return;

        button.OnUnhover();
        button.isHovered = false;

        if (button.indicator != null)
            button.indicator.SetActive(false);

        if (!button.isSelected)
            button.PlayAnimation(button.unhover);
    }

    private void OnMouseDown()
    {
        if (slider == null)
            return;
        SC_scursorManager.instance.SetGrabCursor();

        isDragging = true;
        SetSliderFromMouse();
    }

    private void OnMouseDrag()
    {
        if (isDragging)
            SetSliderFromMouse();
    }

    private void OnMouseUp()
    {
        SC_scursorManager.instance.SetNormalCursor();
        slider.PlayRandom();
        isDragging = false;
    }

    private void SetSliderFromMouse()
    {
        if (slider == null || mainCamera == null)
            return;
        slider.SetValueFromMouse(Input.mousePosition, mainCamera);
    }
}
