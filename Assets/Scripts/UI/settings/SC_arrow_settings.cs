using UnityEngine;

public class SC_arrow_settings : MonoBehaviour
{

    [SerializeField] private SC_Button button;
    
    [SerializeField] private SC_list_text optionList;

    [SerializeField] private bool next = true;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseEnter()
    {
        button.enabled = false;
        button.isPressed = false;
        button.isHovered = true;
        if (button.indicator != null)
        {
            button.indicator.SetActive(true);

        }

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
        {
            button.indicator.SetActive(false);

        }

        if (!button.isSelected)
        {
            button.PlayAnimation(button.unhover);
        }
    }
    private void OnMouseDown()
    {
        if (optionList == null)
            return;
        button.Press();
        int direction = next ? 1 : -1;
        optionList.juice.PlayJuice();

        optionList.SetIndex(optionList.index + direction);
    }
}
