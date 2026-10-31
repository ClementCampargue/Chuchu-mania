
using UnityEngine;

public class SC_arrow_settings : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SC_Button button;
    [SerializeField] private SC_list_text optionList;

    [Header("Settings")]
    [SerializeField] private bool next = true;

    private bool mouseInside;


    private void OnMouseEnter()
    {
        if (button == null)
            return;

        mouseInside = true;

        //    button.enabled = false;
        //    button.isPressed = false;
        //    button.isHovered = true;

        if (button.indicator != null)
            button.indicator.SetActive(true);

        button.PlayAnimation(button.hover);
    //    button.Hover();
    }

    private void OnMouseExit()
    {
        if (button == null)
            return;

        mouseInside = false;

        //       button.enabled = true;

        if (SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller)
            return;

        //    button.OnUnhover();
        //    button.isHovered = false;

        if (button.indicator != null)
            button.indicator.SetActive(false);

        if (!button.isSelected)
            button.PlayAnimation(button.unhover);
    }

    private void OnMouseDown()
    {
        if (button == null || optionList == null)
            return;

        if (!mouseInside)
            return;

        if (!button.gameObject.activeInHierarchy)
            return;

        if (!enabled)
            return;

        button.Press();

        int direction = next ? 1 : -1;

        if (optionList.juice != null)
            optionList.juice.PlayJuice();

        optionList.SetIndex(
            optionList.index + direction
        );
    }

    private void OnDisable()
    {
        mouseInside = false;

        if (button == null)
            return;

        button.enabled = true;
        button.isHovered = false;
        button.isPressed = false;

        if (button.indicator != null)
            button.indicator.SetActive(false);
    }
}
