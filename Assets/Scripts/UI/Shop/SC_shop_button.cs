using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class SC_shop_button : MonoBehaviour
{
    public Image image;

    public SO_Sticker sticker;
    public Button button;

    public InputActionReference selectAction;

    private bool selected;
    public Animator anim;

    public SC_juiciness juice;
    public SC_juiciness juice2;

    public TextMeshProUGUI name_;
    public TextMeshProUGUI price;



    private void OnEnable()
    {
        image.sprite = sticker.sticker_sprite;
        name_.text = sticker.name;
        price.text = sticker.Price.ToString();

        selectAction.action.Enable();
    }



    private void Update()
    {
        if (button != null)
        {
            button.image.raycastTarget = !SC_controller_manager.instance.using_controller;
        }

        if (selected && selectAction.action.WasPressedThisFrame())
        {
            sticker.unlocked = true;
            anim.SetTrigger("Press");
        }
    }

    public void unhover()
    {
        selected = false;
    }

    public void update_infos()
    {
        selected = true;
        juice.PlayJuice();
    }
}